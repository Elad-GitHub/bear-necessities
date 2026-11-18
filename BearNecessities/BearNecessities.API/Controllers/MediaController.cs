using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.Media.ConfirmUpload;
using BearNecessities.Application.UseCases.Media.DeleteMedia;
using BearNecessities.Application.UseCases.Media.GetMediaByOwner;
using BearNecessities.Application.UseCases.Media.GetMediaContent;
using BearNecessities.Application.UseCases.Media.ModeratePhotos;
using BearNecessities.Application.UseCases.Media.ReorderMedia;
using BearNecessities.Application.UseCases.Media.RequestUpload;
using BearNecessities.Domain.Enums;

namespace BearNecessities.API.Controllers;

public class MediaController : BaseApiController
{
    private readonly RequestUploadHandler _requestUploadHandler;
    private readonly ConfirmUploadHandler _confirmUploadHandler;
    private readonly DeleteMediaHandler _deleteHandler;
    private readonly GetMediaByOwnerHandler _byOwnerHandler;
    private readonly ReorderMediaHandler _reorderHandler;
    private readonly GetMediaContentHandler _contentHandler;
    private readonly GetPendingPhotosHandler _pendingPhotosHandler;
    private readonly ReviewPhotoHandler _reviewPhotoHandler;

    public MediaController(
        RequestUploadHandler requestUploadHandler,
        ConfirmUploadHandler confirmUploadHandler,
        DeleteMediaHandler deleteHandler,
        GetMediaByOwnerHandler byOwnerHandler,
        ReorderMediaHandler reorderHandler,
        GetMediaContentHandler contentHandler,
        GetPendingPhotosHandler pendingPhotosHandler,
        ReviewPhotoHandler reviewPhotoHandler)
    {
        _pendingPhotosHandler = pendingPhotosHandler;
        _reviewPhotoHandler = reviewPhotoHandler;
        _requestUploadHandler = requestUploadHandler;
        _confirmUploadHandler = confirmUploadHandler;
        _deleteHandler = deleteHandler;
        _byOwnerHandler = byOwnerHandler;
        _reorderHandler = reorderHandler;
        _contentHandler = contentHandler;
    }

    [HttpGet("pending")]
    [Authorize]
    public async Task<IActionResult> GetPendingPhotos(CancellationToken cancellationToken = default) =>
        Ok(await _pendingPhotosHandler.HandleAsync(new GetPendingPhotosRequest(), cancellationToken));

    [HttpPost("{id:guid}/approve")]
    [Authorize]
    public async Task<IActionResult> ApprovePhoto(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await _reviewPhotoHandler.HandleAsync(new ReviewPhotoRequest(id, IsApproved: true), cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [Authorize]
    public async Task<IActionResult> RejectPhoto(Guid id, CancellationToken cancellationToken = default) =>
        Ok(await _reviewPhotoHandler.HandleAsync(new ReviewPhotoRequest(id, IsApproved: false), cancellationToken));

    [HttpGet("{id:guid}/content")]
    public async Task<IActionResult> GetContent(Guid id, CancellationToken cancellationToken = default)
    {
        var download = await _contentHandler.HandleAsync(new GetMediaContentRequest(id), cancellationToken);
        if (download is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=86400";
        return File(download.Content, download.ContentType);
    }

    [HttpPost("request-upload")]
    [Authorize]
    public async Task<IActionResult> RequestUpload(
        [FromBody] RequestUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _requestUploadHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("confirm-upload")]
    [Authorize]
    public async Task<IActionResult> ConfirmUpload(
        [FromBody] ConfirmUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _confirmUploadHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{ownerType}/{ownerId:guid}")]
    public async Task<IActionResult> GetByOwner(
        [FromRoute] MediaOwnerType ownerType,
        [FromRoute] Guid ownerId,
        CancellationToken cancellationToken = default)
    {
        var response = await _byOwnerHandler.HandleAsync(new GetMediaByOwnerRequest(ownerType, ownerId), cancellationToken);
        return Ok(response);
    }

    [HttpPut("reorder")]
    [Authorize]
    public async Task<IActionResult> Reorder(
        [FromBody] ReorderMediaRequest request,
        CancellationToken cancellationToken = default)
    {
        await _reorderHandler.HandleAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await _deleteHandler.HandleAsync(new DeleteMediaRequest(id), cancellationToken);
        return NoContent();
    }
}
