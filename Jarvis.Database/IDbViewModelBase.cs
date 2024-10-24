using Jarvis.Database;
using System.ComponentModel;

namespace Jarvis.Database
{
	public interface IDbViewModelBase<TItem> : INotifyPropertyChanged where TItem : IId
	{
		string Id { get; }
		TItem Item { get; }

	}
}