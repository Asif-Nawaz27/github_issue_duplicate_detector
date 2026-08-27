using IssueSense.Domain.Entities;

namespace IssueSense.Application.Persistence;

public sealed record SimilarIssueMatch(Issue Issue, double SimilarityScore);
