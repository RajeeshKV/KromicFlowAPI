using KromicFlow.Application.Features.Instagram.ConnectAccount;
using KromicFlow.Application.Features.Instagram.DisconnectAccount;
using KromicFlow.Application.Features.Instagram.GetInstagramMedia;
using KromicFlow.Application.Features.Instagram.ListInstagramAccounts;
using KromicFlow.Application.Features.Instagram.SyncInstagramAccount;
using KromicFlow.Application.Abstractions;
using KromicFlow.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KromicFlow.Api.Controllers;

[Authorize]
[Route("api/v1/instagram")]
public sealed class InstagramController(
    IMediator mediator,
    IKromicFlowDbContext db,
    HttpClient httpClient,
    ILogger<InstagramController> logger) : ApiControllerBase
{
    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new ListInstagramAccountsQuery(User.GetSubjectId()), cancellationToken));

    [HttpPost("{id:guid}/connect")]
    public async Task<IActionResult> Connect(Guid id, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new ConnectAccountCommand(id), cancellationToken));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Disconnect(Guid id, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new DisconnectAccountCommand(id), cancellationToken));

    [HttpGet("{id:guid}/media")]
    public async Task<IActionResult> Media(
        Guid id,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? mediaType = null,
        [FromQuery] string? search = null)
    {
        MediaType? parsedMediaType = null;
        if (!string.IsNullOrWhiteSpace(mediaType) && Enum.TryParse<MediaType>(mediaType, true, out var parsed))
        {
            parsedMediaType = parsed;
        }
        
        return Ok(await mediator.Send(new GetInstagramMediaQuery(id, page, pageSize, parsedMediaType, search), cancellationToken));
    }

    [HttpPost("{id:guid}/sync")]
    public async Task<IActionResult> Sync(Guid id, CancellationToken cancellationToken) =>
        FromResult(await mediator.Send(new SyncInstagramAccountCommand(User.GetSubjectId(), id), cancellationToken));

    /// <summary>
    /// GET /api/v1/instagram/{id}/profile-image
    /// 
    /// Returns the Instagram profile picture for the account.
    /// The image is fetched from Meta API and returned as a stream.
    /// Frontend can display as: &lt;img src="/api/v1/instagram/{id}/profile-image" alt="Profile" /&gt;
    /// 
    /// Authorization: Required (JWT)
    /// Response: Image stream (image/jpeg or image/png)
    /// </summary>
    [HttpGet("{id:guid}/profile-image")]
    [Produces("image/jpeg", "image/png")]
    public async Task<IActionResult> GetProfileImage(Guid id, CancellationToken cancellationToken)
    {
        // Verify the account exists and belongs to the current user
        var account = await db.InstagramAccounts
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == User.GetSubjectId(), cancellationToken);

        if (account is null)
        {
            logger.LogWarning("Profile image requested for non-existent or unauthorized account {AccountId}", id);
            return NotFound(new { error = "Account not found" });
        }

        if (string.IsNullOrEmpty(account.ProfilePicture))
        {
            logger.LogWarning("Profile image requested but no URL available for account {AccountId}", id);
            return NotFound(new { error = "No profile picture available" });
        }

        try
        {
            logger.LogInformation("Fetching profile image from Meta API for account {AccountId}", id);

            // Fetch the image from Meta API
            using var response = await httpClient.GetAsync(account.ProfilePicture, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Meta API returned {StatusCode} when fetching profile image for account {AccountId}",
                    response.StatusCode, id);
                return StatusCode(502, new { error = "Failed to fetch profile image from Instagram" });
            }

            var imageBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";

            logger.LogInformation("Successfully fetched profile image for account {AccountId}, size: {Size} bytes",
                id, imageBytes.Length);

            // Return image with cache headers (profile pictures don't change frequently)
            return File(imageBytes, contentType, enableRangeProcessing: true);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP error while fetching profile image for account {AccountId}", id);
            return StatusCode(502, new { error = "Failed to fetch profile image" });
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout while fetching profile image for account {AccountId}", id);
            return StatusCode(504, new { error = "Request timeout" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while fetching profile image for account {AccountId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
