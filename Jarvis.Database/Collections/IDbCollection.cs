using Jarvis.COM;
using Jarvis.Common;

namespace Jarvis.Database
{
	public interface IDbCollection : IConnectable, IDisposable
	{
		bool IsEnableItems { get; set; }

		void Subscribe();
	}

	public interface IDbCollection<T> : IDataCollection<T>, IDbCollection where T : IId
	{
		Task<IEnumerable<T>> Where(Func<T, bool> predicate);

	}
}