using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using OldSchool.Extensibility;
using OldSchool.Ifx.Session;

namespace OldSchool.Ifx.Networking
{
    public class TelnetClient : INetworkClient
    {
        private static readonly byte[] m_ShutdownMessage = Encoding.ASCII.GetBytes("Server shutting down...");
        private byte[] m_Buffer;

        private bool m_IsEchoEnabled = true;

        private bool m_MaskNextInput;
        private Socket m_Socket;

        public TelnetClient(Socket socket)
        {
            m_Socket = socket;
            ClientAddress = ((IPEndPoint)m_Socket.RemoteEndPoint).Address;
            Console.WriteLine($"New Client Connected :: ({ClientAddress})");
            m_Buffer = new byte[] { };
            var state = new SocketObject();
            m_Socket.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, EndReceive, state);
            Send(new byte[] { 255, (byte)TelnetResponseCode.Will, 1 }); // We want to enable echo
        }

        public Action<INetworkClient> OnClientTerminated { private get; set; }

        public Guid Id { get; } = Guid.NewGuid();

        public Action<Guid, byte[]> OnDataReceived { private get; set; }
        public Action<Guid> OnSendComplete { private get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IPAddress ClientAddress { get; }

        public async Task Send(Stream stream, IDictionary<string, object> properties)
        {
            Send(await stream.GetBytes());
            SetMaskedInput(properties);
        }

        public void Disconnect()
        {
            if (m_Socket == null)
                return;

            if (m_Socket.Connected)
                m_Socket.Disconnect(false);

            m_Socket.Dispose();
            m_Socket = null;
        }

        public Task Send(string message, IDictionary<string, object> properties)
        {
            return Task.Factory.StartNew(() =>
                                         {
                                             SetMaskedInput(properties);
                                             var data = Encoding.ASCII.GetBytes(message);
                                             Send(data);
                                         });
        }

        private void SetMaskedInput(IDictionary<string, object> properties)
        {
            m_MaskNextInput = false;
            var value = properties.Get(SessionConstants.MaskNextInput);
            if (value != null)
                m_MaskNextInput = (bool)value;
        }

        private void Send(byte[] data)
        {
            m_Socket?.BeginSend(data, 0, data.Length, SocketFlags.None, SendComplete, null);
        }

        private void EndReceive(IAsyncResult asyncResult)
        {
            var state = (SocketObject)asyncResult.AsyncState;
            var bytesRead = m_Socket?.EndReceive(asyncResult);
            if (!bytesRead.HasValue || (bytesRead.Value == 0))
            {
                HandleDisconnect();
                return;
            }


            var raw = new byte[bytesRead.Value];
            Buffer.BlockCopy(state.Buffer, 0, raw, 0, bytesRead.Value);
            m_Buffer = m_Buffer.Append(raw);

            // If this is an IAC request, don't bother processing it or sending it back in an echo
            if (HandleIac())
            {
                m_Socket?.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, EndReceive, state);
                return;
            }

            if (m_IsEchoEnabled)
            {
                var echoed = new byte[raw.Length];
                Buffer.BlockCopy(raw, 0, echoed, 0, raw.Length);
                echoed = echoed.ExpandBackspaces();

                if (m_MaskNextInput)
                {
                    for (var i = 0; i < echoed.Length; i++)
                    {
                        if (echoed[i] == 13)
                            continue;

                        if (echoed[i] == 10)
                            continue;

                        if (echoed[i] == 8)
                            continue;

                        if (echoed[i] == 32)
                            continue;

                        echoed[i] = 42; // *
                    }
                }

                Send(echoed);
            }
            m_Buffer = m_Buffer.RemoveBackspaces();
            ProcessBuffer();
            m_Socket?.BeginReceive(state.Buffer, 0, state.BufferSize, SocketFlags.None, EndReceive, state);
        }

        private void ProcessBuffer()
        {
            var carriageReturn = m_Buffer.LocateFirst(13);
            if (!carriageReturn.HasValue) // Any Carriage returns?
                return;

            var skipCount = 1;
            if (m_Buffer[carriageReturn.Value + 1] == '\n')
                skipCount = 2;

            m_MaskNextInput = false;
            var command = m_Buffer.Substring(0, carriageReturn.Value); // Get the command/response
            command = command.RemoveBackspaces();
            m_Buffer = m_Buffer.Trim(carriageReturn.Value + skipCount); // Reset the buffer excluding the above + the \r
            OnDataReceived?.Invoke(Id, command);
        }

        private void HandleDisconnect()
        {
            if (m_Socket == null)
                return; // Happens when server shutdown while a client was connected

            Console.WriteLine($"Client disconnected :: ({ClientAddress})");
            m_Socket.Close();
            m_Socket.Dispose();
            m_Socket = null;
            OnClientTerminated?.Invoke(this);
        }

        private void SendComplete(IAsyncResult asyncResult)
        {
            m_Socket?.EndReceive(asyncResult);
            OnSendComplete?.Invoke(Id);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            if (m_Socket == null)
                return;

            Send(m_ShutdownMessage);

            if (m_Socket.Connected)
                m_Socket.Disconnect(false);

            m_Socket.Dispose();
            m_Socket = null;
        }

        private bool SetFlag(byte command, bool value)
        {
            switch (command)
            {
                case 1: // Echo
                    m_IsEchoEnabled = value;
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleIac()
        {
            while (true)
            {
                var iacCommandIndex = m_Buffer.LocateFirst(255);
                if (!iacCommandIndex.HasValue)
                    return false;

                var iacCommand = m_Buffer.Substring(iacCommandIndex.Value, 3);
                if (iacCommand.Length != 3)
                    return false;

                // We have a solid value, strip it from the buffer
                m_Buffer = m_Buffer.Substring(3);

                switch ((TelnetResponseCode)iacCommand[1])
                {
                    case TelnetResponseCode.Dont:
                        SetFlag(iacCommand[2], false); // Set Flag if we're aware of it
                        Send(new byte[] { 255, (byte)TelnetResponseCode.Wont, iacCommand[2] });
                        break;
                    case TelnetResponseCode.Will:
                        if (!SetFlag(iacCommand[2], true)) // Set Flag if we're aware of it
                            Send(new byte[] { 255, (byte)TelnetResponseCode.Wont, iacCommand[2] });
                        else
                            Send(new byte[] { 255, (byte)TelnetResponseCode.Will, iacCommand[2] });
                        break;
                    case TelnetResponseCode.Do:
                        if (!SetFlag(iacCommand[2], true)) // Set Flag if we're aware of it
                            Send(new byte[] { 255, (byte)TelnetResponseCode.Wont, iacCommand[2] });
                        else
                            Send(new byte[] { 255, (byte)TelnetResponseCode.Will, iacCommand[2] });
                        break;
                }

                return true;
            }
        }
    }

    public class SocketObject
    {
        public SocketObject()
        {
            BufferSize = 2048;
            Buffer = new byte[BufferSize];
        }

        public int BufferSize { get; }
        public byte[] Buffer { get; }
    }
}