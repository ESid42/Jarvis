using Jarvis.Common;
using Jarvis.Utils;
using Timer = System.Timers.Timer;

namespace Jarvis.Database
{
    public abstract class DbClientBase : IDbClient
    {
        #region Events

        public event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;

        public EventHandler<ErrorOccuredEventArgs>? ErrorOccured { get; set; }

        protected void InvokeConnectionChanged(bool value)
        {
            ConnectionChanged?.Invoke(this, new ConnectionChangedEventArgs(value ? ConnectionStatusType.Connected : ConnectionStatusType.Disconnected));
        }

        protected void InvokeErrorOccurred(Exception e)
        {
            ErrorOccured?.Invoke(this, new ErrorOccuredEventArgs(e));
        }

        protected void InvokeErrorOccurred(string msg)
        {
            ErrorOccured?.Invoke(this, new ErrorOccuredEventArgs(msg));
        }

        #endregion Events

        #region Definitions

        private Timer? _cdTimer;

        public IDatabaseInfo DbInfo
        {
            get => _dbInfo ?? throw new ArgumentNullException();
            protected set
            {
                if (value != null) _dbInfo = value;
            }
        }

        private IDatabaseInfo? _dbInfo;
        public bool IsConnected { get; protected set; }

        #endregion Definitions

        #region Constructor

        protected DbClientBase(IDatabaseInfo info)
        {
            DbInfo = info;
            Ini();
            PostInit();
        }

        protected abstract Task<bool> CheckConnection();

        protected abstract void Ini();

        protected abstract void PostInit();

        private async Task MonitorConnection()
        {
            bool iniRes = await CheckConnection().ConfigureAwait(false);
            //if (!iniRes)
            //{
            //    throw new TimeoutException($"Could not establish connection to the database server. " +
            //        $"Please check if the server is properly configured and reachable with the passed {nameof(IDatabaseSettings)}.");
            //}
            if (iniRes != IsConnected)
            {
                IsConnected = iniRes;
                InvokeConnectionChanged(IsConnected);
            }
            _cdTimer ??= new Timer()
            {
                Interval = 30000
            };
            _cdTimer.AutoReset = true;
            _cdTimer.Elapsed += async (s, e) =>
            {
                bool res = await CheckConnection().ConfigureAwait(false);
                if (res != IsConnected)
                {
                    IsConnected = res;
                    InvokeConnectionChanged(IsConnected);
                }
            };
            _cdTimer.Enabled = true;
        }

        #endregion Constructor

        #region Methods

        #region Public

        public abstract Task<bool> Close();

        public abstract Task<bool> CollectionExists(string name);

        public abstract Task<bool> CreateCollection<T>(string name) where T : IId;

        public abstract Task<bool> CreateDatabase(string name);

        public abstract Task<bool> DatabaseExists(string name);

        public abstract Task<bool> DeleteCollection(string name);

        public abstract Task<bool> DeleteDatabase(string name);

        public abstract IDbCollection<T> GetCollection<T>(string name) where T : IId;

        public abstract IDbCollection GetCollection(Type modelType, string name);

        public Task<bool> Start()
        {
            _ = MonitorConnection();
            return Task.FromResult(true);
        }

        public Task<bool> Stop()
        {
            if (_cdTimer != null)
            {
                _cdTimer.Enabled = false;
            }
            Disconnect();
            return Task.FromResult(true);
        }

        protected abstract void Disconnect();

        protected void ValidateConnection()
        {
            if (!IsConnected)
            {
                ThrowConnectionException();
            }
        }

        private static void ThrowConnectionException()
        {
            throw new InvalidOperationException($"The {nameof(IDbClient)} is not connected to the server. Ensure the database server can be reached and the client has been started.");
        }

        #region IDisposable Support

        protected bool _disposedValue;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _ = Close();
                    DisposeImpl();
                }
                _disposedValue = true;
            }
        }

        protected abstract void DisposeImpl();

        #endregion IDisposable Support

        #endregion Public

        #endregion Methods
    }
}