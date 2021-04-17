namespace PlainQueryExtensions
{
    public interface ISqlAdapter
    {
        bool NullabilityCheckEnabled { get; }
        
        string InsertQueryText(string table, string columnsClause, string valuesClause, string outClause);
        
        string EscapedName(string name);
    }
}