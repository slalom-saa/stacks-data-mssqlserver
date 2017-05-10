using System;
using System.Data.SqlClient;

namespace Slalom.Stacks.Logging.SqlServer.Core
{
    public class SqlConnectionManager : IDisposable
    {
        private readonly Lazy<SqlConnection> _connection;

        public SqlConnectionManager(string connectionString)
        {
            _connection = new Lazy<SqlConnection>(() =>
            {
                var connection = new SqlConnection(connectionString);
                connection.Open();
                return connection;
            });
        }

        public SqlConnection Connection => _connection.Value;

        #region IDisposable Implementation

        bool _disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SqlConnectionManager"/> class.
        /// </summary>
        ~SqlConnectionManager()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // free other managed objects that implement IDisposable only
                if (_connection.IsValueCreated)
                {
                    _connection.Value.Dispose();
                }
            }

            // release any unmanaged objects
            // set the object references to null

            _disposed = true;
        }

        #endregion
    }
}