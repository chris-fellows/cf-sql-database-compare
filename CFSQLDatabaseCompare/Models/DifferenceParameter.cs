using CFSQLDatabaseCompare.Enums;

namespace CFSQLDatabaseCompare.Models
{
    /// <summary>
    /// Parameter for difference
    /// </summary>
    public class DifferenceParameter
    {
        /// <summary>
        /// Parameter type
        /// </summary>
        public DifferenceParameterTypes ParameterType { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public object? Value { get; set; }

        /// <summary>
        /// If relates to specific database then either 1 or 2
        /// </summary>
        public short? DatabaseIndex { get; set; } 
    }
}
