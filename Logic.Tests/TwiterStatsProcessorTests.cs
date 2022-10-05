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
                .CreateMany<string>(100000)
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
                    targetIdx = rand.Next(99);
                    fakeTweetTexts[targetIdx] += $" @{fakeHashtag}";
                }
            }

            var results = TwitterStatsProcessor.GetTop(fakeTweetTexts, @"\#\w+");

            Assert.Equal(10, results.Count());

            results = TwitterStatsProcessor.GetTop(fakeTweetTexts, @"\@\w+");

            Assert.Equal(10, results.Count());


            //var topTenHashtagsTask = Task.Factory.StartNew(() => TwitterStatsProcessor.GetTop(fakeTweetTexts!, @"\#\w+"));

            //var topTenMentionsTask = Task.Factory.StartNew(() => TwitterStatsProcessor.GetTop(fakeTweetTexts!, @"\@\w+"));

            //Task.WaitAll(new[] { topTenHashtagsTask, topTenMentionsTask });

            //Assert.Equal(10, topTenHashtagsTask.Result.Count());
            //Assert.Equal(10, topTenMentionsTask.Result.Count());


        }
    }
}