using Raven.Client.Documents;
using Raven.Client.Documents.Operations;
using Raven.Client.Exceptions;
using Raven.Client.Exceptions.Database;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;

namespace Jarvis.Database.Raven
{
	public class DbClient : DbClientBase
	{
		#region Definitions

		private DocumentStore? _documentStore;

		#endregion Definitions

		public DbClient(DatabaseInfo info) : base(info)
		{
		}

		protected override void Ini()
		{
			_documentStore = new DocumentStore()
			{
				Urls = new[] { DbInfo.ConnectionString },
				Database = DbInfo.DatabaseName,
				//Certificate = new X509Certificate2(Path.Join(SpecialDirectories.Desktop, "free.Jarvis-demo.client.certificate.pfx"), "")
			};
			_documentStore?.Initialize();
		}

		protected override void PostInit()
		{
			//_session = _store?.OpenSession();
		}

		public override Task<bool> Close()
		{
			return Task.FromResult(false);
		}

		public override Task<bool> CollectionExists(string name)
		{
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("Collection name can't be null.");
			DetailedCollectionStatistics? collectionStats = (_documentStore?.Maintenance.Send(new GetDetailedCollectionStatisticsOperation())) ?? throw new InvalidOperationException("Couldn't get database details.");
			foreach (KeyValuePair<string, CollectionDetails> collection in collectionStats.Collections)
			{
				if (name.Equals(collection.Key, StringComparison.OrdinalIgnoreCase))
				{
					return Task.FromResult(true);
				}
			}
			return Task.FromResult(false);
		}

		public override Task<bool> CreateCollection<T>(string name)
		{
			return Task.FromResult(false);
		}

		public override Task<bool> CreateDatabase(string dbName)
		{
			if (DatabaseExists(dbName).Result)
			{
				return Task.FromResult(true);
			}
			try
			{
				_documentStore?.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(dbName)));
			}
			catch (ConcurrencyException)
			{
				return Task.FromResult(true);
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.Message);
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		public override Task<bool> DatabaseExists(string name)
		{
			if (_documentStore == null) throw new InvalidOperationException("Store is null.");
			if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
			try
			{
				_documentStore.Maintenance.ForDatabase(name).Send(new GetStatisticsOperation());
			}
			catch (DatabaseDoesNotExistException)
			{
				return Task.FromResult(false);
			}
			return Task.FromResult(true);
		}

		public override Task<bool> DeleteCollection(string name)
		{
			return Task.FromResult(false);
		}

		public override Task<bool> DeleteDatabase(string name)
		{
			DeleteDatabasesOperation.Parameters parameters = new()
			{
				DatabaseNames = new[] { name },
				HardDelete = true,
			};
			try
			{
				_ = _documentStore?.Maintenance.Server.Send(new DeleteDatabasesOperation(parameters));
				return Task.FromResult(true);
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.Message);
				return Task.FromResult(false);
			}
		}

		public override IDbCollection<T> GetCollection<T>(string name)
		{
			if (_documentStore == null) throw new InvalidOperationException("Store is null.");
			return string.IsNullOrWhiteSpace(name)
				? throw new ArgumentNullException(nameof(name))
				: new DbCollection<T>(_documentStore, new DbCollectionInfo(DbInfo, name));
		}

		public override IDbCollection GetCollection(Type modelType, string name)
		{
			Type? classType = Type.GetType(GetType().Namespace + ".DbCollection`1");
			Type[] typeParams = new Type[] { modelType };
			Type? constructedType = classType?.MakeGenericType(typeParams);
			DbCollectionInfo? settings = new(DbInfo, name);
			IDbCollection? col = null;
			if (constructedType != null)
			{
				col = (IDbCollection?)Activator.CreateInstance(constructedType, settings);
			}
			if (col is null)
			{
				throw new InvalidOperationException($"Couldn't create collection of type {nameof(modelType)}.");
			}
			return col;
		}

		protected override Task<bool> CheckConnection()
		{
			if (_documentStore == null) throw new InvalidOperationException("Client is not initialized");
			try
			{
				Task[] tasks = new Task[1] { _documentStore.Maintenance.Server.SendAsync(new GetBuildNumberOperation()) };
				bool success = Task.WaitAll(tasks, 10000);
				return Task.FromResult(success);
			}
			catch
			{
				return Task.FromResult(false);
			}
		}

		protected override void Disconnect()
		{
		}

		protected override void DisposeImpl()
		{
			_documentStore?.Dispose();
		}
	}
}