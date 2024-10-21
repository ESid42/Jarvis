using Jarvis.Common;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using System.Diagnostics;
using System.Net;

namespace Jarvis.Database.MongoDB
{
	public class DbCollection<T> : DbCollectionBase<T> where T : IId
	{
		#region Definitions

		private MongoClient? _client;

		/// <summary>
		/// The name of the Collection to manage.
		/// </summary>
		public string? CollectionName => Info.CollectionName;

		///<inheritdoc/>
		protected override ICollectionCRUD<T>? CollectionCRUD { get; set; }

		#endregion Definitions

		#region Constructor

		/// <summary>
		/// Constructor that initializes <see cref="DbClient{T}"/> and Gets the database using
		/// database settings and the Collection name. <typeparamref name="T"/> is <see cref="Item"/>.
		/// </summary>
		/// <param name="settings">The <see cref="DatabaseInfo"/> used to initializes the object.</param>
		public DbCollection(IDbCollectionInfo settings) : base(settings)
		{
		}

		///<inheritdoc/>
		protected override void IniCollectionCRUD()
		{
			if (_client != null && Info != null && Info.DatabaseInfo != null && Info.DatabaseInfo.DatabaseName != null && Info.CollectionName != null)
			{
				CollectionCRUD = new CollectionCRUD<T>(_client, Info.DatabaseInfo.DatabaseName, Info.CollectionName);
				CollectionCRUD.ErrorOccurred += (s, e) =>
				{
					InvokeErrorOccurred(e.Message);
				};
			}
		}

		///<inheritdoc/>
		protected override void Init()
		{
			//Info = Info;
			//_client = new MongoClient(_settings.ConnectionString);
			//MongoClientSettings settings = MongoClientSettings.FromConnectionString(_settings.ConnectionString);
			//_client = new MongoClient(settings);
			if (_client == null && Info != null && Info.DatabaseInfo is DatabaseInfo info && Info.DatabaseInfo.ConnectionString != null)
			{
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS0612 // Type or member is obsolete
				MongoClientSettings mongoSettings = GetSettings(Info.DatabaseInfo.ConnectionString, null);
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore IDE0079 // Remove unnecessary suppression
				_client = new MongoClient(mongoSettings);
			}
		}

		///<inheritdoc/>
		protected override void PostInit()
		{
		}

		#endregion Constructor

		#region Methods

		///<inheritdoc/>
		public override Task<bool> Close()
		{
			//According to Documentation, there is no need to do this.
			return Task.FromResult(true);
		}

		protected override async Task<bool> CheckConnection()
		{
			if (_client != null)
			{
				try
				{
					IAsyncCursor<global::MongoDB.Bson.BsonDocument> names = await _client.ListDatabasesAsync().ConfigureAwait(false);
					return true;
				}
				catch (Exception exc)
				{
					Debug.WriteLine(exc.StackTrace);
					//throw;
				}
			}
			return false;
		}

		///<inheritdoc/>
		protected override void Disconnect()
		{
			//According to Documentation, there is no need to do this.
		}

		protected override void DisposeImpl()
		{
		}

		protected override async void Listen()
		{
			string? msgError = "OpLog error";
			if (_client != null && Info != null)
			{
				IMongoCollection<T> collection = _client.GetDatabase(Info.DatabaseInfo?.DatabaseName).GetCollection<T>(Info.CollectionName);
				try
				{
					if (collection != null)
					{
						msgError = null;

						using IChangeStreamCursor<ChangeStreamDocument<T>> cursor = await collection.WatchAsync().ConfigureAwait(false);
						await cursor.ForEachAsync(change =>
						{
							switch (change.OperationType)
							{
								case ChangeStreamOperationType.Insert:
									InvokeCollectionChanged(new CollectionChangedEventArgs(CollectionChange.ElementAdded));
									break;

								case ChangeStreamOperationType.Update:
									InvokeCollectionChanged(new CollectionChangedEventArgs(CollectionChange.ElementUpdated));
									break;

								case ChangeStreamOperationType.Replace:
									InvokeCollectionChanged(new CollectionChangedEventArgs(CollectionChange.ElementUpdated));
									break;

								case ChangeStreamOperationType.Delete:
									global::MongoDB.Bson.BsonElement element = change.DocumentKey.Elements.FirstOrDefault();
									string? id = null;
									id = element.Value.ToString();
									InvokeCollectionChanged(new CollectionChangedEventArgs(CollectionChange.ElementRemoved));
									break;

								case ChangeStreamOperationType.Invalidate:
									break;

								case ChangeStreamOperationType.Rename:
									break;

								case ChangeStreamOperationType.Drop:
									break;

								default:
									break;
							}
						}).ConfigureAwait(false);
					}
				}
				catch (Exception ex)
				{
					msgError += ".\n" + ex.Message;
					Listen();
				}
			}
		}

		#endregion Methods

		#region Creator

		[Obsolete]
		internal static MongoUrlBuilder CreateMongoUrlBuilder(string url, IEnumerable<IPAddress>? networkAddresses = null)
		{
			MongoUrlBuilder builder = new();
			ConnectionString connectionString = new(url);
			connectionString = connectionString.Resolve(true);

			builder.AllowInsecureTls = connectionString.TlsInsecure.GetValueOrDefault(false);
			builder.ApplicationName = connectionString.ApplicationName;
			builder.AuthenticationMechanism = connectionString.AuthMechanism;
			builder.AuthenticationMechanismProperties = connectionString.AuthMechanismProperties.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
			builder.AuthenticationSource = connectionString.AuthSource;
			//builder.Compressors = connectionString.Compressors;
			//            if (connectionString.ConnectionModeSwitch == ConnectionModeSwitch.UseConnectionMode)
			//            {
			//                switch (connectionString.Connect)
			//                {
			//                    case MongoDriver.Core.Clusters.ClusterConnectionMode.Direct:
			//                        builder.ConnectionMode = MongoDriver.ConnectionMode.Direct;
			//                        break;
			//                    case MongoDriver.Core.Clusters.ClusterConnectionMode.ReplicaSet:
			//                        builder.ConnectionMode = MongoDriver.ConnectionMode.ReplicaSet;
			//                        break;
			//                    case MongoDriver.Core.Clusters.ClusterConnectionMode.Sharded:
			//                        builder.ConnectionMode = MongoDriver.ConnectionMode.ShardRouter;
			//                        break;
			//                    case MongoDriver.Core.Clusters.ClusterConnectionMode.Standalone:
			//                        builder.ConnectionMode = MongoDriver.ConnectionMode.Standalone;
			//                        break;
			//                    default:
			//                        builder.ConnectionMode = MongoDriver.ConnectionMode.Automatic;
			//                        break;
			//                }
			//            }
			//            builder.ConnectionModeSwitch = connectionString.ConnectionModeSwitch;
			//            builder.ConnectTimeout = connectionString.ConnectTimeout.GetValueOrDefault(MongoDefaults.ConnectTimeout);
			//            builder.DatabaseName = connectionString.DatabaseName;
			//            if (connectionString.ConnectionModeSwitch == ConnectionModeSwitch.UseDirectConnection)
			//            {
			//                builder.DirectConnection = connectionString.DirectConnection;
			//            }
			//            builder.Fsync = connectionString.FSync;
			//            if (BsonDefaults.GuidRepresentationMode == GuidRepresentationMode.V2)
			//            {
			//                _guidRepresentation = connectionString.UuidRepresentation.GetValueOrDefault(MongoDefaults.GuidRepresentation);
			//            }
			//            else
			//            {
			//                if (connectionString.UuidRepresentation.HasValue)
			//                {
			//                    throw new InvalidOperationException("ConnectionString.UuidRepresentation can only be used when BsonDefaults.GuidRepresentationMode is V2.");
			//                }
			//            }
			builder.HeartbeatInterval = connectionString.HeartbeatInterval ?? ServerSettings.DefaultHeartbeatInterval;
			builder.HeartbeatTimeout = connectionString.HeartbeatTimeout ?? ServerSettings.DefaultHeartbeatTimeout;
			builder.IPv6 = connectionString.Ipv6.GetValueOrDefault(false);
			builder.Journal = connectionString.Journal;
			builder.MaxConnectionIdleTime = connectionString.MaxIdleTime.GetValueOrDefault(MongoDefaults.MaxConnectionIdleTime);
			builder.MaxConnectionLifeTime = connectionString.MaxLifeTime.GetValueOrDefault(MongoDefaults.MaxConnectionLifeTime);
			builder.MaxConnectionPoolSize = connectionString.MaxPoolSize.GetValueOrDefault(MongoDefaults.MaxConnectionPoolSize);
			builder.MinConnectionPoolSize = connectionString.MinPoolSize.GetValueOrDefault(MongoDefaults.MinConnectionPoolSize);
			builder.Password = connectionString.Password;
			builder.ReadConcernLevel = connectionString.ReadConcernLevel;
			if (connectionString.ReadPreference.HasValue || connectionString.ReadPreferenceTags != null || connectionString.MaxStaleness.HasValue)
			{
				if (!connectionString.ReadPreference.HasValue)
				{
					throw new MongoConfigurationException("readPreference mode is required when using tag sets or max staleness.");
				}
				builder.ReadPreference = new ReadPreference(connectionString.ReadPreference.Value, connectionString.ReadPreferenceTags, connectionString.MaxStaleness);
			}
			builder.ReplicaSetName = connectionString.ReplicaSet;
			builder.RetryReads = connectionString.RetryReads;
			builder.RetryWrites = connectionString.RetryWrites;
			builder.LocalThreshold = connectionString.LocalThreshold.GetValueOrDefault(MongoDefaults.LocalThreshold);
			//builder.Scheme = connectionString.Scheme;
			builder.Servers = connectionString.Hosts.Select(endPoint =>
			{
				if (endPoint is DnsEndPoint dnsEndPoint)
				{
					return new MongoServerAddress(dnsEndPoint.Host, dnsEndPoint.Port);
				}
				else if (endPoint is IPEndPoint ipEndPoint)
				{
					string address = ipEndPoint.Address.ToString();
					if (ipEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
					{
						address = "[" + address + "]";
					}
					return new MongoServerAddress(address, ipEndPoint.Port);
				}
				else
				{
					throw new NotSupportedException("Only DnsEndPoint and IPEndPoints are supported in the connection string.");
				}
			});
			builder.ServerSelectionTimeout = connectionString.ServerSelectionTimeout.GetValueOrDefault(MongoDefaults.ServerSelectionTimeout);
			builder.SocketTimeout = connectionString.SocketTimeout.GetValueOrDefault(MongoDefaults.SocketTimeout);
			builder.TlsDisableCertificateRevocationCheck = connectionString.TlsDisableCertificateRevocationCheck ?? false;
			builder.Username = connectionString.Username;
			builder.UseTls = connectionString.Tls.GetValueOrDefault(false);
			builder.W = connectionString.W;
			if (connectionString.WaitQueueSize != null)
			{
				builder.WaitQueueSize = connectionString.WaitQueueSize.Value;
				builder.WaitQueueMultiple = 0.0;
			}
			else if (connectionString.WaitQueueMultiple != null)
			{
				builder.WaitQueueMultiple = connectionString.WaitQueueMultiple.Value;
				builder.WaitQueueSize = 0;
			}
			builder.WaitQueueTimeout = connectionString.WaitQueueTimeout.GetValueOrDefault(MongoDefaults.WaitQueueTimeout);
			builder.WTimeout = connectionString.WTimeout;

			return builder;
		}

		[Obsolete]
		internal static MongoClientSettings GetSettings(string url, IEnumerable<IPAddress>? networkAddresses = null)
		{
			MongoUrlBuilder builder = CreateMongoUrlBuilder(url, networkAddresses);

			MongoClientSettings clientSettings = new();

			MongoCredential? credential = null;
			try
			{
				MongoIdentityEvidence evidence = builder.Password == null ? new ExternalEvidence() : new PasswordEvidence(builder.Password);
				credential = FromComponents(
						builder.AuthenticationMechanism,
						builder.AuthenticationSource,
						builder.DatabaseName,
						builder.Username,
						evidence);
			}
			catch (ArgumentException)
			{
			}

			clientSettings.AllowInsecureTls = builder.AllowInsecureTls;
			clientSettings.ApplicationName = builder.ApplicationName;
			clientSettings.AutoEncryptionOptions = null; // must be configured via code
			clientSettings.Compressors = builder.Compressors;

			clientSettings.ConnectTimeout = builder.ConnectTimeout;
			if (credential != null)
			{
				foreach (KeyValuePair<string, string> property in builder.AuthenticationMechanismProperties)
				{
					credential = property.Key.Equals("CANONICALIZE_HOST_NAME", StringComparison.OrdinalIgnoreCase)
						? credential.WithMechanismProperty(property.Key, bool.Parse(property.Value))
						: credential.WithMechanismProperty(property.Key, property.Value);
				}
				clientSettings.Credential = credential;
			}

			clientSettings.HeartbeatInterval = builder.HeartbeatInterval;
			clientSettings.HeartbeatTimeout = builder.HeartbeatTimeout;
			clientSettings.IPv6 = builder.IPv6;
			clientSettings.MaxConnectionIdleTime = builder.MaxConnectionIdleTime;
			clientSettings.MaxConnectionLifeTime = builder.MaxConnectionLifeTime;
			clientSettings.MaxConnectionPoolSize = builder.MaxConnectionPoolSize;
			clientSettings.MinConnectionPoolSize = builder.MinConnectionPoolSize;
			clientSettings.ReadConcern = new ReadConcern(builder.ReadConcernLevel);
			clientSettings.ReadEncoding = null; // ReadEncoding must be provided in code
			clientSettings.ReadPreference = builder.ReadPreference ?? ReadPreference.Primary;
			clientSettings.ReplicaSetName = builder.ReplicaSetName;
			clientSettings.RetryReads = builder.RetryReads.GetValueOrDefault(true);
			clientSettings.RetryWrites = builder.RetryWrites.GetValueOrDefault(true);
			clientSettings.LocalThreshold = builder.LocalThreshold;
			clientSettings.Scheme = builder.Scheme;
			clientSettings.Servers = new List<MongoServerAddress>(builder.Servers);
			clientSettings.ServerSelectionTimeout = builder.ServerSelectionTimeout;
			clientSettings.SocketTimeout = builder.SocketTimeout;
			clientSettings.SslSettings = null;
			if (builder.TlsDisableCertificateRevocationCheck)
			{
				clientSettings.SslSettings = new SslSettings { CheckCertificateRevocation = false };
			}
			clientSettings.UseTls = builder.UseTls;
			clientSettings.WaitQueueSize = builder.ComputedWaitQueueSize;
			clientSettings.WaitQueueTimeout = builder.WaitQueueTimeout;
			clientSettings.WriteConcern = builder.GetWriteConcern(true); // WriteConcern is enabled by default for MongoClient
			clientSettings.WriteEncoding = null; // WriteEncoding must be provided in code
			return clientSettings;
		}

		private static void EnsureNullOrExternalSource(string? mechanism, string source)
		{
			if (source is not null and not "$external")
			{
				throw new ArgumentException($"A {mechanism} source must be $external.", nameof(source));
			}
		}

		private static MongoCredential? FromComponents(string mechanism, string source, string databaseName, string username, MongoIdentityEvidence evidence)
		{
			string defaultedMechanism = (mechanism ?? "DEFAULT").Trim().ToUpperInvariant();
			switch (defaultedMechanism)
			{
				case "DEFAULT":
				case "MONGODB-CR":
				case "SCRAM-SHA-1":
				case "SCRAM-SHA-256":
					// it is allowed for a password to be an empty string, but not a username
					source = databaseName ?? "admin";
					return evidence is null or not PasswordEvidence
						? null
						: new MongoCredential(
							mechanism,
							new MongoInternalIdentity(source, username),
							evidence);

				case "MONGODB-AWS":
					// MUST be "$external". Defaults to $external.
					EnsureNullOrExternalSource(mechanism, source);
					if (username == null)
					{
						return evidence is PasswordEvidence
							? throw new ArgumentException("A MONGODB-AWS credential must have an access key id.")
							: new MongoCredential(
							mechanism,
							new MongoExternalAwsIdentity(),
							evidence);
					}
					if (evidence is null or ExternalEvidence)
					{
						throw new ArgumentException("A MONGODB-AWS credential must have a secret access key.");
					}

					return new MongoCredential(
						mechanism,
						new MongoExternalIdentity(username),
						evidence);

				case "MONGODB-X509":
					// MUST be "$external". Defaults to $external.
					EnsureNullOrExternalSource(mechanism, source);
					if (evidence is null or not ExternalEvidence)
					{
						throw new ArgumentException("A MONGODB-X509 does not support a password.");
					}

					return new MongoCredential(
						mechanism,
						new MongoX509Identity(username),
						evidence);

				case "GSSAPI":
					// MUST be "$external". Defaults to $external.
					EnsureNullOrExternalSource(mechanism, source);

					return new MongoCredential(
						mechanism,
						new MongoExternalIdentity(username),
						evidence);

				case "PLAIN":
					source = databaseName ?? "$external";
					if (evidence is null or not PasswordEvidence)
					{
						throw new ArgumentException("A PLAIN credential must have a password.");
					}

					MongoIdentity identity;
					identity = source == "$external" ? new MongoExternalIdentity(username) : new MongoInternalIdentity(source, username);

					return new MongoCredential(
						mechanism,
						identity,
						evidence);

				default:
					throw new NotSupportedException(string.Format("Unsupported MongoAuthenticationMechanism {0}.", mechanism));
			}
		}

		#endregion Creator
	}
}