namespace PlainQuery.PostgreSqlTests
{
    [Table("Comment2s")]
    public class Comment
    {
        public int Id { get; set; }
        public string? Text { get; set; }
    }
}