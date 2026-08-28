namespace IssueSense.Evaluation;

/// <summary>Ground-truth relationship between a candidate new issue and an existing one.</summary>
public enum IssueRelationship
{
    /// <summary>The new issue reports the same underlying problem as the existing one.</summary>
    Duplicate,

    /// <summary>Same general topic/area but a genuinely different issue — not a duplicate.</summary>
    Related,

    /// <summary>No meaningful connection.</summary>
    Unrelated
}

public sealed record EvaluationIssue(string Title, string? Body);

/// <summary>One labeled (new issue, existing issue) pair — the dataset unit described in the task.</summary>
public sealed record EvaluationCase(string Id, EvaluationIssue NewIssue, EvaluationIssue ExistingIssue, IssueRelationship ExpectedRelationship);
