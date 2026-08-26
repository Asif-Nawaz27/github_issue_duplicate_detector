namespace IssueSense.Application.Import;

public interface IIssueImportService
{
    /// <summary>
    /// Imports every issue from the given GitHub repository, creating the repository record
    /// if needed. Safe to call repeatedly: existing repositories/issues are matched by their
    /// GitHub identity and updated in place rather than duplicated.
    /// </summary>
    Task<IssueImportResult> ImportAsync(string owner, string name, CancellationToken cancellationToken = default);
}
