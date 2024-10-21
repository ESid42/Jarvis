using System.Collections;
using System.ComponentModel;

namespace Jarvis.Utils
{
	public class ConcurentListViewModel<T> : INotifyPropertyChanged
	{
		public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public ConcurentList<T> Items { get; } = new();

		public int Count => Items.Count;

		public bool IsReadOnly => Items.IsReadOnly;

		public event PropertyChangedEventHandler? PropertyChanged;

		public virtual void Add(T item)
		{
			Items.Add(item);
		}

		public void Clear()
		{
			Items.Clear();
		}

		public bool Contains(T item)
		{
			return Items.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Items.CopyTo(array, arrayIndex);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return Items.GetEnumerator();
		}

		public int IndexOf(T item)
		{
			return Items.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			Items.Insert(index, item);
		}

		public bool Remove(T item)
		{
			return Items.Remove(item);
		}

		public void RemoveAt(int index)
		{
			Items.RemoveAt(index);
		}

		public void Edit(T item)
		{
			Items.Edit(item);
		}

		public IEnumerable<T> Where(Func<T,bool> predicate)
		{
			return Items.Where(predicate);	
		}
	}
}