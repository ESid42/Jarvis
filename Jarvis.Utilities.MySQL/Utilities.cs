using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Reflection;

namespace Jarvis.Utilities.MySQL
{
    /// <summary>
    /// The utilities class
    /// </summary>
    internal static class Utilities
    {
        /// <summary>
        /// The db access token
        /// </summary>
        private static readonly SemaphoreSlim _dbAccessToken = new(1, 1);

        /// <summary>
        /// Executes the non query using the specified connection
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="cmd">The cmd</param>
        public static async Task<DbDataReader> ExecuteReader(MySqlConnection connection, MySqlCommand cmd)
        {
            try
            {
                cmd.Connection = connection;
                await _dbAccessToken.WaitAsync();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                var res = await cmd.ExecuteReaderAsync();
                connection.Close();
                cmd.Dispose();
                _ = _dbAccessToken.Release();
                return res;
            }
            catch
            {
                connection.Close();
                _ = _dbAccessToken.Release();
                throw;
            }
        }

        /// <summary>
        /// Executes the non query using the specified connection
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="cmd">The cmd</param>
        public static async Task<int> ExecuteNonQuery(MySqlConnection connection, MySqlCommand cmd)
        {
            try
            {
                cmd.Connection = connection;
                await _dbAccessToken.WaitAsync();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                int res = await cmd.ExecuteNonQueryAsync();
                connection.Close();
                cmd.Dispose();
                _ = _dbAccessToken.Release();
                return res;
            }
            catch
            {
                connection.Close();
                _ = _dbAccessToken.Release();
                throw;
            }
        }

        /// <summary>
        /// Executes the scalar using the specified connection
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="cmd">The cmd</param>
        /// <returns>A task containing the int</returns>
        public static async Task<int> ExecuteScalar(MySqlConnection connection, MySqlCommand cmd)
        {
            try
            {
                cmd.Connection = connection;
                await _dbAccessToken.WaitAsync();
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    await connection.OpenAsync();
                }
                object? res = await cmd.ExecuteScalarAsync();
                connection.Close();
                cmd.Dispose();
                _ = _dbAccessToken.Release();
                if (res != null)
                {
                    return TryConvert.GetInt32(res) ?? -1;
                }
            }
            catch (Exception e)
            {
                connection.Close();
                _ = _dbAccessToken.Release();
                throw;
            }
            return -1;
        }

        public static MySqlItemColumnAttribute GetMySqlItemColumnAttribute(this PropertyInfo prop)
        {
            return new()
            {
                Name = prop.Name,
                MySQLDataType = prop.GetMySqlDataType(),
            };
        }

        public static MySqlDataTypes GetMySqlDataType(this PropertyInfo property)
        {
            Type type = property.PropertyType;

            // Handle Nullable<T>
            if (Nullable.GetUnderlyingType(type) is Type innerType)
            {
                type = innerType;
            }

            if (type == typeof(string))
                return MySqlDataTypes.VARCHAR;
            if (type == typeof(bool))
                return MySqlDataTypes.BOOLEAN;
            if (type == typeof(byte))
                return MySqlDataTypes.TINYINT;
            if (type == typeof(short))
                return MySqlDataTypes.SMALLINT;
            if (type == typeof(int))
                return MySqlDataTypes.INT;
            if (type == typeof(long))
                return MySqlDataTypes.BIGINT;
            if (type == typeof(float))
                return MySqlDataTypes.FLOAT;
            if (type == typeof(double))
                return MySqlDataTypes.DOUBLE;
            if (type == typeof(decimal))
                return MySqlDataTypes.DECIMAL;
            if (type == typeof(DateTime))
                return MySqlDataTypes.DATETIME;
            if (type == typeof(byte[]))
                return MySqlDataTypes.BLOB;
            if (type == typeof(Guid))
                return MySqlDataTypes.CHAR; // or BINARY(16) depending on your design
            if (type.IsEnum)
                return MySqlDataTypes.INT;

            throw new NotSupportedException($"Type {type.FullName} is not supported.");
        }

        public static string GetBaseConnectionString(this string connStr)
        {
            string baseConnStr = string.Join(";", connStr.Split(';').Where(part => !part.TrimStart().StartsWith("Database=", StringComparison.OrdinalIgnoreCase)));
            return baseConnStr;
        }
    }
}