/*Copyright (C) 2020 by Technica International
/*************************************************************************
* Programmer: Patrick Abi Abdallah (p.abdallah)
* Created: 2020, 11, 10, 10:59
*
* Technica CONFIDENTIAL
* _____________________
*
*    [2016] - [2020] Technica International
*  All Rights Reserved.
*
* NOTICE:  All information contained herein Is, And remains
* the property of Technica International And its suppliers,
* if any.  The intellectual And technical concepts contained
* herein are proprietary to Technica International
* And its suppliers And may be covered by Foreign Patents,
* patents in process, And are protected by trade secret Or copyright law.
* Dissemination of this information Or reproduction of this material
* Is strictly forbidden unless prior written permission Is obtained
* from Technica International.
*
*************************************************************************
*/

using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Jarvis.Database.MongoDB
{
	/// <summary>
	/// Handles data I/O from a database hosted on mongoDb Server.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class DbClient : DbClientBase
	{
		#region Events

		/// <summary>
		/// Raised when a change in data occurs on the database server.
		/// </summary>
		public event EventHandler<DbChangedEventArgs>? ServerDataChanged;

		/// <summary>
		/// Invokes the server data changed using the specified args
		/// </summary>
		/// <param name="args">The args</param>
		protected void InvokeServerDataChanged(DbChangedEventArgs args)
		{
			ServerDataChanged?.Invoke(this, args);
		}

		#endregion Events

		#region Definitions

		///<inheritdoc/>
		private MongoClient? _client;

		/// <summary>
		/// The settings
		/// </summary>
		private DatabaseInfo? _settings;

		/// <summary>
		/// The Connection string used to connect to mongoDb.
		/// </summary>
		public string? ConnectionString => _settings?.ConnectionString;

		#endregion Definitions

		#region Constructor

		/// <summary>
		/// Constructor that initializes <see cref="DbClient{T}"/> and Gets the database using
		/// database settings and the Collection name. <typeparamref name="T"/> is <see cref="Item"/>.
		/// </summary>
		/// <param name="info">The <see cref="IDatabaseInfo"/> used to initializes the object.</param>
		public DbClient(IDatabaseInfo info) : base(info)
		{
		}

		///<inheritdoc/>
		protected override void Init()
		{
			if (DbInfo != null)
			{
				if (DbInfo.ConnectionString.IsNullOrEmpty())
				{
					_settings = new(new DatabaseUser(DbInfo.Username, DbInfo.Password), DbInfo.HostName, DbInfo.DatabaseName, DbInfo.IsLocal);
					Settings = _settings;
				}
				else
				{
					_settings = new(DbInfo.ConnectionString, DbInfo.DatabaseName, DbInfo.IsLocal);
					Settings = _settings;
				}
			}
			else if (Settings != null)
			{
				if (Settings.ConnectionString == null)
				{
					_settings = new(Settings.User, Settings.HostName, Settings.DatabaseName, networkAdresses: null);
				}
				else
				{
					_settings = new(Settings.ConnectionString, Settings.DatabaseName, null);
				}
			}

			string? connectionString = _settings?.ConnectionString;
			if (connectionString == null || string.IsNullOrEmpty(connectionString))
			{
				throw new ArgumentException("Connection string is empty.");
			}

			try
			{
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS0612 // Type or member is obsolete
				MongoClientSettings? mongoSettings = GetSettings(connectionString, _settings?.NetworkAddresses);
#pragma warning restore CS0612 // Type or member is obsolete
#pragma warning restore IDE0079 // Remove unnecessary suppression
				_client = new MongoClient(mongoSettings);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Cannot connect to Mongo.");
			}
		}

		///<inheritdoc/>
		protected override void PostInit()
		{
		}

		#endregion Constructor

		#region Methods

		#region Private

		///<inheritdoc/>
		public override Task<bool> Close()
		{
			//According to Documentation, there is no need to do this.
			return Task.FromResult(true);
		}

		///<inheritdoc/>
		protected override void Disconnect()
		{
			//According to Documentation, there is no need to do this.
		}

		///<inheritdoc/>

		#endregion Private

		#region Overrides and Public

		///<inheritdoc/>
		public override async Task<bool> CollectionExists(string name)
		{
			ValidateConnection();
			try
			{
				if (_client == null) { throw new InvalidOperationException("Client is null."); }
				if (_settings == null) { throw new InvalidOperationException("Settings is null."); }

				IAsyncCursor<string>? enumo = await _client.GetDatabase(_settings.DatabaseName).ListCollectionNamesAsync().ConfigureAwait(false);
				while (enumo.MoveNext())
				{
					return enumo.Current.Contains(name) || enumo.Current.Contains(name);
				}
			}
			catch (TimeoutException ex)
			{
				InvokeErrorOccurred(ex.Message);
			}
			catch (MongoException ex)
			{
				InvokeErrorOccurred(ex.Message);
			}
			return false;
		}

		///<inheritdoc/>
		public override async Task<bool> CreateCollection<T>(string name)
		{
			ValidateConnection();
			bool exists = await CollectionExists(name).ConfigureAwait(false);
			if (exists)
			{
				return true;
			}
			bool res = false;
			try
			{
				if (_client == null) { throw new InvalidOperationException("Client is null."); }
				if (_settings == null) { throw new InvalidOperationException("Settings is null."); }

				await _client.GetDatabase(_settings.DatabaseName).CreateCollectionAsync(name).ConfigureAwait(false);
				res = true;
			}
			catch (TimeoutException ex)
			{
				InvokeErrorOccurred(ex.Message);
			}
			catch (MongoException ex)
			{
				InvokeErrorOccurred(ex.Message);
				res = false;
			}
			return res;
		}

		///<inheritdoc/>
		public override async Task<bool> CreateDatabase(string name)
		{
			ValidateConnection();
			bool exists = await DatabaseExists(name).ConfigureAwait(false);
			if (exists)
			{
				return true;
			}
			bool res = false;
			try
			{
				if (_client == null) { throw new InvalidOperationException("Client is null."); }
				if (_settings == null) { throw new InvalidOperationException("Settings is null."); }

				res = await Task.Run(() => _client.GetDatabase(_settings.DatabaseName) != null).ConfigureAwait(false);
			}
			catch (TimeoutException ex)
			{
				InvokeErrorOccurred(ex.Message);
			}
			catch (MongoException ex)
			{
				InvokeErrorOccurred(ex.Message);
				res = false;
			}
			return res;
			//throw new NotImplementedException("Not needed, an Insert operation with create the database.");
		}

		///<inheritdoc/>
		public override async Task<bool> DatabaseExists(string name)
		{
			ValidateConnection();
			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentNullException(nameof(name));
			}

			if (_client == null) { throw new InvalidOperationException("Client is null."); }
			if (_settings == null) { throw new InvalidOperationException("Settings is null."); }
			try
			{
				IAsyncCursor<string>? dbName = await _client.ListDatabaseNamesAsync().ConfigureAwait(false);
				while (dbName.MoveNext())
				{
					if (dbName.Current.Contains(name))
					{
						return true;
					}
				}
			}
			catch (TimeoutException ex)
			{
				InvokeErrorOccurred(ex.Message);
			}
			catch (MongoException ex)
			{
				InvokeErrorOccurred(ex.Message);
			}
			return false;
		}

		///<inheritdoc/>
		public override async Task<bool> DeleteCollection(string name)
		{
			ValidateConnection();
			bool res = false;
			try
			{
				if (_client == null) { throw new InvalidOperationException("Client is null."); }
				if (_settings == null) { throw new InvalidOperationException("Settings is null."); }

				await _client.GetDatabase(_settings.DatabaseName).DropCollectionAsync(name).ConfigureAwait(false);
				res = true;
			}
			catch (TimeoutException ex)
			{
				InvokeErrorOccurred(ex.Message);
			}
			catch (MongoException ex)
			{
				InvokeErrorOccurred(ex.Message);
				res = false;
			}
			return res;
		}

		///<inheritdoc/>
		public override async Task<bool> DeleteDatabase(string name)
		{
			ValidateConnection();
			bool res = false;
			try
			{
				if (_client == null) { throw new InvalidOperationException("Client is null."); }
				if (_settings == null) { throw new InvalidOperationException("Settings is null."); }

				await _client.DropDatabaseAsync(name).ConfigureAwait(false);
				res = true;
			}
			catch (TimeoutException ex)
			{
				InvokeErrorOccurred(ex.Message);
			}
			catch (MongoException ex)
			{
				InvokeErrorOccurred(ex.Message);
				res = false;
			}
			return res;
		}

		/// <summary>
		/// Gets the collection using the specified name
		/// </summary>
		/// <typeparam name="T">The</typeparam>
		/// <param name="name">The name</param>
		/// <exception cref="ArgumentNullException"></exception>
		/// <returns>A db collection of t</returns>
		public override IDbCollection<T> GetCollection<T>(string name)
		{
			//ValidateConnection();
			return string.IsNullOrWhiteSpace(name)
				? throw new ArgumentNullException(nameof(name))
				: new DbCollection<T>(new DbCollectionSettings() { DatabaseSettings = Settings, Name = name });
		}

		/// <summary>
		/// Gets the collection using the specified model type
		/// </summary>
		/// <param name="modelType">The model type</param>
		/// <param name="name">The name</param>
		/// <returns>The col</returns>
		public override IDbCollection GetCollection(Type modelType, string name)
		{
			//ValidateConnection();
			Type? classType = Type.GetType(GetType().Namespace + ".DbCollection`1");
			Type[] typeParams = [modelType];
			Type? constructedType = classType?.MakeGenericType(typeParams);
			DbCollectionSettings? settings = new() { DatabaseSettings = Settings, Name = name };
			if (constructedType != null)
			{
				IDbCollection? col = (IDbCollection?)Activator.CreateInstance(constructedType, settings);
				if (col != null)
				{
					return col;
				}
			}
			throw new ArgumentException("Collection");
		}

		/// <summary>
		/// Checks the connection
		/// </summary>
		/// <returns>A task containing the bool</returns>
		protected override async Task<bool> CheckConnection()
		{
			if (_client != null)
			{
				try
				{
					IAsyncCursor<global::MongoDB.Bson.BsonDocument>? names = await _client.ListDatabasesAsync().ConfigureAwait(false);
					return true;
				}
				catch (Exception exc) /*when (exc is MongoException || exc is SocketException || exc is TimeoutException)*/
				{
					Debug.WriteLine(exc.StackTrace);
					InvokeErrorOccurred(exc.ToString());
				}
			}
			return false;
		}

		#endregion Overrides and Public

		#region IDisposable Support

		protected override void DisposeImpl()
		{
			ServerDataChanged = null;
		}

		#endregion IDisposable Support

		#endregion Methods

		#region Creator

		/// <summary>
		/// Creates the mongo url builder using the specified url
		/// </summary>
		/// <param name="url">The url</param>
		/// <param name="networkAddresses">The network addresses</param>
		/// <exception cref="NotSupportedException">
		/// Only DnsEndPoint and IPEndPoints are supported in the connection string.
		/// </exception>
		/// <exception cref="MongoConfigurationException">
		/// readPreference mode is required when using tag sets or max staleness.
		/// </exception>
		/// <returns>The builder</returns>
		[Obsolete]
		internal static MongoUrlBuilder CreateMongoUrlBuilder(string url, IEnumerable<IPAddress>? networkAddresses = null)
		{
			MongoUrlBuilder? builder = new();
			ConnectionString? connectionString = new(url, networkAddresses);
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
					string? address = ipEndPoint.Address.ToString();
					if (ipEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
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

		/// <summary>
		/// Gets the settings using the specified url
		/// </summary>
		/// <param name="url">The url</param>
		/// <param name="networkAddresses">The network addresses</param>
		/// <returns>The client settings</returns>
		[Obsolete]
		internal static MongoClientSettings GetSettings(string url, IEnumerable<IPAddress>? networkAddresses = null)
		{
			try
			{
				MongoUrlBuilder? builder = CreateMongoUrlBuilder(url, networkAddresses);

				MongoClientSettings? clientSettings = new();

				MongoIdentityEvidence? evidence = builder.Password == null ? new ExternalEvidence() : new PasswordEvidence(builder.Password);
				MongoCredential? credential = null;
				credential = FromComponents(
					builder.AuthenticationMechanism,
					builder.AuthenticationSource,
					builder.DatabaseName,
					builder.Username,
					evidence);

				clientSettings.AllowInsecureTls = builder.AllowInsecureTls;
				clientSettings.ServerApi = new ServerApi(ServerApiVersion.V1);
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
			catch (ArgumentException ex)
			{
				throw;
			}
		}

		/// <summary>
		/// Ensures the null or external source using the specified mechanism
		/// </summary>
		/// <param name="mechanism">The mechanism</param>
		/// <param name="source">The source</param>
		/// <exception cref="ArgumentException">A {mechanism} source must be $external.</exception>
		private static void EnsureNullOrExternalSource(string? mechanism, string source)
		{
			if (source is not null and not "$external")
			{
				throw new ArgumentException($"A {mechanism} source must be $external.", nameof(source));
			}
		}

		/// <summary>
		/// Creates the components using the specified mechanism
		/// </summary>
		/// <param name="mechanism">The mechanism</param>
		/// <param name="source">The source</param>
		/// <param name="databaseName">The database name</param>
		/// <param name="username">The username</param>
		/// <param name="evidence">The evidence</param>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="ArgumentException">
		/// A MONGODB-AWS credential must have a secret access key.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// A MONGODB-AWS credential must have an access key id.
		/// </exception>
		/// <exception cref="ArgumentException">A MONGODB-X509 does not support a password.</exception>
		/// <exception cref="ArgumentException">A PLAIN credential must have a password.</exception>
		/// <returns>The mongo credential</returns>
		private static MongoCredential? FromComponents(string mechanism, string source, string databaseName, string username, MongoIdentityEvidence evidence)
		{
			string? defaultedMechanism = (mechanism ?? "DEFAULT").Trim().ToUpperInvariant();
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

	#region Classes

	/// <summary>
	/// Events args for a database changed event.
	/// </summary>
	/// <remarks>The Event Arguments passed if a change event is invoked</remarks>
	/// <param name="collectionName">
	/// The name of the collection where the item has changed, inserted or deleted
	/// </param>
	/// <param name="item">
	/// The item that is changed as BsonDocument, you can deserialize it to the recommended type.
	/// you can check its status by "changeItem.OperationType"
	/// </param>
	public class DbChangedEventArgs(string collectionName, object item) : EventArgs
	{
		#region Definitions

		/// <summary>
		/// The name of the collection where the item has changed, inserted or deleted
		/// </summary>
		public string CollectionName { get; set; } = collectionName;

		/// <summary>
		/// The item that is changed as BsonDocument, you can deserialize it to the recommended
		/// type. you can check its status by "changeItem.OperationType"
		/// </summary>
		public object Item { get; set; } = item;

		#endregion Definitions
	}

	#endregion Classes
}