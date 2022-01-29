using System.Text;

namespace Dapper.Addition
{
    public class Sql
    {
        private readonly StringBuilder _stringBuilder = new();

        internal Sql(Parameters parameters) => Parameters = parameters;

        public Sql() : this(new Parameters())
        {
        }

        public Sql(string sqlText): this() => Append(sqlText);

        public Sql(string sqlText, object param): this() => Append(sqlText, param);
        
        public string Text => _stringBuilder.ToString();

        public Parameters Parameters { get; }

        public Sql Append(string sqlText)
        {
            _stringBuilder.Append(sqlText);
            return this;
        }
        
        public Sql Append(string sqlText, object param) => Append(sqlText).AddParameters(param);
        
        /// <summary>
        /// Adds parameters in format <code>new {A = 1, B = 2}</code> 
        /// </summary>
        public Sql AddParameters(object param)
        {
            Parameters.AddParameters(param);
            return this;
        }
        
        public override string ToString() => Text;
        
        public static bool MappingCheckEnabled { get; set; }
    }
}