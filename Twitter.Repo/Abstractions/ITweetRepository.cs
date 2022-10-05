namespace Twitter.Repo.Abstractions;

public interface ITweetRepository
{
    Task<bool> CheckStatus(CancellationToken cancellationToken);
    Task GetSampleStreamAsync(CancellationToken cancellationToken);
}
