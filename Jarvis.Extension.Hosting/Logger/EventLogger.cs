using Jarvis.Extension.Hosting.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;

namespace Jarvis.Extension.Hosting
{
	public class EventLogger : ILogger
	{
		#region Definitions

		public string LogName { get; set; }
		public string Source { get; set; }

		#endregion Definitions

		#region Contructor

		public EventLogger(IEventLoggerConfiguration config)
		{
			Source = config.Source;
			LogName = config.LogName;
			CreateEventLog(LogName, Source);
		}

		#endregion Contructor

		#region Methods

		public static (bool Status, bool IsError) CreateEventLog(string logName, string source)
		{
			bool isError = false;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				try
				{
					if (EventLog.Exists(logName))
					{
						EventLog? eventLog = EventLog.GetEventLogs(logName).FirstOrDefault();
						if (eventLog != null && eventLog.Source.Equals(source) == false)
						{
							EventLog.CreateEventSource(source, logName);
						}
					}
					else
					{
						EventLog.CreateEventSource(source: source, logName: logName);
					}
					if (!EventLog.SourceExists(source: source))
					{
						EventLog.CreateEventSource(source: source, logName: logName);
						return (true, isError);
					}
				}
				catch (SecurityException ex)
				{
					Debug.WriteLine(ex.Message);
					isError = true;
				}
				catch (PlatformNotSupportedException)
				{
					throw;
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}
			}
			return (false, isError);
		}

		public static (bool Status, bool IsError) LogEvent(string logName, string source, string message, LogLevel logLevel, EventId eventId)
		{
			if (logName != null) { }
			bool isError = false;
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				try
				{
					if (EventLog.SourceExists(source: source))
					{
						EventLogEntryType type = EventLogEntryType.Information;
						switch (logLevel)
						{
							case LogLevel.Trace:
								type = EventLogEntryType.Information;
								break;

							case LogLevel.Debug:
								type = EventLogEntryType.Information;
								break;

							case LogLevel.Information:
								type = EventLogEntryType.Information;
								break;

							case LogLevel.Warning:
								type = EventLogEntryType.Warning;
								break;

							case LogLevel.Error:
								type = EventLogEntryType.Error;
								break;

							case LogLevel.Critical:
								type = EventLogEntryType.Error;
								break;

							case LogLevel.None:
								type = EventLogEntryType.Information;
								break;

							default:
								break;
						}
						EventLog.WriteEntry(source, message, type, eventId.Id);
						return (true, isError);
					}
				}
				catch (SecurityException ex)
				{
					Debug.WriteLine(ex.Message);
					isError = true;
				}
				catch (PlatformNotSupportedException)
				{
					throw;
				}
			}
			return (false, isError);
		}

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel != LogLevel.None;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}
			string message = formatter(state, exception);
			LogEvent(LogName, Source, message, logLevel, eventId);
		}

		#endregion Methods
	}

	public static class LogLevelExtension
	{
		public static LogLevel GetLogLevel(this EventLogLevel logLevel)
		{
			return logLevel switch
			{
				EventLogLevel.Debug => LogLevel.Debug,
				EventLogLevel.Information => LogLevel.Information,
				EventLogLevel.Warning => LogLevel.Warning,
				EventLogLevel.Error => LogLevel.Error,
				_ => LogLevel.Information,
			};
		}

		public static EventLogLevel GetLogLevel(this LogLevel logLevel)
		{
			return logLevel switch
			{
				LogLevel.Debug => EventLogLevel.Debug,
				LogLevel.Information => EventLogLevel.Information,
				LogLevel.Warning => EventLogLevel.Warning,
				LogLevel.Error => EventLogLevel.Error,
				LogLevel.Trace => EventLogLevel.Trace,
				LogLevel.Critical => EventLogLevel.Critical,
				LogLevel.None => EventLogLevel.None,
				_ => EventLogLevel.Information,
			};
		}
	}
}