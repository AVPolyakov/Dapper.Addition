using System;
using System.Data;

namespace Dapper.Addition
{
    public interface ISqlAdapter
    {
        bool NullabilityCheckEnabled { get; }
        
        string InsertQueryText(string table, string columnsClause, string valuesClause, string outClause);
        
        string EscapedName(string name);

        private static ISqlAdapter? _current;
        public static ISqlAdapter Current
        {
            get
            {
                if (_current == null)
                    throw new Exception($"{nameof(ISqlAdapter)}.{nameof(Current)} not set");
                
                return _current;
            }
            set => _current = value;
        }

        Type GetFieldType(IDataReader reader, int ordinal) => reader.GetFieldType(ordinal);
    }
}