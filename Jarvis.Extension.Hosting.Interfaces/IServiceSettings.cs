namespace Jarvis.Extension.Hosting.Interfaces
{
	public interface IServiceSettings<TProcessSettings>
	{
		EventLoggerConfiguration? EventLoggerConfiguration { get; }
		TProcessSettings? ProcessSettings { get; }
	}
}