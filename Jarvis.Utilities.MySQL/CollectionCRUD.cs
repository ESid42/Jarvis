using Jarvis.Common;
using Jarvis.Database;
using Jarvis.Utils;
using MySql.Data.MySqlClient;
using System.Data;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Jarvis.Utilities.MySQL
{
    internal partial class CollectionCRUD<T> : ICollectionCRUD<T> where T : IId
    {
        #region Events

        public event EventHandler<ErrorOccuredEventArgs>? ErrorOccurred;

        protected void InvokeErrorOccurred(string value) => ErrorOccurred?.Invoke(this, new ErrorOccuredEventArgs(value));

        #endregion Events

        #region Definitions

        private readonly Dictionary<string, string> _propertyDataTypes = [];
        private readonly Dictionary<string, string> _propertyMap = [];
        private readonly MySqlConnection _connection;
        private readonly List<string> _columns = [];
        public int ParametersLimit { get; set; } = 65535;
        public string TableName { get; private set; }

        #endregion Definitions

        #region Constructors

        public CollectionCRUD(MySqlConnection connection, string tableName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            TableName = string.IsNullOrWhiteSpace(tableName) ? throw new ArgumentNullException(nameof(tableName)) : tableName.ToLower();
            Init();
        }

        private void Init() => MapColumnsToProperties();

        #endregion Constructors

        #region Methods

        #region Private

        //private bool AppendFilters(IEnumerable<IQueryFilter> filters, MySqlCommand cmd)
        //{
        //    if (!filters.Any())
        //    {
        //        return false;
        //    }

        //    cmd.CommandText += " WHERE ";
        //    string condition;
        //    bool firstElement = true;
        //    foreach (IQueryFilter? filter in filters)
        //    {
        //        if (!firstElement)
        //        {
        //            condition = filter.LogicOperator switch
        //            {
        //                LogicalOperator.And => "AND",
        //                LogicalOperator.Or => "OR",
        //                LogicalOperator.XOR => throw new NotImplementedException("XOR is yet to be implemented."),
        //                LogicalOperator.NAND => throw new NotImplementedException("NAND is yet to be implemented."),
        //                _ => throw new InvalidOperationException("Illegal Logic Operator passed.")
        //            };
        //            cmd.CommandText += " " + condition + " ";
        //        }
        //        else
        //        { firstElement = false; }

        //        if (_propertyMap.ContainsKey(filter.ItemPropertyFilter.PropertyName))
        //        {
        //            string convertTo = _propertyDataTypes[filter.ItemPropertyFilter.PropertyName];
        //            string pName = filter.ItemPropertyFilter.PropertyName;
        //            switch (filter.ComparisonOperator)
        //            {
        //                case ComparisonOperator.Equals:
        //                    if (filter.ItemPropertyFilter.Value == null || string.IsNullOrEmpty(filter.ItemPropertyFilter.Value.ToString()))
        //                    {
        //                        return false;
        //                    }

        //                    MySQLCmdBuilder.IsEqual(_propertyMap[pName], filter.ItemPropertyFilter.Value.ToString().ThrowIfNull(), cmd, convertTo);
        //                    break;

        //                case ComparisonOperator.NotEqual:
        //                    if (filter.ItemPropertyFilter.Value == null || string.IsNullOrEmpty(filter.ItemPropertyFilter.Value.ToString()))
        //                    {
        //                        return false;
        //                    }

        //                    MySQLCmdBuilder.IsNotEqual(_propertyMap[pName], filter.ItemPropertyFilter.Value.ToString().ThrowIfNull(), cmd, convertTo);
        //                    break;

        //                case ComparisonOperator.GreaterThan:
        //                    if (filter.ItemPropertyFilter.MinValue == null || string.IsNullOrEmpty(filter.ItemPropertyFilter.MinValue.ToString()))
        //                    {
        //                        return false;
        //                    }

        //                    MySQLCmdBuilder.GreaterThan(_propertyMap[pName], filter.ItemPropertyFilter.MinValue.ToString().ThrowIfNull(), cmd, convertTo);
        //                    break;

        //                case ComparisonOperator.LessThan:
        //                    if (filter.ItemPropertyFilter.MaxValue == null || string.IsNullOrEmpty(filter.ItemPropertyFilter.MaxValue.ToString()))
        //                    {
        //                        return false;
        //                    }

        //                    MySQLCmdBuilder.LessThan(_propertyMap[pName], filter.ItemPropertyFilter.MaxValue.ToString().ThrowIfNull(), cmd, convertTo);
        //                    break;

        //                case ComparisonOperator.BetweenExclusive:
        //                    if (filter.ItemPropertyFilter.MaxValue == null || string.IsNullOrEmpty(filter.ItemPropertyFilter.MaxValue.ToString()))
        //                    {
        //                        return false;
        //                    }

        //                    if (filter.ItemPropertyFilter.MinValue == null || string.IsNullOrEmpty(filter.ItemPropertyFilter.MinValue.ToString()))
        //                    {
        //                        return false;
        //                    }

        //                    MySQLCmdBuilder.IsBetween(_propertyMap[pName], filter.ItemPropertyFilter.MinValue.ToString().ThrowIfNull(), filter.ItemPropertyFilter.MaxValue.ToString().ThrowIfNull(), cmd, convertTo);
        //                    break;

        //                case ComparisonOperator.StringContains:
        //                    if (filter.ItemPropertyFilter.Value == null || string.IsNullOrEmpty(filter.ItemPropertyFilter.Value.ToString()))
        //                    {
        //                        return false;
        //                    }

        //                    MySQLCmdBuilder.Contains(_propertyMap[pName], filter.ItemPropertyFilter.Value.ToString().ThrowIfNull(), cmd);
        //                    break;
        //            }
        //        }
        //    }
        //    return true;
        //}

        private void GetTableColumns()
        {
            string sql = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{TableName}'";
            try
            {
                using var command = new MySqlCommand(sql, _connection);
                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }
                //command.Parameters.AddWithValue("@Table", TableName);
                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    _columns.Add(reader.GetString(0));
                }
            }
            catch (Exception e)
            {
            }
        }

        private void MapColumnsToProperties()
        {
            GetTableColumns();
            foreach (PropertyInfo? property in typeof(T).GetProperties())
            {
                var colAtt = property.GetMySqlItemColumnAttribute();
                if (colAtt != null)
                {
                    string attName = property.Name;
                    foreach (string dbCol in _columns)
                    {
                        if (dbCol.Equals(colAtt.Name, StringComparison.InvariantCulture))
                        {
                            _propertyMap[attName] = dbCol;
                            _propertyDataTypes[attName] = colAtt.MySQLDataType.ToString();
                            break;
                        }
                    }
                }
            }
        }

        private async Task<long> CreateBatch(IEnumerable<T> items)
        {
            string createQuery = string.Format(CultureInfo.InvariantCulture, "INSERT INTO {0} ", TableName);
            PropertyInfo pi;
            using MySqlCommand createCommand = new();
            try
            {
                string colValues = "(";
                string values = "";

                foreach (KeyValuePair<string, string> entry in _propertyMap)
                {
                    colValues += entry.Value + ", ";
                }
                int iter = 0;
                foreach (T? item in items)
                {
                    values += "(";
                    foreach (KeyValuePair<string, string> entry in _propertyMap)
                    {
                        string paramName = entry.Key + iter.ToString();
                        values += "@" + paramName + ", ";
                        pi = typeof(T).GetProperty(entry.Key).ThrowIfNull();
                        _ = createCommand.Parameters.AddWithValue(paramName, CollectionCRUD<T>.GetPropertyValue(pi, item));
                    }
                    iter++;
                    values = values[..^2] + "), ";
                }

                colValues = colValues[..^2] + ")";
                values = values[..^2] + ";";

                createQuery += colValues + " VALUES " + values;
                createCommand.CommandText = createQuery;
                return await Utilities.ExecuteNonQuery(_connection, createCommand);
                //return items.Count();
            }
            catch (Exception ex)
            {
                if (ex != null) { }
                return -1;
            }
        }

        private async Task<long> DeleteBatch(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (!items.Any())
            {
                return 0;
            }
            try
            {
                MySqlCommand deleteCommand = new();
                string idColName = nameof(IId.Id);
                if (typeof(IId).IsAssignableFrom(typeof(T)))
                {
                    idColName = typeof(T).GetProperty(nameof(IId.Id))?.GetMySqlItemColumnAttribute().Name ?? nameof(IId.Id);
                }
                string deleteQuery = $"DELETE FROM {TableName} Where {idColName} IN ";
                int iter = 0;
                string idValues = "(";
                foreach (T item in items)
                {
                    idValues += $"'{item.Id}', ";
                    _ = deleteCommand.Parameters.AddWithValue("id" + iter.ToString(), item.Id);
                    iter++;
                }
                deleteQuery += idValues[..^2] + ");";
                deleteCommand.CommandText = deleteQuery;
                return await Utilities.ExecuteNonQuery(_connection, deleteCommand);
            }
            catch (Exception ex)
            {
                if (ex != null) { }
                return 0;
            }
        }

        private async Task<long> UpdateBatch(IEnumerable<T> item)
        {
            List<Task> tasks = [];

            ArgumentNullException.ThrowIfNull(item);
            if (!item.Any())
            {
                return 0;
            }
            try
            {
                int iter = 0;
                MySqlCommand updateCmd = new();
                string updateQueryBase = $"UPDATE {TableName} SET";
                string updateQuery = string.Empty;
                string idColName = nameof(IId.Id);
                if (typeof(IId).IsAssignableFrom(typeof(T)))
                {
                    idColName = typeof(T).GetProperty(nameof(IId.Id))?.GetMySqlItemColumnAttribute().Name ?? idColName;
                }
                PropertyInfo[]? props = typeof(T).GetProperties();
                foreach (T? elem in item)
                {
                    PropertyInfo pi;

                    string idParam = "id" + iter.ToString();
                    _ = updateCmd.Parameters.AddWithValue(idParam, elem.Id);
                    string itemQuery = "";
                    foreach (KeyValuePair<string, string> entry in _propertyMap)
                    {
                        if (!entry.Value.Equals(idColName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            string paramName = entry.Key + iter.ToString();
                            itemQuery += " " + entry.Value + " = @" + paramName + ",";
                            pi = typeof(T).GetProperty(entry.Key).ThrowIfNull();

                            _ = updateCmd.Parameters.AddWithValue(paramName, CollectionCRUD<T>.GetPropertyValue(pi, elem));
                        }
                    }
                    updateQuery += $"{updateQueryBase} {itemQuery[..^1]} Where {idColName}=@{idParam}";

                    iter++;
                    updateQuery += ";\n";
                }
                updateCmd.CommandText = updateQuery;

                return await Utilities.ExecuteNonQuery(_connection, updateCmd);
            }
            catch (Exception e)
            {
                return -1;
            }
        }

        private List<IEnumerable<T>> SplitData(IEnumerable<T> data)
        {
            int nbFields = _columns.Count;
            int batchSize = (ParametersLimit - 1) / (1 + nbFields);
            List<IEnumerable<T>> res = [];
            if (data.Count() * _propertyMap.Count < ParametersLimit)
            {
                res.Add(new List<T>(data));
            }
            else
            {
                res.Add(new List<T>());
                for (int i = 0; i < data.Count(); i++)
                {
                    if (i != 0 && i % batchSize == 0)
                    {
                        res.Add(new List<T>());
                    }
                    if (res.Last() is List<T> lista)
                    {
                        lista.Add(data.ElementAt(i));
                    }
                }
            }

            return res;
        }

        private static object GetPropertyValue(PropertyInfo pi, T item)
        {
            if (pi.PropertyType == typeof(List<string>))
            {
                if (pi.GetValue(item) is List<string> listRes)
                {
                    return string.Join(",", listRes.Select(x => x.ToString()).ToArray());
                }
            }
            return pi.GetValue(item).ThrowIfNull();
        }

        #endregion Private

        #region CRUD

        public async Task<bool> Create(T item)
        {
            string query = $"INSERT INTO {TableName} ";
            using var command = new MySqlCommand();
            string cols = "(";
            string vals = "(";
            foreach (var entry in _propertyMap)
            {
                cols += entry.Value + ", ";
                vals += "@" + entry.Key + ", ";
                command.Parameters.AddWithValue("@" + entry.Key, typeof(T).GetProperty(entry.Key)!.GetValue(item));
            }
            cols = cols[..^2] + ")";
            vals = vals[..^2] + ")";
            command.CommandText = query + cols + " VALUES " + vals;
            return await Utilities.ExecuteNonQuery(_connection, command) > 0;
        }

        public async Task<long> Create(IEnumerable<T> items)
        {
            IEnumerable<IEnumerable<T>> dataList = SplitData(items);
            Task<long>[] tasks = new Task<long>[dataList.Count()];
            long count = 0;
            for (int i = 0; i < dataList.Count(); i++)
            {
                Task<long> task = CreateBatch(dataList.ElementAt(i));
                tasks[i] = task;
            }
            _ = await Task.WhenAll(tasks);
            foreach (Task<long>? task in tasks)
            {
                count += task.Result;
            }
            return count;
        }

        //public async Task<IEnumerable<T>> Retrieve(IEnumerable<IQueryFilter>? filters = null)
        //{
        //    var list = new List<T>();
        //    string query = $"SELECT * FROM {TableName}";
        //    using var command = new MySqlCommand(query, _connection);
        //    if (filters != null && filters.Any())
        //    {
        //        command.CommandText += " WHERE " + string.Join(" AND ", filters.Select(f => f.ToSqlCondition(_propertyMap)));
        //        foreach (var filter in filters)
        //        {
        //            command.Parameters.AddWithValue(filter.ParamName(), filter.ItemPropertyFilter.Value);
        //        }
        //    }
        //    if (_connection.State != ConnectionState.Open)
        //    {
        //        await _connection.OpenAsync();
        //    }
        //    using var reader = command.ExecuteReader();
        //    while (await reader.ReadAsync())
        //    {
        //        var item = Activator.CreateInstance<T>();
        //        foreach (var prop in typeof(T).GetProperties())
        //        {
        //            if (_propertyMap.TryGetValue(prop.Name, out var colName))
        //            {
        //                var val = reader[colName];
        //                prop.SetValue(item, val == DBNull.Value ? null : val);
        //            }
        //        }
        //        list.Add(item);
        //    }
        //    _connection.Close();
        //    return list;
        //}

        public async Task<IEnumerable<T>> Retrieve(IEnumerable<string> ids)
        {
            var list = new List<T>();
            string inCluase = ids.GetIdQuery();
            string query = $"SELECT * FROM {TableName} WHERE {nameof(IId.Id)} IN ({inCluase})";
            using var command = new MySqlCommand(query, _connection);
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            using var reader = command.ExecuteReader();
            while (await reader.ReadAsync())
            {
                var item = Activator.CreateInstance<T>();
                foreach (var prop in typeof(T).GetProperties())
                {
                    if (_propertyMap.TryGetValue(prop.Name, out var colName))
                    {
                        var val = reader[colName];
                        prop.SetValue(item, val == DBNull.Value ? null : val);
                    }
                }
                list.Add(item);
            }
            _connection.Close();
            return list;
        }

        public async Task<IEnumerable<T>> Retrieve(Expression<Func<T, bool>>? expression)
        {
            var list = new List<T>();
            string query = $"SELECT * FROM {TableName}";
            if (expression != null) query = $"{query} {expression.GetSqlQuery()}";
            using var command = new MySqlCommand(query, _connection);
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            using var reader = command.ExecuteReader();
            while (await reader.ReadAsync())
            {
                var item = Activator.CreateInstance<T>();
                foreach (var prop in typeof(T).GetProperties())
                {
                    if (_propertyMap.TryGetValue(prop.Name, out var colName))
                    {
                        var val = reader[colName];
                        prop.SetValue(item, val == DBNull.Value ? null : val);
                    }
                }
                list.Add(item);
            }
            _connection.Close();
            return list;
        }

        public async Task<IEnumerable<T>> Retrieve(string sortBy, int limit = int.MaxValue, bool isAscending = true, Expression<Func<T, bool>>? expression = null)
        {
            var list = new List<T>();
            string query = $"SELECT *";
            query = $"SELECT *  FROM {TableName} {expression?.GetSqlQuery() ?? string.Empty} ORDER BY {sortBy} {(isAscending ? "ASC" : "DESC")}  {(limit < int.MaxValue ? $"LIMIT {limit} " : "")} ";
            using var command = new MySqlCommand(query, _connection);
            if (_connection.State != ConnectionState.Open)
            {
                await _connection.OpenAsync();
            }
            using var reader = command.ExecuteReader();
            while (await reader.ReadAsync())
            {
                var item = Activator.CreateInstance<T>();
                foreach (var prop in typeof(T).GetProperties())
                {
                    if (_propertyMap.TryGetValue(prop.Name, out var colName))
                    {
                        var val = reader[colName];
                        prop.SetValue(item, val == DBNull.Value ? null : val);
                    }
                }
                list.Add(item);
            }
            _connection.Close();
            return list;
        }

        public async Task<bool> Update(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            try
            {
                string updateQuery = $"UPDATE {TableName} SET";
                PropertyInfo pi;
                MySqlCommand updateCmd = new();
                foreach (KeyValuePair<string, string> entry in _propertyMap)
                {
                    if (!entry.Value.Equals(nameof(IId.Id), StringComparison.InvariantCultureIgnoreCase))
                    {
                        updateQuery += " " + entry.Value + " = @" + entry.Key + ",";
                        pi = typeof(T).GetProperty(entry.Key).ThrowIfNull();
                        _ = updateCmd.Parameters.AddWithValue(entry.Key, CollectionCRUD<T>.GetPropertyValue(pi, item));
                    }
                }

                updateQuery = updateQuery[..^1] + $" Where {nameof(IId.Id)}=@id";
                _ = updateCmd.Parameters.AddWithValue("id", item.Id);
                updateCmd.CommandText = updateQuery;

                await Utilities.ExecuteNonQuery(_connection, updateCmd);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<long> Update(IEnumerable<T> items)
        {
            IEnumerable<IEnumerable<T>> dataList = SplitData(items);
            Task<long>[] tasks = new Task<long>[dataList.Count()];
            long count = 0;
            for (int i = 0; i < dataList.Count(); i++)
            {
                Task<long> task = UpdateBatch(dataList.ElementAt(i));
                tasks[i] = task;
            }
            _ = await Task.WhenAll(tasks);
            foreach (Task<long>? task in tasks)
            {
                count += task.Result;
            }
            return count;
        }

        public async Task<bool> Delete(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            try
            {
                string deleteQuery = string.Format(CultureInfo.InvariantCulture, "DELETE FROM {0} Where Id=@id\nSELECT @@ROWCOUNT", TableName);
                MySqlCommand deleteCommand = new(deleteQuery);
                _ = deleteCommand.Parameters.AddWithValue("Id", item.Id);
                int res = await Utilities.ExecuteScalar(_connection, deleteCommand);
                return res == 1;
            }
            catch (Exception ex)
            {
                if (ex != null) { }
                return false;
            }
        }

        public async Task<long> Delete(IEnumerable<T> items)
        {
            IEnumerable<IEnumerable<T>> dataList = SplitData(items);
            Task<long>[] tasks = new Task<long>[dataList.Count()];
            long count = 0;
            for (int i = 0; i < dataList.Count(); i++)
            {
                Task<long> task = DeleteBatch(dataList.ElementAt(i));
                tasks[i] = task;
            }
            _ = await Task.WhenAll(tasks);
            foreach (Task<long>? task in tasks)
            {
                count += task.Result;
            }
            return count;
        }

        public async Task<long> Delete(Expression<Func<T, bool>> expression)
        {
            try
            {
                string query = $"DELETE FROM {TableName}";
                if (expression != null) query = $"{query} {expression.GetSqlQuery()}";
                using var command = new MySqlCommand(query, _connection);
                if (_connection.State != ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }
                int res = await Utilities.ExecuteNonQuery(_connection, command);
                _connection.Close();
                return res;
            }
            catch (Exception ex)
            {
                _connection.Close();
                return -1;
            }
        }

        #endregion CRUD

        #region Dispose

        private bool _disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~CollectionCRUD()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task<bool> CheckConnecetion()
        {
            if (_connection == null) { return false; }
            if (_connection.State == ConnectionState.Open)
            {
                return true;
            }
            try
            {
                await _connection.OpenAsync();
                CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                while (cancellation.IsCancellationRequested == false && _connection.State != ConnectionState.Open)
                {
                    await Task.Delay(10);
                }
                var res = _connection.State == ConnectionState.Open;
                _connection.Close();
                return res;
            }
            catch (Exception ex)
            {
                return true;
            }
        }

        public Task<IEnumerable<T>> Retrieve()
        {
            throw new NotImplementedException();
        }

        #endregion Dispose

        #endregion Methods
    }

    //public static class QueryFilterExtensions
    //{
    //    public static string ToSqlCondition(this IQueryFilter filter, Dictionary<string, string> map)
    //    {
    //        var col = map[filter.ItemPropertyFilter.PropertyName];
    //        return $"{col} = @{filter.ParamName}";
    //    }

    //    public static string ParamName(this IQueryFilter filter) => "p_" + filter.ItemPropertyFilter.PropertyName;
    //}
}