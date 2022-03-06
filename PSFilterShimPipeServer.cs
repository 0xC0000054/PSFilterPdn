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
using System.IO;
using System.IO.Pipes;
using System.Text;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimPipeServer : IDisposable
    {
        private NamedPipeServerStream server;
        private readonly byte[] oneByteParameterMessageBuffer;
        private readonly byte[] oneByteParameterReplyBuffer;
        private readonly byte[] replySizeBuffer;

        private readonly Func<bool> abortFunc;
        private readonly PluginData pluginData;
        private readonly PSFilterShimSettings settings;
        private readonly Action<string> errorCallback;
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
        /// </exception>
        public PSFilterShimPipeServer(Func<bool> abort,
                                      PluginData plugin,
                                      PSFilterShimSettings settings,
                                      Action<string> error,
                                      Action<byte> progress)
        {
            PipeName = "PSFilterShim_" + Guid.NewGuid().ToString();
            abortFunc = abort;
            pluginData = plugin ?? throw new ArgumentNullException(nameof(plugin));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            errorCallback = error;
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
            SetErrorMessage
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
                    using (MemoryStream stream = new MemoryStream())
                    {
                        DataContractSerializerUtil.Serialize(stream, pluginData);
                        SendReplyToClient(stream.GetBuffer(), 0, (int)stream.Length);
                    }
                    break;
                case Command.GetSettings:
                    using (MemoryStream stream = new MemoryStream())
                    {
                        DataContractSerializerUtil.Serialize(stream, settings);
                        SendReplyToClient(stream.GetBuffer(), 0, (int)stream.Length);
                    }
                    break;
                case Command.SetErrorMessage:
                    errorCallback(Encoding.UTF8.GetString(messageBytes, 1, messageLength - 1));
                    SendEmptyReplyToClient();
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

        private void SendReplyToClient(byte[] data, int offset, int count)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (count == 0)
            {
                SendEmptyReplyToClient();
            }
            else
            {
                byte[] messageBytes = new byte[sizeof(int) + count];

                messageBytes[0] = (byte)(count & 0xff);
                messageBytes[1] = (byte)((count >> 8) & 0xff);
                messageBytes[2] = (byte)((count >> 16) & 0xff);
                messageBytes[3] = (byte)((count >> 24) & 0xff);
                Array.Copy(data, offset, messageBytes, 4, count);

                server.Write(messageBytes, 0, messageBytes.Length);
            }
        }
    }
}
