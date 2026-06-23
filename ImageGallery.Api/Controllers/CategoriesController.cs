using ImageGallery.Api.DTOs;
using ImageGallery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService categoryService, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Lista todas as categorias com suas subcategorias — público, sem login.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        return Ok(await categoryService.GetAllAsync());
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request)
    {
        await currentUser.EnsureAdminAsync();
        var category = await categoryService.CreateAsync(request, currentUser.UserId);
        return CreatedAtAction(nameof(GetAll), category);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        await currentUser.EnsureAdminAsync();
        await categoryService.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("subcategories")]
    [Authorize]
    public async Task<ActionResult<SubcategoryDto>> CreateSubcategory(CreateSubcategoryRequest request)
    {
        await currentUser.EnsureAdminAsync();
        var subcategory = await categoryService.CreateSubcategoryAsync(request, currentUser.UserId);
        return CreatedAtAction(nameof(GetAll), subcategory);
    }

    [HttpDelete("subcategories/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteSubcategory(Guid id)
    {
        await currentUser.EnsureAdminAsync();
        await categoryService.DeleteSubcategoryAsync(id);
        return NoContent();
    }
}
