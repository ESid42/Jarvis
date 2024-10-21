using Jarvis.Common;
using Jarvis.Utils;
using System.Diagnostics;
using System.Linq.Expressions;
using Timer = System.Timers.Timer;

namespace Jarvis.Database
{
	public abstract class DbCollectionBase<T> : IDbCollection<T> where T : IId
	{
		#region Events

		public event EventHandler<ConnectionChangedEventArgs>? ConnectionChanged;

		public event EventHandler<ErrorOccuredEventArgs>? ErrorOccurred;

		public event EventHandler<CollectionChangedEventArgs>? CollectionChanged;

		protected void InvokeCollectionChanged(CollectionChangedEventArgs args)
		{
			CollectionChanged?.Invoke(this, args);
		}

		protected void InvokeConnectionChanged(bool value)
		{
			ConnectionChanged?.Invoke(this, new ConnectionChangedEventArgs(value ? ConnectionStatusType.Connected : ConnectionStatusType.Disconnected));
		}

		protected void InvokeErrorOccurred(string value)
		{
			ErrorOccurred?.Invoke(this, new ErrorOccuredEventArgs(value));
		}

		protected void InvokeErrorOccurred(Exception e)
		{
			ErrorOccurred?.Invoke(this, new ErrorOccuredEventArgs(e));
		}

		protected void InvokeErrorOccurred(ErrorOccuredEventArgs e)
		{
			ErrorOccurred?.Invoke(this, e);
		}

		#endregion Events

		#region Definitions

		private Timer? _cdTimer;
		public int Count => Items.Count;

		public bool IsConnected { get; protected set; }

		public bool IsEnableItems { get; set; } = true;
		public bool IsSynchronized => false;

		public ItemList<T> Items { get; protected set; } = new ItemList<T>(nameof(IId.Id));

		public IDbCollectionInfo Info { get; protected set; }

		public bool Subscribed { get; private set; }

		protected abstract ICollectionCRUD<T>? CollectionCRUD { set; get; }
		public bool IsRealtime { get; set; }

		#endregion Definitions

		#region Constructor

		protected DbCollectionBase(IDbCollectionInfo settings)
		{
			Info = settings;
			Items.CollectionChanged += ItemsChanged;
			Init();
			PostInit();
		}

		protected abstract Task<bool> CheckConnection();

		protected abstract void IniCollectionCRUD();

		protected abstract void Init();

		protected abstract void PostInit();

		private async Task MonitorConnection()
		{
			bool iniRes = await CheckConnection();
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
				bool res = await CheckConnection();
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

		public async Task<bool> Add(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item));
			}
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			bool res = false;
			try
			{
				if (CollectionCRUD == null)
				{
					throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
				}
				res = await CollectionCRUD!.Create(item).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is ArgumentException or ArgumentNullException or InvalidOperationException)
			{
				Debug.WriteLine(ex.Message);
				Debug.WriteLine(ex.StackTrace);
				InvokeErrorOccurred(ex.ToString());
			}
			if (res)
			{
				if (IsEnableItems)
				{
					Items.Edit(item);
				}
			}
			return res;
		}

		public async Task<long> Add(IEnumerable<T> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException(nameof(items));
			}
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			long res = 0;
			try
			{
				if (CollectionCRUD == null)
				{
					throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
				}
				res = await CollectionCRUD!.Create(items).ConfigureAwait(false);
			}
			catch (Exception ex) when (ex is ArgumentException or ArgumentNullException or InvalidOperationException)
			{
				Debug.WriteLine(ex.Message);
				Debug.WriteLine(ex.StackTrace);
				InvokeErrorOccurred(ex.ToString());
			}
			if (res > 0)
			{
				if (IsEnableItems)
				{
					Items.Edit(items);
				}
			}
			return res;
		}

		public abstract Task<bool> Close();

		public async Task<IEnumerable<T>> Get(Expression<Func<T, bool>> expression)
		{
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			if (CollectionCRUD == null)
			{
				throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
			}

			try
			{
				IEnumerable<T>? res = await CollectionCRUD.Retrieve(expression);

				if (IsEnableItems)
				{
					Items.Clear();
					Items.Append(res);
					return GetItemsClone();
				}
				return res;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}

			return Enumerable.Empty<T>();
		}

		public async Task<IEnumerable<T>> Get()
		{
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			if (CollectionCRUD == null)
			{
				throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
			}

			try
			{
				//IEnumerable<T>? res = await CollectionCRUD.Retrieve().ConfigureAwait(false);
				//Items.Clear();
				//Items.Append(res);
				//return GetItemsClone();
				if (IsEnableItems)
				{
					Items.Clear();
					var res = await CollectionCRUD.Retrieve();
					Items.Append(res);
					return GetItemsClone();
				}
				return await CollectionCRUD.Retrieve();
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}

			return Enumerable.Empty<T>();
		}

		public System.Collections.IEnumerator GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		public async Task<bool> Remove(T item)
		{
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			if (CollectionCRUD == null)
			{
				throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
			}

			try
			{
				bool res = await CollectionCRUD.Delete(item).ConfigureAwait(false);
				if (res)
				{
					if (IsEnableItems)
					{
						Items.Remove(item);
					}
				}
				return res;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}

			return false;
		}

		public async Task<long> Remove(IEnumerable<T> items)
		{
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			if (CollectionCRUD == null)
			{
				throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
			}

			try
			{
				long res = await CollectionCRUD.Delete(items).ConfigureAwait(false);
				if (res > 0)
				{
					if (IsEnableItems)
					{
						Items.Remove(items);
					}
				}
				return res;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}

			return -1;
		}

		public async Task<long> Remove(Expression<Func<T, bool>> predicate)
		{
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			if (CollectionCRUD == null)
			{
				throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
			}

			try
			{
				long res = await CollectionCRUD.Delete(predicate).ConfigureAwait(false);
				if (res > 0)
				{
					if (IsEnableItems)
					{
						var toDelete = Items.Where(predicate.Compile());
						Items.Remove(toDelete);
					}
				}
				return res;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}

			return -1;
		}

		public async Task<bool> Set(T item)
		{
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			bool res;

			if (IsEnableItems)
			{
				Items.Edit(item);
			}
			if (CollectionCRUD == null)
			{
				throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
			}

			try
			{
				res = await CollectionCRUD.Update(item).ConfigureAwait(false);

				//Items.Remove(item);
				//if (res)
				//{
				//    InvokeCollectionChanged(new CollectionChangedEventArgs(CollectionChangedEventArgs.CollectionChange.ElementUpdated, item, -1));
				//}
				return res;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}

			return false;
		}

		public async Task<long> Set(IEnumerable<T> items)
		{
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			if (items == null)
			{
				throw new ArgumentNullException(nameof(items));
			}
			if (CollectionCRUD == null)
			{
				throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
			}

			try
			{
				long res = await CollectionCRUD.Update(items).ConfigureAwait(false);
				if (res > 0 && res == items.Count())
				{
					if (IsEnableItems)
					{
						Items.Edit(items);
					}
				}
				return res;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}

			return -1;
		}

		public async Task<bool> Start()
		{
			bool iniRes = await CheckConnection();
			if (iniRes != IsConnected)
			{
				IsConnected = iniRes;
				InvokeConnectionChanged(IsConnected);
			}
			_ = MonitorConnection();
			return IsConnected;
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

		public void Subscribe()
		{
			if (!Subscribed)
			{
				Subscribed = true;
				Listen();
			}
		}

		public Task<IEnumerable<T>> Where(Func<T, bool> predicate)
		{
			if (CollectionCRUD == null)
			{
				IniCollectionCRUD();
			}
			if (CollectionCRUD == null)
			{
				throw new InvalidOperationException($"{nameof(CollectionCRUD)} is not initialized.");
			}
			return Task.FromResult(Items.Where(predicate));
		}

		protected abstract void Disconnect();

		protected abstract void Listen();

		private IEnumerable<T> GetItemsClone()
		{
			return Items.Data.Clone() is IEnumerable<T> data
				? data
				: throw new InvalidOperationException("Could not clone results. You should fix this.");
		}

		#endregion Methods

		#region IDisposable Support

		private bool _disposedValue;

		public void CopyTo(Array array, int index)
		{
			Array.Copy(Items.ToArray(), index, array, 0, Items.Count);
		}

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
					CollectionCRUD?.Dispose();
					DisposeImpl();
				}
				_disposedValue = true;
			}
		}

		protected abstract void DisposeImpl();

		#endregion IDisposable Support

		#region Callbacks

		private void ItemsChanged(object? sender, CollectionChangedEventArgs args)
		{
			if (!Subscribed)
			{
				InvokeCollectionChanged(args);
			}
			else
			{
				return;
			}
		}

		#endregion Callbacks
	}
}