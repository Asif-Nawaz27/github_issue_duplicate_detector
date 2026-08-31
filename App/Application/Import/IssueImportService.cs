using IssueSense.Application.GitHub;
using IssueSense.Application.GitHub.Models;
using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using IssueSense.Domain.Enums;

namespace IssueSense.Application.Import;

public sealed class IssueImportService(
    IGitHubService gitHubService,
    IRepositoryRepository repositoryRepository,
    IIssueRepository issueRepository,
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork) : IIssueImportService
{
    public async Task<IssueImportResult> ImportAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        var repository = await GetOrCreateRepositoryAsync(owner, name, cancellationToken);

        var discovered = 0;
        var created = 0;
        var updated = 0;
        var skipped = 0;

        // GitHub's issues endpoint also returns pull requests; IGitHubService already
        // filters those out, so every item here is a genuine issue.
        await foreach (var githubIssue in gitHubService.GetIssuesAsync(owner, name, GitHubIssueStateFilter.All, cancellationToken))
        {
            discovered++;

            var existingIssue = await issueRepository.GetByRepositoryIdAndGitHubIssueIdAsync(
                repository.Id, githubIssue.Id, cancellationToken);

            if (existingIssue is null)
            {
                issueRepository.Add(CreateIssue(repository.Id, githubIssue));
                created++;
            }
            else if (HasChanges(existingIssue, githubIssue))
            {
                ApplyChanges(existingIssue, githubIssue);
                updated++;
            }
            else
            {
                skipped++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssueImportResult(discovered, created, updated, skipped);
    }

    private async Task<Repository> GetOrCreateRepositoryAsync(string owner, string name, CancellationToken cancellationToken)
    {
        var existing = await repositoryRepository.GetByOwnerAndNameAsync(owner, name, cancellationToken);
        if (existing is not null)
            return existing;

        var repositoryInfo = await gitHubService.GetRepositoryAsync(owner, name, cancellationToken);
        var ownerId = await GetOrCreateOwnerIdAsync(repositoryInfo.Owner, cancellationToken);
        var repository = Repository.Create(repositoryInfo.Id, ownerId, repositoryInfo.Name, DateTimeOffset.UtcNow);
        repositoryRepository.Add(repository);

        return repository;
    }

    // GitHub is the source of truth for the owner's login, so a not-yet-known owner is created
    // here automatically rather than requiring it to be added by hand first. Owner.Id is a
    // DB-generated identity, and Repository has no navigation property to fix it up after the
    // fact, so this needs its own SaveChanges to obtain a real id before Repository.Create can
    // use it - only paid on the first import for a given owner.
    private async Task<int> GetOrCreateOwnerIdAsync(string ownerName, CancellationToken cancellationToken)
    {
        var existingOwner = await ownerRepository.GetByNameAsync(ownerName, cancellationToken);
        if (existingOwner is not null)
            return existingOwner.Id;

        var newOwner = Owner.Create(ownerName, DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified));
        ownerRepository.Add(newOwner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return newOwner.Id;
    }

    private static Issue CreateIssue(Guid repositoryId, GitHubIssueInfo githubIssue)
    {
        var issue = Issue.Create(
            repositoryId,
            githubIssue.Id,
            githubIssue.Number,
            githubIssue.Title,
            githubIssue.Body,
            githubIssue.HtmlUrl,
            githubIssue.CreatedAt,
            githubIssue.UpdatedAt,
            githubIssue.Labels);

        if (githubIssue.State == IssueState.Closed)
            issue.Close(githubIssue.ClosedAt ?? githubIssue.UpdatedAt);

        return issue;
    }

    private static bool HasChanges(Issue existing, GitHubIssueInfo latest) =>
        existing.Title != latest.Title
        || existing.Body != latest.Body
        || existing.State != latest.State
        || existing.UpdatedAt != latest.UpdatedAt
        || !existing.Labels.SequenceEqual(latest.Labels, StringComparer.OrdinalIgnoreCase);

    private static void ApplyChanges(Issue existing, GitHubIssueInfo latest)
    {
        existing.UpdateContent(latest.Title, latest.Body, latest.UpdatedAt);
        existing.ReplaceLabels(latest.Labels);

        if (latest.State == IssueState.Closed && existing.State != IssueState.Closed)
            existing.Close(latest.ClosedAt ?? latest.UpdatedAt);
        else if (latest.State == IssueState.Open && existing.State != IssueState.Open)
            existing.Reopen(latest.UpdatedAt);
    }
}
