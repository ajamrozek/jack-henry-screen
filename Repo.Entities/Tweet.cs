namespace Repo.Entities;

public class Tweet
{
    public Data data { get; set; }
}

public class Data
{
    public string[] edit_history_tweet_ids { get; set; }
    public string id { get; set; }
    public string text { get; set; }
}

