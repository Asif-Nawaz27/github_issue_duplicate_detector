using System.Diagnostics;
using IssueSense.Api.Contracts.DuplicateDetection;
using IssueSense.Application.DuplicateDetection;
using IssueSense.Application.Embeddings;
using IssueSense.Application.GitHub;
using IssueSense.Application.Import;
using IssueSense.Application.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace IssueSense.Api.Controllers;

[ApiController]
[Route("api/repositories")]
public sealed partial class RepositoriesController(
    IIssueImportService importService,
    IEmbeddingGenerationService embeddingGenerationService,
    IDuplicateDetectionService duplicateDetectionService,
    ILogger<RepositoriesController> logger) : ControllerBase
{
    [HttpPost("{owner}/{repository}/import")]
    public async Task<ActionResult<IssueImportResult>> ImportIssues(
        string owner,
        string repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await importService.ImportAsync(owner, repository, cancellationToken);
            return Ok(result);
        }
        catch (GitHubNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (GitHubRateLimitExceededException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = ex.Message, resetAt = ex.ResetAt });
        }
    }

    [HttpPost("{owner}/{repository}/generate-embeddings")]
    public async Task<ActionResult<EmbeddingGenerationResult>> GenerateEmbeddings(
        string owner,
        string repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await embeddingGenerationService.GenerateEmbeddingsAsync(owner, repository, cancellationToken);
            return Ok(result);
        }
        catch (RepositoryNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Checks a candidate issue (title and body) against a repository's existing issues for
    /// likely duplicates, using vector similarity search. Read-only: this only returns
    /// recommendations and never creates, closes, or modifies anything.
    /// </summary>
    /// <param name="owner">The GitHub repository owner.</param>
    /// <param name="repository">The GitHub repository name. Must already be imported.</param>
    /// <param name="request">The title and body of the issue to check.</param>
    /// <param name="cancellationToken">Cancellation token for the request.</param>
    /// <response code="200">The check completed; see <c>candidates</c> for any matches found.</response>
    /// <response code="400">The request failed validation (e.g. missing title).</response>
    /// <response code="404">The repository hasn't been imported yet.</response>
    [HttpPost("{owner}/{repository}/check-duplicate")]
    [ProducesResponseType(typeof(CheckDuplicateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CheckDuplicateResponse>> CheckDuplicate(
        string owner,
        string repository,
        [FromBody] CheckDuplicateRequest request,
        CancellationToken cancellationToken)
    {
        LogCheckDuplicateStarted(owner, repository);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await duplicateDetectionService.FindDuplicatesAsync(
                owner, repository, request.Title, request.Body, cancellationToken);

            stopwatch.Stop();

            var response = ToResponse(result, stopwatch.ElapsedMilliseconds);

            LogCheckDuplicateCompleted(owner, repository, response.Candidates.Count, response.IsPotentialDuplicate);

            return Ok(response);
        }
        catch (RepositoryNotFoundException ex)
        {
            LogRepositoryNotFound(owner, repository);
            return NotFound(new { error = ex.Message });
        }
    }

    private static CheckDuplicateResponse ToResponse(DuplicateDetectionResult result, long processingTimeMs)
    {
        var candidates = result.Candidates
            .Select(c => new DuplicateCandidateResponse(c.IssueNumber, c.Title, c.Url, c.SimilarityScore, c.Confidence.ToString()))
            .ToList();

        var isPotentialDuplicate = result.Candidates.Any(c => c.Confidence != DuplicateConfidence.Unlikely);
        var strongestConfidence = result.Candidates.Count == 0
            ? DuplicateConfidence.Unlikely
            : result.Candidates.Max(c => c.Confidence);

        var processing = new ProcessingInfoResponse(result.EmbeddingModelUsed, result.SimilarityThresholdUsed, processingTimeMs);

        return new CheckDuplicateResponse(isPotentialDuplicate, strongestConfidence.ToString(), candidates, processing);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Checking for duplicates in {Owner}/{Repository}")]
    private partial void LogCheckDuplicateStarted(string owner, string repository);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Duplicate check for {Owner}/{Repository} found {CandidateCount} candidate(s); IsPotentialDuplicate={IsPotentialDuplicate}")]
    private partial void LogCheckDuplicateCompleted(string owner, string repository, int candidateCount, bool isPotentialDuplicate);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Duplicate check requested for unimported repository {Owner}/{Repository}")]
    private partial void LogRepositoryNotFound(string owner, string repository);
}
