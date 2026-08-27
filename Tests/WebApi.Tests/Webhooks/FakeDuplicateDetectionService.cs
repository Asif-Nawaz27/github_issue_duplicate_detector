using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.GitHub.Models;

namespace IssueSense.WebApi.Tests.Webhooks;

public sealed class FakeDuplicateDetectionService : IDuplicateDetectionService
{
    public List<(string Owner, string Name)> Calls { get; } = [];

    public DuplicateDetectionResult NextResult { get; set; } = new([], "fake-model", 0.5);

    public Task<DuplicateDetectionResult> FindDuplicatesAsync(
        string owner, string name, GitHubIssueInfo newIssue, CancellationToken cancellationToken = default)
    {
        Calls.Add((owner, name));
        return Task.FromResult(NextResult);
    }

    public Task<DuplicateDetectionResult> FindDuplicatesAsync(
        string owner, string name, string title, string? body, CancellationToken cancellationToken = default)
    {
        Calls.Add((owner, name));
        return Task.FromResult(NextResult);
    }
}
