using System.ComponentModel.DataAnnotations.Schema;

namespace Dapper.Addition.SqlServer.Tests
{
    [Table("Comment2s")]
    public class Comment
    {
        public int Id { get; set; }
        public string? Text { get; set; }
    }
}