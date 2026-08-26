using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace IssueSense.Application.Embeddings;

public sealed partial class EmbeddingGenerationService(
    IEmbeddingService embeddingService,
    IRepositoryRepository repositoryRepository,
    IIssueRepository issueRepository,
    IIssueEmbeddingRepository issueEmbeddingRepository,
    IUnitOfWork unitOfWork,
    ILogger<EmbeddingGenerationService> logger) : IEmbeddingGenerationService
{
    public async Task<EmbeddingGenerationResult> GenerateEmbeddingsAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        var repository = await repositoryRepository.GetByOwnerAndNameAsync(owner, name, cancellationToken)
            ?? throw new RepositoryNotFoundException(
                $"Repository '{owner}/{name}' has not been imported yet. Run the import endpoint first.");

        var totalIssues = await issueRepository.CountByRepositoryIdAsync(repository.Id, cancellationToken);
        var issuesWithoutEmbedding = await issueRepository.GetWithoutEmbeddingAsync(repository.Id, cancellationToken);

        var generated = 0;
        var failures = 0;

        foreach (var issue in issuesWithoutEmbedding)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await TryGenerateEmbeddingAsync(issue, cancellationToken))
                generated++;
            else
                failures++;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var skipped = totalIssues - issuesWithoutEmbedding.Count;

        return new EmbeddingGenerationResult(totalIssues, generated, skipped, failures);
    }

    private async Task<bool> TryGenerateEmbeddingAsync(Issue issue, CancellationToken cancellationToken)
    {
        try
        {
            var result = await embeddingService.GenerateEmbeddingAsync(issue.Title, issue.Body, cancellationToken);
            var embedding = IssueEmbedding.Create(issue.Id, result.Vector, result.ModelName, DateTimeOffset.UtcNow);
            issueEmbeddingRepository.Add(embedding);

            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // One bad issue shouldn't abort the whole batch — record it and keep going.
            LogEmbeddingGenerationFailed(ex, issue.Id, issue.GitHubIssueNumber);

            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to generate an embedding for issue {IssueId} (GitHub #{IssueNumber})")]
    private partial void LogEmbeddingGenerationFailed(Exception exception, Guid issueId, int issueNumber);
}
