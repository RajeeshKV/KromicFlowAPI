using KromicFlow.Application.Features.User.DeleteUser;
using KromicFlow.Application.Features.User.GetUserProfile;
using KromicFlow.Application.Features.Users.SendVerificationEmail;
using KromicFlow.Application.Features.Users.VerifyEmailToken;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/users")]
public sealed class UserController(IMediator mediator) : ApiControllerBase
{
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var profile = await mediator.Send(new GetUserProfileQuery(User.GetSubjectId()), cancellationToken);
        return Ok(profile);
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> SendVerificationEmail(
        [FromBody] SendVerificationEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        var result = await mediator.Send(
            new SendVerificationEmailCommand(User.GetSubjectId(), request.Email),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true, message = $"Verification email sent to {request.Email}. Check your inbox within 5 minutes." });
    }

    [HttpPost("verify-email-token")]
    public async Task<IActionResult> VerifyEmailToken(
        [FromBody] VerifyEmailTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { error = "Token is required" });
        }

        var result = await mediator.Send(
            new VerifyEmailTokenCommand(User.GetSubjectId(), request.Token),
            cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true, message = "Email verified successfully! You can now create automations", emailVerified = true });
    }

    [HttpDelete("account")]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new DeleteUserCommand(), cancellationToken));
}

public sealed record SendVerificationEmailRequest(string Email);
public sealed record VerifyEmailTokenRequest(string Token);
