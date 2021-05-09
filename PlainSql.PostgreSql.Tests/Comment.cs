using System.ComponentModel.DataAnnotations.Schema;
    
namespace PlainSql.PostgreSql.Tests
{
    [Table("comment2s")]
    public class Comment
    {
        public int Id { get; set; }
        public string? Text { get; set; }
    }
}