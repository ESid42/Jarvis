using Microsoft.Extensions.Logging;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

namespace Jarvis.Extension.Hosting
{
    public static class EventLoggerUtils
    {
        public static IEnumerable<EventLogInfo> ReadEventLogs(string logPath, string logName, string queryText = "*", DateTime? startTime = null, DateTime? endTime = null)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                string path = string.IsNullOrEmpty(logName) ? logPath : $"{logPath}/{logName}";
                EventLogQuery query = new(path, PathType.LogName, queryText);
                EventLogReader reader = new(query);
                EventRecord eventRecord;
                List<EventLogInfo> events = new();
                bool isAdd = true;
                bool isCheckTime = startTime != null && endTime != null;

                while ((eventRecord = reader.ReadEvent()) != null)
                {
                    if (isCheckTime)
                    {
                        isAdd = eventRecord.TimeCreated > startTime && eventRecord.TimeCreated < endTime;
                    }
                    if (isAdd)
                    {
                        EventLogLevel logLevel = EventLogLevel.Information;
                        if (Enum.TryParse(eventRecord.LevelDisplayName, true, out EventLogLevel logLvl)) logLevel = logLvl;
                        events.Add(new EventLogInfo(TruncateDescription(eventRecord.FormatDescription()), eventRecord.Id, logLevel) { TimeCreated = eventRecord.TimeCreated ?? DateTime.MaxValue });
                    }
                }

                return events;
            }
            else
            {
                throw new InvalidOperationException("Operation only supported on Windows platform");
            }
        }

        private static string TruncateDescription(string description)
        {
            if (description.Contains("EventId:"))
            {
                string desc = description.Split("EventId:")[1];
                int index = desc.IndexOf("\n");
                desc = desc[(index + 1)..].Trim();
                return desc;
            }
            return description;
        }
    }
}