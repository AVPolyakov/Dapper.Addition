using System;

namespace Dapper.Addition.PostgreSql.Tests
{
    public class PostInfo3
    {
        public long PostId { get; set; } 
        public string? Text { get; set; }
        public DateTime CreationDate { get; set; }
    }
}