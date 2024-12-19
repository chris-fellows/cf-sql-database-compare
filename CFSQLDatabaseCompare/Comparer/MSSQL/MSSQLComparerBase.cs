using CFSQLDatabaseCompare.Enums;
using CFSQLDatabaseCompare.Models;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using System.Data;

namespace CFSQLDatabaseCompare.Comparer.MSSQL
{
    public abstract class MSSQLComparerBase
    {
        protected Database? _database1 = null;
        protected Database? _database2 = null;

        /// <summary>
        /// Connects to SQL Server
        /// </summary>
        /// <param name="serverName"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static Server ConnectToServer(string serverName, string userName, string password)
        {
            var server = new Server(serverName);
            if (string.IsNullOrEmpty(userName))
            {
                server.ConnectionContext.LoginSecure = true;
            }
            else
            {
                server.ConnectionContext.LoginSecure = false;
                server.ConnectionContext.Login = userName;
                server.ConnectionContext.Password = password;
            }
            server.ConnectionContext.Connect();
            server.Refresh();
            return server;
        }

        protected void DisplayStatus(string status)
        {
            /*
            if (OnStatus != null)
            {
                OnStatus(this, status);
            }
            */
        }

        protected static SqlCommand GetCommand(SqlConnection connection, CommandType commandType, string sql)
        {
            SqlCommand command = new SqlCommand(sql, connection);
            command.CommandType = commandType;
            return command;
        }

        protected static SQLObjectInfo GetObjectInfo(Database database)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.Database, database.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(Table table)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.UserTable, table.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(View view)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.View, view.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(StoredProcedure storedProcedure)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.StoredProcedure, storedProcedure.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(UserDefinedFunction userDefinedFunction)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.UserDefinedFunction, userDefinedFunction.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(Trigger trigger)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.Trigger, trigger.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(Synonym synonym)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.Synonym, synonym.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(ForeignKey foreignKey)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.ForeignKey, foreignKey.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(UserDefinedType userDefinedType)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.UserDefinedType, userDefinedType.Name);
            return objectInfo;
        }

        protected static SQLObjectInfo GetObjectInfo(DatabaseDdlTrigger trigger)
        {
            var objectInfo = new SQLObjectInfo(SQLObjectTypes.Trigger, trigger.Name);
            return objectInfo;
        }
    }
}
