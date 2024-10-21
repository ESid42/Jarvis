/*Copyright (C) 2020 by Technica International
/*************************************************************************
* Programmer: Hicham Khawand (h.khawand)
* Created: 2020, 1, 29, 3:58 PM
*
* Technica CONFIDENTIAL
* _____________________
*
*    [2016] - [2020] Technica International
*  All Rights Reserved.
*
* NOTICE:  All information contained herein Is, And remains
* the property of Technica International And its suppliers,
* if any.  The intellectual And technical concepts contained
* herein are proprietary to Technica International
* And its suppliers And may be covered by Foreign Patents,
* patents in process, And are protected by trade secret Or copyright law.
* Dissemination of this information Or reproduction of this material
* Is strictly forbidden unless prior written permission Is obtained
* from Technica International.
*
*************************************************************************
*/

using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Jarvis.Database;
using Jarvis.Events;
using Jarvis.Interfaces;

namespace Jarvis.Utilities.MongoDB
{
    /// <summary>
    /// Executes mongoDb commands on a Collection stored in an mongoDb Server Database.
    /// </summary>
    /// <typeparam name="MongoDbItem"></typeparam>
    internal class CollectionCRUD<T> : IDisposable, ICollectionCRUD<T> where T : IId
    {
        #region Events

        /// <summary>
        /// Invoked on any error occurrence in the database.
        /// </summary>
        public event ErrorOccurredEventHandler? ErrorOccurred;

        /// <summary>
        /// Invokes <see cref="ErrorOccurred"/>.
        /// </summary>
        /// <param name="value">The message to send.</param>
        protected void InvokeErrorOccurred(string value)
        {
            ErrorOccurred?.Invoke(this, new EventArgs<string>(value));
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

        /// <summary>
        /// Check if current collection name.
        /// </summary>
        /// <returns></returns>
        public bool CollectionExists()
        {
            return CollectionExists(CollectionName);
        }

        /// <summary>
        /// Check if collection exists in database.
        /// </summary>
        /// <param name="name">Collection name.</param>
        /// <returns>True if collection exists, false otherwise.</returns>
        public bool CollectionExists(string name)
        {
            IEnumerable<string>? collectionListNames = GetCollectionList() ?? throw new ArgumentException("Collection names is empty");
            string? strResult = collectionListNames.FirstOrDefault(s => s == name);

            return strResult != null;
        }

        /// <summary>
        /// Insert a document or several documents to the collection
        /// </summary>
        /// <param name="item">A document that is inherited from the abstract class MongoDbItem</param>
        /// <returns><see cref="true"/> if the operation was successful, false otherwise.</returns>
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

        /// <summary>
        /// Insert a document or several documents to the collection
        /// </summary>
        /// <param name="items">A list of any type that is inherited from the abstract class MongoDbItem</param>
        /// <returns><see cref="true"/> if the operation was successful, false otherwise.</returns>
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

        /// <summary>
        /// Deletes one or more documents asynchronously
        /// </summary>
        /// <param name="item">the item to be deleted</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
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

        /// <summary>
        /// Deletes one or more documents asynchronously
        /// </summary>
        /// <param name="filters">A List of MongoQuery to filter items that needs delete</param>
        public async Task<long> Delete(IEnumerable<IQueryFilter>? filters = null)
        {
            try
            {
                if (_collection == null) { throw new ArgumentException("Collection is null."); }
                if (filters == null)
                {
                    DeleteResult resulto = await _collection.DeleteManyAsync(Builders<T>.Filter.Empty);
                    return resulto.DeletedCount;
                }
                string? queryString = MQLBuilder.BuildQuery(filters);
                DeleteResult resultMongo = await _collection.DeleteManyAsync(BsonDocument.Parse(queryString)).ConfigureAwait(false);
                return resultMongo.DeletedCount;
            }
            catch (Exception ex)
            {
                InvokeErrorOccurred(ex.ToString());
            }
            return -1;
        }

        ///// <summary>
        ///// Deletes one or more documents
        ///// </summary>
        ///// <returns>The number of deleted documents</returns>
        //public async Task<long> Delete(Predicate<T> predicate)
        //{
        //    try
        //    {
        //        Debug.WriteLine($"{GetType().Namespace}.{nameof(CollectionCRUD<T>)}: When deleting documents with a predicate use the Remove method based on Expression<Func<T, bool>> for better performance.", "Warning");
        //        IEnumerable<T> toDelete = await Retrieve(predicate).ConfigureAwait(false);
        //        long result = 0;
        //        if (toDelete.Any())
        //        {
        //            result = await Delete(toDelete).ConfigureAwait(false);
        //        }
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        InvokeErrorOccurred(ex.ToString());
        //    }
        //    return -1;
        //}

        /// <summary>
        /// Deletes one or more documents
        /// </summary>
        /// <returns>The number of deleted documents</returns>
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

        /// <summary>
        /// Deletes multiple Items from the database.
        /// </summary>
        /// <param name="items">To delete.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Gets a list of documents using a query filter asynchronously
        /// </summary>
        /// <param name="predicate">a List of MongoQuery filters</param>
        /// <returns></returns>

        /// <summary>
        /// Gets a list of documents using a query filter asynchronously
        /// </summary>
        /// <param name="filters">a List of MongoQuery filters</param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> Retrieve(IEnumerable<IQueryFilter>? filters = null)
        {
            try
            {
                if (_collection == null) { throw new ArgumentException("Collection is null."); }
                IEnumerable<T> result = Enumerable.Empty<T>();

                try
                {
                    if (filters != null)
                    {
                        string? queryString = MQLBuilder.BuildQuery(filters);

                        IAsyncCursor<T> reading = await _collection.FindAsync<T>(BsonDocument.Parse(queryString)).ConfigureAwait(false);
                        result = reading.ToEnumerable();
                    }
                    else
                    {
                        IAsyncCursor<T>? res = await _collection.FindAsync(_ => true);
                        result = res.ToList();
                    }
                }
                catch (Exception ex)
                {
                    //Debug.WriteLine(ex.Message);
                    InvokeErrorOccurred(ex.ToString());
                }

                return result;
            }
            catch (TimeoutException)
            {
            }
            catch (Exception ex)
            {
                InvokeErrorOccurred(ex.ToString());
            }

            return Enumerable.Empty<T>();
        }

        public async Task<IEnumerable<T>> Retrieve(Expression<Func<T, bool>>? expression)
        {
            IEnumerable<T> res = Enumerable.Empty<T>();

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

        /// <summary>
        /// Replace an item in the database by its modified clone asynchronously
        /// </summary>
        /// <param name="item">The modified item</param>
        /// <returns>True is the operation was successful, False otherwise.</returns>
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

        /// <summary>
        /// Updates multiple items in the database.
        /// </summary>
        /// <param name="items">To update.</param>
        /// <returns>The number of updated items.</returns>
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

        #endregion Public

        #endregion Methods

        #region IDisposable Support

        private bool _disposedValue = false;

        /// <summary>
        /// Disposes the object's resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the object's resources. As of today's version of MongoDB (v2.0.1.27 for
        /// MongoDB.Driver), there's no need to close or dispose of connections. The client handles
        /// it automatically.
        /// </summary>
        /// <param name="disposing"></param>
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