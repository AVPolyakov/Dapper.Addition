namespace PlainDataAccess
{
    internal class ColumnInfo
    {
        public string ColumnName { get; }
        public bool IsKey { get; }
        public bool IsAutoIncrement { get; }

        public ColumnInfo(string columnName, bool isKey, bool isAutoIncrement)
        {
            ColumnName = columnName;
            IsKey = isKey;
            IsAutoIncrement = isAutoIncrement;
        }
    }
}