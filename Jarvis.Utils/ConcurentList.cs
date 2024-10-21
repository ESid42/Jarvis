using System.Collections;

namespace Jarvis.Utils
{
	public class ConcurentList<T> : IList<T>
	{
		public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public int Count => throw new NotImplementedException();

		public bool IsReadOnly => throw new NotImplementedException();

		protected readonly List<T> _items = new();

		private readonly object _lock = new();

		public virtual void Add(T item)
		{
			lock (_lock)
			{
				_items.Add(item);
			}
		}

		public virtual void Clear()
		{
			lock (_lock)
			{
				_items.Clear();
			}
		}

		public virtual bool Contains(T item)
		{
			lock (_lock)
			{
				return _items.Contains(item);
			}
		}

		public virtual void CopyTo(T[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<T> GetEnumerator()
		{
			lock (_lock)
			{
				return _items.GetEnumerator();
			}
		}

		public int IndexOf(T item)
		{
			lock (_lock)
			{
				return _items.IndexOf(item);
			}
		}

		public void Insert(int index, T item)
		{
			lock (_lock)
			{
				_items.Insert(index, item);
			}
		}

		public virtual bool Remove(T item)
		{
			lock (_lock)
			{
				return _items.Remove(item);
			}
		}

		public int Remove(Func<T, bool> predicate)
		{
			var res = Where(predicate);
			lock (_lock)
			{
				res.ForEach(x => Remove(x));
			}
			return res.Count();
		}

		public void RemoveAt(int index)
		{
			lock (_lock)
			{
				_items.RemoveAt(index);
			}
		}

		public void Edit(T item)
		{
			var index = _items.IndexOf(item);
			if (index >= 0)
			{
				_items.RemoveAt(index);
				_items.Insert(index, item);
			}
			else
			{
				_items.Add(item);
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		public IEnumerable<T> Where(Func<T, bool> predicate)
		{
			lock (_lock)
			{
				return _items.Where(predicate).ToList();
			}
		}
	}
}