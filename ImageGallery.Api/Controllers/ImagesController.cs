using ImageGallery.Api.DTOs;
using ImageGallery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Api.Controllers;

[ApiController]
[Route("api/images")]
public class ImagesController(IImageService imageService, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Galeria pública — últimas imagens aprovadas, com filtro opcional por categoria/subcategoria.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ImageListItemDto>>> GetAll(
        [FromQuery] Guid? categoryId,
        [FromQuery] Guid? subcategoryId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24)
    {
        return Ok(await imageService.GetApprovedAsync(categoryId, subcategoryId, page, pageSize));
    }

    /// <summary>Imagens enviadas pelo usuário logado (qualquer status), exibidas no perfil.</summary>
    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<List<ImageListItemDto>>> GetMine()
    {
        return Ok(await imageService.GetMineAsync(currentUser.UserId));
    }

    /// <summary>Detalhe da imagem — também registra o clique para o contador.</summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ImageDetailDto>> GetById(Guid id, [FromQuery] bool countClick = true)
    {
        return Ok(await imageService.GetByIdAsync(id, countClick));
    }

    [HttpGet("{id:guid}/thumbnail")]
    [AllowAnonymous]
    public async Task<IActionResult> GetThumbnail(Guid id)
    {
        var stream = await imageService.GetFileStreamAsync(id, thumbnail: true);
        return File(stream, "image/webp");
    }

    /// <summary>Arquivo original — usado para abrir em tamanho maior e para download.</summary>
    [HttpGet("{id:guid}/file")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFile(Guid id)
    {
        var (stream, fileName) = await imageService.GetDownloadAsync(id);
        return File(stream, "application/octet-stream", fileName);
    }

    [HttpPost]
    [Authorize]
    [RequestSizeLimit(15_000_000)]
    public async Task<ActionResult<ImageDetailDto>> Upload(
        [FromForm] Guid categoryId,
        [FromForm] Guid subcategoryId,
        [FromForm] string? title,
        [FromForm] IFormFile file)
    {
        await currentUser.EnsureApprovedAsync();
        var result = await imageService.UploadAsync(currentUser.UserId, categoryId, subcategoryId, title, file);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var profile = await currentUser.GetProfileAsync();
        await imageService.DeleteAsync(id, currentUser.UserId, profile.Role == "admin");
        return NoContent();
    }
}
