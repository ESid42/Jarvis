using Jarvis.Database;
using System.ComponentModel;

namespace Jarvis.Database.Util
{
	public interface IDbViewModelBase<TItem> : INotifyPropertyChanged where TItem : IId
	{
		string Id { get; }
		TItem Item { get; }

	}
}