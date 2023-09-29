/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
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

        public PSFilterShimPipeClient(string pipeName)
        {
            this.pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
        }

        private enum Command : byte
        {
            GetAbortEventHandleName = 0,
            ReportProgress,
            GetSettings,
            SetErrorInfo,
            GetExifMetadata,
            GetXmpMetadata,
            GetIccProfile,
            GetIptcCaptionRecord,
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

        public string GetAbortEventHandleName()
        {
            return ReadString(Command.GetAbortEventHandleName);
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

        public byte[] GetIptcCaptionRecordData()
        {
            return SendMessageToServer(Command.GetIptcCaptionRecord);
        }

        public byte[] GetXmpData()
        {
            return SendMessageToServer(Command.GetXmpMetadata);
        }

        public ImageSurface GetSourceImage(IWICFactory factory)
        {
            ImageSurface surface;

            string mapName = ReadString(Command.GetSourceImage);

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

            string mapName = ReadString(Command.GetSelectionMask);

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

            string mapName = ReadString(Command.GetTransparencyCheckerboardImage);

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

                SendDestinationImageToServer(mapName, options);
            }
        }

        private T DeserializeClass<T>(Command command) where T : class
        {
            ReadOnlyMemory<byte> reply = SendMessageToServer(command);

            return MessagePackSerializerUtil.Deserialize<T>(reply, PSFilterShimResolver.Options);
        }

        [SkipLocalsInit]
        private string ReadString(Command command)
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

        private byte[] SendDestinationImageToServer(string mapName, FilterPostProcessingOptions options)
        {
            byte[] reply = Array.Empty<byte>();

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                stream.WriteByte((byte)Command.SetDestinationImage);
                WriteString(stream, mapName);
                stream.WriteInt32LittleEndian((int)options);

                int replyLength = stream.ReadInt32LittleEndian();

                if (replyLength > 0)
                {
                    reply = GC.AllocateUninitializedArray<byte>(replyLength);
                    stream.ReadExactly(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }

            return reply;
        }

        private byte[] SendErrorMessageToServer(string message, string details)
        {
            byte[] reply = Array.Empty<byte>();

            using (NamedPipeClientStream stream = new(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous))
            {
                stream.Connect();

                stream.WriteByte((byte)Command.SetErrorInfo);
                WriteString(stream, message);
                WriteString(stream, details);

                int replyLength = stream.ReadInt32LittleEndian();

                if (replyLength > 0)
                {
                    reply = GC.AllocateUninitializedArray<byte>(replyLength);
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
                    reply = GC.AllocateUninitializedArray<byte>(replyLength);
                    stream.ReadExactly(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }
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
                    reply = GC.AllocateUninitializedArray<byte>(replyLength);
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
                    reply = GC.AllocateUninitializedArray<byte>(replyLength);
                    stream.ReadExactly(reply, 0, replyLength);
                }

                stream.WaitForPipeDrain();
            }

            return reply;
        }

        [SkipLocalsInit]
        private void WriteString(Stream stream, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                stream.WriteInt32LittleEndian(0);
                return;
            }

            int maxStringLength = Encoding.UTF8.GetMaxByteCount(value.Length);
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

                int bytesWritten = Encoding.UTF8.GetBytes(value, buffer);
                stream.WriteInt32LittleEndian(bytesWritten);
                stream.Write(buffer.Slice(0, bytesWritten));
            }
            finally
            {
                if (arrayFromPool != null)
                {
                    ArrayPool<byte>.Shared.Return(arrayFromPool);
                }
            }
        }
    }
}
