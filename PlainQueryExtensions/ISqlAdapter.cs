namespace PlainQueryExtensions
{
    public interface ISqlAdapter
    {
        bool CheckNullabilityEnabled { get; }
        string InsertQueryText(string table, string columnsClause, string valuesClause, string outClause);
    }
}