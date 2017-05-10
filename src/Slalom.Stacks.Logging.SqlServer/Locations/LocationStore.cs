using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Slalom.Stacks.Logging.SqlServer.Core;

namespace Slalom.Stacks.Logging.SqlServer.Locations
{
    public class LocationStore : IDisposable
    {
        private readonly SqlConnectionManager _connection;
        private readonly IPInformationProvider _provider;

        private DataTable _locationsTable;

        public LocationStore(SqlConnectionManager connection, IPInformationProvider provider)
        {
            _connection = connection;
            _provider = provider;
        }

        public DataTable CreateTable()
        {
            var table = new DataTable("Locations");

            table.Columns.Add(new DataColumn
            {
                DataType = typeof(int),
                ColumnName = "Id",
                AutoIncrement = true,
                AllowDBNull = false
            });
            table.PrimaryKey = new[] { table.Columns[0] };
            table.Columns.Add(new DataColumn("IPAddress")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Latitude")
            {
                DataType = typeof(decimal)
            });
            table.Columns.Add(new DataColumn("Longitude")
            {
                DataType = typeof(decimal)
            });
            table.Columns.Add(new DataColumn("ISP")
            {
                DataType = typeof(string)
            });
            return table;
        }

        public async Task UpdateAsync(params string[] addresses)
        {
            var table = this.GetTable();

            foreach (var address in addresses)
            {
                if (table.Rows.OfType<DataRow>().All(e => e[1].ToString() != address))
                {
                    var current = _provider.Get(address);
                    table.Rows.Add(null, current.IPAddress, current.Latitude, current.Longitude, current.Isp);
                }
            }

            var changes = table.GetChanges();
            if (changes != null && changes.Rows.Count != 0)
            {
                using (var copy = new SqlBulkCopy(_connection.Connection))
                {
                    copy.DestinationTableName = string.Format(table.TableName);
                    foreach (var column in table.Columns)
                    {
                        var columnName = ((DataColumn)column).ColumnName;
                        var mapping = new SqlBulkCopyColumnMapping(columnName, columnName);
                        copy.ColumnMappings.Add(mapping);
                    }

                    await copy.WriteToServerAsync(changes).ConfigureAwait(false);

                    table.AcceptChanges();
                }
            }
        }

        private DataTable GetTable()
        {
            if (_locationsTable == null)
            {
                _locationsTable = this.CreateTable();
                using (var adapter = new SqlDataAdapter("SELECT * FROM " + _locationsTable.TableName, _connection.Connection))
                {
                    adapter.Fill(_locationsTable);
                    _locationsTable.AcceptChanges();
                }
            }
            return _locationsTable;
        }

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
        /// Finalizes an instance of the <see cref="LocationStore"/> class.
        /// </summary>
        ~LocationStore()
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
                _locationsTable?.Dispose();
            }

            // release any unmanaged objects
            // set the object references to null
            _locationsTable = null;

            _disposed = true;
        }

        #endregion
    }
}