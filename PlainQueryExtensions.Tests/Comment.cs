namespace PlainQueryExtensions.Tests
{
    [Table("Comments")]
    public class Comment
    {
        public int Id { get; set; }
        public string? Text { get; set; }
    }
}