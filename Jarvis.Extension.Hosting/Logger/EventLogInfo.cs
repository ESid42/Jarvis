using Jarvis.Extension.Hosting;
using EventLogLevel = Jarvis.Extension.Hosting.EventLogLevel;

namespace Jarvis.Extension.Hosting
{ 
	public class EventLogInfo : EventArgs, IEventLogInfo
    {
        public EventLogInfo(string message, int eventId, EventLogLevel level)
        {
            Message = message;
            EventId = eventId;
            Level = level;
        }

        public EventLogInfo(string message, int eventId, Microsoft.Extensions.Logging.LogLevel level)
        {
            Message = message;
            EventId = eventId;
            Level = level.GetLogLevel();
        }



        public EventLogInfo(string message, int eventId)
        {
            Message = message;
            EventId = eventId;
            Level = EventLogLevel.Information;
        }

        public EventLogInfo(string message)
        {
            Message = message;
            EventId = 909;
            Level = EventLogLevel.Information;
        }

        public int EventId { get; set; }
        public EventLogLevel Level { get; set; }
        public string Message { get; set; }
        public DateTime TimeCreated { get; set; } = DateTime.Now;
    }
}