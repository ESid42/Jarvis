using Jarvis.Common;
using Jarvis.Utils;
using Nito.AsyncEx;
using Raven.Client.Documents;
using Raven.Client.Documents.BulkInsert;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using System.Linq.Expressions;
using System.Reflection;

namespace Jarvis.Database.Raven
{
	internal class CollectionCRUD<TItem> : ICollectionCRUD<TItem> where TItem : IId
	{
		private readonly Dictionary<string, Expression<Func<TItem, object?>>> _allProperties = new();

		private readonly IDocumentStore _documentStore;

		public CollectionCRUD(string collectionName, IDocumentStore store)
		{
			CollectionName = collectionName;
			_documentStore = store;

			foreach (System.Reflection.PropertyInfo prop in typeof(TItem).GetProperties())
			{
				Expression<Func<TItem, object?>> expression = prop.Name.ToExpression<TItem>();
				_allProperties.Add(prop.Name, expression);
			}
		}

		public event EventHandler<ErrorOccuredEventArgs>? ErrorOccurred;

		public string CollectionName { get; set; }

		public async Task<bool> Create(TItem item)
		{
			if (item is null)
			{
				throw new ArgumentNullException(nameof(item), "Cannot insert null Item.");
			}
			using IAsyncDocumentSession session = _documentStore.OpenAsyncSession();
			try
			{
				await session.StoreAsync(item, item.Id);
				await session.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				InvokeErrorOccured(ex.Message);
				return false;
			}
			return true;
		}

		public async Task<long> Create(IEnumerable<TItem> items)
		{
			if (!items.Any())
			{
				return 0;
			}
			BulkInsertOperation? bulkInsert = null;
			List<Task> tasks = new();

			using IAsyncDocumentSession session = _documentStore.OpenAsyncSession();
			try
			{
				//bulkInsert = _documentStore.BulkInsert();
				foreach (TItem item in items)
				{
					tasks.Add(session.StoreAsync(item, item.Id));
					//await bulkInsert.StoreAsync(item, item.Id);
				}
				await tasks.WhenAll();
				await session.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				InvokeErrorOccured(ex.Message);
				return -1;
			}
			finally
			{
				if (bulkInsert != null)
				{
					await bulkInsert.DisposeAsync().ConfigureAwait(false);
				}
			}
			return items.Count();
		}

		public async Task<bool> Delete(TItem item)
		{
			if (item is null)
			{
				throw new ArgumentNullException(nameof(item), "Cannot insert null Item.");
			}
			using IAsyncDocumentSession session = _documentStore.OpenAsyncSession();
			try
			{
				session.Delete(item.Id);
				await session.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				InvokeErrorOccured(ex.Message);
				return false;
			}
			return true;
		}

		public async Task<long> Delete(IEnumerable<TItem> items)
		{
			if (!items.Any())
			{
				return 0;
			}
			using IAsyncDocumentSession session = _documentStore.OpenAsyncSession();
			foreach (TItem item in items)
			{
				session.Delete(item.Id);
			}
			try
			{
				await session.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				InvokeErrorOccured(ex.Message);
				return -1;
			}
			return items.Count();
		}

		public async Task<long> Delete(Expression<Func<TItem, bool>> expression)
		{
			IEnumerable<TItem> toDelete = Retrieve(expression).GetAwaiter().GetResult();

			return await Delete(toDelete);
		}

		public void Dispose()
		{
			ErrorOccurred = null;
		}

		public async Task<IEnumerable<TItem>> Retrieve(Expression<Func<TItem, bool>>? predicate)
		{
			//using IDocumentSession session = _store.OpenSession();
			//try
			//{
			//    Expression<Func<TItem, bool>> expression = (x) => predicate(x);
			//    var all = session.Query<TItem>();
			//    return all.Where(predicate)?.ToList() ?? Enumerable.Empty<TItem>();
			//}
			using IAsyncDocumentSession session = _documentStore.OpenAsyncSession();
			try
			{
				var res = await Retrieve();
				if (predicate is null)
				{
					return res;
				}
				//return await session
				//.Advanced.AsyncDocumentQuery<TItem>() // Use DocumentQuery
				//.WhereRegex(x => predicate(x),"").ToListAsync(); // Query for all 'Employee' documents that match this predicate
				return res.Where(predicate.Compile());
			}
			catch (Exception ex)
			{
				InvokeErrorOccured(ex.Message);
				return [];
			}
		}

		public async Task<bool> Update(TItem item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item), "Cannot update null item.");
			}
			try
			{
				using IAsyncDocumentSession session = _documentStore.OpenAsyncSession();
				TItem toUpdate = await session.LoadAsync<TItem>(item.Id);
				foreach (var prop in typeof(TItem).GetProperties())
				{
					object? val = typeof(TItem).GetProperty(prop.Name)?.GetValue(item);
					toUpdate.TrySetValue(prop, val);
				}
				//foreach (KeyValuePair<string, Expression<Func<TItem, object?>>> prop in _allProperties)
				//{
				//	object? val = typeof(TItem).GetProperty(prop.Key)?.GetValue(item);
				//	session.Advanced.Patch(item.Id, prop.Value, val);
				//}

				await session.SaveChangesAsync();

				return true;
			}
			catch (Exception ex)
			{
				InvokeErrorOccured(ex.ToString());
			}
			return false;
		}

		public async Task<long> Update(IEnumerable<TItem> items)
		{
			if (items.Any() == false)
			{
				return 0;
			}
			try
			{
				using IAsyncDocumentSession session = _documentStore.OpenAsyncSession();
				foreach (TItem item in items)
				{
					TItem toUpdate = await session.LoadAsync<TItem>(item.Id);
					foreach (var prop in typeof(TItem).GetProperties())
					{
						object? val = typeof(TItem).GetProperty(prop.Name)?.GetValue(item);
						toUpdate.TrySetValue(prop, val);
					}
					//foreach (KeyValuePair<string, Expression<Func<TItem, object?>>> prop in _allProperties)
					//{
					//	session.Advanced.Patch(item.Id, prop.Value, typeof(TItem).GetProperty(prop.Key)?.GetValue(item));
					//}
				}

				await session.SaveChangesAsync();

				return items.Count();
			}
			catch (Exception ex)
			{
				InvokeErrorOccured(ex.ToString());
			}
			return -1;
		}

		private void InvokeErrorOccured(string error)
		{
			ErrorOccurred?.Invoke(this, new(error));
		}

		public async Task<IEnumerable<TItem>> Retrieve()
		{
			using IAsyncDocumentSession session = _documentStore.OpenAsyncSession();
			try
			{
				var res = await session.Query<TItem>().ToListAsync();
				return res;
			}
			catch (Exception ex)
			{
				InvokeErrorOccured(ex.Message);
				return Enumerable.Empty<TItem>();
			}
		}
	}
}