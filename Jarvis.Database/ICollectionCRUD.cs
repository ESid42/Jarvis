using Jarvis.Common;
using Jarvis.Utils;
using System.Linq.Expressions;

namespace Jarvis.Database
{
	public interface ICollectionCRUD<T> : IDisposable where T : IId
	{
		event EventHandler<ErrorOccuredEventArgs>? ErrorOccurred;

		Task<bool> Create(T item);

		Task<long> Create(IEnumerable<T> items);

		Task<bool> Delete(T item);

		Task<long> Delete(IEnumerable<T> items);

		Task<long> Delete(Expression<Func<T, bool>> expression);

		Task<IEnumerable<T>> Retrieve();

		Task<IEnumerable<T>> Retrieve(Expression<Func<T, bool>>? expression);

		Task<bool> Update(T item);

		Task<long> Update(IEnumerable<T> item);
	}
}