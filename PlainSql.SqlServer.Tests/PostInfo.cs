using System;

namespace PlainSql.SqlServer.Tests
{
    public class PostInfo
    {
        public int PostId { get; set; } 
        public string? Text { get; set; }
        public DateTime CreationDate { get; set; }
    }
}