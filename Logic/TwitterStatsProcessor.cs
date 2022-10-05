using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Logic;

public static class TwitterStatsProcessor
{
    public static IEnumerable<string> GetTopByRegex(this string[] tweetTexts, string matchPattern, int n = 10)
    {
        if (!tweetTexts.Any())
        {
            return Enumerable.Empty<string>();
        }

        var regex = new Regex(matchPattern);

        var resultsDictionary = new Dictionary<string, long>();
        foreach (var tweetText in tweetTexts)
        {
            var matches = regex.Matches(tweetText).Distinct();
            foreach (var match in matches)
            {
                if (resultsDictionary.ContainsKey(match.Value))
                {
                    resultsDictionary[match.Value]++;
                }
                else
                {
                    resultsDictionary.Add(match.Value, 1);
                }
            }

        }

        var results = resultsDictionary
            .OrderByDescending(_ => _.Value)
            .Take(n)
            .Select(_ => _.Key);
        return results;
    }


}