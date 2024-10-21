namespace Jarvis.Extension.Hosting
{
    public interface IEventLogInfo
    {
        int EventId { get; set; }
        EventLogLevel Level { get; set; }
        string Message { get; set; }
        DateTime TimeCreated { get; set; }
    }

    //public class EventLogInfo : IEventLogInfo
    //{
    //    public int EventId { get; set; }
    //    public EventLogLevel Level { get; set; }
    //    public string Message { get; set; }
    //    public DateTime TimeCreated { get; set; }

    //    public EventLogInfo(string message,  EventLogLevel level = EventLogLevel.Information, int eventId = 420, DateTime? timeCreated = null)
    //    {
    //        EventId = eventId;
    //        Level = level;
    //        Message = message;
    //        TimeCreated = timeCreated ?? DateTime.Now;
    //    }
    //}

    public enum EventLogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
        None = 6,
    }
}