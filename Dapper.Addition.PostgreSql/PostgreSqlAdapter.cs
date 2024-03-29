using System;
using System.Data;

namespace Dapper.Addition.PostgreSql
{
    public class PostgreSqlAdapter : ISqlAdapter
    {
        //All columns of a view are defined as nullable, whether they can assume
        //that value or not.  PostgreSQL does not attempt to deduce the nullability
        //of columns from the view definition. 
        //https://www.postgresql-archive.org/is-nullable-column-of-information-schema-columns-table-td6117273.html
        public bool NullabilityCheckEnabled => false;
        
        public string InsertQueryText(string table, string columnsClause, string valuesClause, string outClause) => $@"
INSERT INTO {table} ({columnsClause}) 
VALUES ({valuesClause})
RETURNING {outClause}";
        
        public string EscapedName(string name) => $"\"{name}\"";
        
        public Type GetFieldType(IDataReader reader, int ordinal)
        {
            var row = reader.GetSchemaTable()!.Rows[ordinal];
            if (row["DataTypeName"] is string dataTypeName)
            {
                switch (dataTypeName)
                {
                    case "xid8":
                        return typeof(Xid8);
                }
            }
            
            return reader.GetFieldType(ordinal);
        }
    }
}