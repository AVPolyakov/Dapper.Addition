using System;

namespace PlainSql.PostgreSql.Tests
{
    public class Post
    {
        public int PostId { get; set; }
        public string? Text { get; set; }
        public DateTime CreationDate { get; set; }
    }
}