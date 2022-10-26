/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi;
using PSFilterPdn;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace PSFilterShim
{
    internal sealed class PSFilterShimPipeClient
    {
        private readonly string pipeName;
        private readonly byte[] replyLengthBuffer;
        private readonly byte[] oneByteReplyBuffer;
        private readonly byte[] noParameterMessageBuffer;
        private readonly byte[] oneByteParameterMessageBuffer;

        public PSFilterShimPipeClient(string pipeName)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            replyLengthBuffer = new byte[4];
            oneByteReplyBuffer = new byte[1];
            noParameterMessageBuffer = CreateNoParameterMessageBuffer();
            oneByteParameterMessageBuffer = CreateOneByteParameterMessageBuffer();
        }

        private enum Command : byte
        {
            AbortCallback = 0,
            ReportProgress,
            GetPluginData,
            GetSettings,
            SetErrorMessage,
            SetPostProcessingOptions,
            GetExifMetadata,
            GetXmpMetadata,
            GetIccProfile
        }

        public bool AbortFilter()
        {
            byte[] reply = SendMessageToServer(Command.AbortCallback);

            return reply != null && reply[0] != 0;
        }

        public PluginData GetPluginData()
        {
            return DeserializeClass<PluginData>(Command.GetPluginData);
        }

        public PSFilterShimSettings GetShimSettings()
        {
            return DeserializeClass<PSFilterShimSettings>(Command.GetSettings);
        }

        public void SetProxyErrorMessage(string errorMessage)
        {
            SendMessageToServer(Command.SetErrorMessage, errorMessage);
        }

        public void SetPostProcessingOptions(FilterPostProcessingOptions options)
        {
            // None is the default, most filters do not require any post processing.
            if (options != FilterPostProcessingOptions.None)
            {
                SendMessageToServer(Command.SetPostProcessingOptions, (int)options);
            }
        }

        public void UpdateFilterProgress(byte progressPercentage)
        {
            SendMessageToServer(Command.ReportProgress, progressPercentage);
        }

        public byte[] GetExifData()
        {
            return SendMessageToServer(Command.GetExifMetadata);
        }

        public byte[] GetIccProfileData()
        {
            return SendMessageToServer(Command.GetIccProfile);
        }

        public byte[] GetXmpData()
        {
            return SendMessageToServer(Command.GetXmpMetadata);
        }

        private static byte[] CreateNoParameterMessageBuffer()
        {
            const int dataLength = sizeof(byte);

            byte[] noParameterMessageBuffer = new byte[sizeof(int) + dataLength];

            noParameterMessageBuffer[0] = dataLength & 0xff;
            noParameterMessageBuffer[1] = (dataLength >> 8) & 0xff;
            noParameterMessageBuffer[2] = (dataLength >> 16) & 0xff;
            noParameterMessageBuffer[3] = (dataLength >> 24) & 0xff;

            return noParameterMessageBuffer;
        }

        private static byte[] CreateOneByteParameterMessageBuffer()
        {
            const int dataLength = sizeof(byte) * 2;

            byte[] oneByteParameterMessageBuffer = new byte[sizeof(int) + dataLength];

            oneByteParameterMessageBuffer[0] = dataLength & 0xff;
            oneByteParameterMessageBuffer[1] = (dataLength >> 8) & 0xff;
            oneByteParameterMessageBuffer[2] = (dataLength >> 16) & 0xff;
            oneByteParameterMessageBuffer[3] = (dataLength >> 24) & 0xff;

            return oneByteParameterMessageBuffer;
        }

        private T DeserializeClass<T>(Command command) where T : class
        {
            T deserialized = null;

            byte[] reply = SendMessageToServer(command);

            using (MemoryStream stream = new(reply))
            {
                DataContractSerializer serializer = new(typeof(T));

                deserialized = (T)serializer.ReadObject(stream);
            }

            return deserialized;
        }

        private byte[] SendMessageToServer(Command command)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                noParameterMessageBuffer[4] = (byte)command;

                stream.Write(noParameterMessageBuffer, 0, noParameterMessageBuffer.Length);

                stream.ProperRead(replyLengthBuffer, 0, replyLengthBuffer.Length);

                int replyLength = BitConverter.ToInt32(replyLengthBuffer, 0);

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ProperRead(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }

            return reply;
        }

        private byte[] SendMessageToServer(Command command, byte value)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                oneByteParameterMessageBuffer[4] = (byte)command;
                oneByteParameterMessageBuffer[5] = value;

                stream.Write(oneByteParameterMessageBuffer, 0, oneByteParameterMessageBuffer.Length);

                stream.ProperRead(replyLengthBuffer, 0, replyLengthBuffer.Length);

                int replyLength = BitConverter.ToInt32(replyLengthBuffer, 0);

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ProperRead(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }

            return reply;
        }

        [SkipLocalsInit]
        private byte[] SendMessageToServer(Command command, int value)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                const int dataLength = sizeof(byte) + sizeof(int);

                Span<byte> messageBuffer = stackalloc byte[sizeof(int) + dataLength];

                BinaryPrimitives.WriteInt32LittleEndian(messageBuffer, dataLength);
                messageBuffer[4] = (byte)command;
                BinaryPrimitives.WriteInt32LittleEndian(messageBuffer.Slice(5), value);

                stream.Write(messageBuffer);

                stream.ProperRead(replyLengthBuffer, 0, replyLengthBuffer.Length);

                int replyLength = BitConverter.ToInt32(replyLengthBuffer, 0);

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ProperRead(reply, 0, replyLength);
                }
            }

            return reply;
        }

        private byte[] SendMessageToServer(Command command, string value)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                int dataLength = sizeof(byte) + Encoding.UTF8.GetByteCount(value);

                byte[] messageBuffer = new byte[sizeof(int) + dataLength];

                messageBuffer[0] = (byte)(dataLength & 0xff);
                messageBuffer[1] = (byte)((dataLength >> 8) & 0xff);
                messageBuffer[2] = (byte)((dataLength >> 16) & 0xff);
                messageBuffer[3] = (byte)((dataLength >> 24) & 0xff);
                messageBuffer[4] = (byte)command;
                Encoding.UTF8.GetBytes(value, 0, value.Length, messageBuffer, 5);

                stream.Write(messageBuffer, 0, messageBuffer.Length);

                stream.ProperRead(replyLengthBuffer, 0, replyLengthBuffer.Length);

                int replyLength = BitConverter.ToInt32(replyLengthBuffer, 0);

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ProperRead(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }

            return reply;
        }
    }
}
