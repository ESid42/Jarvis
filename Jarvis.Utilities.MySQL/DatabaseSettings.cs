/*Copyright (C) 2020 by Technica International
/*************************************************************************
* Programmer: Patrick Abi Abdallah (p.abdallah)
* Created: 2020, 10, 2, 14:26
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

using System.Linq.Expressions;
using System.Net;
using System.Text;
using Jarvis.Authentication.Database;
using Jarvis.Database;
using Jarvis.Interfaces;

namespace Jarvis.Utilities.MySQL
{
    /// <summary>
    /// Settings used to establish a connection to a Mongo database.
    /// </summary>
    public class DatabaseSettings : DatabaseSettingsBase
    {
        #region Definitions

        /// <summary>
        /// The user
        /// </summary>
        private readonly DatabaseUser? _user;

        /// <summary>
        /// Initial Filter to be used when loading the database, if <see cref="Filters"/> is null.
        /// </summary>
        public Expression<Func<object, bool>>? ExpFilter { get; set; }

        ///// <summary>
        ///// The name of the database to which the connection will be established.
        ///// </summary>
        //public string DatabaseName { get; set; }
        /// <summary>
        /// The <see cref="List{T}"/> to be used when initially loading the database.
        /// </summary>
        public List<IQueryFilter>? Filters { get; set; }

        /// <summary>
        /// </summary>
        public bool IsLocal { get; }

        /// <summary>
        /// Initial Query to be used in <see cref="Filters"/> is null.
        /// </summary>
        public Func<Item, bool>? Query { get; set; }

        #endregion Definitions

        #region Constructor

        /// <summary>
        /// Initializes the settings by defining the Connection string, collection name and whether
        /// initial data loading will include deleted items.
        /// </summary>
        /// <param name="connectionString">The connection string to be used.</param>
        /// <param name="collectionName">The collection at which data will be queried.</param>
        /// <param name="includeDeleted">
        /// Specifies whether to include deleted items when first loading the data.
        /// </param>
        public DatabaseSettings(string? connectionString, string? databaseName, bool isLocal = true)
        {
            databaseName?.NullCheck(nameof(databaseName));
            connectionString?.NullCheck(nameof(connectionString));
            ConnectionString = connectionString ?? string.Empty;
            DatabaseName = databaseName ?? string.Empty;
            IsLocal = isLocal;
        }

        /// <summary>
        /// Initializes the settings to be used when connecting to a local MongoDB server.
        /// </summary>
        /// <param name="user">The user to be used for authentication.</param>
        /// <param name="hostName">The host name.</param>
        /// <param name="databaseName">The database to query data.</param>
        /// <param name="collectionName">The collection to query data.</param>
        /// <param name="port">The sever port to which connection will be established.</param>
        /// <param name="includeDeleted">
        /// Specifies whether to include deleted items when first loading the data.
        /// </param>
        public DatabaseSettings(DatabaseUser? user, string hostName, string databaseName, bool isLocal = false)
        {
            //if (port < 1)
            //{
            //    throw new ArgumentException("Port must be strictly positive.", nameof(port));
            //}

            //if (string.IsNullOrWhiteSpace(databaseName))
            //{
            //    throw new ArgumentNullException(nameof(databaseName), "Cannot connect to a null database.");
            //}
            user ??= new DatabaseUser("", "");
            _user = user ?? throw new ArgumentNullException(nameof(user), "Cannot authenticate with a null DatabaseUser. In case of no user profiles defined simple set DatabaseUser.Name to null");
            DatabaseName = databaseName;
            HostName = hostName;
            Port = 3306;
            IsLocal = isLocal;
            InitConnectionString();
        }

        /// <summary>
        /// Initializes the settings by defining the Connection string, collection name and whether
        /// initial data loading will include deleted items.
        /// </summary>
        /// <param name="connectionString">The connection string to be used.</param>
        /// <param name="collectionName">The collection at which data will be queried.</param>
        /// <param name="includeDeleted">
        /// Specifies whether to include deleted items when first loading the data.
        /// </param>
        /// <param name="networkAdresses">
        /// Sets the network addresses to be used when connecting to server.
        /// </param>
        public DatabaseSettings(string connectionString, string databaseName, IEnumerable<IPAddress>? networkAdresses = null)
        {
            databaseName.NullCheck(nameof(databaseName));
            connectionString.NullCheck(nameof(connectionString));
            ConnectionString = connectionString;
            NetworkAddresses = networkAdresses;
            DatabaseName = databaseName;
        }

        /// <summary>
        /// Initializes the settings to be used when connecting to a local MongoDB server.
        /// </summary>
        /// <param name="user">The user to be used for authentication.</param>
        /// <param name="hostName">The host name.</param>
        /// <param name="databaseName">The database to query data.</param>
        /// <param name="collectionName">The collection to query data.</param>
        /// <param name="port">The sever port to which connection will be established.</param>
        /// <param name="includeDeleted">
        /// Specifies whether to include deleted items when first loading the data.
        /// </param>
        /// <param name="networkAdresses">
        /// Sets the network addresses to be used when connecting to server.
        /// </param>
        public DatabaseSettings(DatabaseUser? user, string? hostName, string? databaseName, bool isLocal = false, IEnumerable<IPAddress>? networkAdresses = null)
        {
            //if (port < 1)
            //{
            //    throw new ArgumentException("Port must be strictly positive.", nameof(port));
            //}

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName), "Cannot connect to a null database.");
            }
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException(nameof(hostName), "Cannot connect to a null hostName.");
            }
            _user = user ?? throw new ArgumentNullException(nameof(user), "Cannot authenticate with a null DatabaseUser. In case of no user profiles defined simple set DatabaseUser.Name to null");
            DatabaseName = databaseName;
            HostName = hostName;
            Port = 27017;

            NetworkAddresses = networkAdresses;
            IsLocal = isLocal;
            InitConnectionString();
        }

        /// <summary>
        /// Inits the connection string
        /// </summary>
        protected override void InitConnectionString()
        {
            StringBuilder sb = new();

            _ = sb.Append("Server=");
            _ = sb.Append(HostName);

            if (!string.IsNullOrWhiteSpace(Port.ToString()))
            {
                _ = sb.Append(";Port=");
                _ = sb.Append(Port);
            }

            if (_user != null && !string.IsNullOrEmpty(_user.Name))
            {
                _ = sb.Append(";User Id=");
                _ = sb.Append(_user.Name);
                _ = sb.Append(";Password=");
                _ = sb.Append(_user.Password);
            }

            // Optional common settings
            _ = sb.Append(";SslMode=");
            _ = sb.Append(IsLocal ? "None" : "Preferred");

            if (!string.IsNullOrEmpty(DatabaseName))
            {
                _ = sb.Append(";Database=");
                _ = sb.Append(DatabaseName);
            }

            ConnectionString = sb.ToString();
        }

        #endregion Constructor
    }
}