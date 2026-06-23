using ImageGallery.Api.DTOs;
using ImageGallery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminController(
    IAdminUserService adminUserService,
    IImageService imageService,
    ISuggestionService suggestionService,
    ICurrentUserService currentUser) : ControllerBase
{
    // ---------------------- Usuários ----------------------

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers([FromQuery] string? status)
    {
        await currentUser.EnsureAdminAsync();
        return Ok(await adminUserService.GetAllAsync(status));
    }

    [HttpPost("users")]
    public async Task<ActionResult<AdminUserDto>> CreateUser(CreateUserByAdminRequest request)
    {
        await currentUser.EnsureAdminAsync();
        return Ok(await adminUserService.CreateUserAsync(request, currentUser.UserId));
    }

    [HttpPost("users/{id:guid}/approve")]
    public async Task<IActionResult> ApproveUser(Guid id)
    {
        await currentUser.EnsureAdminAsync();
        await adminUserService.ApproveAsync(id, currentUser.UserId);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/reject")]
    public async Task<IActionResult> RejectUser(Guid id)
    {
        await currentUser.EnsureAdminAsync();
        await adminUserService.RejectAsync(id, currentUser.UserId);
        return NoContent();
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await currentUser.EnsureAdminAsync();
        await adminUserService.DeleteAsync(id);
        return NoContent();
    }

    // ---------------------- Imagens (upload) ----------------------

    [HttpGet("images/pending")]
    public async Task<ActionResult<List<ImageListItemDto>>> GetPendingImages()
    {
        await currentUser.EnsureAdminAsync();
        return Ok(await imageService.GetPendingAsync());
    }

    [HttpPost("images/{id:guid}/review")]
    public async Task<IActionResult> ReviewImage(Guid id, ReviewImageRequest request)
    {
        await currentUser.EnsureAdminAsync();
        await imageService.ReviewAsync(id, currentUser.UserId, request.Approve);
        return NoContent();
    }

    // ---------------------- Sugestões de categoria ----------------------

    [HttpGet("category-suggestions")]
    public async Task<ActionResult<List<SuggestionDto>>> GetCategorySuggestions()
    {
        await currentUser.EnsureAdminAsync();
        return Ok(await suggestionService.GetPendingCategorySuggestionsAsync());
    }

    [HttpPost("category-suggestions/{id:guid}/review")]
    public async Task<IActionResult> ReviewCategorySuggestion(Guid id, ReviewSuggestionRequest request)
    {
        await currentUser.EnsureAdminAsync();
        await suggestionService.ReviewCategorySuggestionAsync(id, currentUser.UserId, request.Approve);
        return NoContent();
    }

    // ---------------------- Sugestões de subcategoria ----------------------

    [HttpGet("subcategory-suggestions")]
    public async Task<ActionResult<List<SuggestionDto>>> GetSubcategorySuggestions()
    {
        await currentUser.EnsureAdminAsync();
        return Ok(await suggestionService.GetPendingSubcategorySuggestionsAsync());
    }

    [HttpPost("subcategory-suggestions/{id:guid}/review")]
    public async Task<IActionResult> ReviewSubcategorySuggestion(Guid id, ReviewSuggestionRequest request)
    {
        await currentUser.EnsureAdminAsync();
        await suggestionService.ReviewSubcategorySuggestionAsync(id, currentUser.UserId, request.Approve);
        return NoContent();
    }
}
