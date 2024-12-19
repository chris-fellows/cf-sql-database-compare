using CFSQLDatabaseCompare.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CFSQLDatabaseCompare.Utilities
{
    internal static class InternalUtilities
    {       
        public static string GetConnectionString(DatabaseInfo databaseInfo)
        {
            if (String.IsNullOrEmpty(databaseInfo.UserName))
            {
                return string.Format("Server={0};Database={1};Trusted_Connection=True;",
                                        databaseInfo.ServerName, databaseInfo.DatabaseName);
            }
            return string.Format("Server={0};Database={1};User Id={2}; Password={3};", 
                            databaseInfo.ServerName, databaseInfo.DatabaseName, databaseInfo.UserName, databaseInfo.Password);
        }
    }
}
