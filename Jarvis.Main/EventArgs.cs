namespace Jarvis.Main
{
    public class ConnectionChangedEventArgs
    {
        public string EndpointIp { get; set; }
        public ConnectionStatusType Status { get; set; }

        public ConnectionChangedEventArgs(ConnectionStatusType isConnected, string endpointIp = "")
        {
            EndpointIp = endpointIp;
            Status = isConnected;
        }
    }

    public class MessageReceivedEventArgs
    {
        public string Message { get; set; }

        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }
    }

    public class BytesReceivedEventArgs
    {
        public BytesReceivedEventArgs(byte[] data)
        {
            Data = data;
        }

        public byte[] Data { get; set; }
    }
}