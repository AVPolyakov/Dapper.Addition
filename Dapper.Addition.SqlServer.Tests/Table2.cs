using System.ComponentModel.DataAnnotations.Schema;

namespace Dapper.Addition.SqlServer.Tests
{
    public class Table2
    {
        public int Id { get; set; }
        public string? Text { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int ReadOnlyColumn1 { get; set; }        
    }
}