using MySql.Data.MySqlClient;
using System.Reflection;
using System.Xml.Linq;
using Jarvis.Database;

namespace Jarvis.Utilities.MySQL
{
    public class DbClient : DbClientBase
    {
        private DatabaseSettings? _settings;

        //public DbClient(DatabaseSettings databaseSettings) : base(databaseSettings)
        //{
        //    Ini();
        //}

        public DbClient(DatabaseInfo dbInfo) : base(dbInfo)
        {
            Ini();
        }

        private MySqlConnection? _connection;
        public string DatabaseName => DbInfo?.DatabaseName.ToLower() ?? string.Empty;
        public string ConnectionString => DbInfo?.ConnectionString ?? string.Empty;
        public override Task<bool> Close()
        {
            if (_connection != null && _connection.State != System.Data.ConnectionState.Closed)
            {
                _connection.Close();
                _connection.Dispose();
            }
            return Task.FromResult(true);
        }

        public override async Task<bool> CollectionExists(string name)
        {
            if (_connection == null) return false;
            string existQuery = $"SELECT COUNT(*) FROM information_schema.tables WHERE (table_schema = '{DatabaseName}') AND (table_name = '{name.ToLower()}')";
            var cmd = new MySqlCommand(existQuery);
            var result = await Utilities.ExecuteScalar(_connection, cmd);

            return result == 1;
        }

        public override async Task<bool> CreateCollection<T>(string name)
        {
            if (_connection != null)
            {
                bool tableRes = await CollectionExists(name);
                if (tableRes)
                {
                    return true;
                }

                PropertyInfo[] Pis = typeof(T).GetProperties();
                string createQuerry = $"Create TABLE {name.ToLower()} (";
                PropertyInfo? idProp = Pis.FirstOrDefault(x => x.Name == nameof(IId.Id));
                string idColName = nameof(IId.Id);
                string primaryKeyQuerry = $"PRIMARY KEY ({idColName})";
                if (Pis != null)
                {
                    foreach (PropertyInfo pi in Pis)
                    {
                        MySqlItemColumnAttribute? colAtt = pi.GetMySqlItemColumnAttribute();
                        if (colAtt.Name?.Equals(nameof(IId.Id)) is true)
                        {
                            createQuerry += @$" `{colAtt.Name}` {MySqlItemColumnAttribute.MySqlDataTypes.VARCHAR}(255)";
                        }
                        else if (colAtt.MySQLDataType == MySqlItemColumnAttribute.MySqlDataTypes.VARCHAR)
                        {
                            createQuerry += @$" `{colAtt.Name}` {colAtt.MySQLDataType}(255)";
                        }
                        else
                        {
                            createQuerry += @$" `{colAtt.Name}` {colAtt.MySQLDataType}";
                        }
                        createQuerry += ", ";
                    }
                    createQuerry += primaryKeyQuerry + " );";
                    MySqlCommand? cmd = new(createQuerry);
                    var res = await Utilities.ExecuteScalar(_connection, cmd);
                    return res == 1;
                }

                return false;
            }
            return false;
        }

        public override async Task<bool> CreateDatabase(string name)
        {
            try
            {
                if (_connection == null) return false;
                if (await DatabaseExists(name)) return true;
                var cmd = new MySqlCommand($"CREATE DATABASE `{name}`");
                var res = await Utilities.ExecuteNonQuery(_connection, cmd);
                return res == 1;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public override async Task<bool> DatabaseExists(string name)
        {
            try
            {
                if (_connection == null) return false;
                var cmd = new MySqlCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{name}'");
                var res = await Utilities.ExecuteNonQuery(_connection, cmd);
                return res == 1;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public override async Task<bool> DeleteCollection(string name)
        {
            if (_connection == null) return false;
            if (!await CollectionExists(name)) return true;
            var cmd = new MySqlCommand($"DROP TABLE IF EXISTS {name}", _connection);
            var res = await Utilities.ExecuteNonQuery(_connection, cmd);
            return res == 1;
        }

        public override async Task<bool> DeleteDatabase(string name)
        {
            if (_connection == null) return false;
            if (!await DatabaseExists(name)) return true;
            var cmd = new MySqlCommand($"DROP DATABASE `{name}`");
            var res = await Utilities.ExecuteNonQuery(_connection, cmd);
            return res == 1;
        }

        public override IDbCollection<T> GetCollection<T>(string name)
        {
            CreateCollection<T>(name).GetAwaiter().GetResult();
            return new DbCollection<T>(new DbCollectionSettings(Settings ?? throw new ArgumentNullException(nameof(Settings)), name), _connection);
        }

        public override IDbCollection GetCollection(Type modelType, string name)
        {
            var genericType = typeof(DbCollection<>).MakeGenericType(modelType);
            return (IDbCollection)Activator.CreateInstance(genericType, new DbCollectionSettings(Settings ?? throw new ArgumentNullException(nameof(Settings)), name), _connection)!;
        }

        protected override async Task<bool> CheckConnection()
        {
            if (_connection == null) return false;
            if (_connection.State == System.Data.ConnectionState.Open) return true;
            await _connection.OpenAsync();
            CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            while (cancellation.IsCancellationRequested == false && _connection.State != System.Data.ConnectionState.Open)
            {
                await Task.Delay(10);
            }
            return _connection.State == System.Data.ConnectionState.Open;
        }

        protected override void Disconnect() => _connection?.Close();

        protected override void DisposeImpl()
        { }

        private void Ini()
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
            {
                using MySqlConnection baseConn = new(ConnectionString.GetBaseConnectionString());
                var cmd = new MySqlCommand($"SELECT COUNT(*) FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{DatabaseName}'");
                var res = Utilities.ExecuteScalar(baseConn, cmd).GetAwaiter().GetResult();
                if (res != 1)
                {
                    var createcmd = new MySqlCommand($"CREATE DATABASE `{DatabaseName}`");
                    var ress = Utilities.ExecuteNonQuery(baseConn, createcmd).GetAwaiter().GetResult();
                }
            }
            _connection = new MySqlConnection(ConnectionString);
        }

        protected override void Init()
        {
        }

        protected override void PostInit()
        {
        }
    }
}