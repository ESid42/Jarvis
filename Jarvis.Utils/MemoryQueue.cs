using System.Diagnostics.CodeAnalysis;

namespace Jarvis.Utils
{
	public class MemoryQueue<TItem> : ConcurentList<TItem>
	{
		public int MaxSize { get; set; } = -1;

		public TItem? FirstItem => GetFirst();
		public bool IsEnabled { get; set; } = true;

		public new void Add(TItem item)
		{
			if (MaxSize > 0 && Count >= MaxSize)
			{
				if (TryDequeue(out _))
				{
					Enqueue(item);
				}
				else
				{
					throw new System.Exception($"Failed to dequeue");
				}
			}
			else
			{
				Enqueue(item);
			}
		}

		public void Enqueue(TItem item)
		{
			base.Add(item);
		}

		public bool TryDequeue(out TItem? result)
		{
			if (TryPeek(out result))
			{
				_items.RemoveAt(Count - 1);
				return true;
			}
			return false;
		}

		public MemoryQueue(int maxSize = 5000)
		{
			MaxSize = maxSize;
		}

		public bool TryPeek([MaybeNullWhen(false)] out TItem result)
		{
			if (_items.Any())
			{
				result = _items[Count - 1];
				return true;
			}
			result = default;
			return false;
		}

		private TItem? GetFirst()
		{
			if (TryPeek(out TItem? first))
			{
				return first;
			}
			return default;
		}
	}
}