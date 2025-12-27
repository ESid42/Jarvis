using MySql.Data.MySqlClient;
using Jarvis.Database;
using Jarvis.Interfaces;

namespace Jarvis.Utilities.MySQL
{
    public class DbCollection<T> : DbCollectionBase<T> where T : IId
    {
        private MySqlConnection? _connection;

        public DbCollection(IDbCollectionSettings settings, MySqlConnection? connection) : base(settings)
        {
            _connection = connection ?? new MySqlConnection(Settings.DatabaseSettings?.ConnectionString);
        }

        protected override Task<bool> CheckConnection()
        {
            if (CollectionCRUD == null)
            {
                IniCollectionCRUD();
            }
            return CollectionCRUD?.CheckConnecetion() ?? Task.FromResult(false);
        }

        protected override void IniCollectionCRUD()
        {
            if (_connection == null)
            {
                throw new ArgumentNullException(nameof(MySqlConnection));
            }
            if (Settings.Name == null)
            {
                throw new ArgumentNullException(nameof(Settings.Name));
            }
            CollectionCRUD = new CollectionCRUD<T>(_connection, Settings.Name);
        }

        protected override void Init()
        {
        }

        protected override void PostInit()
        {
        }

        public override Task<bool> Close()
        {
            return Task.FromResult(true);
        }

        protected override void Disconnect()
        {
        }

        protected override void Listen()
        {
        }

        protected override void DisposeImpl()
        {
        }

        protected override ICollectionCRUD<T>? CollectionCRUD { get; set; }
    }
}