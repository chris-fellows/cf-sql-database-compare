namespace CFSQLDatabaseCompare.Models
{
    /// <summary>
    /// Options for data compare
    /// </summary>
    public class DataCompareOptions
    {        
        public DatabaseInfo DatabaseInfo1 { get; set; } = new DatabaseInfo();
        public DatabaseInfo DatabaseInfo2 { get; set; } = new DatabaseInfo();
    }
}
