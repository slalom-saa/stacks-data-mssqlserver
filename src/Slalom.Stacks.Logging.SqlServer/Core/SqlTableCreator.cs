// Copyright 2013-2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Modifications copyright(C) 2017 Stacks Contributors


using System;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Slalom.Stacks.Logging.SqlServer.Core
{
    internal class SqlTableCreator
    {
        private readonly string _connectionString;
        private string _tableName;

        #region Constructor

        public SqlTableCreator(string connectionString)
        {
            _connectionString = connectionString;
        }

        #endregion

        #region Instance Methods				
        public int CreateTable(DataTable table)
        {
            if (table == null) return 0;

            if (string.IsNullOrWhiteSpace(table.TableName) || string.IsNullOrWhiteSpace(_connectionString)) return 0;

            _tableName = table.TableName;
            using (var conn = new SqlConnection(_connectionString))
            {
                string sql = GetSqlFromDataTable(_tableName, table);
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }

            }
        }
        #endregion

        #region Static Methods

        private static string GetSqlFromDataTable(string tableName, DataTable table)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("IF NOT EXISTS (SELECT * FROM sysobjects WHERE name = '{0}' AND xtype = 'U')", tableName);
            sql.AppendLine(" BEGIN");
            sql.AppendFormat(" CREATE TABLE [{0}] ( ", tableName);

            // columns
            int numOfColumns = table.Columns.Count;
            int i = 1;
            foreach (DataColumn column in table.Columns)
            {
                sql.AppendFormat("[{0}] {1}", column.ColumnName, SqlGetType(column));
                if (column.ColumnName.ToUpper().Equals("ID") || column.AutoIncrement)
                    sql.Append(" IDENTITY(1,1) ");
                if (numOfColumns > i)
                    sql.AppendFormat(", ");
                i++;
            }

            // primary keys
            if (table.PrimaryKey.Length > 0)
            {
                sql.AppendFormat(" CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED (", tableName);

                int numOfKeys = table.PrimaryKey.Length;
                i = 1;
                foreach (DataColumn column in table.PrimaryKey)
                {
                    sql.AppendFormat("[{0}]", column.ColumnName);
                    if (numOfKeys > i)
                        sql.AppendFormat(", ");

                    i++;
                }
                sql.Append("))");
            }
            sql.AppendLine(" END");
            return sql.ToString();
        }

        // Return T-SQL data type definition, based on schema definition for a column
        private static string SqlGetType(object type, int columnSize, int numericPrecision, int numericScale,
            bool allowDbNull)
        {
            string sqlType;

            switch (type.ToString())
            {
                case "System.Boolean":
                    sqlType = "BIT";
                    break;

                case "System.Byte":
                    sqlType = "TINYINT";
                    break;

                case "System.String":
                    sqlType = "NVARCHAR(" + ((columnSize == -1) ? "MAX" : columnSize.ToString()) + ")";
                    break;

                case "System.Decimal":
                    if (numericScale > 0)
                        sqlType = "REAL";
                    else if (numericPrecision > 10)
                        sqlType = "BIGINT";
                    else
                        sqlType = "INT";
                    break;

                case "System.Double":
                case "System.Single":
                    sqlType = "REAL";
                    break;

                case "System.Int64":
                    sqlType = "BIGINT";
                    break;

                case "System.Int16":
                case "System.Int32":
                    sqlType = "INT";
                    break;

                case "System.DateTime":
                    sqlType = "DATETIME";
                    break;

                case "System.Guid":
                    sqlType = "UNIQUEIDENTIFIER";
                    break;

                case "System.TimeSpan":
                    sqlType = "TIME(7)";
                    break;

                case "System.DateTimeOffset":
                    sqlType = "DATETIMEOFFSET";
                    break;

                default:
                    throw new Exception(string.Format("{0} not implemented.", type));
            }

            sqlType += " " + (allowDbNull ? "NULL" : "NOT NULL");

            return sqlType;
        }

        // Overload based on DataColumn from DataTable type
        private static string SqlGetType(DataColumn column)
        {
            return SqlGetType(column.DataType, column.MaxLength, 10, 2, column.AllowDBNull);
        }

        #endregion
    }
}
