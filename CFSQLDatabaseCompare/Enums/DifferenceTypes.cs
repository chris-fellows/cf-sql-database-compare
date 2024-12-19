using System.ComponentModel;

namespace CFSQLDatabaseCompare.Enums
{
    public enum DifferenceTypes : short
    {
        [Description("Data type is different")]
        ColumnDataTypeDifferent = 1,
        [Description("Text different")]
        ObjectTextDifferent = 2,

        [Description("Object not in 1")]
        ObjectNotIn1 = 3,
        [Description("Object not in 2")]
        ObjectNotIn2 = 4,

        [Description("Column nullability")]
        ColumnNullability = 5,
        [Description("Column identity")]
        ColumnIdentity = 6,
        [Description("Column primary key")]
        ColumnPrimaryKey = 7,
        [Description("Column precision")]
        ColumnNumericPrecision = 8,
        [Description("Column scale")]
        ColumnNumericScale = 9,
        [Description("Column length")]
        ColumnMaxLength = 10,
        [Description("Column identity seed")]
        ColumnIdentitySeed = 11,
        [Description("Column identity increment")]
        ColumnIdentityIncrement = 12,
        [Description("Column ANSI padding")]
        ColumnAnsiPaddingStatus = 13,

        [Description("Encryption")]
        Encrpyted = 14,
        [Description("Collation")]
        Collation = 15,

        [Description("ANSI NULL")]
        AnsiNullStatus = 100,
        [Description("Quoted identifier")]
        QuotedIdentifierStatus = 101,

        [Description("Row count")]
        TableRowCounts = 200,
        [Description("Record not found")]
        TableRecordNotFound = 201,
        [Description("Row column values different")]
        TableRowColumnsDifferent = 202,

        [Description("ANSI NULL default")]
        DatabaseAnsiNullsDefault = 300,
        [Description("ANSI padding default")]
        DatabaseAnsiPaddingDefault = 301,
        [Description("ANSI NULL enabled")]
        DatabaseAnsiNullsEnabled = 302,
        [Description("ANSI warnings")]
        DatabaseAnsiWarningsEnabled = 303,
        [Description("Arithmetic abort")]
        DatabaseArithmeticAbortEnabled = 304,
        [Description("Auto close")]
        DatabaseAutoClose = 305,
        [Description("Auto shrink")]
        DatabaseAutoShrink = 306,

        [Description("Index fill factor")]
        IndexFillFactor = 400,
        [Description("Clustered index")]
        IndexClustered = 401,
        [Description("Index type")]
        IndexType = 402
    }
}
