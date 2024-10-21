namespace Jarvis.Utils
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> enumeration, int n)
        {
            T[] res = new T[n];
            for (int i = n; i < enumeration.Count(); i++)
            {
                res[i - n] = enumeration.ElementAt(i);
            }
            return res.AsEnumerable();
        }

        public static IEnumerable<T> Append<T>(this IEnumerable<T> items, IEnumerable<T> toAdd)
        {
            toAdd.ForEach(x => items = items.Append(x));
            return items;
        }

        public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> transactions, int size)
        {
            List<IEnumerable<T>> res = new();
            if (transactions.Count() <= size)
            {
                res.Add(transactions);
            }
            else
            {
                int count = transactions.Count() / size;
                var remaining = transactions.ToArray();
                for (int i = 0; i <= count; i++)
                {
                    if (remaining.Length > size)
                    {
                        res.Add(new List<T>(remaining.Take(size)));
                        remaining = remaining[size..];
                    }
                    else
                    {
                        res.Add(remaining);
                        return res;
                    }
                }
            }
            return res;
        }
    }
}