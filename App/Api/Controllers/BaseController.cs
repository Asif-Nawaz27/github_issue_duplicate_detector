using Microsoft.AspNetCore.Mvc;

namespace IssueSense.Api.Controllers;

/// <summary>
/// Shared base for API controllers: centralizes [ApiController] and gives every controller a
/// correctly-categorized logger (named after the concrete controller type, same as
/// ILogger&lt;RepositoriesController&gt; would produce) without each one injecting its own.
/// </summary>
[ApiController]
[Route("api/{Controller}")]
public abstract class BaseController : ControllerBase
{
    protected ILogger Logger { get; }

    protected BaseController(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }
}
