namespace Repo.Abstractions
{
    public interface IRepo
    {
        Task SaveStats(long cont, string[] topTenHashtags);
        Task<IEnumerable<Statistic>> GetStats();    
    }
}