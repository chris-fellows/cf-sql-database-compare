using CFSQLDatabaseCompare.Enums;

namespace CFSQLDatabaseCompare.Models
{
    /// <summary>
    /// Details of a database object
    /// </summary>
    public class SQLObjectInfo
    {               
        public string ObjectName { get; set; }
        public SQLObjectTypes ObjectType { get; set; }


        public SQLObjectInfo(SQLObjectTypes objectType,string name)
        {
            ObjectType = objectType;
            ObjectName = name;
        }
        
        public bool IsSameAs(SQLObjectInfo objectInfo)
        {
            return objectInfo != null && 
                objectInfo.ObjectType == this.ObjectType && 
                objectInfo.ObjectName.Equals(this.ObjectName);
        }
    }
}
