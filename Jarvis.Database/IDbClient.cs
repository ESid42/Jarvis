using Jarvis.COM;
using Jarvis.Common;
using Jarvis.Utils;

namespace Jarvis.Database
{
    public interface IDbClient : IConnectable, IDisposable
    {
        public EventHandler<ErrorOccuredEventArgs>? ErrorOccured { get; set; }
        IDatabaseInfo DbInfo { get; }

        Task<bool> CollectionExists(string name);

        Task<bool> CreateCollection<T>(string name) where T : IId;

        Task<bool> CreateDatabase(string name);

        Task<bool> DatabaseExists(string name);

        Task<bool> DeleteCollection(string name);

        Task<bool> DeleteDatabase(string name);

        IDbCollection<T> GetCollection<T>(string name) where T : IId;

        IDbCollection GetCollection(Type modelType, string name);
    }
}