using System.ComponentModel.DataAnnotations;

namespace IssueSense.Api.Contracts.Owners;

public sealed class UpdateOwnerRequest
{
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(256, ErrorMessage = "Name cannot exceed 256 characters.")]
    public string Name { get; init; } = string.Empty;
}
