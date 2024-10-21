namespace Jarvis.Database
{
    public interface IDbCollectionInfo
    {
        IDatabaseInfo? DatabaseInfo { get; }

        string CollectionName { get; }
    }
}