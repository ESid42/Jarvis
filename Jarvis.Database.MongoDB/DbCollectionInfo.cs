namespace Jarvis.Database.MongoDB
{
	public class DbCollectionInfo : IDbCollectionInfo
	{
		public IDatabaseInfo DatabaseInfo
		{ get => _databaseInfo ?? throw new ArgumentNullException(); set { if (value != null) _databaseInfo = value; } }

		private IDatabaseInfo? _databaseInfo;

		public string CollectionName { get; set; } = string.Empty;

		public DbCollectionInfo(IDatabaseInfo databaseInfo, string collectionName)
		{
			DatabaseInfo = databaseInfo;
			CollectionName = collectionName;
		}
	}
}