using AutoFixture;
using System.Net.WebSockets;

namespace Logic.Tests
{
    public class TwiterStatsProcessorTests
    {
        Fixture autoFixture = new Fixture(); 
        [Fact]
        public void GetTopTenHashTags_Nominal()
        {
            var fakeTweetTexts = autoFixture
                .CreateMany<string>(100)
                .ToArray();

            var fakeHashtags = autoFixture
                .CreateMany<string>(20)
                .ToArray();

            var rand = new Random(0);

            for (int i = 19; i >= 0; i--)
            {
                string fakeHashtag = fakeHashtags[i]
                    .Split("-")[0];

                for (int j = i; j >= 0; j--)
                {
                    var targetIdx = rand.Next(99);
                    fakeTweetTexts[targetIdx] += $" #{fakeHashtag}";
                }
            }

            var target = new TwitterStatsProcessor();


            var results = target.GetTopTenHashTags(fakeTweetTexts);

            Assert.Equal(10, results.Count());
        }
    }
}