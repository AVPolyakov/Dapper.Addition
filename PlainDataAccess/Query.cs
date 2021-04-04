using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;

namespace PlainDataAccess
{
    public class Query
    {
        public StringBuilder StringBuilder { get; } = new();

        public List<Action<SqlCommand>> DbCommandActions { get; }

        public IConnectionInfo ConnectionInfo { get; }

        public Query(IConnectionInfo connectionInfo, List<Action<SqlCommand>> dbCommandActions)
        {
            ConnectionInfo = connectionInfo;
            DbCommandActions = dbCommandActions;
        }

        public Query(IConnectionInfo connectionInfo) :this (connectionInfo, new List<Action<SqlCommand>>())
        {
        }

        public override string ToString() => StringBuilder.ToString();
    }
}