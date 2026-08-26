namespace IssueSense.Application.Import;

public sealed record IssueImportResult(int IssuesDiscovered, int IssuesCreated, int IssuesUpdated, int IssuesSkipped);
