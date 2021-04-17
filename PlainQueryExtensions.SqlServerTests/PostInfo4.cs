using System;

namespace PlainQueryExtensions.SqlServerTests
{
    public class PostInfo4
    {
        public int PostId { get; set; }
        public string? Text { get; set; }
        public string? IgnoredProperty { get; set; }
        public DateTime CreationDate { get; set; }
    }
}