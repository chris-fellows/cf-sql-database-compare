using CFSQLDatabaseCompare.Enums;

namespace CFSQLDatabaseCompare.Models
{
    /// <summary>
    /// Difference between database objects
    /// </summary>
    public class Difference
    {     
        /// <summary>
        /// Object info
        /// </summary>
        public SQLObjectInfo ObjectInfo { get; internal set; }

        /// <summary>
        /// Parent object info. E.g. If difference if for column then parent is table info
        /// </summary>
        public SQLObjectInfo ParentObjectInfo { get; internal set; }
        
        /// <summary>
        /// Difference type
        /// </summary>
        public DifferenceTypes DifferenceType { get; internal set; }
        
        /// <summary>
        /// Difference parameters for extra information
        /// </summary>
        
        public List<DifferenceParameter> Parameters = new List<DifferenceParameter>();

        public Difference(SQLObjectInfo parentObjectInfo, SQLObjectInfo objectInfo, DifferenceTypes differenceType)
        {
            this.ParentObjectInfo = parentObjectInfo;
            this.ObjectInfo = objectInfo;
            this.DifferenceType = differenceType;            
        }
    }
}
