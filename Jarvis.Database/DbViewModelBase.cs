using Jarvis.Utils;
using System.ComponentModel;
using System.Reflection;

namespace Jarvis.Database
{
    public class DbViewModelBase<TItem> : INotifyPropertyChanged, IDbViewModelBase<TItem> where TItem : IId
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void InvokePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TItem Item { get; protected set; }
        public string Id => Item.Id;

        public DbViewModelBase(TItem item)
        {
            Item = item;
        }

        public void UpdateItem(TItem item)
        {
            foreach (var prop in item.GetType().GetProperties())
            {
                if(prop.Name.Equals(nameof(IId.Id)))
                {
                    continue;
                }
                Item.TrySetValue(prop, prop.GetValue(item));
            }
        }

        public bool UpdateItem(string propertyName, object value)
        {
            if (Item.GetType().GetProperties().FirstOrDefault(p => p.Name.Equals(propertyName)) is PropertyInfo prop)
            {
                Item.TrySetValue(prop, value);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}