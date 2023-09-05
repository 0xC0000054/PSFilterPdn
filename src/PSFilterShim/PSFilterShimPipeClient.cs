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

using CommunityToolkit.HighPerformance.Buffers;
using PSFilterLoad.PSApi;
using PSFilterLoad.PSApi.Imaging;
using PSFilterLoad.PSApi.Imaging.Internal;
using PSFilterPdn;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.MemoryMappedFiles;
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
            GetExifMetadata,
            GetXmpMetadata,
            GetIccProfile,
            GetSourceImage,
            GetSelectionMask,
            GetTransparencyCheckerboardImage,
            ReleaseMappedFile,
            SetDestinationImage,
            SetFilterData
        }

        private enum FilterDataType : byte
        {
            Parameter = 0,
            PseudoResource,
            DescriptorRegistry
        }

        public bool AbortFilter()
        {
            byte[] reply = SendMessageToServer(Command.AbortCallback);

            return reply[0] != 0;
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

        public ImageSurface GetSourceImage(IWICFactory factory)
        {
            ImageSurface surface;

            string mapName = GetMemoryMappedFileName(Command.GetSourceImage);

            using (MemoryMappedFile file = MemoryMappedFile.OpenExisting(mapName))
            using (MemoryMappedViewStream stream = file.CreateViewStream())
            {
                PSFilterShimImageHeader header = new(stream);

                if (header.Format != SurfacePixelFormat.Bgra32)
                {
                    throw new InvalidOperationException("This method requires an image that uses the Bgra32 format.");
                }

                surface = new WICBitmapSurface<ColorBgra32>(header.Width, header.Height, factory);

                int rowLengthInBytes = header.Stride;

                unsafe
                {
                    using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Write))
                    {
                        for (int y = 0; y < header.Height; y++)
                        {
                            byte* dst = surfaceLock.GetRowPointerUnchecked(y);

                            stream.ReadExactly(new Span<byte>(dst, rowLengthInBytes));
                        }
                    }
                }
            }

            SendMessageToServer(Command.ReleaseMappedFile);

            return surface;
        }

        public MaskSurface? GetSelectionMask(IWICFactory factory)
        {
            MaskSurface? surface = null;

            string mapName = GetMemoryMappedFileName(Command.GetSelectionMask);

            if (!string.IsNullOrEmpty(mapName))
            {
                using (MemoryMappedFile file = MemoryMappedFile.OpenExisting(mapName))
                using (MemoryMappedViewStream stream = file.CreateViewStream())
                {
                    PSFilterShimImageHeader header = new(stream);

                    if (header.Format != SurfacePixelFormat.Gray8)
                    {
                        throw new InvalidOperationException("This method requires an image that uses the Gray8 format.");
                    }

                    surface = new ShimMaskSurface(header.Width, header.Height, factory);

                    int rowLengthInBytes = header.Stride;

                    unsafe
                    {
                        using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Write))
                        {
                            for (int y = 0; y < header.Height; y++)
                            {
                                byte* dst = surfaceLock.GetRowPointerUnchecked(y);

                                stream.ReadExactly(new Span<byte>(dst, rowLengthInBytes));
                            }
                        }
                    }
                }

                SendMessageToServer(Command.ReleaseMappedFile);
            }

            return surface;
        }

        public TransparencyCheckerboardSurface GetTransparencyCheckerboard(IWICFactory factory)
        {
            TransparencyCheckerboardSurface surface;

            string mapName = GetMemoryMappedFileName(Command.GetTransparencyCheckerboardImage);

            using (MemoryMappedFile file = MemoryMappedFile.OpenExisting(mapName))
            using (MemoryMappedViewStream stream = file.CreateViewStream())
            {
                PSFilterShimImageHeader header = new(stream);

                if (header.Format != SurfacePixelFormat.Pbgra32)
                {
                    throw new InvalidOperationException("This method requires an image that uses the Pbgra32 format.");
                }

                surface = new ShimTransparencyCheckerboardSurface(header.Width, header.Height, factory);

                int rowLengthInBytes = header.Stride;

                unsafe
                {
                    using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Write))
                    {
                        for (int y = 0; y < header.Height; y++)
                        {
                            byte* dst = surfaceLock.GetRowPointerUnchecked(y);

                            stream.ReadExactly(new Span<byte>(dst, rowLengthInBytes));
                        }
                    }
                }
            }

            SendMessageToServer(Command.ReleaseMappedFile);

            return surface;
        }

        public void SetParameterData(ParameterData parameterData)
        {
            ArgumentNullException.ThrowIfNull(parameterData, nameof(parameterData));

            SendFilterDataToServer(FilterDataType.Parameter, parameterData);
        }

        public void SetPseudoResources(PseudoResourceCollection pseudoResources)
        {
            ArgumentNullException.ThrowIfNull(pseudoResources, nameof(pseudoResources));

            SendFilterDataToServer(FilterDataType.PseudoResource, pseudoResources);
        }

        public void SetDescriptorRegistry(DescriptorRegistryValues values)
        {
            ArgumentNullException.ThrowIfNull(values, nameof(values));

            SendFilterDataToServer(FilterDataType.DescriptorRegistry, values);
        }

        [SkipLocalsInit]
        public void SetDestinationImage(ISurface<ImageSurface> image, FilterPostProcessingOptions options)
        {
            string mapName = Guid.NewGuid().ToString("n");
            PSFilterShimImageHeader header = new(image.Width,
                                                 image.Height,
                                                 SurfacePixelFormat.Bgra32);

            using (MemoryMappedFile file = MemoryMappedFile.CreateNew(mapName, header.GetTotalFileSize()))
            {
                using (MemoryMappedViewStream stream = file.CreateViewStream())
                {
                    header.Save(stream);

                    int rowLengthInBytes = header.Stride;

                    unsafe
                    {
                        using (ISurfaceLock surfaceLock = image.Lock(SurfaceLockMode.Read))
                        {
                            for (int y = 0; y < header.Height; y++)
                            {
                                byte* src = surfaceLock.GetRowPointerUnchecked(y);

                                stream.Write(new ReadOnlySpan<byte>(src, rowLengthInBytes));
                            }
                        }
                    }
                }

                // A GUID string using the 'n' format has a fixed length of 32 characters.
                const int StringLength = 32;
                const int BufferSize = sizeof(int) + StringLength + sizeof(int);

                Span<byte> buffer = stackalloc byte[BufferSize];

                BinaryPrimitives.WriteInt32LittleEndian(buffer, StringLength);
                Encoding.UTF8.GetBytes(mapName, buffer.Slice(sizeof(int), StringLength));
                BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(sizeof(int) + StringLength), (int)options);

                SendMessageToServer(Command.SetDestinationImage, buffer);
            }
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
        private string GetMemoryMappedFileName(Command command)
        {
            string name = string.Empty;

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
                    byte[]? arrayFromPool = null;

                    try
                    {
                        const int MaxStackBufferSize = 128;

                        Span<byte> buffer = stackalloc byte[MaxStackBufferSize];

                        if (replyLength > MaxStackBufferSize)
                        {
                            arrayFromPool = ArrayPool<byte>.Shared.Rent(replyLength);
                            buffer = arrayFromPool;
                        }

                        Span<byte> messageBuffer = buffer.Slice(0, replyLength);

                        stream.ReadExactly(messageBuffer);

                        name = Encoding.UTF8.GetString(messageBuffer);
                    }
                    finally
                    {
                        if (arrayFromPool != null)
                        {
                            ArrayPool<byte>.Shared.Return(arrayFromPool);
                        }
                    }
                }

                stream.WaitForPipeDrain();
            }

            return name;
        }

        [SkipLocalsInit]
        private byte[] SendMessageToServer(Command command)
        {
            byte[] reply = Array.Empty<byte>();

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
            byte[] reply = Array.Empty<byte>();

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
            byte[] reply = Array.Empty<byte>();

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
            byte[] reply = Array.Empty<byte>();

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

                byte[]? arrayFromPool = null;

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

        [SkipLocalsInit]
        private byte[] SendMessageToServer(Command command, ReadOnlySpan<byte> bytes)
        {
            byte[] reply = Array.Empty<byte>();

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                int dataLength = sizeof(byte) + bytes.Length;
                int totalMessageLength = sizeof(int) + dataLength;

                byte[]? arrayFromPool = null;

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
                    messageBuffer[4] = (byte)command;
                    bytes.CopyTo(messageBuffer.Slice(5));

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

        private void SendFilterDataToServer<T>(FilterDataType type, T value) where T : class
        {
            using (ArrayPoolBufferWriter<byte> writer = new())
            {
                WriteByte(writer, (byte)type);

                MessagePackSerializerUtil.Serialize(writer, value, MessagePackResolver.Options);

                SendMessageToServer(Command.SetFilterData, writer.WrittenSpan);
            }

            static void WriteByte(IBufferWriter<byte> writer, byte value)
            {
                Span<byte> span = writer.GetSpan(1);
                span[0] = value;
                writer.Advance(1);
            }
        }
    }
}
