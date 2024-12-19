namespace CFSQLDatabaseCompare.Models
{
    /// <summary>
    /// Options for schema compare
    /// </summary>
    public class SchemaCompareOptions
    {       
        public DatabaseInfo DatabaseInfo1 { get; set; } = new DatabaseInfo();
        public DatabaseInfo DatabaseInfo2 { get; set; } = new DatabaseInfo();
    }
}
