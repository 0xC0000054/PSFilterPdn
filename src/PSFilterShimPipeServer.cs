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

using PaintDotNet.IO;
using PSFilterLoad.PSApi;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimPipeServer : IDisposable
    {
        private NamedPipeServerStream server;
        private readonly byte[] oneByteParameterMessageBuffer;
        private readonly byte[] oneByteParameterReplyBuffer;
        private readonly byte[] replySizeBuffer;
        private readonly IDocumentMetadataProvider documentMetadataProvider;

        private readonly Func<bool> abortFunc;
        private readonly PluginData pluginData;
        private readonly PSFilterShimSettings settings;
        private readonly Action<string> errorCallback;
        private readonly Action<FilterPostProcessingOptions> postProcessingOptionsCallback;
        private readonly Action<byte> progressCallback;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSFilterShimService"/> class.
        /// </summary>
        /// <param name="abort">The abort callback.</param>
        /// <param name="plugin">The plug-in data.</param>
        /// <param name="settings">The settings for the shim application.</param>
        /// <param name="error">The error callback.</param>
        /// <param name="progress">The progress callback.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="plugin"/> is null.
        /// or
        /// <paramref name="settings"/> is null.
        /// or
        /// <paramref name="error"/> is null.
        /// or
        /// <paramref name="postProcessingOptions"/> is null.
        /// or
        /// <paramref name="effectEnvironment"/> is null.
        /// </exception>
        public PSFilterShimPipeServer(Func<bool> abort,
                                      PluginData plugin,
                                      PSFilterShimSettings settings,
                                      Action<string> error,
                                      Action<FilterPostProcessingOptions> postProcessingOptions,
                                      Action<byte> progress,
                                      IDocumentMetadataProvider documentMetadataProvider)
        {
            ArgumentNullException.ThrowIfNull(nameof(plugin));
            ArgumentNullException.ThrowIfNull(nameof(settings));
            ArgumentNullException.ThrowIfNull(nameof(error));
            ArgumentNullException.ThrowIfNull(nameof(postProcessingOptions));
            ArgumentNullException.ThrowIfNull(nameof(documentMetadataProvider));

            PipeName = "PSFilterShim_" + Guid.NewGuid().ToString();
            abortFunc = abort;
            pluginData = plugin;
            this.settings = settings;
            errorCallback = error;
            postProcessingOptionsCallback = postProcessingOptions;
            this.documentMetadataProvider = documentMetadataProvider;
            progressCallback = progress;
            // One byte for the command index and one byte for the payload.
            oneByteParameterMessageBuffer = new byte[2];
            // 4 bytes for the payload length and one byte for the payload.
            oneByteParameterReplyBuffer = new byte[5];
            Array.Copy(BitConverter.GetBytes(sizeof(byte)), oneByteParameterReplyBuffer, sizeof(int));
            replySizeBuffer = new byte[sizeof(int)];

            server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            server.BeginWaitForConnection(WaitForConnectionCallback, null);
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

        public string PipeName { get; }

        public void Dispose()
        {
            if (server != null)
            {
                server.Dispose();
                server = null;
            }
        }

        private void WaitForConnectionCallback(IAsyncResult result)
        {
            if (server is null)
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

            server.ProperRead(replySizeBuffer, 0, replySizeBuffer.Length);

            int messageLength = BitConverter.ToInt32(replySizeBuffer, 0);

            byte[] messageBytes;

            if (messageLength <= oneByteParameterMessageBuffer.Length)
            {
                messageBytes = oneByteParameterMessageBuffer;
            }
            else
            {
                messageBytes = new byte[messageLength];
            }

            server.ProperRead(messageBytes, 0, messageLength);

            Command command = (Command)messageBytes[0];

            switch (command)
            {
                case Command.AbortCallback:
                    SendReplyToClient((byte)(abortFunc() ? 1 : 0));
                    break;
                case Command.ReportProgress:
                    progressCallback(messageBytes[1]);
                    SendEmptyReplyToClient();
                    break;
                case Command.GetPluginData:
                    using (MemoryStream stream = new())
                    {
                        DataContractSerializerUtil.Serialize(stream, pluginData);
                        SendReplyToClient(new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int)stream.Length));
                    }
                    break;
                case Command.GetSettings:
                    using (MemoryStream stream = new())
                    {
                        DataContractSerializerUtil.Serialize(stream, settings);
                        SendReplyToClient(new ReadOnlySpan<byte>(stream.GetBuffer(), 0, (int)stream.Length));
                    }
                    break;
                case Command.SetErrorMessage:
                    errorCallback(Encoding.UTF8.GetString(messageBytes, 1, messageLength - 1));
                    SendEmptyReplyToClient();
                    break;
                case Command.SetPostProcessingOptions:
                    postProcessingOptionsCallback(GetPostProcessingOptions(messageBytes, 1, messageLength - 1));
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
                default:
                    throw new InvalidOperationException($"Unknown command value: { command }.");
            }

            server.WaitForPipeDrain();

            // Start a new server and wait for the next connection.
            server.Dispose();
            server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            server.BeginWaitForConnection(WaitForConnectionCallback, null);
        }

        private static FilterPostProcessingOptions GetPostProcessingOptions(byte[] buffer, int startIndex, int length)
        {
            int options = BinaryPrimitives.ReadInt32LittleEndian(buffer.AsSpan(startIndex, length));

            return (FilterPostProcessingOptions)options;
        }

        private void SendEmptyReplyToClient()
        {
            // Send a zero length reply.

            replySizeBuffer[0] = replySizeBuffer[1] = replySizeBuffer[2] = replySizeBuffer[3] = 0;

            server.Write(replySizeBuffer, 0, replySizeBuffer.Length);
        }

        private void SendReplyToClient(byte data)
        {
            // The constructor already set the message header.
            oneByteParameterReplyBuffer[4] = data;
            server.Write(oneByteParameterReplyBuffer, 0, oneByteParameterReplyBuffer.Length);
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

                Span<byte> messageBytes = stackalloc byte[MaxStackAllocBufferSize];

                if (totalMessageLength > MaxStackAllocBufferSize)
                {
                    messageBytes = new byte[totalMessageLength];
                }

                BinaryPrimitives.WriteInt32LittleEndian(messageBytes, count);
                data.CopyTo(messageBytes.Slice(sizeof(int)));

                server.Write(messageBytes.Slice(0, totalMessageLength));
            }
        }
    }
}
