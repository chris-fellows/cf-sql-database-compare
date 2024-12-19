using CFSQLDatabaseCompare.Models;

namespace CFSQLDatabaseCompare.Interfaces
{
    /// <summary>
    /// Compares schema in 2 SQL databases
    /// </summary>
    public interface ISQLSchemaComparer
    {
        List<Difference> Compare(SchemaCompareOptions schemaCompareOptions, CancellationToken cancellationToken);
    }
}
