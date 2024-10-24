using Jarvis.Common;
using Jarvis.Utils;
using MongoDB.Driver;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Jarvis.Database.MongoDB
{
	internal class CollectionCRUD<T> : IDisposable, ICollectionCRUD<T> where T : IId
	{
		#region Events

		public event EventHandler<ErrorOccuredEventArgs>? ErrorOccurred;

		private void InvokeErrorOccurred(string error)
		{
			ErrorOccurred?.Invoke(this, new(error));
		}

		#endregion Events

		#region Definitions

		private const string _duplicateIdErrorCode = "E11000";

		private readonly MongoClient? _mongoDBClient;

		private IMongoCollection<T>? _collection;

		private IMongoDatabase? _database;

		/// <summary>
		/// The Name of the Collection to edit.
		/// </summary>
		public string CollectionName { get; private set; }

		/// <summary>
		/// The Name of the Collection to edit.
		/// </summary>
		public string DatabaseName { get; private set; }

		#endregion Definitions

		#region Constructor

		/// <summary>
		/// Constructor. Initializes the <see cref="mongoDbConnection"/> and corresponding
		/// Collection name.
		/// </summary>
		/// <param name="connectionString">Connection string to establish the mongoDb Connection.</param>
		/// <param name="collectionName">The Collection name on the database.</param>
		internal CollectionCRUD(MongoClient client, string databaseName, string collectionName)
		{
			if (_duplicateIdErrorCode != null) { }

			_mongoDBClient = client;
			DatabaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
			CollectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
			Init();
		}

		private void Init()
		{
			_database = _mongoDBClient?.GetDatabase(DatabaseName);
			try
			{
				_collection = _database?.GetCollection<T>(CollectionName);
				if (_collection == null)
				{
					throw new ArgumentNullException(nameof(_collection), "Cannot get collection " + CollectionName);
				}
			}
			catch (MongoException mex)
			{
				Debug.WriteLine(mex.Message);
				InvokeErrorOccurred(mex.Message);
			}
		}

		#endregion Constructor

		#region Methods

		#region Private

		private List<string>? GetCollectionList()
		{
			if (_mongoDBClient == null) { return null; }
			List<string> res = [];
			try
			{
				List<string> list = _database?.ListCollectionNames().ToList() ?? throw new ArgumentException("Collection list is null.");
				foreach (string item in list)
				{
					res.Add(item.ToString());
				}
			}
			catch (TimeoutException ex)
			{
				if (ex != null) { }
			}
			catch (MongoException ex)
			{
				if (ex != null) { }
			}
			return res;
		}

		#endregion Private

		#region Public

		public bool CollectionExists()
		{
			return CollectionExists(CollectionName);
		}

		public bool CollectionExists(string name)
		{
			IEnumerable<string>? collectionListNames = GetCollectionList() ?? throw new ArgumentException("Collection names is empty");
			string? strResult = collectionListNames.FirstOrDefault(s => s == name);

			return strResult != null;
		}

		public async Task<bool> Create(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item), "Cannot insert null Item.");
			}

			bool result = false;
			try
			{
				if (_collection != null)
				{
					await _collection.InsertOneAsync(item).ConfigureAwait(false);
					result = true;
				}
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
				//if (ex.Message.Contains(_duplicateIdErrorCode))
				//{
				//    throw new ArgumentException("A document with the same Id already exists in the collection.", nameof(item));
				//}
				result = false;
			}
			return result;
		}

		public async Task<long> Create(IEnumerable<T> items)
		{
			if (items == null)
			{
				throw new ArgumentNullException(nameof(items), "Cannot insert null Item.");
			}

			if (!items.Any())
			{
				return 0;
			}
			long result = -1;
			try
			{
				if (_collection != null)
				{
					await _collection.InsertManyAsync(items).ConfigureAwait(false);
					result = items.Count();
				}
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
				//if (ex.Message.Contains(_duplicateIdErrorCode))
				//{
				//    throw new ArgumentException("A document with the same Id already exists in the collection.", nameof(items));
				//}
				result = -1;
			}
			return result;
		}

		public async Task<IEnumerable<T>> Retrieve(Expression<Func<T, bool>>? expression)
		{
			IEnumerable<T> res = [];

			bool isQueryError = false;

			if (_collection != null)
			{
				try
				{
					if (expression != null)
					{
						res = await (await _collection.FindAsync(expression)).ToListAsync();
					}
					else
					{
						res = await (await _collection.FindAsync(_ => true)).ToListAsync();
					}
				}
				catch (Exception ex)
				{
					InvokeErrorOccurred(ex.ToString());
					isQueryError = true;
				}
			}

			if (isQueryError && expression != null)
			{
				try
				{
					return (await (await _collection.FindAsync(_ => true)).ToListAsync()).Where(expression.Compile());
				}
				catch (Exception ex)
				{
					InvokeErrorOccurred(ex.ToString());
				}
			}

			return res;
		}

		public async Task<IEnumerable<T>> Retrieve(string sortBy, int limit = int.MaxValue, bool isAscending = true, Expression<Func<T, bool>>? expression = null)
		{
			IEnumerable<T> res = Enumerable.Empty<T>();
			expression ??= _ => true;
			bool isQueryError = false;

			if (_collection != null)
			{
				try
				{
					//var query = _collection.AsQueryable().Where(expression);
					IFindFluent<T, T> findFluent = _collection.Find(expression, new FindOptions() { AllowDiskUse = true });

					if (sortBy.IsNullOrWhiteSpace())
					{
						if (limit > 0)
						{
							res = await findFluent.Limit(limit).ToListAsync();
							//res = await query.Take(limit).ToListAsync();
						}
						else
						{
							res = await findFluent.ToListAsync();
							//res = await query.ToListAsync();
						}
					}
					else if (typeof(T).GetProperties().Where(x => x.Name.Equals(sortBy, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault() is PropertyInfo prop)
					{
						SortDefinition<T> sort;
						if (isAscending)
						{
							sort = Builders<T>.Sort.Ascending(prop.Name);
						}
						else
						{
							sort = Builders<T>.Sort.Descending(prop.Name);
						}
						findFluent = findFluent.Sort(sort);
						if (limit > 0)
						{
							findFluent = findFluent.Limit(limit);
						}
						res = await findFluent.ToListAsync();

						//ParameterExpression pe = Expression.Parameter(typeof(T), prop.Name);
						//MemberExpression me = Expression.Property(pe, prop);
						//Expression conversion = Expression.Convert(me, prop.PropertyType);
						//Expression<Func<T, long>> orderExpression = Expression.Lambda<Func<T, long>>(conversion, new[] { pe });

						//if (limit < 0)
						//{
						//    res = await query.ToListAsync();
						//}
						//else if (isAscending)
						//{
						//    res = await query.OrderBy(orderExpression).Take(limit).ToListAsync();
						//}
						//else
						//{
						//    res = await query.OrderByDescending(orderExpression).Take(limit).ToListAsync();
						//}
					}
					else if (limit > 0)
					{
						res = await findFluent.Limit(limit).ToListAsync();
						//res = await query.Take(limit).ToListAsync();
					}
					else
					{
						res = await findFluent.ToListAsync();
						//res = await query.ToListAsync();
					}
				}
				catch (Exception ex)
				{
					InvokeErrorOccurred(ex.ToString());
					isQueryError = true;
				}
			}

			if (isQueryError)
			{
				try
				{
					return (await (await _collection.FindAsync(_ => true)).ToListAsync()).Where(expression.Compile());
				}
				catch (Exception ex)
				{
					InvokeErrorOccurred(ex.ToString());
				}
			}

			return res;
		}

		public Task<IEnumerable<T>> Retrieve()
		{
			return Retrieve(null);
		}

		public async Task<bool> Update(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException(nameof(item), "Cannot update null item.");
			}
			try
			{
				ReplaceOneResult res = await _collection.ReplaceOneAsync(x => x.Id == item.Id, item).ConfigureAwait(false);
				return res.MatchedCount > 0;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}
			return false;
		}

		public async Task<long> Update(IEnumerable<T> items)
		{
			if (_collection == null) { throw new ArgumentException("Collection is null."); }
			if (items == null)
			{
				throw new ArgumentNullException(nameof(items), "Cannot update null items.");
			}
			if (!items.Any())
			{
				return 0;
			}
			try
			{
				List<WriteModel<T>> updates = [];
				foreach (T doc in items)
				{
					FilterDefinition<T> filter = Builders<T>.Filter.Eq(x => x.Id, doc.Id);
					updates.Add(new ReplaceOneModel<T>(filter, doc));
				}
				BulkWriteResult<T> res = await _collection.BulkWriteAsync(updates, new BulkWriteOptions() { IsOrdered = false });
				if (res.MatchedCount != items.Count())
				{
				}
				return res.MatchedCount;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}
			return -1;
		}

		public async Task<bool> Delete(T item)
		{
			try
			{
				if (item == null)
				{
					throw new ArgumentNullException(nameof(item), "Cannot update null item.");
				}

				DeleteResult resultMongo = await _collection.DeleteOneAsync(x => x.Id == item.Id).ConfigureAwait(false);
				return resultMongo.DeletedCount > 0;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}
			return false;
		}

		public async Task<long> Delete(Expression<Func<T, bool>> predicate)
		{
			try
			{
				if (_collection == null) { throw new ArgumentException("Collection is null."); }
				ArgumentNullException.ThrowIfNull(predicate);
				DeleteResult resultMongo = await _collection.DeleteManyAsync(predicate).ConfigureAwait(false);
				return resultMongo.DeletedCount;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}
			return -1;
		}

		public async Task<long> Delete(IEnumerable<T> items)
		{
			try
			{
				if (_collection == null) { throw new ArgumentException("Collection is null."); }
				if (items == null)
				{
					throw new ArgumentNullException(nameof(items), "Cannot update null items.");
				}
				if (!items.Any())
				{
					return 0;
				}
				List<DeleteManyModel<T>> deletes = [];
				foreach (T doc in items)
				{
					FilterDefinition<T> filter = Builders<T>.Filter.Eq(x => x.Id, doc.Id);
					deletes.Add(new DeleteManyModel<T>(filter));
				}
				BulkWriteResult<T> res = await _collection.BulkWriteAsync(deletes, new BulkWriteOptions() { IsOrdered = false });
				return res.DeletedCount;
			}
			catch (Exception ex)
			{
				InvokeErrorOccurred(ex.ToString());
			}
			return -1;
		}

		#endregion Public

		#endregion Methods

		#region IDisposable Support

		private bool _disposedValue = false;

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					//As of today's version of MongoDB (v2.0.1.27 for MongoDB.Driver), there's no need to close or dispose of connections. The client handles it automatically.
					//https://stackoverflow.com/questions/32703051/properly-shutting-down-mongodb-database-connection-from-c-sharp-2-1-driver
				}

				_disposedValue = true;
			}
		}

		#endregion IDisposable Support
	}
}