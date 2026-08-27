using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub.Models;
using IssueSense.Application.Persistence;

namespace IssueSense.Application.Tests.Webhooks.Fakes;

internal sealed class FakeDuplicateDetectionService : IDuplicateDetectionService
{
    public List<(string Owner, string Name)> Calls { get; } = [];

    public DuplicateDetectionResult NextResult { get; set; } = new([], "fake-model", 0.5);

    public bool ThrowRepositoryNotFound { get; set; }

    public Task<DuplicateDetectionResult> FindDuplicatesAsync(
        string owner, string name, GitHubIssueInfo newIssue, CancellationToken cancellationToken = default)
    {
        Calls.Add((owner, name));

        if (ThrowRepositoryNotFound)
            throw new RepositoryNotFoundException($"Repository '{owner}/{name}' has not been imported yet.");

        return Task.FromResult(NextResult);
    }

    public Task<DuplicateDetectionResult> FindDuplicatesAsync(
        string owner, string name, string title, string? body, CancellationToken cancellationToken = default)
    {
        Calls.Add((owner, name));

        if (ThrowRepositoryNotFound)
            throw new RepositoryNotFoundException($"Repository '{owner}/{name}' has not been imported yet.");

        return Task.FromResult(NextResult);
    }
}
