namespace IssueSense.Api.Contracts.Owners;

public sealed record OwnerResponse(int Id, string Name, DateTime? CreatedDate, DateTime? ChangedDate);
