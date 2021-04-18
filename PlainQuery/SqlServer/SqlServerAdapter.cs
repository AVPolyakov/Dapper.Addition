namespace PlainQuery.SqlServer
{
    internal class SqlServerAdapter : ISqlAdapter
    {
        public bool NullabilityCheckEnabled => true;
        
        public string InsertQueryText(string table, string columnsClause, string valuesClause, string outClause) => $@"
INSERT INTO {table} ({columnsClause}) 
OUTPUT inserted.{outClause}
VALUES ({valuesClause})";

        public string EscapedName(string name) => $"[{name}]";
    }
}