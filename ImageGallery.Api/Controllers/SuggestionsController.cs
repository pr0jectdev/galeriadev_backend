using ImageGallery.Api.DTOs;
using ImageGallery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Api.Controllers;

[ApiController]
[Route("api/suggestions")]
[Authorize]
public class SuggestionsController(ISuggestionService suggestionService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpPost("categories")]
    public async Task<ActionResult<SuggestionDto>> SuggestCategory(CreateCategorySuggestionRequest request)
    {
        await currentUser.EnsureApprovedAsync();
        return Ok(await suggestionService.CreateCategorySuggestionAsync(currentUser.UserId, request));
    }

    [HttpPost("subcategories")]
    public async Task<ActionResult<SuggestionDto>> SuggestSubcategory(CreateSubcategorySuggestionRequest request)
    {
        await currentUser.EnsureApprovedAsync();
        return Ok(await suggestionService.CreateSubcategorySuggestionAsync(currentUser.UserId, request));
    }
}
