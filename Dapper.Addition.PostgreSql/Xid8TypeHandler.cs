using System;
using System.Data;
using Npgsql;
using NpgsqlTypes;

namespace Dapper.Addition.PostgreSql;

public class Xid8TypeHandler : SqlMapper.ITypeHandler
{
    public static void AddToSqlMapper()
    {
        SqlMapper.AddTypeHandler(typeof(Xid8?), new Xid8TypeHandler());
        DataReaderExtensions.AddScalarReadType(typeof(Xid8?));
    }
    
    public void SetValue(IDbDataParameter parameter, object value)
    {
        if (value is null or DBNull)
        {
            parameter.Value = value;
        }
        else
        {
            parameter.Value = ((Xid8)value).Value;
        }
        var npgsqlParameter = (NpgsqlParameter)parameter;
        npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Xid8;
    }

    public object? Parse(Type destinationType, object value)
    {
        if (value is null or DBNull)
        {
            return null;
        }

        return new Xid8((ulong)value);
    }
}