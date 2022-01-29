using System.ComponentModel.DataAnnotations.Schema;
    
namespace Dapper.Addition.PostgreSql.Tests
{
    [Table("comment2s")]
    public class Comment
    {
        public int Id { get; set; }
        public string? Text { get; set; }
    }
}