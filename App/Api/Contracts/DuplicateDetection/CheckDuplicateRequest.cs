using System.ComponentModel.DataAnnotations;

namespace IssueSense.Api.Contracts.DuplicateDetection;

/// <summary>The title and body of an issue to check for duplicates before it's opened on GitHub.</summary>
public sealed class CheckDuplicateRequest
{
    /// <summary>The issue title. Required.</summary>
    [Required(ErrorMessage = "Title is required.")]
    [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
    public string Title { get; init; } = string.Empty;

    /// <summary>The issue body/description. Optional.</summary>
    [MaxLength(60_000, ErrorMessage = "Body cannot exceed 60,000 characters.")]
    public string? Body { get; init; }
}
