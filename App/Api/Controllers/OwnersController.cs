using IssueSense.Api.Contracts.Owners;
using IssueSense.Application.Owners;
using IssueSense.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace IssueSense.Api.Controllers;

public sealed class OwnersController(IOwnerService ownerService, ILoggerFactory loggerFactory) : BaseController(loggerFactory)
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OwnerResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var owners = await ownerService.GetAllAsync(cancellationToken);
        return Ok(owners.Select(ToResponse).ToList());
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OwnerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnerResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var owner = await ownerService.GetByIdAsync(id, cancellationToken);
        return owner is null ? NotFound(new { error = $"Owner {id} was not found." }) : Ok(ToResponse(owner));
    }

    [HttpPost]
    [ProducesResponseType(typeof(OwnerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OwnerResponse>> Create([FromBody] CreateOwnerRequest request, CancellationToken cancellationToken)
    {
        var owner = await ownerService.CreateAsync(request.Name, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = owner.Id }, ToResponse(owner));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(OwnerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OwnerResponse>> Update(int id, [FromBody] UpdateOwnerRequest request, CancellationToken cancellationToken)
    {
        var owner = await ownerService.UpdateAsync(id, request.Name, cancellationToken);
        return owner is null ? NotFound(new { error = $"Owner {id} was not found." }) : Ok(ToResponse(owner));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await ownerService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound(new { error = $"Owner {id} was not found." });
    }

    private static OwnerResponse ToResponse(Owner owner) => new(owner.Id, owner.Name, owner.CreatedDate, owner.ChangedDate);
}
