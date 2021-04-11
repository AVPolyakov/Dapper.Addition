namespace PlainQuery
{
    public static class QueryExtensions
    {
        public static Query Append(this Query query, string queryText)
        {
            query.StringBuilder.Append(queryText);
            return query;
        }
        
        public static Query Append(this Query query, string queryText, object param) 
            => query.Append(queryText).AddParams(param);
        
        public static Query Query(this Query query) => new(query.Parameters);
        
        public static Query Query(this Query query, string queryText)
            => query.Query().Append(queryText);
        
        public static Query Query(this Query query, string queryText, object param)
            => query.Query().Append(queryText, param);
        
        public static Query AddParams(this Query query, object param)
        {
            query.Parameters.Add(param);
            return query;
        }
    }
}