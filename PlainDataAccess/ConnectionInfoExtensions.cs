namespace PlainDataAccess
{
    public static class ConnectionInfoExtensions
    {
        public static Query Query(this IConnectionInfo connectionInfo) => new(connectionInfo);

        public static Query Query(this IConnectionInfo connectionInfo, string queryText)
            => connectionInfo.Query().Append(queryText);
        
        public static Query Query<T>(this IConnectionInfo connectionInfo, string queryText, T param)
            => connectionInfo.Query().Append(queryText, param);
    }
}