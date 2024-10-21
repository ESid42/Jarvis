using System.Collections.Concurrent;

namespace Jarvis.Utils
{
	public enum CacheItemType
	{
		Add,

		Update,

		Remove,
	}

	public class CacheItem<TItem>
	{
		private TItem? _reference;

		public CacheItem(CacheItemType type, TItem reference)
		{
			reference.ThrowIfNull();
			Type = type;
			_reference = reference;
			ReferenceObject = reference.ThrowIfNull();
		}

		internal CacheItem(CacheItemType type, object referenceObject)
		{
			Type = type;
			if (referenceObject is TItem referenceObjectT)
			{
				_reference = referenceObjectT;
			}
			ReferenceObject = referenceObject;
		}

		public TItem Reference
		{
			get => _reference.ThrowIfNull();
			set
			{
				value.ThrowIfNull();
				_reference = value;
				ReferenceObject = value.ThrowIfNull();
			}
		}

		public object ReferenceObject { get; private set; }

		public CacheItemType Type { get; set; }
	}

	public abstract class MemoryCache<TKey, TItem> : ConcurrentDictionary<TKey, CacheItem<TItem>> where TKey : notnull
	{
		private readonly IDictionary<TKey, TItem> _emptyDictionary = new Dictionary<TKey, TItem>();
		private readonly object _emptyObject = new();

		public bool IsEnabled { get; set; } = true;

		public void AddOrUpdate(CacheItemType cacheType, TItem value)
		{
			if (!IsEnabled) { return; }
			AddOrUpdate(cacheType, GetKey(value), value);
		}

		public void AddOrUpdate(CacheItemType cacheType, IEnumerable<TItem> values)
		{
			if (!IsEnabled) { return; }
			foreach (TItem? item in values)
			{
				AddOrUpdate(cacheType, GetKey(item), item);
			}
		}

		public void AddOrUpdate(CacheItemType cacheType, TKey key, TItem? value = default)
		{
			if (!IsEnabled) { return; }
			CacheItem<TItem> cacheItem;
			if (value == null)
			{
				cacheItem = new CacheItem<TItem>(cacheType, _emptyObject);
			}
			else
			{
				cacheItem = new CacheItem<TItem>(cacheType, value);
			}

			AddOrUpdate(key, cacheItem, (oldkey, oldvalue) =>
			{
				if (value != null)
				{
					return MemoryCache<TKey, TItem>.UpdateItem(oldvalue, cacheType, value);
				}
				return oldvalue;
			});
		}

		public new CacheItem<TItem> AddOrUpdate(TKey key, CacheItem<TItem> addValue, Func<TKey, CacheItem<TItem>, CacheItem<TItem>> updateValueFactory)
		{
			if (!IsEnabled) { throw new InvalidOperationException("Memory cache is not enabled."); }
			return base.AddOrUpdate(key, addValue, updateValueFactory);
		}

		public IEnumerable<TItem> Get()
		{
			if (!IsEnabled) { return Enumerable.Empty<TItem>(); }
			return Get(new CacheItemType[] { CacheItemType.Add, CacheItemType.Update });
		}

		public IEnumerable<TItem> Get(params CacheItemType[] types)
		{
			if (!IsEnabled) { return Enumerable.Empty<TItem>(); }
			return this.Where(x => types.Contains(x.Value.Type)).Select(x => x.Value.Reference);
		}

		public IEnumerable<TKey> GetKeys(params CacheItemType[] types)
		{
			if (!IsEnabled) { return Enumerable.Empty<TKey>(); }
			return this.Where(x => types.Contains(x.Value.Type)).Select(x => x.Key);
		}

		public IDictionary<TKey, TItem> GetWithKeys(params CacheItemType[] types)
		{
			if (!IsEnabled) { return _emptyDictionary; }
			return this.Where(x => types.Contains(x.Value.Type)).ToDictionary(x => x.Key, x => x.Value.Reference);
		}

		public void Remove(TItem value)
		{
			Remove(GetKey(value));
		}

		public void Remove(IEnumerable<TItem> values)
		{
			foreach (TItem? item in values)
			{
				Remove(GetKey(item));
			}
		}

		public bool Remove(TKey key)
		{
			return TryRemove(key, out _);
		}

		protected abstract TKey GetKey(TItem value);

		private static CacheItem<TItem> UpdateItem(CacheItem<TItem> cacheItem, CacheItemType type, TItem value)
		{
			cacheItem.Type = type;
			cacheItem.Reference = value;
			return cacheItem;
		}
	}
}