using System.Linq.Expressions;

namespace Jarvis.Common
{
	public abstract class DataCollectionBase<T, TDataCollection> : IDataCollection<T> where TDataCollection : IDataCollection<T>
	{
		#region Events

		public event EventHandler<CollectionChangedEventArgs>? CollectionChanged;

		public event EventHandler<ErrorOccuredEventArgs>? ErrorOccurred;

		protected void InvokeCollectionChanged(CollectionChangedEventArgs e)
		{
			CollectionChanged?.Invoke(this, e);
		}

		protected void InvokeErrorOccurred(string value)
		{
			ErrorOccurred?.Invoke(this, new ErrorOccuredEventArgs(value));
		}

		#endregion Events

		#region Definitions

		protected readonly TDataCollection _dataCollection;

		private bool disposedValue;

		public bool IsRealtime { get; set; } = false;

		public ItemList<T> Items => _dataCollection.Items;

		#endregion Definitions

		#region Constructor

		protected DataCollectionBase(TDataCollection collection)
		{
			_dataCollection = collection;
		}

		#endregion Constructor

		#region Methods

		public async Task<IEnumerable<T>> Get()
		{
			IEnumerable<T>? items = await _dataCollection.Get();
			return items;
		}

		public Task<IEnumerable<T>> Get(Expression<Func<T, bool>> predicate)
		{
			return _dataCollection.Get(predicate);
		}

		public async Task<long> Add(IEnumerable<T> items)
		{
			return await _dataCollection.Add(items);
		}

		public async Task<bool> Add(T item)
		{
			bool res = await _dataCollection.Add(item);
			return res;
		}

		public async Task<bool> Remove(T item)
		{
			bool res = await _dataCollection.Remove(item);
			return res;
		}

		public async Task<long> Remove(Expression<Func<T, bool>> expression)
		{
			long res = await _dataCollection.Remove(expression);
			return res;
		}

		public async Task<long> Remove(IEnumerable<T> items)
		{
			long res = await _dataCollection.Remove(items);
			return res;
		}

		public async Task<bool> Set(T item)
		{
			bool res = await _dataCollection.Set(item);
			return res;
		}

		public async Task<long> Set(IEnumerable<T> items)
		{
			long res = await _dataCollection.Set(items);

			if (res == items.Count())
			{
				return res;
			}

			return -1;
		}

		#endregion Methods

		#region Callbacks

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_dataCollection.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion Callbacks
	}
}