﻿using System;

namespace Dapper.Addition.SqlServer.Tests.Namaspace2
{
    public class Post
    {
        public long PostId { get; set; }
        public string? Text { get; set; }
        public DateTime CreationDate { get; set; }
    }
}