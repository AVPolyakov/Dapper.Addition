﻿using System;

namespace Dapper.Addition.SqlServer.Tests
{
    public class Post
    {
        public int PostId { get; set; }
        public string? Text { get; set; }
        public DateTime CreationDate { get; set; }
    }
}