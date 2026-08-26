namespace IssueSense.Application.GitHub;

public sealed class GitHubNotFoundException(string message) : Exception(message);
