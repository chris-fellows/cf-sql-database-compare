using CFSQLDatabaseCompare.Enums;
using CFSQLDatabaseCompare.Interfaces;
using CFSQLDatabaseCompare.Models;
using CFSQLDatabaseCompare.Utilities;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFSQLDatabaseCompare.Comparer.MSSQL
{
    /// <summary>
    /// Compares data in 2 SQL Server databases
    /// </summary>
    public class MSSQLDataComparer : MSSQLComparerBase, ISQLDataComparer
    {
        public delegate void Status(object sender, string status);
        public Status? OnStatus;

        public List<Difference> Compare(DataCompareOptions dataCompareOptions, CancellationToken cancellationToken)
        {
            var differences = new List<Difference>();

            // Open database 1
            DisplayStatus(string.Format("Opening database {0}", dataCompareOptions.DatabaseInfo1.DisplayName));
            var server1 = ConnectToServer(dataCompareOptions.DatabaseInfo1.ServerName, dataCompareOptions.DatabaseInfo1.UserName, dataCompareOptions.DatabaseInfo1.Password);
            _database1 = SmoUtilities.GetDatabaseByName(server1, dataCompareOptions.DatabaseInfo1.DatabaseName);

            // Open database 2
            DisplayStatus(string.Format("Opening database {0}", dataCompareOptions.DatabaseInfo2.DisplayName));
            var server2 = ConnectToServer(dataCompareOptions.DatabaseInfo2.ServerName, dataCompareOptions.DatabaseInfo2.UserName, dataCompareOptions.DatabaseInfo2.Password);
            _database2 = SmoUtilities.GetDatabaseByName(server2, dataCompareOptions.DatabaseInfo2.DatabaseName);

            // Compare tables
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareTables(_database1, _database2, dataCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Clean up
            server1.ConnectionContext.Disconnect();
            server2.ConnectionContext.Disconnect();
            return differences;
        }

        private void DisplayStatus(string status)
        {
            if (OnStatus != null)
            {
                OnStatus(this, status);
            }
        }

        private List<Difference> CompareTables(Database database1, Database database2, DataCompareOptions dataCompareOptions, CancellationToken cancellationToken)
        {
            DisplayStatus("Comparing tables");
            List<Difference> differenceList = new List<Difference>();
            //Dictionary<string, string> tablesChecked = new Dictionary<string, string>();

            foreach (Table table1 in database1.Tables)
            {
                if (!table1.IsSystemObject)
                {
                    DisplayStatus(string.Format("Comparing table {0}", table1.Name));

                    var table2 = SmoUtilities.GetTableByName(database2.Tables, table1.Name);
                    //tablesChecked.Add(table1.Name, table1.Name);

                    if (table2 != null) // Only check tables in both databases
                    {
                        var tableDifferencesList = CompareTable(database1, table1, database2, table2, dataCompareOptions, cancellationToken);
                        tableDifferencesList.ForEach(item => differenceList.Add(item));
                    }
                }

                if (cancellationToken.IsCancellationRequested) break;
            }

            //if (!cancellationToken.IsCancellationRequested)
            //{
            //    foreach (Table table2 in database2.Tables)
            //    {
            //        if (!table2.IsSystemObject)
            //        {
            //            if (!tablesChecked.ContainsKey(table2.Name))
            //            {                            
            //                var tableDifferenceList = CompareTable(database1, null, database2, table2, dataCompareOptions, cancellationToken);
            //                tableDifferenceList.ForEach(item => differenceList.Add(item));
            //            }
            //        }

            //        if (cancellationToken.IsCancellationRequested) break;
            //    }
            //}
            return differenceList;
        }

        private List<Difference> CompareTable(Database database1, Table table1, Database database2, Table table2, DataCompareOptions dataCompareOptions, CancellationToken cancellationToken)
        {
            var differenceList = new List<Difference>();

            //if (table1 == null)
            //{
            //    var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table2.Name), DifferenceTypes.ObjectNotIn1);
            //    differenceList.Add(differnce);
            //    return differenceList;
            //}
            //else if (table2 == null)
            //{
            //    var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, database2.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), DifferenceTypes.ObjectNotIn2);
            //    differenceList.Add(differnce);
            //    return differenceList;
            //}
            //else
            //{
                // Compare data, currently we only check row counts
                //if (dataCompareOptions.CompareTableRowCounts)
                // {
                if (table1.RowCount != table2.RowCount)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), DifferenceTypes.TableRowCounts);
                    //difference.Text = string.Format("{0}={1} row(s); {2}={3} row(s)", dataCompareOptions.DatabaseInfo1.DisplayName, table1.RowCount, dataCompareOptions.DatabaseInfo2.DisplayName, table2.RowCount);
                    difference.Parameters.Add(new DifferenceParameter() { ParameterType = DifferenceParameterTypes.TableRowCount, Value = table1.RowCount, DatabaseIndex = 1 });
                    difference.Parameters.Add(new DifferenceParameter() { ParameterType = DifferenceParameterTypes.TableRowCount, Value = table2.RowCount, DatabaseIndex = 2 });
                    differenceList.Add(difference);
                }
                //}

                // Compare PKs
                /*
                if (databaseCompareOptions.CompareTableRowColumns)
                {                    
                    List<Difference> dataDifferenceList = CompareTablePrimaryKeyValues(database1, table1, database2, table2, databaseCompareOptions);
                    dataDifferenceList.ForEach(item => differenceList.Add(item));                    
                }
                */

                // Compare row colums
                //if (dataCompareOptions.CompareTableRows)
                //{
                var rowDifferenceList = CompareTableRows(database1, table1, database2, table2, dataCompareOptions, cancellationToken);
                rowDifferenceList.ForEach(item => differenceList.Add(item));
                //}
            //}

            System.Diagnostics.Debug.WriteLine("Compared " + table1.Name);

            return differenceList;
        }

        /// <summary>
        /// Compares data in two tables
        /// </summary>
        /// <param name="database1"></param>
        /// <param name="table1"></param>
        /// <param name="database2"></param>
        /// <param name="table2"></param>
        /// <param name="dataCompareOptions"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private List<Difference> CompareTableRows(Database database1, Table table1, Database database2, Table table2, 
                            DataCompareOptions dataCompareOptions, CancellationToken cancellationToken)
        {
            var differenceList = new List<Difference>();

            if (table1 != null && table2 != null)
            {
                List<string> pkColumnNames = new List<string>();

                // Generate SELECT clause, work out PK columns
                StringBuilder sql = new StringBuilder("SELECT ");
                int columnCount = 0;
                foreach (Column column1 in table1.Columns)
                {
                    columnCount++;
                    if (columnCount > 1)
                    {
                        sql.Append(", ");
                    }
                    sql.Append(column1.Name);
                    if (column1.InPrimaryKey)
                    {
                        pkColumnNames.Add(column1.Name);
                    }
                }
                sql.Append(string.Format(" FROM {0}", table1.Name));

                if (pkColumnNames.Count > 0)
                {
                    // Connect to db1
                    string connectionString1 = InternalUtilities.GetConnectionString(dataCompareOptions.DatabaseInfo1);
                    SqlConnection connection1 = new SqlConnection(connectionString1);
                    connection1.Open();

                    // Connect to db2
                    string connectionString2 = InternalUtilities.GetConnectionString(dataCompareOptions.DatabaseInfo2);
                    SqlConnection connection2 = new SqlConnection(connectionString2);
                    connection2.Open();

                    // Get PK values in db 1, not very efficient loading all data
                    var dataTable1 = GetTablePrimaryKeyData(connection1, sql.ToString());

                    // Get PK values in db 2, not very efficient loading all data
                    var dataTable2 = GetTablePrimaryKeyData(connection2, sql.ToString());

                    // Compare db1 rows with db2 rows
                    List<int> db2RowsFound = new List<int>();
                    for (int rowIndex1 = 0; rowIndex1 < dataTable1.Rows.Count; rowIndex1++)
                    {
                        int rowIndex2 = GetDataTableMatchingRowIndex(dataTable1, dataTable2, pkColumnNames, rowIndex1);
                        List<Difference> rowDifferenceList = CompareTableRow(database1, table1, dataTable1, rowIndex1, database2, table2, dataTable2, rowIndex2, pkColumnNames, dataCompareOptions);
                        rowDifferenceList.ForEach(item => differenceList.Add(item));
                        if (rowIndex2 != -1)
                        {
                            db2RowsFound.Add(rowIndex2);
                        }
                    }

                    // Check for db2 rows that aren't in db1
                    for (int rowIndex2 = 0; rowIndex2 < dataTable2.Rows.Count; rowIndex2++)
                    {
                        if (!db2RowsFound.Contains(rowIndex2))    // Only check if not matched above
                        {
                            int rowIndex1 = GetDataTableMatchingRowIndex(dataTable2, dataTable1, pkColumnNames, rowIndex2);
                            List<Difference> rowDifferenceList = CompareTableRow(database2, table2, dataTable2, rowIndex2, database1, table1, dataTable1, rowIndex1, pkColumnNames, dataCompareOptions);
                            rowDifferenceList.ForEach(item => differenceList.Add(item));
                        }
                    }

                    // Close db connection
                    connection1.Close();
                    connection2.Close();
                }
            }
            return differenceList;
        }

        private DataTable GetTablePrimaryKeyData(SqlConnection connection, string sql)
        {
            DataTable dataTable = null;
            SqlCommand command = GetCommand(connection, CommandType.Text, sql);

            using (SqlDataReader dataReader = command.ExecuteReader())
            {
                dataTable = new DataTable();
                dataTable.Load(dataReader);
                dataReader.Close();
            }
            return dataTable;
        }

        /// <summary>
        /// Compares the row from table 1 with the row from table 2
        /// </summary>
        /// <param name="database1"></param>
        /// <param name="table1"></param>
        /// <param name="dataTable1"></param>
        /// <param name="rowIndex1"></param>
        /// <param name="database2"></param>
        /// <param name="table2"></param>
        /// <param name="dataTable2"></param>
        /// <param name="rowIndex2"></param>
        /// <param name="pkColumns"></param>
        /// <param name="dataCompareOptions"></param>
        /// <returns></returns>
        private List<Difference> CompareTableRow(Database database1, Table table1, DataTable dataTable1, int rowIndex1,
                                                Database database2, Table table2, DataTable dataTable2, int rowIndex2,
                                                List<string> pkColumns,
                                                DataCompareOptions dataCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (rowIndex1 != -1 && rowIndex2 != -1) // Row exists in both tables, compare columns
            {
                // Compare columns
                bool isRowSame = true;
                for (int columnIndex1 = 0; columnIndex1 < dataTable1.Columns.Count && isRowSame == true; columnIndex1++)
                {
                    if (!pkColumns.Contains(dataTable1.Columns[columnIndex1].ColumnName))   // Ignore PK columns
                    {
                        // Find column in table 2
                        for (int columnIndex2 = 0; columnIndex2 < dataTable2.Columns.Count && isRowSame == true; columnIndex2++)
                        {
                            if (dataTable2.Columns[columnIndex2].ColumnName.Equals(dataTable1.Columns[columnIndex1].ColumnName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                object value1 = dataTable1.Rows[rowIndex1][columnIndex1];
                                object value2 = dataTable2.Rows[rowIndex2][columnIndex2];
                                if (!value1.Equals(value2))    // Row values different
                                {
                                    isRowSame = false;
                                }
                            }
                        }
                    }
                }

                if (!isRowSame)     // Row different
                {
                    string primaryKeyDescription = GetPrimaryKeyDescription(dataTable1, pkColumns, rowIndex1);
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), DifferenceTypes.TableRowColumnsDifferent);
                    difference.Parameters.Add(new DifferenceParameter() { ParameterType = DifferenceParameterTypes.RowPrimaryKeyDescription, Value = primaryKeyDescription });
                    //difference.Text = primaryKeyDescription;
                    differenceList.Add(difference);
                }
            }
            else if (rowIndex1 != -1 && rowIndex2 == -1)   // In db1 but not db2
            {
                string primaryKeyDescription = GetPrimaryKeyDescription(dataTable1, pkColumns, rowIndex1);
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), DifferenceTypes.TableRecordNotFound);
                difference.Parameters.Add(new DifferenceParameter() { ParameterType = DifferenceParameterTypes.RowPrimaryKeyDescription, Value = primaryKeyDescription });
                //difference.Text = primaryKeyDescription;
                differenceList.Add(difference);
            }
            else if (rowIndex1 == -1 && rowIndex2 != -1)   // In db2 but not db1
            {
                string primaryKeyDescription = GetPrimaryKeyDescription(dataTable1, pkColumns, rowIndex2);
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, database2.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table2.Name), DifferenceTypes.TableRecordNotFound);
                difference.Parameters.Add(new DifferenceParameter() { ParameterType = DifferenceParameterTypes.RowPrimaryKeyDescription, Value = primaryKeyDescription });
                //difference.Text = primaryKeyDescription;
                differenceList.Add(difference);
            }
            return differenceList;
        }

        /// <summary>
        /// Returns the row index in dataTable2 that matches the PK values for the specified row in dataTable1.
        /// </summary>
        /// <param name="dataTable1"></param>
        /// <param name="dataTable2"></param>
        /// <param name="pkColumnNames"></param>
        /// <param name="rowIndex1"></param>
        /// <returns></returns>
        private static int GetDataTableMatchingRowIndex(DataTable dataTable1, DataTable dataTable2, List<string> pkColumnNames, int rowIndex1)
        {
            for (int rowIndex2 = 0; rowIndex2 < dataTable2.Rows.Count; rowIndex2++)
            {
                bool matchedRow = true;    // Matched until we've checked columns
                for (int columnIndex2 = 0; columnIndex2 < pkColumnNames.Count && matchedRow == true; columnIndex2++)
                {
                    object value1 = dataTable1.Rows[rowIndex1][pkColumnNames[columnIndex2]];
                    object value2 = dataTable2.Rows[rowIndex2][pkColumnNames[columnIndex2]];
                    if (!value1.Equals(value2))
                    {
                        matchedRow = false;
                    }
                }
                if (matchedRow)
                {
                    return rowIndex2;
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns description of primary key values for specified data table row
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="pkColumnNames"></param>
        /// <param name="rowIndex"></param>
        /// <returns></returns>
        private string GetPrimaryKeyDescription(DataTable dataTable, List<string> pkColumnNames, int rowIndex)
        {
            var output = new StringBuilder("");
            for (int columnIndex = 0; columnIndex < pkColumnNames.Count; columnIndex++)
            {
                if (output.Length > 0)
                {
                    output.Append(", ");
                }
                output.Append(string.Format("{0}={1}", pkColumnNames[columnIndex], dataTable.Rows[rowIndex][pkColumnNames[columnIndex]].ToString()));
            }
            return output.ToString();
        }


        /// <summary>
        /// Compares primary key values for the tables. Identifies PK values in db 1 that aren't in db 2 and vice versa.
        /// </summary>
        /// <param name="database1"></param>
        /// <param name="table1"></param>
        /// <param name="database2"></param>
        /// <param name="table2"></param>
        /// <param name="dataCompareOptions"></param>
        /// <returns></returns>
        private List<Difference> CompareTablePrimaryKeyValues(Database database1, Table table1, Database database2, Table table2, DataCompareOptions dataCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (table1 != null && table2 != null)
            {
                List<string> pkColumnNames = new List<string>();

                // Get list of PK columns, if any  
                StringBuilder sql = new StringBuilder("SELECT ");
                int columnCount = 0;
                foreach (Column column1 in table1.Columns)
                {
                    if (column1.InPrimaryKey)
                    {
                        columnCount++;
                        if (columnCount > 1)
                        {
                            sql.Append(", ");
                        }
                        sql.Append(column1.Name);
                        pkColumnNames.Add(column1.Name);
                    }
                }
                sql.Append(string.Format(" FROM {0}", table1.Name));

                if (pkColumnNames.Count > 0)
                {
                    // Connect to db1
                    string connectionString1 = InternalUtilities.GetConnectionString(dataCompareOptions.DatabaseInfo1);
                    SqlConnection connection1 = new SqlConnection(connectionString1);
                    connection1.Open();

                    // Connect to db2
                    string connectionString2 = InternalUtilities.GetConnectionString(dataCompareOptions.DatabaseInfo2);
                    SqlConnection connection2 = new SqlConnection(connectionString2);
                    connection2.Open();

                    // Get PK values in db 1
                    DataTable dataTable1 = GetTablePrimaryKeyData(connection1, sql.ToString());

                    // Get PK values in db 2
                    DataTable dataTable2 = GetTablePrimaryKeyData(connection2, sql.ToString());

                    // For all rows in db1 then check if row in db2
                    List<int> db2RowsFound = new List<int>();
                    for (int rowIndex1 = 0; rowIndex1 < dataTable1.Rows.Count; rowIndex1++)
                    {
                        int rowIndex2 = GetDataTableMatchingRowIndex(dataTable1, dataTable2, pkColumnNames, rowIndex1);
                        if (rowIndex2 == -1)    // Not in db2
                        {
                            string primaryKeyDescription = GetPrimaryKeyDescription(dataTable1, pkColumnNames, rowIndex1);
                            var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, database2.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), DifferenceTypes.TableRecordNotFound);
                            difference.Parameters.Add(new DifferenceParameter() { ParameterType = DifferenceParameterTypes.RowPrimaryKeyDescription, Value = primaryKeyDescription });
                            //difference.Text = primaryKeyDescription;
                            differenceList.Add(difference);
                        }
                        else    // Row exists in 2, record it so that we don't check it below
                        {
                            db2RowsFound.Add(rowIndex2);
                        }
                    }

                    // For all rows in db2 then check if row in db1
                    for (int rowIndex2 = 0; rowIndex2 < dataTable2.Rows.Count; rowIndex2++)
                    {
                        if (!db2RowsFound.Contains(rowIndex2))
                        {
                            int rowIndex1 = GetDataTableMatchingRowIndex(dataTable2, dataTable1, pkColumnNames, rowIndex2);
                            if (rowIndex1 == -1)     // Not in db1
                            {
                                string primaryKeyDescription = GetPrimaryKeyDescription(dataTable2, pkColumnNames, rowIndex2);
                                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table2.Name), DifferenceTypes.TableRecordNotFound);
                                //difference.Text = primaryKeyDescription;
                                difference.Parameters.Add(new DifferenceParameter() { ParameterType = DifferenceParameterTypes.RowPrimaryKeyDescription, Value = primaryKeyDescription });
                                differenceList.Add(difference);
                            }
                        }
                    }

                    // Close db connection
                    connection1.Close();
                    connection2.Close();
                }
            }
            return differenceList;
        }
    }
}
