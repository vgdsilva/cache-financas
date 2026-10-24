using System.Security.Claims;
using FMS.Communication.Common;
using FMS.Communication.Features.Auth.Commands;
using FMS.Communication.Features.Auth.Dtos;
using FMS.Communication.Features.Auth.Queries;
using FMS.Web.Api.Bussiness.Rules.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FMS.Web.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResultDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request,
        [FromServices] ICommandHandler<LoginCommand, LoginResultDto> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new LoginCommand(
                request.Login,
                request.Password,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString()),
            cancellationToken);

        return ToActionResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<LoggedUserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me(
        [FromServices] IQueryHandler<GetLoggedUserQuery, LoggedUserDto> handler,
        CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(sub, out var userId))
            return Unauthorized();

        return ToActionResult(await handler.HandleAsync(new GetLoggedUserQuery(userId), cancellationToken));
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Value);

        var problem = new ProblemDetails { Detail = result.Error };

        return result.ErrorType switch
        {
            ErrorType.Validation   => BadRequest(problem),
            ErrorType.Unauthorized => Unauthorized(problem),
            ErrorType.Locked       => StatusCode(StatusCodes.Status423Locked, problem),
            ErrorType.NotFound     => NotFound(problem),
            _                      => StatusCode(StatusCodes.Status500InternalServerError, problem)
        };
    }
}
