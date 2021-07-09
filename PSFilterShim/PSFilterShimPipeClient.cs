/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi;
using PSFilterPdn;
using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization;
using System.Text;

namespace PSFilterShim
{
    internal sealed class PSFilterShimPipeClient
    {
        private readonly string pipeName;
        private readonly byte[] replyLengthBuffer;
        private readonly byte[] noParameterMessageBuffer;
        private readonly byte[] oneByteParameterMessageBuffer;

        private static readonly byte[] DoneMessageBytes = Encoding.UTF8.GetBytes("done");

        public PSFilterShimPipeClient(string pipeName)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            replyLengthBuffer = new byte[4];
            noParameterMessageBuffer = CreateNoParameterMessageBuffer();
            oneByteParameterMessageBuffer = CreateOneByteParameterMessageBuffer();
        }

        private enum Command : byte
        {
            AbortCallback = 0,
            ReportProgress,
            GetPluginData,
            GetSettings,
            SetErrorMessage
        }

        public bool AbortFilter()
        {
            byte[] reply = SendMessageSynchronously(Command.AbortCallback);

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
            SendMessageSynchronously(Command.SetErrorMessage, errorMessage);
        }

        public void UpdateFilterProgress(byte progressPercentage)
        {
            SendMessageSynchronously(Command.ReportProgress, progressPercentage);
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

            byte[] noParameterMessageBuffer = new byte[sizeof(int) + dataLength];

            noParameterMessageBuffer[0] = dataLength & 0xff;
            noParameterMessageBuffer[1] = (dataLength >> 8) & 0xff;
            noParameterMessageBuffer[2] = (dataLength >> 16) & 0xff;
            noParameterMessageBuffer[3] = (dataLength >> 24) & 0xff;

            return noParameterMessageBuffer;
        }

        private T DeserializeClass<T>(Command command) where T : class
        {
            T deserialized = null;

            byte[] reply = SendMessageSynchronously(command);

            using (MemoryStream stream = new MemoryStream(reply))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));

                deserialized = (T)serializer.ReadObject(stream);
            }

            return deserialized;
        }

        private byte[] SendMessageSynchronously(Command command)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                noParameterMessageBuffer[4] = (byte)command;

                stream.Write(noParameterMessageBuffer, 0, noParameterMessageBuffer.Length);

                stream.ProperRead(replyLengthBuffer, 0, replyLengthBuffer.Length);

                int replyLength = BitConverter.ToInt32(replyLengthBuffer, 0);

                if (replyLength > 0)
                {
                    reply = new byte[replyLength];

                    stream.ProperRead(reply, 0, replyLength);
                }

                stream.Write(DoneMessageBytes, 0, DoneMessageBytes.Length);
            }

            return reply;
        }

        private byte[] SendMessageSynchronously(Command command, byte value)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                oneByteParameterMessageBuffer[4] = (byte)command;
                oneByteParameterMessageBuffer[5] = value;

                stream.Write(oneByteParameterMessageBuffer, 0, oneByteParameterMessageBuffer.Length);

                stream.ProperRead(replyLengthBuffer, 0, replyLengthBuffer.Length);

                int replyLength = BitConverter.ToInt32(replyLengthBuffer, 0);

                if (replyLength > 0)
                {
                    reply = new byte[replyLength];

                    stream.ProperRead(reply, 0, replyLength);
                }

                stream.Write(DoneMessageBytes, 0, DoneMessageBytes.Length);
            }

            return reply;
        }

        private byte[] SendMessageSynchronously(Command command, string value)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
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

                stream.Write(oneByteParameterMessageBuffer, 0, oneByteParameterMessageBuffer.Length);

                stream.ProperRead(replyLengthBuffer, 0, replyLengthBuffer.Length);

                int replyLength = BitConverter.ToInt32(replyLengthBuffer, 0);

                if (replyLength > 0)
                {
                    reply = new byte[replyLength];

                    stream.ProperRead(reply, 0, replyLength);
                }

                stream.Write(DoneMessageBytes, 0, DoneMessageBytes.Length);
            }

            return reply;
        }
    }
}
