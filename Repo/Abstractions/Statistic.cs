﻿namespace Repo.Abstractions
{
    public class Statistic
    {
        public DateTime AsOf { get; set; }
        public long Count{ get; set; }
        public string[] TopTenHashtags { get; set; }
    }
}