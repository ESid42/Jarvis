namespace Jarvis.Database
{
	public class DatabaseInfo : IDatabaseInfo
	{
		public string ConnectionString { get; set; } = string.Empty;
		public string DatabaseName { get; set; } = string.Empty;
		public string HostName { get; set; } = string.Empty;
		public bool IsLocal { get; set; }
		public string Password { get; set; } = string.Empty;
		public int Port { get; set; } = 0;
		public string Username { get; set; } = string.Empty;
	}

	public interface IDatabaseInfo
	{
		public string ConnectionString { get; }
		public string DatabaseName { get; }
		public string HostName { get; }
		public bool IsLocal { get; }
		public string Password { get; }
		public int Port { get; }
		public string Username { get; }
	}
}