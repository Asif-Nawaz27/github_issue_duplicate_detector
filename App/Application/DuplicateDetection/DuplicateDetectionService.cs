using IssueSense.Application.Embeddings;
using IssueSense.Application.GitHub.Models;
using IssueSense.Application.Persistence;
using Microsoft.Extensions.Options;

namespace IssueSense.Application.DuplicateDetection;

public sealed class DuplicateDetectionService(
    IEmbeddingService embeddingService,
    IRepositoryRepository repositoryRepository,
    IIssueRepository issueRepository,
    IIssueEmbeddingRepository issueEmbeddingRepository,
    IOptions<DuplicateDetectionOptions> options) : IDuplicateDetectionService
{
    public async Task<IReadOnlyList<DuplicateCandidateMatch>> FindDuplicatesAsync(
        string owner,
        string name,
        GitHubIssueInfo newIssue,
        CancellationToken cancellationToken = default)
    {
        var repository = await repositoryRepository.GetByOwnerAndNameAsync(owner, name, cancellationToken)
            ?? throw new RepositoryNotFoundException($"Repository '{owner}/{name}' has not been imported yet.");

        // If the issue being checked is already stored (and possibly already embedded), exclude
        // it from its own results; a brand new issue that hasn't been imported yet has nothing
        // to exclude.
        var existingIssue = await issueRepository.GetByRepositoryIdAndGitHubIssueIdAsync(repository.Id, newIssue.Id, cancellationToken);

        var embeddingResult = await embeddingService.GenerateEmbeddingAsync(newIssue.Title, newIssue.Body, cancellationToken);

        var settings = options.Value;
        var matches = await issueEmbeddingRepository.FindSimilarAsync(
            repository.Id,
            embeddingResult.Vector,
            settings.TopN,
            settings.MinimumSimilarityThreshold,
            existingIssue?.Id,
            cancellationToken);

        return matches.Select(match => ToCandidateMatch(match, repository.FullName, settings)).ToList();
    }

    private static DuplicateCandidateMatch ToCandidateMatch(SimilarIssueMatch match, string repositoryFullName, DuplicateDetectionOptions settings) =>
        new(
            match.Issue.GitHubIssueNumber,
            match.Issue.Title,
            match.Issue.Url,
            match.SimilarityScore,
            repositoryFullName,
            match.Issue.State,
            Classify(match.SimilarityScore, settings));

    private static DuplicateConfidence Classify(double similarity, DuplicateDetectionOptions settings) =>
        similarity switch
        {
            _ when similarity >= settings.HighConfidenceThreshold => DuplicateConfidence.HighConfidence,
            _ when similarity >= settings.PossibleDuplicateThreshold => DuplicateConfidence.Possible,
            _ => DuplicateConfidence.Unlikely
        };
}
