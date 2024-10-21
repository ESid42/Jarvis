namespace Jarvis.Extension.Hosting.Interfaces
{
	public interface IEventLoggerConfiguration
	{
		string LogName { get; set; }
		string Source { get; set; }
	}
}