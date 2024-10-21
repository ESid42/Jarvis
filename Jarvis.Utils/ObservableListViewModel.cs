using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;

namespace Jarvis.Utils
{
    public class ObservableListViewModel<T> : IList<T>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public T this[int index] { get => ((IList<T>)Items)[index]; set => ((IList<T>)Items)[index] = value; }

        public ObservableCollection<T> Items { get; private set; } = new();

        public int Count => Items.Count;

        public bool IsReadOnly => ((ICollection<T>)Items).IsReadOnly;

        public event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add
            {
                Items.CollectionChanged += value;
            }

            remove
            {
                Items.CollectionChanged -= value;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                ((INotifyPropertyChanged)Items).PropertyChanged += value;
            }

            remove
            {
                ((INotifyPropertyChanged)Items).PropertyChanged -= value;
            }
        }

        private readonly object _lock = new();

        public void Add(T item)
        {
            lock (_lock)
            {
                Items.Add(item);
            }
        }

        public void Add(IEnumerable<T> items)
        {
            lock (_lock)
            {
                foreach (var item in items)
                {
                    Items.Add(item);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                Items.Clear();
            }
        }

        public bool Contains(T item)
        {
            return Items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_lock)
            {
                Items.CopyTo(array, arrayIndex);
            }
        }

        public virtual void Edit(T item)
        {
            lock (_lock)
            {
                int index = Items.IndexOf(item);
                if (index > 0)
                {
                    Items[index] = item;
                }
            }
        }

        public int IndexOf(T item)
        {
            return Items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            lock (_lock)
            {
                Items.Insert(index, item);
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return Items.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (_lock)
            {
                Items.RemoveAt(index);
            }
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            return Items.Where(predicate);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}