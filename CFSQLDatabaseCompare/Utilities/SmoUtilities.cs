using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;

namespace CFSQLDatabaseCompare.Utilities
{
    /// <summary>
    /// SMO utilities
    /// </summary>
    internal class SmoUtilities
    {
        public static Trigger? GetTriggerByName(TriggerCollection triggers, string name)
        {
            if (triggers != null)
            {                               
                foreach (Trigger trigger in triggers)
                {
                    if (trigger.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return trigger;
                    }
                }
            }
            return null;
        }

        public static Synonym? GetSynonymByName(SynonymCollection synonyms, string name)
        {
            if (synonyms != null)
            {
                foreach (Synonym synonym in synonyms)
                {
                    if (synonym.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return synonym;
                    }
                }
            }
            return null;
        }

        public static UserDefinedType? GetUserDefinedTypeByName(UserDefinedTypeCollection userDefinedTypes, string name)
        {
            if (userDefinedTypes != null)
            {
                foreach (UserDefinedType userDefinedType in userDefinedTypes)
                {
                    if (userDefinedType.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return userDefinedType;
                    }
                }

            }
            return null;
        }

        public static ForeignKey? GetForiengKeyByName(ForeignKeyCollection foreignKeys, string name)
        {
            if (foreignKeys != null)
            {
                foreach (ForeignKey foreignKey in foreignKeys)
                {
                    if (foreignKey.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return foreignKey;
                    }
                }

            }
            return null;
        }

        public static DatabaseDdlTrigger? GetDatabaseDdlTriggerByName(DatabaseDdlTriggerCollection triggers, string name)
        {
            if (triggers != null)
            {
                foreach (DatabaseDdlTrigger trigger in triggers)
                {
                    if (trigger.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return trigger;
                    }
                }
            }
            return null;
        }

        public static View? GetViewByName(ViewCollection views, string name)
        {
            if (views != null)
            {
                foreach (View view in views)
                {
                    if (view.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return view;
                    }
                }
            }
            return null;
        }

        public static Microsoft.SqlServer.Management.Smo.Index? GetIndexByColumns(IndexCollection indices, IndexType indexType, string[] columnNames)
        {
            foreach(Microsoft.SqlServer.Management.Smo.Index index in indices)
            {
                if (index.IndexType == indexType)
                {
                    int countColumnsFound = 0;
                    foreach (IndexedColumn indexedColumn in index.IndexedColumns)
                    {
                        if (Array.IndexOf(columnNames, indexedColumn.Name) != -1)
                        {
                            countColumnsFound++;
                        }
                    }
                    if (countColumnsFound == columnNames.Length)
                    {
                        return index;
                    }
                }
            }
            return null;
        }
        
        public static StoredProcedure? GetStoredProcedureByName(StoredProcedureCollection storedProcedures, string name)
        {
            if (storedProcedures != null)
            {
                foreach (StoredProcedure storedProcedure in storedProcedures)
                {
                    if (storedProcedure.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return storedProcedure;
                    }
                }
            }
            return null;
        }

        public static UserDefinedFunction? GetUserDefinedFunctionViewByName(UserDefinedFunctionCollection userDefinedFunctions, string name)
        {
            if (userDefinedFunctions != null)
            {
                foreach (UserDefinedFunction userDefinedFunction in userDefinedFunctions)
                {
                    if (userDefinedFunction.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return userDefinedFunction;
                    }
                }
            }
            return null;
        }

        public static Table? GetTableByName(TableCollection tables, string name)
        {
            if (tables != null)
            {
                foreach (Table table in tables)
                {
                    if (table.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return table;
                    }
                }
            }
            return null;
        }

        public static Column? GetColumnByName(ColumnCollection columns, string columnName)
        {
            if (columns != null)
            {
                foreach (Column column in columns)
                {
                    if (column.Name.Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return column;
                    }
                }
            }
            return null;
        }

        public static Database? GetDatabaseByName(Server server, string databaseName)
        {
            if (server != null)
            {
                foreach (Database database in server.Databases)
                {
                    if (database.Name.Equals(databaseName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return database;
                    }
                }
            }
            return null;
        }
    }
}
