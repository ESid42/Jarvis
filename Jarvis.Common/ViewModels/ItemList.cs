using Jarvis.Common.Events;
using System.Collections;
using System.Reflection;

namespace Jarvis.Common
{
	public class ItemList<T> : IEnumerable<T>, ICloneable, ICollectionChanged
	{
		#region Events

		public event EventHandler<CollectionChangedEventArgs>? CollectionChanged;

		protected void InvokeCollectionChanged(CollectionChangedEventArgs args)
		{
			CollectionChanged?.Invoke(this, args);
		}

		#endregion Events

		#region Definitions

		protected List<T> Items;

		private PropertyInfo? _searchProperty;

		public int Count => Items.Count;

		public IEnumerable<T> Data => Items.AsEnumerable();

		public string SearchKey { get; protected set; }

		#endregion Definitions

		#region Constructor

		public ItemList(string searchKey, IEnumerable<T> data) : this(searchKey)
		{
			Items = new List<T>(data);
			SearchKey = searchKey;

			Ini();
		}

		public ItemList(string searchKey)
		{
			Items = new List<T>();
			SearchKey = searchKey;
			Ini();
		}

		public ItemList()
		{
			Items = new List<T>();
			SearchKey = string.Empty;
			Ini();
		}

		private void Ini()
		{
			IniSearchProperty();
		}

		#endregion Constructor

		#region Methods

		private bool Compare(object left, object right)
		{
			if (left == null || right == null)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(SearchKey) && _searchProperty != null)
			{
				object? leftHand = _searchProperty.GetValue(left);
				object? rightHand = _searchProperty.GetValue(right);

				return leftHand != null && rightHand != null ? leftHand.Equals(rightHand) : left.Equals(right);
			}
			else
			{
				return left.Equals(right);
			}
		}

		private void IniSearchProperty()
		{
			if (!string.IsNullOrWhiteSpace(SearchKey))
			{
				PropertyInfo? pi = typeof(T).GetProperty(SearchKey);
				if (pi != null)
				{
					_searchProperty = pi;
				}
			}
		}

		public T this[int index]
		{
			get => Items[index];
			set => EditAt(index, value);
		}

		public void Append(IEnumerable<T> data)
		{
			Items.AddRange(data);
			InvokeCollectionChanged(new CollectionChangedEventArgs(CollectionChange.ElementAdded, item: data));
		}

		public void Append(T data)
		{
			Items.Add(data);
			InvokeCollectionChanged(new(CollectionChange.ElementAdded, item: data));
		}

		public void Clear()
		{
			Items.Clear();
			InvokeCollectionChanged(new(CollectionChange.ElementRemoved));
		}

		public bool Contains(IEnumerable<T> values)
		{
			foreach (T val in values)
			{
				if (!Items.Contains(val))
				{
					return false;
				}
			}
			return true;
		}

		public virtual void Edit(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item), "Cannot edit null item");
			}
			int index = GetIndex(item);
			if (index != -1)
			{
				Items[index] = item;
				InvokeCollectionChanged(new(CollectionChange.ElementUpdated, item: item));
			}
			else
			{
				Items.Add(item);
				InvokeCollectionChanged(new(CollectionChange.ElementAdded, item: item));
			}
		}

		public void Edit(IEnumerable<T> items)
		{
			if (items != null)
			{
				if (items.Any())
				{
					foreach (T? item in items)
					{
						Edit(item);
					}
				}
			}
			else
			{
				throw new ArgumentException();
			}
		}

		public virtual void EditAt(int index, T item)
		{
			Items[index] = item;
			InvokeCollectionChanged(new(CollectionChange.ElementUpdated, item: item));
		}

		public ItemList<T> Filter(Func<T, bool> func)
		{
			return new ItemList<T>(SearchKey, Items.Where(func));
		}

		public int GetIndex(T item)
		{
			return item != null
				? Items.FindIndex(x => x != null && Compare(x, item))
				: throw new ArgumentNullException(nameof(item), "Cannot find a null's index.");
		}

		public void Remove(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item), "Cannot find a null item.");
			}
			bool res = Items.Remove(item);
			if (res)
			{
				InvokeCollectionChanged(new(CollectionChange.ElementUpdated, item: item));
			}
		}

		public void Remove(int index)
		{
			if (index == -1 || index >= Items.Count)
			{
				throw new ArgumentException("Invalid index.", nameof(index));
			}
			T toRemove = Items[index];
			Items.RemoveAt(index);
			InvokeCollectionChanged(new(CollectionChange.ElementUpdated, item: toRemove));
		}

		public void Remove(IEnumerable<T> items)
		{
			if (items != null && items.Any())
			{
				foreach (T? item in items)
				{
					Remove(item);
				}
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		public object Clone()
		{
			throw new NotImplementedException();
		}

		#endregion Methods
	}
}