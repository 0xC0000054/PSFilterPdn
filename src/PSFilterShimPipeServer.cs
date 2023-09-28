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
using System;
using System.Buffers;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimPipeServer : Disposable
    {
        private NamedPipeServerStream server;
        private MemoryMappedFile? memoryMappedFile;
        private readonly string cancellationEventName;
        private readonly EventWaitHandle cancellationEventWaitHandle;
        private readonly PluginData pluginData;
        private readonly PSFilterShimSettings settings;
        private readonly Action<PSFilterShimErrorInfo?> errorCallback;
        private readonly Action<byte>? progressCallback;
        private readonly Action<Stream, FilterPostProcessingOptions> setDestinationImage;
        private readonly IDocumentMetadataProvider documentMetadataProvider;
        private readonly ImageSurface sourceImage;
        private readonly bool ownsSourceImage;
        private readonly MaskSurface? maskImage;
        private readonly bool ownsMaskImage;
        private readonly TransparencyCheckerboardSurface transparencyCheckerboard;
        private readonly bool ownsTransparencyCheckerboard;
        private readonly CancellationToken cancellationToken;
        private readonly CancellationTokenRegistration cancellationTokenRegistration;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSFilterShimService"/> class.
        /// </summary>
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
        public PSFilterShimPipeServer(PluginData plugin,
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
                                      bool ownsTransparencyCheckerboard,
                                      CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(nameof(plugin));
            ArgumentNullException.ThrowIfNull(nameof(settings));
            ArgumentNullException.ThrowIfNull(nameof(error));
            ArgumentNullException.ThrowIfNull(nameof(setDestinationImage));
            ArgumentNullException.ThrowIfNull(nameof(documentMetadataProvider));
            ArgumentNullException.ThrowIfNull(nameof(sourceImage));
            ArgumentNullException.ThrowIfNull(nameof(transparencyCheckerboard));

            PipeName = "PSFilterShim_" + Guid.NewGuid().ToString();
            cancellationEventName = Guid.NewGuid().ToString();
            cancellationEventWaitHandle = EventWaitHandleAcl.Create(false,
                                                                    EventResetMode.ManualReset,
                                                                    cancellationEventName,
                                                                    out _,
                                                                    CreateEventWaitHandleSecurity());
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
            this.cancellationToken = cancellationToken;
            cancellationTokenRegistration = this.cancellationToken.Register(SignalCancelRequest);

            server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            server.BeginWaitForConnection(WaitForConnectionCallback, null);
        }

        private enum Command : byte
        {
            GetAbortEventHandleName = 0,
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
                cancellationTokenRegistration.Dispose();
                cancellationEventWaitHandle.Dispose();

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

        private static EventWaitHandleSecurity CreateEventWaitHandleSecurity()
        {
            // The EventWaitHandle documentation on MSDN warns that the default permissions are
            // machine-wide and that callers may want to restrict access to the current user.
            NTAccount user = new(Environment.UserDomainName, Environment.UserName);

            EventWaitHandleSecurity security = new();

            // Add a rule that grants the current user the right to signal or wait on the event.
            security.AddAccessRule(new EventWaitHandleAccessRule(user,
                                                                 EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize,
                                                                 AccessControlType.Allow));
            // Add a rule that denies the current user the right to change permissions on the event.
            security.AddAccessRule(new EventWaitHandleAccessRule(user,
                                                                 EventWaitHandleRights.ChangePermissions,
                                                                 AccessControlType.Deny));
            return security;
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

            Command command = (Command)server.ReadByteEx();

            switch (command)
            {
                case Command.GetAbortEventHandleName:
                    SendReplyToClient(cancellationEventName);
                    break;
                case Command.ReportProgress:
                    progressCallback!(server.ReadByteEx());
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
                    errorCallback(GetErrorInfo());
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
                    SetDestinationImage();
                    SendEmptyReplyToClient();
                    break;
                case Command.SetFilterData:
                    SetFilterData(server);
                    SendEmptyReplyToClient();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown command value: {command}.");
            }

            server.WaitForPipeDrain();

            // Start a new server and wait for the next connection.
            server.Dispose();
            server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            server.BeginWaitForConnection(WaitForConnectionCallback, null);
        }

        private PSFilterShimErrorInfo? GetErrorInfo()
        {
            string message = ReadString();
            string details = ReadString();

            PSFilterShimErrorInfo? errorInfo = null;

            if (!string.IsNullOrEmpty(message))
            {
                errorInfo = new PSFilterShimErrorInfo(message, details);
            }

            return errorInfo;
        }

        [SkipLocalsInit]
        private string ReadString()
        {
            string value = string.Empty;

            int lengthInBytes = server!.ReadInt32LittleEndian();

            if (lengthInBytes > 0)
            {
                const int MaxStackAllocBufferSize = 256;

                Span<byte> buffer = stackalloc byte[MaxStackAllocBufferSize];
                byte[]? bufferFromPool = null;

                try
                {
                    if (lengthInBytes > MaxStackAllocBufferSize)
                    {
                        bufferFromPool = ArrayPool<byte>.Shared.Rent(lengthInBytes);
                        buffer = bufferFromPool;
                    }

                    Span<byte> stringBytes = buffer.Slice(0, lengthInBytes);

                    server.ReadExactly(stringBytes);

                    value = Encoding.UTF8.GetString(stringBytes);
                }
                finally
                {
                    if (bufferFromPool != null)
                    {
                        ArrayPool<byte>.Shared.Return(bufferFromPool);
                    }
                }
            }

            return value;
        }

        private void SendEmptyReplyToClient()
        {
            server!.Write(EmptyReplyMessage);
        }

        private void SendReplyToClient(ReadOnlySpan<byte> data)
        {
            server!.WriteInt32LittleEndian(data.Length);
            server.Write(data);
        }

        [SkipLocalsInit]
        private void SendReplyToClient(string value)
        {
            const int MaxStackAllocBufferSize = 256;

            int maxStringLengthInBytes = Encoding.UTF8.GetMaxByteCount(value.Length);

            Span<byte> buffer = stackalloc byte[MaxStackAllocBufferSize];
            byte[]? bufferFromPool = null;

            try
            {
                if (maxStringLengthInBytes > MaxStackAllocBufferSize)
                {
                    bufferFromPool = ArrayPool<byte>.Shared.Rent(maxStringLengthInBytes);
                    buffer = bufferFromPool;
                }

                int bytesWritten = Encoding.UTF8.GetBytes(value, buffer);

                server!.WriteInt32LittleEndian(bytesWritten);
                server.Write(buffer.Slice(0, bytesWritten));
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

        private void SetDestinationImage()
        {
            string mapName = ReadString();
            FilterPostProcessingOptions options = (FilterPostProcessingOptions)server.ReadInt32LittleEndian();

            using (MemoryMappedFile file = MemoryMappedFile.OpenExisting(mapName))
            using (MemoryMappedViewStream viewStream = file.CreateViewStream())
            {
                setDestinationImage(viewStream, options);
            }
        }

        private void SetFilterData(Stream stream)
        {
            FilterDataType type = (FilterDataType)stream.ReadByteEx();
            int dataLength = stream.ReadInt32LittleEndian();

            using (MemoryOwner<byte> owner = MemoryOwner<byte>.Allocate(dataLength))
            {
                stream.ReadExactly(owner.Span);

                switch (type)
                {
                    case FilterDataType.Parameter:
                        ParameterData = MessagePackSerializerUtil.Deserialize<ParameterData>(owner.Memory, MessagePackResolver.Options);
                        break;
                    case FilterDataType.PseudoResource:
                        PseudoResources = MessagePackSerializerUtil.Deserialize<PseudoResourceCollection>(owner.Memory, MessagePackResolver.Options);
                        break;
                    case FilterDataType.DescriptorRegistry:
                        DescriptorRegistry = MessagePackSerializerUtil.Deserialize<DescriptorRegistryValues>(owner.Memory, MessagePackResolver.Options);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported {nameof(FilterDataType)}: {type}.");
                }
            }
        }

        private void SignalCancelRequest() => cancellationEventWaitHandle.Set();
    }
}
