using Jarvis.Common;
using Jarvis.Utils;
using System.ComponentModel;

namespace Jarvis.Database.Util
{
	public abstract class DatabaseListViewModel<TItem, TDatabase, TVM> : DataCollectionBase<TItem, TDatabase>, INotifyPropertyChanged where TItem : IId where TDatabase : DbDataCollection<TItem> where TVM : IDbViewModelBase<TItem>
	{
		#region Events

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void InvokePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		#endregion Events

		#region Defintions

		public bool IsAllowRemove { get; set; }
		public bool IsAllowEdit { get; set; }

		public ConcurentListViewModel<TVM> ItemsVM { get; } = new();

		#endregion Defintions

		#region Constructor

		public DatabaseListViewModel(TDatabase collection) : base(collection)
		{
			Init();
		}

		private void Init()
		{
			_dataCollection.CollectionChanged += DataCollection_CollectionChanged;
			Ini();
		}

		protected virtual void Ini()
		{

		}

		#endregion Constructor

		#region Methods

		public async Task<TItem?> GetById(string id)
		{
			if (id == null || string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentNullException(nameof(id));
			}

			IEnumerable<TItem>? items = await _dataCollection.Get(x => x != null && ((IId)x).Id != null && ((IId)x).Id.Equals(id, StringComparison.Ordinal));
			return items.FirstOrDefault();
		}

		public async Task<bool> Add(TVM vm)
		{
			ItemsVM.Add(vm);
			return await Add(vm.Item);
		}

		public async Task<bool> Remove(TVM vm)
		{
			ItemsVM.Remove(vm);
			return await Remove(vm.Item);
		}

		public virtual async Task<bool> Update(TItem item)
		{
			if (IsAllowEdit)
			{
				return await _dataCollection.Set(item);
			}
			return false;
		}

		#endregion Methods

		#region Callbacks

		protected void DataCollection_CollectionChanged(object? sender, CollectionChangedEventArgs e)
		{
			InvokePropertyChanged(nameof(_dataCollection));
		}

		#endregion Callbacks
	}
}