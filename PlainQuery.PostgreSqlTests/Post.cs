using System;

namespace PlainQuery.PostgreSqlTests
{
    public class Post
    {
        public int PostId { get; set; }
        public string? Text { get; set; }
        public DateTime CreationDate { get; set; }
    }
}