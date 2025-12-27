using System;
using Jarvis.Database;

namespace Jarvis.Utilities.MySQL
{
    /// <summary>
    /// The db collection settings class
    /// </summary>
    /// <seealso cref="IDbCollectionSettings"/>
    /// <remarks>Initializes a new instance of the <see cref="DbCollectionSettings"/> class</remarks>
    /// <param name="databaseSettings">The database settings</param>
    /// <param name="name">The name</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    internal class DbCollectionSettings(IDatabaseSettings databaseSettings, string name) : IDbCollectionSettings
    {
        #region Definitions

        /// <summary>
        /// Gets or sets the value of the database settings
        /// </summary>
        public IDatabaseSettings DatabaseSettings { get; private set; } = databaseSettings ?? throw new ArgumentNullException(nameof(databaseSettings));

        /// <summary>
        /// Gets or sets the value of the name
        /// </summary>
        public string Name { get; private set; } = name ?? throw new ArgumentNullException(nameof(name));

        #endregion Definitions
    }
}