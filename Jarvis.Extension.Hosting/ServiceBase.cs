using Jarvis.Common;
using Jarvis.Extension.Hosting.Interfaces;
using Microsoft.Extensions.Logging.EventLog;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Jarvis.Extension.Hosting
{
	public class ServiceBase<TProcess, TProcessSettings, TServiceSettings> where TProcess : IProcessBase<TProcessSettings> where TServiceSettings : IServiceSettings<TProcessSettings> 
	{
		public static async Task Run(TServiceSettings serviceSettings)
		{
			IHost host = CreateHost(serviceSettings);
			await host.RunAsync();
		}

		public static async Task Run(IEnumerable<TServiceSettings> serviceSettings)
		{
			IEnumerable<IHost> hosts = CreateHosts(serviceSettings);
			ConcurrentBag<Task> tasks = new();
			foreach (IHost host in hosts)
			{
				tasks.Add(host.RunAsync());
			}
			await Task.WhenAll(tasks);
		}

		public static async Task RunAsync(TServiceSettings serviceSettings,string jsonPath = "")
		{
			await CreateHost(serviceSettings, jsonPath).RunAsync();
		}

		private static IHost CreateHost(TServiceSettings serviceSettings, string jsonPath = "")
		{
			IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
			.ConfigureLogging((context, logging) =>
			{
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					EventLogSettings eventLogSettings = new()
					{
						LogName = serviceSettings.EventLoggerConfiguration?.LogName,
						SourceName = serviceSettings.EventLoggerConfiguration?.Source
					};
					string configStr = $"{{\"EventLog\":{{\"LogName\":\"{serviceSettings.EventLoggerConfiguration?.LogName}\",\"SourceName\":\"{serviceSettings.EventLoggerConfiguration?.Source}\",\"LogLevel\":{{\"Default\":\"Information\"}}}}}}";
					MemoryStream? stream = new();
					StreamWriter? writer = new(stream);
					writer.Write(configStr);
					writer.Flush();
					stream.Position = 0;
					ConfigurationBuilder configurationBuilder = new();
					_ = configurationBuilder.AddJsonStream(stream);
					IConfigurationRoot configuration = configurationBuilder.Build();
					_ = logging.ClearProviders();
					_ = logging.AddConfiguration(configuration);
					_ = logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
					_ = logging.AddEventLog(eventLogSettings);
					_ = logging.AddConsole();
				}
			})
			.ConfigureAppConfiguration(config=>
			{
				if (string.IsNullOrEmpty(jsonPath) is false)
				{
					config.AddJsonFile(jsonPath);
				}
			})
			.UseWindowsService()
			.ConfigureServices((hostContext, services) =>
			{
				Container container = new();
				container.AddSingleton<TProcessSettings>(serviceSettings.ProcessSettings ?? throw new ArgumentNullException(nameof(serviceSettings.ProcessSettings), $"{nameof(serviceSettings.ProcessSettings)} is null."));
				services.AddSingleton<IContainer>(container);

				services.AddHostedService<WorkerBase<TProcess, TProcessSettings>>();
			});
			return hostBuilder.Build();
		}

		private static IEnumerable<IHost> CreateHosts(IEnumerable<TServiceSettings> settingsList)
		{
			ConcurrentBag<IHost> hosts = new();
			foreach (TServiceSettings settings in settingsList)
			{
				hosts.Add(CreateHost(settings));
			}
			return hosts;
		}
	}
}