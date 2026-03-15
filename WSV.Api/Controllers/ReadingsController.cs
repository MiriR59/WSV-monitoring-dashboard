using Microsoft.AspNetCore.Mvc;
using WSV.Api.Services;
using Microsoft.AspNetCore.Authorization;

namespace WSV.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReadingsController : ControllerBase
{
    private readonly IAuthorizationService _authorization;
    private readonly IReadingService _readingService;

    public ReadingsController(
        IAuthorizationService authorization,
        IReadingService readingService)
    {
        _authorization = authorization;
        _readingService = readingService;
    }   

    [HttpGet("source/{sourceId}")]
    [Authorize(Policy = "CanViewAllSources")]
    public async Task <IActionResult> GetHistoryOne(
        int sourceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? limit
        )
    {
        var source = await _readingService.GetSourceAsync(sourceId);
        if(source is null)
            return NotFound();

        return Ok(await _readingService.GetHistoryAsync(sourceId, from, to, limit));
    }

    [HttpGet("public/source/{sourceId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicOne(
        int sourceId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int? limit
        )
    {
        var source = await _readingService.GetPublicSourceAsync(sourceId);
        if (source is null)
            return NotFound();

        return Ok(await _readingService.GetHistoryAsync(sourceId, from, to, limit));
    }

    [HttpGet("source/{sourceId}/lag")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLagOne(
        int sourceId
    )
    {
        var source = await _readingService.GetSourceAsync(sourceId);
        if (source is null)
            return NotFound();
        
        if(!source.IsPublic)
        {
            var auth = await _authorization.AuthorizeAsync(User, "CanViewAllSources");
            if(!auth.Succeeded)
                return NotFound();
        }

        return Ok(await _readingService.GetLagAsync(sourceId));
    }
}