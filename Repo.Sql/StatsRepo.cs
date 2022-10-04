using Repo.Abstractions;

namespace Repo.Sql
{
    public class StatsRepo : IRepo
    {
        public Task<IEnumerable<Statistic>> GetStats()
        {
            throw new NotImplementedException();
        }

        public Task SaveStats(long cont, string[] topTenHashtags)
        {
            throw new NotImplementedException("Consider adding an ORM like EF or Dapper to persist this to an OLTP like SqlServer, MySql or Postgres");
        }


    }
}