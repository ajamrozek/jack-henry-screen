namespace Twitter.Repo.Abstractions;

public interface ITweetRepository
{
    Task GetSampleStreamAsync(CancellationToken cancellationToken);
}
