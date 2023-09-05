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
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimPipeServer : Disposable
    {
        private NamedPipeServerStream server;
        private MemoryMappedFile? memoryMappedFile;
        private readonly byte[] oneByteParameterReplyBuffer;
        private readonly IDocumentMetadataProvider documentMetadataProvider;
        private readonly ImageSurface sourceImage;
        private readonly bool ownsSourceImage;
        private readonly MaskSurface? maskImage;
        private readonly bool ownsMaskImage;
        private readonly TransparencyCheckerboardSurface transparencyCheckerboard;
        private readonly bool ownsTransparencyCheckerboard;

        private readonly Func<bool> abortFunc;
        private readonly PluginData pluginData;
        private readonly PSFilterShimSettings settings;
        private readonly Action<PSFilterShimErrorInfo?> errorCallback;
        private readonly Action<byte>? progressCallback;
        private readonly Action<Stream, FilterPostProcessingOptions> setDestinationImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSFilterShimService"/> class.
        /// </summary>
        /// <param name="abort">The abort callback.</param>
        /// <param name="plugin">The plug-in data.</param>
        /// <param name="settings">The settings for the shim application.</param>
        /// <param name="error">The error callback.</param>
        /// <param name="progress">The progress callback.</param>
        /// <param name="setDestinationImage">The callback to set the destination image.</param>
        /// <param name="documentMetadataProvider">The document metadata provider.</param>
        /// <param name="sourceImage">The source image.</param>
        /// <param name="ownsSourceImage">
        /// <see langword="true"/> if this instance take ownership of <paramref name="sourceImage"/>; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="maskImage">The source image.</param>
        /// <param name="ownsMaskImage">
        /// <see langword="true"/> if this instance take ownership of <paramref name="maskImage"/>; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="transparencyCheckerboard">The source image.</param>
        /// <param name="ownsTransparencyCheckerboard">
        /// <see langword="true"/> if this instance take ownership of <paramref name="transparencyCheckerboard"/>; otherwise, <see langword="false"/>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="plugin"/> is null.
        /// or
        /// <paramref name="settings"/> is null.
        /// or
        /// <paramref name="error"/> is null.
        /// or
        /// <paramref name="setDestinationImage"/> is null.
        /// or
        /// <paramref name="sourceImage"/> is null.
        /// or
        /// <paramref name="transparencyCheckerboard"/> is null.
        /// </exception>
        public PSFilterShimPipeServer(Func<bool> abort,
                                      PluginData plugin,
                                      PSFilterShimSettings settings,
                                      Action<PSFilterShimErrorInfo?> error,
                                      Action<byte>? progress,
                                      Action<Stream, FilterPostProcessingOptions> setDestinationImage,
                                      IDocumentMetadataProvider documentMetadataProvider,
                                      ImageSurface sourceImage,
                                      bool ownsSourceImage,
                                      MaskSurface? maskImage,
                                      bool ownsMaskImage,
                                      TransparencyCheckerboardSurface transparencyCheckerboard,
                                      bool ownsTransparencyCheckerboard)
        {
            ArgumentNullException.ThrowIfNull(nameof(plugin));
            ArgumentNullException.ThrowIfNull(nameof(settings));
            ArgumentNullException.ThrowIfNull(nameof(error));
            ArgumentNullException.ThrowIfNull(nameof(setDestinationImage));
            ArgumentNullException.ThrowIfNull(nameof(documentMetadataProvider));
            ArgumentNullException.ThrowIfNull(nameof(sourceImage));
            ArgumentNullException.ThrowIfNull(nameof(transparencyCheckerboard));

            PipeName = "PSFilterShim_" + Guid.NewGuid().ToString();
            abortFunc = abort;
            pluginData = plugin;
            this.settings = settings;
            errorCallback = error;
            this.documentMetadataProvider = documentMetadataProvider;
            progressCallback = progress;
            this.setDestinationImage = setDestinationImage;
            this.sourceImage = sourceImage;
            this.ownsSourceImage = ownsSourceImage;
            this.maskImage = maskImage;
            this.ownsMaskImage = ownsMaskImage;
            this.transparencyCheckerboard = transparencyCheckerboard;
            this.ownsTransparencyCheckerboard = ownsTransparencyCheckerboard;

            // 4 bytes for the payload length and one byte for the payload.
            oneByteParameterReplyBuffer = new byte[5];
            BinaryPrimitives.WriteInt32LittleEndian(oneByteParameterReplyBuffer, sizeof(byte));

            server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            server.BeginWaitForConnection(WaitForConnectionCallback, null);
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

        public string PipeName { get; }

        public ParameterData? ParameterData { get; private set; }

        public PseudoResourceCollection? PseudoResources { get; private set; }

        public DescriptorRegistryValues? DescriptorRegistry {  get; private set; }

        // This property represents an Int32 with the value of 0.
        // The client reads this as the required buffer length for the payload.
        private static ReadOnlySpan<byte> EmptyReplyMessage => new byte[] { 0, 0, 0, 0 };

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                server?.Dispose();

                if (ownsSourceImage)
                {
                    sourceImage.Dispose();
                }

                if (ownsMaskImage)
                {
                    maskImage?.Dispose();
                }

                if (ownsTransparencyCheckerboard)
                {
                    transparencyCheckerboard.Dispose();
                }
            }
        }

        [SkipLocalsInit]
        private void WaitForConnectionCallback(IAsyncResult result)
        {
            if (IsDisposed)
            {
                return;
            }

            try
            {
                server.EndWaitForConnection(result);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            Span<byte> replySizeBuffer = stackalloc byte[sizeof(int)];
            server.ReadExactly(replySizeBuffer);

            int messageLength = BinaryPrimitives.ReadInt32LittleEndian(replySizeBuffer);

            const int MaxStackAllocBufferSize = 128;

            Span<byte> buffer = stackalloc byte[MaxStackAllocBufferSize];
            byte[]? bufferFromPool = null;

            try
            {
                if (messageLength > MaxStackAllocBufferSize)
                {
                    bufferFromPool = ArrayPool<byte>.Shared.Rent(messageLength);
                    buffer = bufferFromPool;
                }

                Span<byte> messageBytes = buffer.Slice(0, messageLength);
                server.ReadExactly(messageBytes);

                Command command = (Command)messageBytes[0];

                switch (command)
                {
                    case Command.AbortCallback:
                        SendReplyToClient((byte)(abortFunc() ? 1 : 0));
                        break;
                    case Command.ReportProgress:
                        progressCallback!(messageBytes[1]);
                        SendEmptyReplyToClient();
                        break;
                    case Command.GetPluginData:
                        using (ArrayPoolBufferWriter<byte> bufferWriter = new())
                        {
                            MessagePackSerializerUtil.Serialize(bufferWriter, pluginData, PSFilterShimResolver.Options);
                            SendReplyToClient(bufferWriter.WrittenSpan);
                        }
                        break;
                    case Command.GetSettings:
                        using (ArrayPoolBufferWriter<byte> bufferWriter = new())
                        {
                            MessagePackSerializerUtil.Serialize(bufferWriter, settings, PSFilterShimResolver.Options);
                            SendReplyToClient(bufferWriter.WrittenSpan);
                        }
                        break;
                    case Command.SetErrorInfo:
                        errorCallback(GetErrorInfo(messageBytes.Slice(1)));
                        SendEmptyReplyToClient();
                        break;
                    case Command.GetExifMetadata:
                        SendReplyToClient(documentMetadataProvider.GetExifData());
                        break;
                    case Command.GetXmpMetadata:
                        SendReplyToClient(documentMetadataProvider.GetXmpData());
                        break;
                    case Command.GetIccProfile:
                        SendReplyToClient(documentMetadataProvider.GetIccProfileData());
                        break;
                    case Command.GetSourceImage:
                        SendSourceImageToClient();
                        break;
                    case Command.GetSelectionMask:
                        SendSelectionMaskToClient();
                        break;
                    case Command.GetTransparencyCheckerboardImage:
                        SendTransparencyCheckerboardToClient();
                        break;
                    case Command.ReleaseMappedFile:
                        memoryMappedFile?.Dispose();
                        memoryMappedFile = null;
                        SendEmptyReplyToClient();
                        break;
                    case Command.SetDestinationImage:
                        SetDestinationImage(messageBytes.Slice(1));
                        SendEmptyReplyToClient();
                        break;
                    case Command.SetFilterData:
                        SetFilterData(messageBytes.Slice(1));
                        SendEmptyReplyToClient();
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown command value: {command}.");
                }
            }
            finally
            {
                if (bufferFromPool != null)
                {
                    ArrayPool<byte>.Shared.Return(bufferFromPool);
                }
            }

            server.WaitForPipeDrain();

            // Start a new server and wait for the next connection.
            server.Dispose();
            server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            server.BeginWaitForConnection(WaitForConnectionCallback, null);
        }

        private static PSFilterShimErrorInfo? GetErrorInfo(ReadOnlySpan<byte> buffer)
        {
            const int HeaderSize = sizeof(int) * 2;

            int messageLength = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            int detailsLength = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(sizeof(int)));

            PSFilterShimErrorInfo? errorInfo = null;

            if (messageLength > 0)
            {
                string message = Encoding.UTF8.GetString(buffer.Slice(HeaderSize, messageLength));
                string details = string.Empty;

                if (detailsLength > 0)
                {
                    details = Encoding.UTF8.GetString(buffer.Slice(HeaderSize + messageLength, detailsLength));
                }

                errorInfo = new PSFilterShimErrorInfo(message, details);
            }

            return errorInfo;
        }

        private void SendEmptyReplyToClient()
        {
            server!.Write(EmptyReplyMessage);
        }

        private void SendReplyToClient(byte data)
        {
            // The constructor already set the message header.
            oneByteParameterReplyBuffer[4] = data;
            server!.Write(oneByteParameterReplyBuffer, 0, oneByteParameterReplyBuffer.Length);
        }

        [SkipLocalsInit]
        private void SendReplyToClient(ReadOnlySpan<byte> data)
        {
            int count = data.Length;

            if (count == 0)
            {
                SendEmptyReplyToClient();
            }
            else
            {
                const int MaxStackAllocBufferSize = 256;

                int totalMessageLength = sizeof(int) + count;

                Span<byte> buffer = stackalloc byte[MaxStackAllocBufferSize];
                byte[]? bufferFromPool = null;

                try
                {
                    if (totalMessageLength > MaxStackAllocBufferSize)
                    {
                        bufferFromPool = ArrayPool<byte>.Shared.Rent(totalMessageLength);
                        buffer = bufferFromPool;
                    }

                    Span<byte> messageBytes = buffer.Slice(0, totalMessageLength);

                    BinaryPrimitives.WriteInt32LittleEndian(messageBytes, count);
                    data.CopyTo(messageBytes.Slice(sizeof(int)));

                    server!.Write(messageBytes);
                }
                finally
                {
                    if (bufferFromPool != null)
                    {
                        ArrayPool<byte>.Shared.Return(bufferFromPool);
                    }
                }
            }
        }

        private void SendReplyToClient(string value)
        {
            const int MaxStackAllocBufferSize = 256;

            int stringLengthInBytes = Encoding.UTF8.GetByteCount(value);

            int totalMessageLength = checked(sizeof(int) + stringLengthInBytes);

            Span<byte> buffer = stackalloc byte[MaxStackAllocBufferSize];
            byte[]? bufferFromPool = null;

            try
            {
                if (totalMessageLength > MaxStackAllocBufferSize)
                {
                    bufferFromPool = ArrayPool<byte>.Shared.Rent(totalMessageLength);
                    buffer = bufferFromPool;
                }

                Span<byte> messageBytes = buffer.Slice(0, totalMessageLength);

                BinaryPrimitives.WriteInt32LittleEndian(messageBytes, stringLengthInBytes);
                Encoding.UTF8.GetBytes(value, messageBytes.Slice(sizeof(int)));

                server!.Write(messageBytes);
            }
            finally
            {
                if (bufferFromPool != null)
                {
                    ArrayPool<byte>.Shared.Return(bufferFromPool);
                }
            }
        }

        private string CreateMemoryMappedFile(long capacity)
        {
            if (memoryMappedFile != null)
            {
                throw new InvalidOperationException("Only one memory mapped file can be open at a time.");
            }

            string name = Guid.NewGuid().ToString("n");

            memoryMappedFile = MemoryMappedFile.CreateNew(name, capacity);

            return name;
        }

        private unsafe void SendSourceImageToClient()
        {
            PSFilterShimImageHeader header = new(sourceImage.Width,
                                                 sourceImage.Height,
                                                 sourceImage.Format);

            string name = CreateMemoryMappedFile(header.GetTotalFileSize());

            using (MemoryMappedViewStream viewStream = memoryMappedFile!.CreateViewStream())
            {
                header.Save(viewStream);

                using (ISurfaceLock bitmapLock = sourceImage.Lock(SurfaceLockMode.Read))
                {
                    int bufferStride = header.Stride;

                    for (int y = 0; y < header.Height; y++)
                    {
                        ReadOnlySpan<byte> pixels = new(bitmapLock.GetRowPointerUnchecked(y), bufferStride);

                        viewStream.Write(pixels);
                    }
                }
            }

            SendReplyToClient(name);
        }

        private unsafe void SendSelectionMaskToClient()
        {
            if (maskImage is null)
            {
                SendEmptyReplyToClient();
                return;
            }

            PSFilterShimImageHeader header = new(maskImage.Width,
                                                 maskImage.Height,
                                                 SurfacePixelFormat.Gray8);

            string name = CreateMemoryMappedFile(header.GetTotalFileSize());

            using (MemoryMappedViewStream viewStream = memoryMappedFile!.CreateViewStream())
            {
                header.Save(viewStream);

                using (ISurfaceLock bitmapLock = maskImage.Lock(SurfaceLockMode.Read))
                {
                    int bufferStride = header.Stride;

                    for (int y = 0; y < header.Height; y++)
                    {
                        ReadOnlySpan<byte> pixels = new(bitmapLock.GetRowPointerUnchecked(y), bufferStride);

                        viewStream.Write(pixels);
                    }
                }
            }

            SendReplyToClient(name);
        }

        private unsafe void SendTransparencyCheckerboardToClient()
        {
            PSFilterShimImageHeader header = new(transparencyCheckerboard.Width,
                                                 transparencyCheckerboard.Height,
                                                 SurfacePixelFormat.Pbgra32);

            string name = CreateMemoryMappedFile(header.GetTotalFileSize());

            using (MemoryMappedViewStream viewStream = memoryMappedFile!.CreateViewStream())
            {
                header.Save(viewStream);

                using (ISurfaceLock bitmapLock = transparencyCheckerboard.Lock(SurfaceLockMode.Read))
                {
                    int bufferStride = header.Stride;

                    for (int y = 0; y < header.Height; y++)
                    {
                        ReadOnlySpan<byte> pixels = new(bitmapLock.GetRowPointerUnchecked(y), bufferStride);

                        viewStream.Write(pixels);
                    }
                }
            }

            SendReplyToClient(name);
        }

        private void SetDestinationImage(ReadOnlySpan<byte> message)
        {
            int mapNameLength = BinaryPrimitives.ReadInt32LittleEndian(message);
            string mapName = Encoding.UTF8.GetString(message.Slice(sizeof(int), mapNameLength));
            FilterPostProcessingOptions options = (FilterPostProcessingOptions)BinaryPrimitives.ReadInt32LittleEndian(message.Slice(sizeof(int) + mapNameLength));

            using (MemoryMappedFile file = MemoryMappedFile.OpenExisting(mapName))
            using (MemoryMappedViewStream viewStream = file.CreateViewStream())
            {
                setDestinationImage(viewStream, options);
            }
        }

        private void SetFilterData(ReadOnlySpan<byte> message)
        {
            FilterDataType type = (FilterDataType)message[0];

            ReadOnlySpan<byte> data = message.Slice(1);

            switch (type)
            {
                case FilterDataType.Parameter:
                    ParameterData = MessagePackSerializerUtil.Deserialize<ParameterData>(data, MessagePackResolver.Options);
                    break;
                case FilterDataType.PseudoResource:
                    PseudoResources = MessagePackSerializerUtil.Deserialize<PseudoResourceCollection>(data, MessagePackResolver.Options);
                    break;
                case FilterDataType.DescriptorRegistry:
                    DescriptorRegistry = MessagePackSerializerUtil.Deserialize<DescriptorRegistryValues>(data, MessagePackResolver.Options);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(FilterDataType)}: {type}.");
            }
        }
    }
}
