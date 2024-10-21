namespace Jarvis.COM
{
    public interface ICom
    {
        bool IsAutoConnect { get; set; }

        int ReceiveBufferSize { get; set; }

        Task<bool> Send(string msg);

        Task<bool> SendTo(string hostOrIp, string msg);
    }
}