using CFSQLDatabaseCompare.Enums;
using CFSQLDatabaseCompare.Models;
using CFSQLDatabaseCompare.Utilities;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Common;
using Microsoft.Data.SqlClient;
//using CFUtilities;
using SQLIndex = Microsoft.SqlServer.Management.Smo.Index;

namespace CFSQLDatabaseCompare.Comparer.MSSQL
{
    /// <summary>
    /// Compares schema in 2 SQL Server databases
    /// </summary>
    public class MSSQLSchemaComparer : MSSQLComparerBase
    {
        public delegate void Status(object sender, string status);
        public Status? OnStatus;

        public List<Difference> Compare(SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {
            var differences = new List<Difference>();

            // Open database 1
            DisplayStatus(string.Format("Opening database {0}", databaseCompareOptions.DatabaseInfo1.DisplayName));
            var server1 = ConnectToServer(databaseCompareOptions.DatabaseInfo1.ServerName, databaseCompareOptions.DatabaseInfo1.UserName, databaseCompareOptions.DatabaseInfo1.Password);
            _database1 = SmoUtilities.GetDatabaseByName(server1, databaseCompareOptions.DatabaseInfo1.DatabaseName);

            // Open database 2
            DisplayStatus(string.Format("Opening database {0}", databaseCompareOptions.DatabaseInfo2.DisplayName));
            var server2 = ConnectToServer(databaseCompareOptions.DatabaseInfo2.ServerName, databaseCompareOptions.DatabaseInfo2.UserName, databaseCompareOptions.DatabaseInfo2.Password);
            _database2 = SmoUtilities.GetDatabaseByName(server2, databaseCompareOptions.DatabaseInfo2.DatabaseName);

            // Compare database
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareDatabase(_database1, _database2, databaseCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Compare user defined types
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareUserDefinedTypes(_database1, _database2, databaseCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Compare tables
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareTables(_database1, _database2, databaseCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Compare views
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareViews(_database1, _database2, databaseCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Compare stored procedures
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareStoredProcedures(_database1, _database2, databaseCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Compare user defined functions
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareUserDefinedFunctions(_database1, _database2, databaseCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Compare triggers
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareDatabaseDdlTriggers(_database1, _database2, databaseCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Compare synonyms
            if (!cancellationToken.IsCancellationRequested)
            {
                var currentDifferences = CompareSynonyms(_database1, _database2, databaseCompareOptions, cancellationToken);
                currentDifferences.ForEach(item => differences.Add(item));
            }

            // Clean up
            server1.ConnectionContext.Disconnect();
            server2.ConnectionContext.Disconnect();
            return differences;
        }

        private List<Difference> CompareSynonyms(Database database1, Database database2, SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {            
            DisplayStatus("Comparing synonyms");
            var differenceList = new List<Difference>();
            var objectsChecked = new Dictionary<string, string>();

            foreach (Synonym synonym1 in database1.Synonyms)
            {
                DisplayStatus(string.Format("Comparing synonym {0}", synonym1.Name));

                var synonym2 = SmoUtilities.GetSynonymByName(database2.Synonyms, synonym1.Name);
                objectsChecked.Add(synonym1.Name, synonym1.Name);

                var tableDifferencesList = CompareSynonym(database1, synonym1, database2, synonym2, databaseCompareOptions);
                tableDifferencesList.ForEach(item => differenceList.Add(item));
            }

            foreach (Synonym synonym2 in database2.Synonyms)
            {
                if (!objectsChecked.ContainsKey(synonym2.Name))
                {
                    var tableDifferenceList = CompareSynonym(database1, null, database2, synonym2, databaseCompareOptions);
                    tableDifferenceList.ForEach(item => differenceList.Add(item));
                }
            }
            return differenceList;
        }

        private List<Difference> CompareTables(Database database1, Database database2, SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {
            DisplayStatus("Comparing tables");
            List<Difference> differenceList = new List<Difference>();
            Dictionary<string, string> tablesChecked = new Dictionary<string, string>();

            foreach (Table table1 in database1.Tables)
            {
                if (!table1.IsSystemObject)
                {
                    DisplayStatus(string.Format("Comparing table {0}", table1.Name));

                    var table2 = SmoUtilities.GetTableByName(database2.Tables, table1.Name);
                    tablesChecked.Add(table1.Name, table1.Name);

                    var tableDifferencesList = CompareTable(database1, table1, database2, table2, databaseCompareOptions);
                    tableDifferencesList.ForEach(item => differenceList.Add(item));
                }
            }

            foreach (Table table2 in database2.Tables)
            {
                if (!table2.IsSystemObject)
                {
                    if (!tablesChecked.ContainsKey(table2.Name))
                    {
                        var tableDifferenceList = CompareTable(database1, null, database2, table2, databaseCompareOptions);
                        tableDifferenceList.ForEach(item => differenceList.Add(item));
                    }
                }
            }
            return differenceList;
        }

        //public static bool IsAnyDifferencesForObject(ObjectInfo objectInfo, List<Difference> differenceList)
        //{
        //    // Get all differences for this object
        //    var objectDifferenceList = differenceList.FindAll(item => (item.ObjectInfo.IsSameAs(objectInfo)));

        //    // Get all differences for child objects
        //    var allChildDifferenceList = differenceList.FindAll(item => (item.ParentObjectInfo != null && item.ParentObjectInfo.IsSameAs(objectInfo)));

        //    return objectDifferenceList.Count + allChildDifferenceList.Count > 0;
        //}

        private List<Difference> CompareStoredProcedures(Database database1, Database database2, SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {           
            DisplayStatus("Comparing stored procedures");
            var differenceList = new List<Difference>();
            var objectsChecked = new Dictionary<string, string>();

            foreach (StoredProcedure storedProcedure1 in database1.StoredProcedures)
            {
                if (!storedProcedure1.IsSystemObject)
                {
                    DisplayStatus(string.Format("Comparing stored procedure {0}", storedProcedure1.Name));

                    var storedProcedure2 = SmoUtilities.GetStoredProcedureByName(database2.StoredProcedures, storedProcedure1.Name);
                    objectsChecked.Add(storedProcedure1.Name, storedProcedure1.Name);

                    var storedProcedureDifferenceList = CompareStoredProcedure(database1, storedProcedure1, database2, storedProcedure2, databaseCompareOptions);
                    storedProcedureDifferenceList.ForEach(item => differenceList.Add(item));
                }
            }

            foreach (StoredProcedure storedProcedure2 in database2.StoredProcedures)
            {
                if (!storedProcedure2.IsSystemObject)
                {
                    if (!objectsChecked.ContainsKey(storedProcedure2.Name))
                    {
                        var storedProcedureDifferenceList = CompareStoredProcedure(database1, null, database2, storedProcedure2, databaseCompareOptions);
                        storedProcedureDifferenceList.ForEach(item => differenceList.Add(item));
                    }
                }
            }
            return differenceList;
        }

        private List<Difference> CompareUserDefinedTypes(Database database1, Database database2, SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {          
            DisplayStatus("Comparing user defined types");
            var differenceList = new List<Difference>();

            var objectsChecked = new Dictionary<string, string>();

            foreach (UserDefinedType userDefinedType1 in database1.UserDefinedTypes)
            {
                var userDefinedType2 = SmoUtilities.GetUserDefinedTypeByName(database2.UserDefinedTypes, userDefinedType1.Name);
                objectsChecked.Add(userDefinedType1.Name, userDefinedType1.Name);

                var viewDifferenceList = CompareUserDefinedType(database1, userDefinedType1, database2, userDefinedType2, databaseCompareOptions);
                viewDifferenceList.ForEach(item => differenceList.Add(item));
            }

            foreach (UserDefinedType userDefinedType2 in database2.UserDefinedTypes)
            {
                if (!objectsChecked.ContainsKey(userDefinedType2.Name))
                {
                    var udtDifferenceList = CompareUserDefinedType(database1, null, database2, userDefinedType2, databaseCompareOptions);
                    udtDifferenceList.ForEach(item => differenceList.Add(item));
                }
            }
            return differenceList;
        }

        private List<Difference> CompareViews(Database database1, Database database2, SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {           
            DisplayStatus("Comparing views");
            var differenceList = new List<Difference>();
            var objectsChecked = new Dictionary<string, string>();

            foreach (View view1 in database1.Views)
            {
                if (!view1.IsSystemObject)
                {
                    DisplayStatus(string.Format("Comparing view {0}", view1.Name));

                    var view2 = SmoUtilities.GetViewByName(database2.Views, view1.Name);
                    objectsChecked.Add(view1.Name, view1.Name);

                    var viewDifferenceList = CompareView(database1, view1, database2, view2, databaseCompareOptions);
                    viewDifferenceList.ForEach(item => differenceList.Add(item));
                }
            }

            foreach (View view2 in database2.Views)
            {
                if (!view2.IsSystemObject)
                {
                    if (!objectsChecked.ContainsKey(view2.Name))
                    {
                        var viewDifferenceList = CompareView(database1, null, database2, view2, databaseCompareOptions);
                        viewDifferenceList.ForEach(item => differenceList.Add(item));
                    }
                }
            }
            return differenceList;
        }

        private List<Difference> CompareUserDefinedFunctions(Database database1, Database database2, SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {           
            DisplayStatus("Comparing user defined functions");
            var differenceList = new List<Difference>();
            var objectsChecked = new Dictionary<string, string>();

            foreach (UserDefinedFunction userDefinedFunction1 in database1.UserDefinedFunctions)
            {
                if (!userDefinedFunction1.IsSystemObject)
                {
                    DisplayStatus(string.Format("Comparing user defined function {0}", userDefinedFunction1.Name));

                    var userDefinedFunction2 = SmoUtilities.GetUserDefinedFunctionViewByName(database2.UserDefinedFunctions, userDefinedFunction1.Name);
                    objectsChecked.Add(userDefinedFunction1.Name, userDefinedFunction1.Name);

                    var userDefinedFunctionDifferenceList = CompareUserDefinedFunction(database1, userDefinedFunction1, database2, userDefinedFunction2, databaseCompareOptions);
                    userDefinedFunctionDifferenceList.ForEach(item => differenceList.Add(item));
                }
            }

            foreach (UserDefinedFunction userDefinedFunction2 in database2.UserDefinedFunctions)
            {
                if (!userDefinedFunction2.IsSystemObject)
                {
                    if (!objectsChecked.ContainsKey(userDefinedFunction2.Name))
                    {
                        var userDefinedFunctionDifferenceList = CompareUserDefinedFunction(database1, null, database2, userDefinedFunction2, databaseCompareOptions);
                        userDefinedFunctionDifferenceList.ForEach(item => differenceList.Add(item));
                    }
                }
            }
            return differenceList;
        }

        /*
        private List<Difference> CompareTriggers(Database database1, Database database2, DatabaseCompareOptions databaseCompareOptions)
        {
            List<Difference> differenceList = new List<Difference>();
            Dictionary<string, string> objectsChecked = new Dictionary<string, string>();

            foreach (Trigger trigger1 in database1.Triggers)
            {                
                Trigger trigger2 = SmoUtilities.GetTriggerByName(database2.Triggers, trigger1.Name);
                objectsChecked.Add(trigger1.Name, trigger2.Name);

                List<Difference> triggerDifferenceList = CompareTrigger(database1, trigger1, database2, trigger2, databaseCompareOptions);
                triggerDifferenceList.ForEach(item => differenceList.Add(item));
            }

            foreach (Trigger trigger2 in database2.Triggers)
            {
                if (!objectsChecked.ContainsKey(trigger2.Name))
                {
                    List<Difference> triggerDifferenceList = CompareTrigger(database1, null, database2, trigger2, databaseCompareOptions);
                    triggerDifferenceList.ForEach(item => differenceList.Add(item));
                }
            }
            return differenceList;
        }
        */

        private List<Difference> CompareDatabaseDdlTriggers(Database database1, Database database2, SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {          
            DisplayStatus("Comparing triggers");
            var differenceList = new List<Difference>();
            var objectsChecked = new Dictionary<string, string>();

            foreach (DatabaseDdlTrigger trigger1 in database1.Triggers)
            {
                if (!trigger1.IsSystemObject)
                {
                    DisplayStatus(string.Format("Comparing trigger {0}", trigger1.Name));

                    var trigger2 = SmoUtilities.GetDatabaseDdlTriggerByName(database2.Triggers, trigger1.Name);
                    objectsChecked.Add(trigger1.Name, trigger2.Name);

                    var triggerDifferenceList = CompareDatabaseDdlTrigger(database1, trigger1, database2, trigger2, databaseCompareOptions);
                    triggerDifferenceList.ForEach(item => differenceList.Add(item));
                }
            }

            foreach (DatabaseDdlTrigger trigger2 in database2.Triggers)
            {
                if (!trigger2.IsSystemObject)
                {
                    if (!objectsChecked.ContainsKey(trigger2.Name))
                    {
                        var triggerDifferenceList = CompareDatabaseDdlTrigger(database1, null, database2, trigger2, databaseCompareOptions);
                        triggerDifferenceList.ForEach(item => differenceList.Add(item));
                    }
                }
            }
            return differenceList;
        }

        public List<SQLObjectInfo> GetObjectInfoList(SQLObjectTypes objectType)
        {
            var objectInfoList = new List<SQLObjectInfo>();

            switch (objectType)
            {
                case SQLObjectTypes.Database:
                    objectInfoList.Add(GetObjectInfo(_database1));
                    objectInfoList.Add(GetObjectInfo(_database2));
                    break;
                case SQLObjectTypes.UserTable:
                    foreach (Table table in _database1.Tables)
                    {
                        objectInfoList.Add(GetObjectInfo(table));
                    }
                    foreach (Table table in _database2.Tables)
                    {
                        objectInfoList.Add(GetObjectInfo(table));
                    }
                    break;
                case SQLObjectTypes.View:
                    foreach (View view in _database1.Views)
                    {
                        objectInfoList.Add(GetObjectInfo(view));
                    }
                    foreach (View view in _database2.Views)
                    {
                        objectInfoList.Add(GetObjectInfo(view));
                    }
                    break;
                case SQLObjectTypes.StoredProcedure:
                    foreach (StoredProcedure storedProcedure in _database1.StoredProcedures)
                    {
                        objectInfoList.Add(GetObjectInfo(storedProcedure));
                    }
                    foreach (StoredProcedure storedProcedure in _database2.StoredProcedures)
                    {
                        objectInfoList.Add(GetObjectInfo(storedProcedure));
                    }
                    break;
                case SQLObjectTypes.Trigger:
                    foreach (DatabaseDdlTrigger trigger in _database1.Triggers)
                    {
                        objectInfoList.Add(GetObjectInfo(trigger));
                    }
                    foreach (DatabaseDdlTrigger trigger in _database2.Triggers)
                    {
                        objectInfoList.Add(GetObjectInfo(trigger));
                    }
                    break;
                case SQLObjectTypes.UserDefinedFunction:
                    foreach (UserDefinedFunction userDefinedFunction in _database1.UserDefinedFunctions)
                    {
                        objectInfoList.Add(GetObjectInfo(userDefinedFunction));
                    }
                    foreach (UserDefinedFunction userDefinedFunction in _database2.UserDefinedFunctions)
                    {
                        objectInfoList.Add(GetObjectInfo(userDefinedFunction));
                    }
                    break;
                case SQLObjectTypes.UserDefinedType:
                    foreach (UserDefinedType userDefinedType in _database1.UserDefinedTypes)
                    {
                        objectInfoList.Add(GetObjectInfo(userDefinedType));
                    }
                    foreach (UserDefinedType userDefinedType in _database2.UserDefinedTypes)
                    {
                        objectInfoList.Add(GetObjectInfo(userDefinedType));
                    }
                    break;
                case SQLObjectTypes.Synonym:
                    foreach (Synonym synonym in _database1.Synonyms)
                    {
                        objectInfoList.Add(GetObjectInfo(synonym));
                    }
                    foreach (Synonym synonym in _database2.Synonyms)
                    {
                        objectInfoList.Add(GetObjectInfo(synonym));
                    }
                    break;
            }

            objectInfoList.Sort((x, y) => x.ObjectName.CompareTo(y.ObjectName));    // Name sort
            return objectInfoList;
        }

        private List<Difference> CompareStoredProcedure(Database database1, StoredProcedure? storedProcedure1, Database database2, StoredProcedure? storedProcedure2, SchemaCompareOptions databaseCompareOptions)
        {
            List<Difference> differenceList = new List<Difference>();

            if (storedProcedure1 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.StoredProcedure, storedProcedure2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(differnce);
                return differenceList;
            }
            else if (storedProcedure2 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database2.Name), new SQLObjectInfo(SQLObjectTypes.StoredProcedure, storedProcedure1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(differnce);
                return differenceList;
            }

            if (storedProcedure1.AnsiNullsStatus != storedProcedure2.AnsiNullsStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.StoredProcedure, storedProcedure1.Name), DifferenceTypes.AnsiNullStatus);
                differenceList.Add(difference);
            }

            if (storedProcedure1.QuotedIdentifierStatus != storedProcedure2.QuotedIdentifierStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.StoredProcedure, storedProcedure1.Name), DifferenceTypes.QuotedIdentifierStatus);
                differenceList.Add(difference);
            }

            if (storedProcedure1.IsEncrypted != storedProcedure2.IsEncrypted)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.StoredProcedure, storedProcedure1.Name), DifferenceTypes.Encrpyted);
                differenceList.Add(difference);
            }

            try
            {
                if (storedProcedure1.TextBody != storedProcedure2.TextBody)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.StoredProcedure, storedProcedure1.Name), DifferenceTypes.ObjectTextDifferent);
                    differenceList.Add(difference);
                }
            }
            catch (Microsoft.SqlServer.Management.Smo.PropertyNotSetException exception)
            {
                // Ignore
            }
            return differenceList;
        }

        private List<Difference> CompareUserDefinedFunction(Database database1, UserDefinedFunction? userDefinedFunction1, Database database2, UserDefinedFunction? userDefinedFunction2, SchemaCompareOptions databaseCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (userDefinedFunction1 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserDefinedFunction, userDefinedFunction2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(differnce);
                return differenceList;
            }
            else if (userDefinedFunction2 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database2.Name), new SQLObjectInfo(SQLObjectTypes.UserDefinedFunction, userDefinedFunction1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(differnce);
                return differenceList;
            }

            if (userDefinedFunction1.QuotedIdentifierStatus != userDefinedFunction2.QuotedIdentifierStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserDefinedFunction, userDefinedFunction1.Name), DifferenceTypes.QuotedIdentifierStatus);
                differenceList.Add(difference);
            }

            if (userDefinedFunction1.AnsiNullsStatus != userDefinedFunction2.AnsiNullsStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserDefinedFunction, userDefinedFunction1.Name), DifferenceTypes.AnsiNullStatus);
                differenceList.Add(difference);
            }

            if (userDefinedFunction1.IsEncrypted != userDefinedFunction2.IsEncrypted)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserDefinedFunction, userDefinedFunction1.Name), DifferenceTypes.Encrpyted);
                differenceList.Add(difference);
            }

            try
            {
                if (userDefinedFunction1.TextBody != userDefinedFunction2.TextBody)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserDefinedFunction, userDefinedFunction1.Name), DifferenceTypes.ObjectTextDifferent);
                    differenceList.Add(difference);
                }
            }
            catch (Microsoft.SqlServer.Management.Smo.PropertyNotSetException exception)
            {
                // Ignore
            }
            return differenceList;
        }

        private List<Difference> CompareSynonym(Database database1, Synonym? synonym1, Database database2, Synonym? synonym2, SchemaCompareOptions databaseCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (synonym1 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Synonym, synonym2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(differnce);
                return differenceList;
            }
            else if (synonym2 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database2.Name), new SQLObjectInfo(SQLObjectTypes.Synonym, synonym1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(differnce);
                return differenceList;
            }
            return differenceList;
        }

        private List<Difference> CompareView(Database database1, View? view1, Database database2, View? view2, SchemaCompareOptions databaseCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (view1 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.View, view2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(differnce);
                return differenceList;
            }
            else if (view2 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database2.Name), new SQLObjectInfo(SQLObjectTypes.View, view1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(differnce);
                return differenceList;
            }

            if (view1.QuotedIdentifierStatus != view2.QuotedIdentifierStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.View, view1.Name), DifferenceTypes.QuotedIdentifierStatus);
                differenceList.Add(difference);
            }

            if (view1.IsEncrypted != view2.IsEncrypted)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.View, view1.Name), DifferenceTypes.Encrpyted);
                differenceList.Add(difference);
            }

            if (view1.AnsiNullsStatus != view2.AnsiNullsStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.View, view1.Name), DifferenceTypes.AnsiNullStatus);
                differenceList.Add(difference);
            }

            try
            {
                if (view1.TextBody != view2.TextBody)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.View, view1.Name), DifferenceTypes.ObjectTextDifferent);
                    differenceList.Add(difference);
                }
            }
            catch (Microsoft.SqlServer.Management.Smo.PropertyNotSetException exception)
            {
                // Ignore
            }
            return differenceList;
        }

        private List<Difference> CompareUserDefinedType(Database database1, UserDefinedType? userDefinedType1, Database database2, UserDefinedType? userDefinedType2, SchemaCompareOptions databaseCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (userDefinedType1 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserDefinedType, userDefinedType2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(differnce);
                return differenceList;
            }
            else if (userDefinedType2 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database2.Name), new SQLObjectInfo(SQLObjectTypes.UserDefinedType, userDefinedType1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(differnce);
                return differenceList;
            }
            return differenceList;
        }

        private List<Difference> CompareTrigger(Database database1, Trigger? trigger1, Database database2, Trigger? trigger2, SchemaCompareOptions databaseCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (trigger1 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(differnce);
                return differenceList;
            }
            else if (trigger2 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database2.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(differnce);
                return differenceList;
            }

            if (trigger1.QuotedIdentifierStatus != trigger2.QuotedIdentifierStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.QuotedIdentifierStatus);
                differenceList.Add(difference);
            }

            if (trigger1.AnsiNullsStatus != trigger2.AnsiNullsStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.AnsiNullStatus);
                differenceList.Add(difference);
            }

            if (trigger1.IsEncrypted != trigger2.IsEncrypted)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.Encrpyted);
                differenceList.Add(difference);
            }

            try
            {
                if (trigger1.TextBody != trigger2.TextBody)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.ObjectTextDifferent);
                    differenceList.Add(difference);
                }
            }
            catch (Microsoft.SqlServer.Management.Smo.PropertyNotSetException exception)
            {
                // Ignore
            }
            return differenceList;
        }

        private List<Difference> CompareDatabase(Database database1, Database database2, SchemaCompareOptions databaseCompareOptions, CancellationToken cancellationToken)
        {          
            var differenceList = new List<Difference>();

            if (database1.AnsiNullDefault != database2.AnsiNullDefault)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Server, ""), new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), DifferenceTypes.DatabaseAnsiNullsDefault);
                differenceList.Add(differnce);
            }
            if (database1.AnsiPaddingEnabled != database2.AnsiPaddingEnabled)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Server, ""), new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), DifferenceTypes.DatabaseAnsiPaddingDefault);
                differenceList.Add(differnce);
            }
            if (database1.AnsiNullsEnabled != database2.AnsiNullsEnabled)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Server, ""), new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), DifferenceTypes.DatabaseAnsiNullsEnabled);
                differenceList.Add(differnce);
            }
            if (database1.AnsiWarningsEnabled != database2.AnsiWarningsEnabled)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Server, ""), new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), DifferenceTypes.DatabaseAnsiWarningsEnabled);
                differenceList.Add(differnce);
            }
            if (database1.ArithmeticAbortEnabled != database2.ArithmeticAbortEnabled)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Server, ""), new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), DifferenceTypes.DatabaseArithmeticAbortEnabled);
                differenceList.Add(differnce);
            }
            if (database1.AutoClose != database2.AutoClose)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Server, ""), new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), DifferenceTypes.DatabaseAutoClose);
                differenceList.Add(differnce);
            }
            if (database1.AutoShrink != database2.AutoShrink)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Server, ""), new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), DifferenceTypes.DatabaseAutoShrink);
                differenceList.Add(differnce);
            }
            if (database1.Collation != database2.Collation)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Server, ""), new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), DifferenceTypes.Collation);
                differenceList.Add(differnce);
            }
            return differenceList;
        }

        private List<Difference> CompareDatabaseDdlTrigger(Database database1, DatabaseDdlTrigger? trigger1, Database database2, DatabaseDdlTrigger? trigger2, SchemaCompareOptions databaseCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (trigger1 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(differnce);
                return differenceList;
            }
            else if (trigger2 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database2.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(differnce);
                return differenceList;
            }

            if (trigger1.QuotedIdentifierStatus != trigger2.QuotedIdentifierStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.QuotedIdentifierStatus);
                differenceList.Add(difference);
            }

            if (trigger1.AnsiNullsStatus != trigger2.AnsiNullsStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.AnsiNullStatus);
                differenceList.Add(difference);
            }

            if (trigger1.IsEncrypted != trigger2.IsEncrypted)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.Encrpyted);
                differenceList.Add(difference);
            }

            try
            {
                if (trigger1.TextBody != trigger2.TextBody)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.Trigger, trigger1.Name), DifferenceTypes.ObjectTextDifferent);
                    differenceList.Add(difference);
                }
            }
            catch (Microsoft.SqlServer.Management.Smo.PropertyNotSetException exception)
            {
                // Ignore
            }
            return differenceList;
        }

        //private void WriteProfile(string name, long elapsed)
        //{
        //    System.Diagnostics.Debug.WriteLine(string.Format("{0}: {1} ms", name, elapsed));
        //}

        private List<Difference> CompareTable(Database database1, Table? table1, Database database2, Table? table2, SchemaCompareOptions schemaCompareOptions)
        {
            var differenceList = new List<Difference>();

            if (table1 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(differnce);
                return differenceList;
            }
            else if (table2 == null)
            {
                var differnce = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, database2.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(differnce);
                return differenceList;
            }
            else
            {
                // Compare columns
                var columnNamesChecked = new Dictionary<string, string>();
                foreach (Column column1 in table1.Columns)
                {
                    columnNamesChecked.Add(column1.Name, column1.Name);
                    var column2 = SmoUtilities.GetColumnByName(table2 == null ? null : table2.Columns, column1.Name);
                    var columnDifferenceList = CompareTableColumn(table1, column1, table2, column2, schemaCompareOptions);
                    columnDifferenceList.ForEach(item => differenceList.Add(item));
                }

                // Compare columns in db 2 that aren't in db 1
                foreach (Column column2 in table2.Columns)
                {
                    if (!columnNamesChecked.ContainsKey(column2.Name))
                    {
                        var column1 = SmoUtilities.GetColumnByName(table1 == null ? null : table1.Columns, column2.Name);
                        var columnDifferenceList = CompareTableColumn(table1, column1, table2, column2, schemaCompareOptions);
                        columnDifferenceList.ForEach(item => differenceList.Add(item));
                    }
                }

                if (table1.QuotedIdentifierStatus != table2.QuotedIdentifierStatus)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), DifferenceTypes.QuotedIdentifierStatus);
                    differenceList.Add(difference);
                }

                if (table1.AnsiNullsStatus != table2.AnsiNullsStatus)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.Database, database1.Name), new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), DifferenceTypes.AnsiNullStatus);
                    differenceList.Add(difference);
                }

                // Compare foreign keys
                //_profiling.ProfileStart("Table.ForeignKeys");
                var foreignKeysChecked = new List<string>();
                foreach (ForeignKey foreignKey1 in table1.ForeignKeys)
                {
                    foreignKeysChecked.Add(foreignKey1.Name);
                    var foreignKey2 = SmoUtilities.GetForiengKeyByName(table2.ForeignKeys, foreignKey1.Name);
                    if (foreignKey2 == null)
                    {
                        var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.ForeignKey, foreignKey1.Name), DifferenceTypes.ObjectNotIn2);
                        differenceList.Add(difference);
                    }
                }
                foreach (ForeignKey foreignKey2 in table2.ForeignKeys)
                {
                    if (!foreignKeysChecked.Contains(foreignKey2.Name))
                    {
                        var foreignKey1 = SmoUtilities.GetForiengKeyByName(table1.ForeignKeys, foreignKey2.Name);
                        if (foreignKey1 == null)
                        {
                            var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table2.Name), new SQLObjectInfo(SQLObjectTypes.ForeignKey, foreignKey2.Name), DifferenceTypes.ObjectNotIn1);
                            differenceList.Add(difference);
                        }
                    }
                }
                //WriteProfile("Table.ForeignKeys", _profiling.GetProfileElapsed("Table.ForeignKeys"));

                // Compare indices
                bool doIndexes = false;
                var indexesChecked = new Dictionary<string, string>();
                if (doIndexes)
                {
                    //_profiling.ProfileStart("Table.Indices");
                    foreach (SQLIndex index1 in table1.Indexes)
                    {
                        string indexKey = GetIndexColumnsKey(index1);
                        System.Diagnostics.Debug.WriteLine(string.Format("Table={0}, Index={1}, Type={2}", table1.Name, indexKey, index1.IndexType));
                        if (!indexesChecked.ContainsKey(indexKey))
                        {
                            indexesChecked.Add(indexKey, indexKey);

                            string[] columnNames = new string[index1.IndexedColumns.Count];
                            int count = -1;
                            foreach (IndexedColumn indexedColumn1 in index1.IndexedColumns)
                            {
                                count++;
                                columnNames[count] = indexedColumn1.Name;
                            }
                            var index2 = SmoUtilities.GetIndexByColumns(table2.Indexes, index1.IndexType, columnNames);
                            if (index2 == null)
                            {
                                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.Index, index1.Name), DifferenceTypes.ObjectNotIn2);
                                differenceList.Add(difference);
                            }
                            else
                            {
                                if (index1.FillFactor != index2.FillFactor)
                                {
                                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.Index, index1.Name), DifferenceTypes.IndexFillFactor);
                                    differenceList.Add(difference);
                                }
                                if (index1.IsClustered != index2.IsClustered)
                                {
                                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.Index, index1.Name), DifferenceTypes.IndexClustered);
                                    differenceList.Add(difference);
                                }
                                if (index1.IndexType != index2.IndexType)
                                {
                                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.Index, index1.Name), DifferenceTypes.IndexType);
                                    differenceList.Add(difference);
                                }

                            }
                        }
                    }
                    //WriteProfile("Table.Indices", _profiling.GetProfileElapsed("Table.Indices"));

                    // Check indexed not in db 1
                    //_profiling.ProfileStart("Table.Indexed");
                    foreach (SQLIndex index2 in table2.Indexes)
                    {
                        string indexKey = GetIndexColumnsKey(index2);
                        if (!indexesChecked.ContainsKey(indexKey))
                        {
                            indexesChecked.Add(indexKey, indexKey);
                            Difference difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table2.Name), new SQLObjectInfo(SQLObjectTypes.Index, index2.Name), DifferenceTypes.ObjectNotIn1);
                            differenceList.Add(difference);
                        }
                    }
                    //WriteProfile("Table.Indexed", _profiling.GetProfileElapsed("Table.Indexed"));
                }

                /*
                // Compare data, currently we only check row counts
                if (schemaCompareOptions.CompareTableRowCounts)
                {
                    if (table1.RowCount != table2.RowCount)
                    {
                        var difference = new Difference(new ObjectInfo(ObjectTypes.Database, database1.Name), new ObjectInfo(ObjectTypes.UserTable, table1.Name), Difference.DifferenceTypes.TableRowCounts);
                        difference.Text = string.Format("{0}={1} row(s); {2}={3} row(s)", schemaCompareOptions.DatabaseInfo1.DisplayName, table1.RowCount, schemaCompareOptions.DatabaseInfo2.DisplayName, table2.RowCount);
                        differenceList.Add(difference);
                    }
                }
                */

                // Compare PKs
                /*
                if (databaseCompareOptions.CompareTableRowColumns)
                {                    
                    List<Difference> dataDifferenceList = CompareTablePrimaryKeyValues(database1, table1, database2, table2, databaseCompareOptions);
                    dataDifferenceList.ForEach(item => differenceList.Add(item));                    
                }
                */

                //// Compare row colums
                //if (databaseCompareOptions.CompareTableRows)
                //{
                //    var rowDifferenceList = CompareTableRows(database1, table1, database2, table2, databaseCompareOptions);
                //    rowDifferenceList.ForEach(item => differenceList.Add(item));
                //}            
            }

            System.Diagnostics.Debug.WriteLine("Compared " + table1.Name);

            return differenceList;
        }

        private static string GetIndexColumnsKey(SQLIndex index)
        {
            var columns = new List<string>();
            foreach (IndexedColumn indexedColumn in index.IndexedColumns)
            {
                columns.Add(indexedColumn.Name);
            }
            columns.Sort();

            var columnList = new StringBuilder("");
            for (int index1 = 0; index1 < columns.Count; index1++)
            {
                if (columnList.Length > 0)
                {
                    columnList.Append(",");
                }
                columnList.Append(columns[index1]);
            }
            return string.Format("{0}#{1}", index.IndexType, columnList.ToString());
        }

        private List<Difference> CompareTableColumn(Table table1, Column? column1, Table table2, Column? column2, SchemaCompareOptions schemaCompareOptions)
        {
            var differenceList = new List<Difference>();
            if (column1 == null)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column2.Name), DifferenceTypes.ObjectNotIn1);
                differenceList.Add(difference);
                return differenceList;
            }
            else if (column2 == null)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table2.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ObjectNotIn2);
                differenceList.Add(difference);
                return differenceList;
            }

            if (column1.Collation != column2.Collation)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.Collation);
                differenceList.Add(difference);
            }

            if (column1.Nullable != column2.Nullable)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnNullability);
                differenceList.Add(difference);
            }

            if (column1.AnsiPaddingStatus != column2.AnsiPaddingStatus)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnAnsiPaddingStatus);
                differenceList.Add(difference);
            }

            /*
            if (column1.IsEncrypted != column2.IsEncrypted)
            {
                Difference difference = new Difference(new ObjectInfo(ObjectTypes.UserTable, table1.Name), new ObjectInfo(ObjectTypes.UserTableColumn, column1.Name), Difference.DifferenceTypes.Encrpyted);
                differenceList.Add(difference);
            }            
            */

            if (column1.Identity != column2.Identity)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnIdentity);
                differenceList.Add(difference);
            }

            if (column1.Identity == true && column2.Identity == true)
            {
                if (column1.IdentitySeed != column2.IdentitySeed)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnIdentitySeed);
                    differenceList.Add(difference);
                }

                if (column1.IdentityIncrement != column2.IdentityIncrement)
                {
                    var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnIdentityIncrement);
                    differenceList.Add(difference);
                }
            }

            if (column1.InPrimaryKey != column2.InPrimaryKey)
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnPrimaryKey);
                differenceList.Add(difference);
            }

            // Compare data types
            if (column1.DataType.SqlDataType == column2.DataType.SqlDataType)   // Same data type
            {
                if (column1.DataType.NumericPrecision != column2.DataType.NumericPrecision)
                {
                    var difference1 = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnNumericPrecision);
                    differenceList.Add(difference1);
                }

                if (column1.DataType.NumericScale != column2.DataType.NumericScale)
                {
                    var difference1 = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnNumericScale);
                    differenceList.Add(difference1);
                }

                if (column1.DataType.MaximumLength != column2.DataType.MaximumLength)
                {
                    var difference1 = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnMaxLength);
                    differenceList.Add(difference1);
                }
            }
            else
            {
                var difference = new Difference(new SQLObjectInfo(SQLObjectTypes.UserTable, table1.Name), new SQLObjectInfo(SQLObjectTypes.UserTableColumn, column1.Name), DifferenceTypes.ColumnDataTypeDifferent);
                differenceList.Add(difference);
            }
            return differenceList;
        }
    }
}
