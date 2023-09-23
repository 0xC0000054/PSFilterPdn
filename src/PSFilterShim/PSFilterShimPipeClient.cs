﻿/////////////////////////////////////////////////////////////////////////////////
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
using System.IO;
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

        public PSFilterShimPipeClient(string pipeName)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            oneByteReplyBuffer = new byte[1];
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

                stream.WriteByte((byte)command);

                int replyLength = stream.ReadInt32LittleEndian();

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

                stream.WriteByte((byte)command);

                int replyLength = stream.ReadInt32LittleEndian();

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

                stream.WriteByte((byte)command);
                stream.WriteByte(value);

                int replyLength = stream.ReadInt32LittleEndian();

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

                stream.WriteByte((byte)Command.SetErrorInfo);
                stream.WriteInt32LittleEndian(errorMessageLength);
                stream.WriteInt32LittleEndian(errorDetailsLength);

                if (errorMessageLength > 0)
                {
                    int maxStringLength = Math.Max(errorMessageLength, errorDetailsLength);
                    byte[]? arrayFromPool = null;

                    try
                    {
                        const int MaxStackBufferSize = 256;

                        Span<byte> buffer = stackalloc byte[MaxStackBufferSize];

                        if (maxStringLength > MaxStackBufferSize)
                        {
                            arrayFromPool = ArrayPool<byte>.Shared.Rent(maxStringLength);
                            buffer = arrayFromPool;
                        }

                        int bytesWritten = Encoding.UTF8.GetBytes(message, buffer);
                        stream.Write(buffer.Slice(0, bytesWritten));

                        if (errorDetailsLength > 0)
                        {
                            bytesWritten = Encoding.UTF8.GetBytes(details, buffer);
                            stream.Write(buffer.Slice(0, bytesWritten));
                        }
                    }
                    finally
                    {
                        if (arrayFromPool != null)
                        {
                            ArrayPool<byte>.Shared.Return(arrayFromPool);
                        }
                    }
                }

                int replyLength = stream.ReadInt32LittleEndian();

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ReadExactly(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }

            return reply;
        }

        private byte[] SendMessageToServer(Command command, ReadOnlySpan<byte> bytes)
        {
            byte[] reply = Array.Empty<byte>();

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                stream.WriteByte((byte)command);
                stream.Write(bytes);

                int replyLength = stream.ReadInt32LittleEndian();

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
            byte[] reply;

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                stream.WriteByte((byte)Command.SetFilterData);
                stream.WriteByte((byte)type);

                using (ArrayPoolBufferWriter<byte> writer = new())
                {
                    MessagePackSerializerUtil.Serialize(writer, value, MessagePackResolver.Options);

                    ReadOnlySpan<byte> bytes = writer.WrittenSpan;

                    stream.WriteInt32LittleEndian(bytes.Length);
                    stream.Write(bytes);
                }

                int replyLength = stream.ReadInt32LittleEndian();

                if (replyLength > 0)
                {
                    reply = replyLength == 1 ? oneByteReplyBuffer : new byte[replyLength];

                    stream.ReadExactly(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }
        }
    }
}
