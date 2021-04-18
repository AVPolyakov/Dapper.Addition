using System;

namespace PlainQuery.SqlServerTests
{
    public class Post
    {
        public int PostId { get; set; }
        public string? Text { get; set; }
        public DateTime CreationDate { get; set; }
    }
}