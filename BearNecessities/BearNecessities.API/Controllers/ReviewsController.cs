using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BearNecessities.Application.UseCases.Reviews.AdminRemoveReview;
using BearNecessities.Application.UseCases.Reviews.CreateReview;
using BearNecessities.Application.UseCases.Reviews.FlagReview;
using BearNecessities.Application.UseCases.Reviews.GetMyReviews;
using BearNecessities.Application.UseCases.Reviews.GetReviewsByTarget;
using BearNecessities.Application.UseCases.Reviews.SubmitCounterReview;

namespace BearNecessities.API.Controllers;

public class ReviewsController : BaseApiController
{
    private readonly CreateReviewHandler _createHandler;
    private readonly SubmitCounterReviewHandler _counterHandler;
    private readonly GetReviewsByTargetHandler _byTargetHandler;
    private readonly GetMyReviewsHandler _myReviewsHandler;
    private readonly FlagReviewHandler _flagHandler;
    private readonly AdminRemoveReviewHandler _removeHandler;

    public ReviewsController(
        CreateReviewHandler createHandler,
        SubmitCounterReviewHandler counterHandler,
        GetReviewsByTargetHandler byTargetHandler,
        GetMyReviewsHandler myReviewsHandler,
        FlagReviewHandler flagHandler,
        AdminRemoveReviewHandler removeHandler)
    {
        _createHandler = createHandler;
        _counterHandler = counterHandler;
        _byTargetHandler = byTargetHandler;
        _myReviewsHandler = myReviewsHandler;
        _flagHandler = flagHandler;
        _removeHandler = removeHandler;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _createHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/counter")]
    [Authorize]
    public async Task<IActionResult> SubmitCounter(
        Guid id,
        [FromBody] SubmitCounterReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var merged = request with { OriginalReviewId = id };
        var response = await _counterHandler.HandleAsync(merged, cancellationToken);
        return Ok(response);
    }

    [HttpGet("mine")]
    [Authorize]
    public async Task<IActionResult> GetMine(
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _myReviewsHandler.HandleAsync(new GetMyReviewsRequest(pageSize, cursor), cancellationToken);
        return Ok(response);
    }

    [HttpGet("{targetType}/{targetId:guid}")]
    public async Task<IActionResult> GetByTarget(
        string targetType,
        Guid targetId,
        [FromQuery] int pageSize = 10,
        [FromQuery] Guid? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _byTargetHandler.HandleAsync(
            new GetReviewsByTargetRequest(targetType, targetId, pageSize, cursor), cancellationToken);
        return Ok(response);
    }

    [HttpPost("{id:guid}/flag")]
    [Authorize]
    public async Task<IActionResult> Flag(
        Guid id,
        [FromBody] FlagReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var merged = request with { ReviewId = id };
        await _flagHandler.HandleAsync(merged, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> AdminRemove(
        Guid id,
        [FromBody] AdminRemoveReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        var merged = request with { ReviewId = id };
        await _removeHandler.HandleAsync(merged, cancellationToken);
        return NoContent();
    }
}
