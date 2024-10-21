using Jarvis.Extension.Hosting.Interfaces;
using System.Text.Json.Serialization;

namespace Jarvis.Extension.Hosting.Interfaces
{

    public class EventLoggerConfiguration : IEventLoggerConfiguration
    {
        [JsonRequired]
        public string LogName { get; set; } = string.Empty;

        [JsonRequired]
        public string Source { get; set; } = string.Empty;
    }
}