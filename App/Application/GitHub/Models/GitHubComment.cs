namespace IssueSense.Application.GitHub.Models;

public sealed record GitHubComment(long Id, string Body, string? AuthorLogin);
