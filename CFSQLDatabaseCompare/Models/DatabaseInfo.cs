using CFSQLDatabaseCompare.Enums;

namespace CFSQLDatabaseCompare.Models
{
    /// <summary>
    /// Database information
    /// </summary>
    public class DatabaseInfo
    {
        public SQLProducts Product { get; set; } = SQLProducts.MSSQL;
        public string ServerName { get; set; } = String.Empty;
        public string UserName { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
        public string DatabaseName { get; set; } = String.Empty;
        public string DisplayName { get; set; } = String.Empty;
    }
}
