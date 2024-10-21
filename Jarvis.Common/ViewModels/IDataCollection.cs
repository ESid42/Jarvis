using System.Collections.Specialized;
using System.Linq.Expressions;

namespace Jarvis.Common
{
	public interface IDataCollection<T> : IDataCollection
	{
		public event EventHandler<CollectionChangedEventArgs>? CollectionChanged;

		ItemList<T> Items { get; }

		public Task<long> Add(IEnumerable<T> items);

		public Task<bool> Add(T item);

		public Task<IEnumerable<T>> Get();

		public Task<IEnumerable<T>> Get(Expression<Func<T, bool>> predicate);

		public Task<bool> Remove(T item);

		public Task<long> Remove(IEnumerable<T> items);

		public Task<long> Remove(Expression<Func<T, bool>> predicate);

		public Task<long> Set(IEnumerable<T> items);

		public Task<bool> Set(T item);
	}

	public interface IDataCollection : IDisposable
	{
		event EventHandler<ErrorOccuredEventArgs>? ErrorOccurred;

		bool IsRealtime { get; set; }
	}
}