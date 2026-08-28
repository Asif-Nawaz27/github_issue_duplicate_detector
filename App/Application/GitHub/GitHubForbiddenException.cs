namespace IssueSense.Application.GitHub;

/// <summary>Thrown when GitHub rejects a request as forbidden — typically an insufficient token scope or permission.</summary>
public sealed class GitHubForbiddenException(string message) : Exception(message);
