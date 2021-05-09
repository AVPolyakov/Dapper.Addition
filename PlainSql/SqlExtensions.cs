namespace PlainSql
{
    public static class SqlExtensions
    {
        public static Sql Sql(this Sql sql) => new(sql.Parameters);
        
        public static Sql Sql(this Sql sql, string sqlText)
            => sql.Sql().Append(sqlText);
        
        public static Sql Sql(this Sql sql, string sqlText, object param)
            => sql.Sql().Append(sqlText, param);
    }
}