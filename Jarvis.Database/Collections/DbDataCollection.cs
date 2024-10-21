using Jarvis.Common;

namespace Jarvis.Database
{
	public abstract class DbDataCollection<TItem> : DataCollectionBase<TItem, IDbCollection<TItem>> where TItem : IId
	{
		#region Events

		public event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;

		protected void InvokeConnectionChanged(ConnectionChangedEventArgs args)
		{
			ConnectionChanged?.Invoke(this, args);
		}

		#endregion Events

		#region Definitions

		public bool IsEnableItems { get => _dataCollection.IsEnableItems; set => _dataCollection.IsEnableItems = value; }

		public bool IsConnected => _dataCollection.IsConnected;

		#endregion Definitions

		#region Constructor

		protected DbDataCollection(IDbCollection<TItem> dbCollection) : base(dbCollection)
		{
		}

		#endregion Constructor

		public async Task<bool> Close()
		{
			return await _dataCollection.Close();
		}

		public async Task<bool> Start()
		{
			return await _dataCollection.Start();
		}

		public async Task<bool> Stop()
		{
			return await _dataCollection.Stop();
		}

		public void Subscribe()
		{
			_dataCollection.Subscribe();
		}

		public async Task<TItem?> GetById(string id)
		{
			var res =  await Get(x=>x.Id.Equals(id,StringComparison.InvariantCultureIgnoreCase));
			return res.FirstOrDefault();
		}

		public Task<long> Remove(string id)
		{
			return base.Remove(x => x.Id == id);
		}
		public Task<long> Remove(IEnumerable<string> ids)
		{
			return base.Remove(x =>ids.Contains(x.Id));
		}

	}
}