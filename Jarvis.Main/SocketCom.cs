using System.Net;
using System.Net.Sockets;

namespace Jarvis.Main
{
    public enum SocketComType
    {
        Server,

        Client,
    }

    public class SocketCom
    {
        #region Events

        public event EventHandler<BytesReceivedEventArgs>? BytesReceived;

        public event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;

        public event EventHandler<MessageReceivedEventArgs>? DataReceived;

        protected void InvokeConnectionChanged(ConnectionStatusType status)
        {
            IsConnected = status == ConnectionStatusType.Connected;

            if (status == ConnectionStatusType.Connecting)
            {
                if (_isConnectingStarted)
                {
                    return;
                }
                else
                {
                    _isConnectingStarted = true;
                }
            }

            if (status == ConnectionStatusType.WaitingForServer)
            {
                if (_iswaitingForServerStarted)
                {
                    return;
                }
                else
                {
                    _iswaitingForServerStarted = true;
                }
            }

            if (status == ConnectionStatusType.Connected || status == ConnectionStatusType.Disconnected || status == ConnectionStatusType.Closed)
            {
                _isConnectingStarted = false;
                _iswaitingForServerStarted = false;
            }

            ConnectionChanged?.Invoke(this, new ConnectionChangedEventArgs(status, Host));
        }

        protected void InvokeDataReceived(string data)
        {
            DataReceived?.Invoke(this, new MessageReceivedEventArgs(data));
        }

        protected void InvokeDataReceived(byte[] data)
        {
            BytesReceived?.Invoke(this, new BytesReceivedEventArgs(data));
        }

        #endregion Events

        #region Definitions

        private readonly Dictionary<TcpClient, byte[]> _clients = new();

        public string Host { get; private set; }

        private bool _isConnectingStarted;
        private bool _iswaitingForServerStarted;
        private int _port;

        private TcpListener? _server;
        private SocketComType _type;

        public bool IsAutoConnect { get; set; } = true;

        public bool IsConnected { get; private set; }
        public int ReceiveBufferSize { get; set; } = 65536;

        public EncodingType ReceiveEncoding { get; set; } = EncodingType.ASCII;

        public EncodingType SendEncoding { get; set; } = EncodingType.ASCII;

        #endregion Definitions

        #region Constructor

        public SocketCom(SocketComType type, int port, string hostOrIp)
        {
            _type = type;
            Host = hostOrIp;
            _port = port;
        }

        public SocketCom()
        {
            Host = string.Empty;
        }

        protected void IniCom(SocketComType type, int port, string hostOrIp)
        {
            _type = type;
            Host = hostOrIp;
            _port = port;

            IniCom();
        }

        private void IniCom()
        {
            switch (_type)
            {
                case SocketComType.Server:
                    IPAddress ipAddress = IPAddress.Parse(Host);
                    _server = new TcpListener(ipAddress, _port);
                    _server.Start();
                    break;

                case SocketComType.Client:
                    _clients.Add(new TcpClient(), new byte[ReceiveBufferSize]);
                    break;

                default:
                    break;
            }
        }

        #endregion Constructor

        #region Methods

        public Task<bool> Close()
        {
            InvokeConnectionChanged(ConnectionStatusType.Closing);

            foreach (KeyValuePair<TcpClient, byte[]> client in _clients)
            {
                Close(client.Key);
            }

            if (_server != null)
            {
                _server.Stop();
                _server = null;
            }

            InvokeConnectionChanged(ConnectionStatusType.Closed);

            return Task.FromResult(true);
        }

        public async Task<bool> Send(string msg)
        {
            List<Task<bool>> tasks = new();

            foreach (KeyValuePair<TcpClient, byte[]> client in _clients)
            {
                tasks.Add(Send(client.Key, msg));
            }

            await Task.WhenAll(tasks);

            return true;
        }

        public async Task<bool> Send(byte[] msg)
        {
            List<Task<bool>> tasks = new();

            foreach (KeyValuePair<TcpClient, byte[]> client in _clients)
            {
                tasks.Add(Send(client.Key, msg));
            }

            await Task.WhenAll(tasks);

            return true;
        }

        public async Task<bool> SendTo(string hostOrIp, string msg)
        {
            //return await Send(_clients.FirstOrDefault(x => x.Client.LocalEndPoint.ToString().Equals(hostOrIp)), msg);

            foreach (KeyValuePair<TcpClient, byte[]> client in _clients)
            {
                if (client.Key.Client.LocalEndPoint is IPEndPoint iPEndPoint && iPEndPoint.Address.ToString().Equals(hostOrIp))
                {
                    return await Send(client.Key, msg);
                }
            }

            return true;
        }

        public async Task<bool> Start()
        {
            if (_server == null && !_clients.Any())
            {
                IniCom();
            }

            switch (_type)
            {
                case SocketComType.Server:
                    return await StartServer();

                case SocketComType.Client:
                    return await StartClient();

                default:
                    break;
            }

            return false;
        }

        public async Task<bool> Stop()
        {
            return await Close();
        }

        protected async Task<bool> StartClient()
        {
            foreach (KeyValuePair<TcpClient, byte[]> client in _clients)
            {
                await StartClient(client);
            }

            return true;
        }

        private static Task<bool> Close(TcpClient client)
        {
            if (client != null)
            {
                client.Close();
                client.Dispose();
            }
            return Task.FromResult(true);
        }

        private static async Task<bool> Send(TcpClient? client, byte[] msg)
        {
            if (client != null)
            {
                await client.GetStream().WriteAsync(msg);
                return true;
            }
            return false;
        }

        private async void OnClientAccepted(IAsyncResult result)
        {
            if (result.AsyncState is TcpListener server)
            {
                TcpClient client = server.EndAcceptTcpClient(result);
                _clients.Add(client, new byte[ReceiveBufferSize]);
                await StartClient();
                InvokeConnectionChanged(ConnectionStatusType.Connected);
                server.BeginAcceptTcpClient(OnClientAccepted, server);
            }
        }

        private async void OnDataReceived(IAsyncResult result)
        {
            if (result.AsyncState is KeyValuePair<TcpClient, byte[]> client && IsConnected)
            {
                int received = client.Key.Client.EndReceive(result);

                if (received > 0 && client.Key.Connected)
                {
                    InvokeDataReceived(client.Value);
                    string msg = GetEncoding(ReceiveEncoding).GetString(client.Value)[..received];

                    client.Key.Client.BeginReceive(client.Value, 0, client.Value.Length, SocketFlags.None, OnDataReceived, client);
                    InvokeDataReceived(msg);
                }
                else
                {
                    if (_type == SocketComType.Client)
                    {
                        InvokeConnectionChanged(ConnectionStatusType.Disconnected);
                        await Close();
                        await Start();
                    }
                    else if (_type == SocketComType.Server)
                    {
                        _clients.Remove(client.Key);
                        if (!_clients.Any())
                        {
                            InvokeConnectionChanged(ConnectionStatusType.Disconnected);
                        }
                    }
                }
            }
        }

        private async Task<bool> Send(TcpClient? client, string msg)
        {
            if (client != null)
            {
                byte[] bytes = GetEncoding(SendEncoding).GetBytes(msg);
                await client.GetStream().WriteAsync(bytes);
                return true;
            }
            return false;
        }

        private async Task<bool> StartClient(KeyValuePair<TcpClient, byte[]> client)
        {
            if (client.Key == null)
            {
                throw new InvalidOperationException("Client is null.");
            }

            client.Key.ReceiveBufferSize = ReceiveBufferSize;

            InvokeConnectionChanged(ConnectionStatusType.Connecting);

            try
            {
                if (_type == SocketComType.Client)
                {
                    await client.Key.ConnectAsync(Host, _port);
                }
                client.Key.Client.BeginReceive(client.Value, 0, client.Value.Length, SocketFlags.None, OnDataReceived, client);

                InvokeConnectionChanged(ConnectionStatusType.Connected);
            }
            catch (SocketException)
            {
                if (IsAutoConnect)
                {
                    InvokeConnectionChanged(ConnectionStatusType.WaitingForServer);
                    await Task.Delay(1000);
                    return await StartClient();
                }
            }

            return IsConnected;
        }

        private async Task<bool> StartServer()
        {
            if (_server == null)
            {
                throw new InvalidOperationException("Server is null.");
            }

            try
            {
                InvokeConnectionChanged(ConnectionStatusType.WaitingForClient);
                _server.BeginAcceptTcpClient(OnClientAccepted, _server);
            }
            catch (SocketException)
            {
                if (IsAutoConnect)
                {
                    //InvokeConnectionChanged(ConnectionStatusType.WaitingForServer);
                    await Task.Delay(1000);
                    return await StartServer();
                }
            }

            return false;
        }

        #endregion Methods

        #region Classes

        public enum EncodingType
        {
            NONE,

            ASCII,

            UNICODE,

            UNICODE_INV,

            HEX,

            BYTES,

            STRUCT,

            OBJ,

            CUSTOM
        }

        private static System.Text.Encoding GetEncoding(EncodingType type)
        {
            return type switch
            {
                EncodingType.NONE => System.Text.Encoding.ASCII,
                EncodingType.ASCII => System.Text.Encoding.ASCII,
                EncodingType.UNICODE => System.Text.Encoding.Unicode,
                EncodingType.UNICODE_INV => System.Text.Encoding.BigEndianUnicode,
                EncodingType.HEX => System.Text.Encoding.UTF8,
                EncodingType.BYTES => System.Text.Encoding.BigEndianUnicode,
                _ => System.Text.Encoding.ASCII,
            };
        }

        #endregion Classes
    }
}