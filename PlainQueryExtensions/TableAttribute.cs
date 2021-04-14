using System;

namespace PlainQueryExtensions
{
    /// <summary>
    /// Defines the name of a table in the database
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        public TableAttribute(string tableName) => Name = tableName;

        /// <summary>
        /// The name of the table in the database
        /// </summary>
        public string Name { get; set; }
    }
}