
namespace CFSQLDatabaseCompare.Enums
{
    /// <summary>
    /// SQL object types
    /// </summary>
    public enum SQLObjectTypes : byte
    {
        Server = 0,
        Database = 1,
        UserTable = 2,
        UserTableColumn = 3,
        StoredProcedure = 4,
        View = 5,
        Trigger = 6,
        UserDefinedFunction = 7,
        ForeignKey = 8,
        Synonym = 9,
        UserDefinedType = 10,
        Index = 11
    }
}
