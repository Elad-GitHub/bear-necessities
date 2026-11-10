using Microsoft.AspNetCore.Mvc;

namespace BearNecessities.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult OkOrNotFound<T>(T? value) where T : class
        => value is null ? NotFound() : Ok(value);
}
