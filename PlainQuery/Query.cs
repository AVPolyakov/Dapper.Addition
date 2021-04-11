using System.Collections.Generic;
using System.Text;

namespace PlainQuery
{
    public class Query
    {
        public StringBuilder StringBuilder { get; } = new();

        public List<object> Parameters { get; }

        public Query(List<object> parameters) => Parameters = parameters;

        public Query() :this(new List<object>())
        {
        }

        public Query(string queryText): this() => this.Append(queryText);

        public Query(string queryText, object param): this() => this.Append(queryText, param);

        public override string ToString() => StringBuilder.ToString();
    }
}