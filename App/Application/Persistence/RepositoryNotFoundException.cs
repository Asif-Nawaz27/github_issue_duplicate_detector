namespace IssueSense.Application.Persistence;

/// <summary>Thrown when an operation needs a repository that hasn't been imported yet.</summary>
public sealed class RepositoryNotFoundException(string message) : Exception(message);
