using Jarvis.Extension.Hosting.Interfaces;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Jarvis.Extension.Hosting
{
	public static class EventLoggerExtensions
	{
		public static ILoggingBuilder AddEventLogger(
			this ILoggingBuilder builder)
		{
			builder.AddConfiguration();

			builder.Services.TryAddEnumerable(
				ServiceDescriptor.Singleton<ILoggerProvider, EventLoggerProvider>());
			LoggerProviderOptions.RegisterProviderOptions
			<IEventLoggerConfiguration, EventLoggerProvider>(builder.Services);
			return builder;
		}

		public static ILoggingBuilder AddEventLogger(
			this ILoggingBuilder builder, Action<IEventLoggerConfiguration> configure)
		{
			builder.AddConfiguration();

			builder.Services.TryAddEnumerable(
				ServiceDescriptor.Singleton<ILoggerProvider, EventLoggerProvider>());
			LoggerProviderOptions.RegisterProviderOptions
			<IEventLoggerConfiguration, EventLoggerProvider>(builder.Services);

			//builder.AddEventLogger();
			builder.Services.Configure(configure);

			return builder;
		}
	}

	[ProviderAlias("Event")]
	public sealed class EventLoggerProvider : ILoggerProvider
	{
		private readonly IEventLoggerConfiguration _currentConfig;

		private readonly ConcurrentDictionary<string, EventLogger> _loggers =
				new(StringComparer.OrdinalIgnoreCase);

		public EventLoggerProvider(IOptionsMonitor<IEventLoggerConfiguration> config)
		{
			_currentConfig = config.CurrentValue;
		}

		public ILogger CreateLogger(string categoryName)
		{
			return _loggers.GetOrAdd(categoryName, (name) => new EventLogger(_currentConfig));
		}

		public void Dispose()
		{
			_loggers.Clear();
		}
	}
}