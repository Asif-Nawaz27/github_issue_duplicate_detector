using IssueSense.Application.GitHub;
using IssueSense.Application.Import;
using Microsoft.AspNetCore.Mvc;

namespace IssueSense.Api.Controllers;

[ApiController]
[Route("api/repositories")]
public sealed class RepositoriesController(IIssueImportService importService) : ControllerBase
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
}
