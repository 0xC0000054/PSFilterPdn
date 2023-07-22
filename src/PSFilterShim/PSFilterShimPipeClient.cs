/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi;
using PSFilterPdn;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;

namespace PSFilterShim
{
    internal sealed class PSFilterShimPipeClient
    {
        private readonly string pipeName;
        private readonly byte[] oneByteReplyBuffer;
        private readonly byte[] noParameterMessageBuffer;
        private readonly byte[] oneByteParameterMessageBuffer;

        public PSFilterShimPipeClient(string pipeName)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
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
            SetErrorInfo,
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
            SendErrorMessageToServer(errorMessage, string.Empty);
        }

        public void SetProxyErrorMessage(Exception exception)
        {
            SendErrorMessageToServer(exception.Message, exception.ToString());
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
            ReadOnlyMemory<byte> reply = SendMessageToServer(command);

            return MessagePackSerializerUtil.Deserialize<T>(reply, PSFilterShimResolver.Options);
        }

        [SkipLocalsInit]
        private byte[] SendMessageToServer(Command command)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                noParameterMessageBuffer[4] = (byte)command;

                stream.Write(noParameterMessageBuffer, 0, noParameterMessageBuffer.Length);

                Span<byte> replyLengthBuffer = stackalloc byte[4];

                stream.ReadExactly(replyLengthBuffer);

                int replyLength = BinaryPrimitives.ReadInt32LittleEndian(replyLengthBuffer);

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ReadExactly(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }

            return reply;
        }

        [SkipLocalsInit]
        private byte[] SendMessageToServer(Command command, byte value)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                oneByteParameterMessageBuffer[4] = (byte)command;
                oneByteParameterMessageBuffer[5] = value;

                stream.Write(oneByteParameterMessageBuffer, 0, oneByteParameterMessageBuffer.Length);

                Span<byte> replyLengthBuffer = stackalloc byte[4];

                stream.ReadExactly(replyLengthBuffer);

                int replyLength = BinaryPrimitives.ReadInt32LittleEndian(replyLengthBuffer);

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ReadExactly(reply, 0, replyLength);
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

                Span<byte> replyLengthBuffer = stackalloc byte[4];

                stream.ReadExactly(replyLengthBuffer);

                int replyLength = BinaryPrimitives.ReadInt32LittleEndian(replyLengthBuffer);

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.Read(reply, 0, replyLength);
                }
            }

            return reply;
        }

        [SkipLocalsInit]
        private byte[] SendErrorMessageToServer(string message, string details)
        {
            byte[] reply = null;

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                int errorMessageLength = 0;
                int errorDetailsLength = 0;

                if (!string.IsNullOrEmpty(message))
                {
                    errorMessageLength = Encoding.UTF8.GetByteCount(message);

                    if (!string.IsNullOrEmpty(details))
                    {
                        errorDetailsLength = Encoding.UTF8.GetByteCount(details);
                    }
                }

                const int HeaderLength = sizeof(int) * 2;

                int dataLength = sizeof(byte) + HeaderLength + errorMessageLength + errorDetailsLength;
                int totalMessageLength = sizeof(int) + dataLength;

                byte[] arrayFromPool = null;

                try
                {
                    const int MaxStackBufferSize = 256;

                    Span<byte> buffer = stackalloc byte[MaxStackBufferSize];

                    if (totalMessageLength > MaxStackBufferSize)
                    {
                        arrayFromPool = ArrayPool<byte>.Shared.Rent(totalMessageLength);
                        buffer = arrayFromPool;
                    }

                    Span<byte> messageBuffer = buffer.Slice(0, totalMessageLength);

                    BinaryPrimitives.WriteInt32LittleEndian(messageBuffer, dataLength);
                    messageBuffer[4] = (byte)Command.SetErrorInfo;

                    BinaryPrimitives.WriteInt32LittleEndian(messageBuffer.Slice(5), errorMessageLength);
                    BinaryPrimitives.WriteInt32LittleEndian(messageBuffer.Slice(5 + sizeof(int)), errorDetailsLength);

                    if (errorMessageLength > 0)
                    {
                        Encoding.UTF8.GetBytes(message, messageBuffer.Slice(5 + HeaderLength));

                        if (errorDetailsLength > 0)
                        {
                            Encoding.UTF8.GetBytes(details, messageBuffer.Slice(5 + HeaderLength + errorMessageLength));
                        }
                    }

                    stream.Write(messageBuffer);
                }
                finally
                {
                    if (arrayFromPool != null)
                    {
                        ArrayPool<byte>.Shared.Return(arrayFromPool);
                    }
                }

                Span<byte> replyLengthBuffer = stackalloc byte[4];

                stream.ReadExactly(replyLengthBuffer);

                int replyLength = BinaryPrimitives.ReadInt32LittleEndian(replyLengthBuffer);

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ReadExactly(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }

            return reply;
        }
    }
}
