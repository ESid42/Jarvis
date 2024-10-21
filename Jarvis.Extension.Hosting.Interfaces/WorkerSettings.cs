namespace Jarvis.Extension.Hosting.Interfaces
{
    public interface IWorkerSettings
    {
        bool IsRunOnce { get; set; }
    }

    public class WorkerSettings : IWorkerSettings
    {
        public EventLogLevel MinLogLevel { get; set; }
        public bool IsRunOnce { get; set; }
    }
}