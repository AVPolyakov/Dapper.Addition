using System;

namespace PlainQuery.PostgreSqlTests
{
    public class PostInfo3
    {
        public long PostId { get; set; } 
        public string? Text { get; set; }
        public DateTime CreationDate { get; set; }
    }
}