using CFSQLDatabaseCompare.Models;

namespace CFSQLDatabaseCompare.Interfaces
{
    /// <summary>
    /// Compares data in 2 SQL databases
    /// </summary>
    public interface ISQLDataComparer
    {
        List<Difference> Compare(DataCompareOptions dataCompareOptions, CancellationToken cancellationToken);
    }
}
