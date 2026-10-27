using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Nomadica.Application.UseCases.Telemetry.IngestTelemetry;

namespace Nomadica.API.Controllers;

public class TelemetryController : BaseApiController
{
    private readonly IngestTelemetryHandler _handler;

    public TelemetryController(IngestTelemetryHandler handler)
    {
        _handler = handler;
    }

    // Anonymous so we capture pre-login behaviour; rate-limited to blunt abuse.
    [HttpPost("batch")]
    [AllowAnonymous]
    [EnableRateLimiting("anonymous")]
    public async Task<IActionResult> Ingest(
        [FromBody] IngestTelemetryRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _handler.HandleAsync(request, cancellationToken);
        return Accepted(response);
    }
}
