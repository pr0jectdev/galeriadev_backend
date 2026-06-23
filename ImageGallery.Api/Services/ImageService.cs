using ImageGallery.Api.Common;
using ImageGallery.Api.DTOs;
using ImageGallery.Api.Models;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Supabase.Postgrest;
using Constants = Supabase.Postgrest.Constants;

namespace ImageGallery.Api.Services;

public interface IImageService {
    Task<PagedResult<ImageListItemDto>> GetApprovedAsync(Guid? categoryId, Guid? subcategoryId, int page, int pageSize);
    Task<List<ImageListItemDto>> GetMineAsync(Guid userId);
    Task<List<ImageListItemDto>> GetPendingAsync();
    Task<ImageDetailDto> GetByIdAsync(Guid id, bool registerClick);
    Task<Stream> GetFileStreamAsync(Guid id, bool thumbnail);
    Task<(Stream Stream, string FileName)> GetDownloadAsync(Guid id);
    Task<ImageDetailDto> UploadAsync(Guid userId, Guid categoryId, Guid subcategoryId, string? title, IFormFile file);
    Task ReviewAsync(Guid id, Guid adminId, bool approve);
    Task DeleteAsync(Guid id, Guid requesterId, bool requesterIsAdmin);
}

public class ImageService(
    ISupabaseClientProvider supabase,
    IOptions<SupabaseOptions> supabaseOptions,
    IOptions<UploadOptions> uploadOptions,
    ILogger<ImageService> logger) : IImageService {
    private readonly SupabaseOptions _supabaseOptions = supabaseOptions.Value;
    private readonly UploadOptions _uploadOptions = uploadOptions.Value;

    public async Task<PagedResult<ImageListItemDto>> GetApprovedAsync(Guid? categoryId, Guid? subcategoryId, int page, int pageSize) {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var countQuery = supabase.Client.From<ImageItem>()
            .Filter("status", Constants.Operator.Equals, "approved");
        if (categoryId.HasValue)
            countQuery = countQuery.Filter("category_id", Constants.Operator.Equals, categoryId.Value.ToString());
        if (subcategoryId.HasValue)
            countQuery = countQuery.Filter("subcategory_id", Constants.Operator.Equals, subcategoryId.Value.ToString());

        var countResult = await countQuery.Count(Constants.CountType.Exact);

        var listQuery = supabase.Client.From<ImageItem>()
            .Filter("status", Constants.Operator.Equals, "approved");
        if (categoryId.HasValue)
            listQuery = listQuery.Filter("category_id", Constants.Operator.Equals, categoryId.Value.ToString());
        if (subcategoryId.HasValue)
            listQuery = listQuery.Filter("subcategory_id", Constants.Operator.Equals, subcategoryId.Value.ToString());

        var page1 = await listQuery
            .Order("created_at", Constants.Ordering.Descending)
            .Range((page - 1) * pageSize, page * pageSize - 1)
            .Get();

        var items = await EnrichListAsync(page1.Models);
        return new PagedResult<ImageListItemDto>(items, page, pageSize, countResult);
    }

    public async Task<List<ImageListItemDto>> GetMineAsync(Guid userId) {
        var result = await supabase.Client.From<ImageItem>()
            .Filter("uploaded_by", Constants.Operator.Equals, userId.ToString())
            .Order("created_at", Constants.Ordering.Descending)
            .Get();

        return await EnrichListAsync(result.Models);
    }

    public async Task<List<ImageListItemDto>> GetPendingAsync() {
        var result = await supabase.Client.From<ImageItem>()
            .Filter("status", Constants.Operator.Equals, "pending")
            .Order("created_at", Constants.Ordering.Ascending)
            .Get();

        return await EnrichListAsync(result.Models);
    }

    public async Task<ImageDetailDto> GetByIdAsync(Guid id, bool registerClick) {
        var image = await GetEntityOrThrow(id);

        if (registerClick) {
            await supabase.Client.Rpc("increment_image_clicks", new Dictionary<string, object> { ["p_image_id"] = id });
            image.Clicks += 1;
        }

        return await EnrichDetailAsync(image);
    }

    public async Task<Stream> GetFileStreamAsync(Guid id, bool thumbnail) {
        var image = await GetEntityOrThrow(id);
        var path = thumbnail ? image.ThumbnailPath : image.OriginalPath;

        // OBS: a assinatura exata de Download() pode variar conforme a versão
        // instalada do pacote "Supabase" — confira IntelliSense/changelog se
        // o compilador reclamar aqui (ex: pode pedir um Action<float>? de progresso).
        var bytes = await supabase.Client.Storage.From(_supabaseOptions.StorageBucket).Download(path, (EventHandler<float>?)null);
        return new MemoryStream(bytes);
    }

    public async Task<(Stream Stream, string FileName)> GetDownloadAsync(Guid id) {
        var image = await GetEntityOrThrow(id);
        var bytes = await supabase.Client.Storage.From(_supabaseOptions.StorageBucket).Download(image.OriginalPath, (EventHandler<float>?)null);
        var safeTitle = string.IsNullOrWhiteSpace(image.Title) ? image.Id.ToString() : image.Title;
        var fileName = $"{safeTitle}.{image.OriginalExt}";
        return (new MemoryStream(bytes), fileName);
    }

    public async Task<ImageDetailDto> UploadAsync(Guid userId, Guid categoryId, Guid subcategoryId, string? title, IFormFile file) {
        ValidateFile(file);

        var category = await supabase.Client.From<Category>()
            .Filter("id", Constants.Operator.Equals, categoryId.ToString()).Single()
            ?? throw ApiException.NotFound("Categoria não encontrada.");

        var subcategory = await supabase.Client.From<Subcategory>()
            .Filter("id", Constants.Operator.Equals, subcategoryId.ToString()).Single()
            ?? throw ApiException.NotFound("Subcategoria não encontrada.");

        if (subcategory.CategoryId != category.Id)
            throw ApiException.BadRequest("A subcategoria selecionada não pertence à categoria selecionada.");

        var imageId = Guid.NewGuid();
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        await using var inputStream = file.OpenReadStream();
        using var image = await Image.LoadAsync(inputStream);

        var width = image.Width;
        var height = image.Height;

        // Gera o thumbnail otimizado em WebP — reduz drasticamente o tamanho
        // mantendo boa qualidade visual para o grid da galeria.
        using var thumbnail = image.Clone(ctx => ctx.Resize(new ResizeOptions {
            Mode = ResizeMode.Max,
            Size = new Size(_uploadOptions.ThumbnailMaxWidth, _uploadOptions.ThumbnailMaxHeight)
        }));

        using var thumbnailStream = new MemoryStream();
        await thumbnail.SaveAsync(thumbnailStream, new WebpEncoder { Quality = 80 });
        thumbnailStream.Position = 0;

        var originalPath = $"originals/{imageId}{ext}";
        var thumbnailPath = $"thumbnails/{imageId}.webp";

        inputStream.Position = 0;
        using var originalBuffer = new MemoryStream();
        await inputStream.CopyToAsync(originalBuffer);

        var bucket = supabase.Client.Storage.From(_supabaseOptions.StorageBucket);
        await bucket.Upload(originalBuffer.ToArray(), originalPath);
        await bucket.Upload(thumbnailStream.ToArray(), thumbnailPath);

        var entity = new ImageItem {
            Id = imageId,
            Title = title,
            OriginalPath = originalPath,
            ThumbnailPath = thumbnailPath,
            OriginalExt = ext.TrimStart('.'),
            MimeType = file.ContentType,
            FileSizeBytes = file.Length,
            Width = width,
            Height = height,
            CategoryId = categoryId,
            SubcategoryId = subcategoryId,
            UploadedBy = userId,
            Status = "pending",
            Clicks = 0,
            CreatedAt = DateTime.UtcNow
        };

        await supabase.Client.From<ImageItem>().Insert(entity);

        logger.LogInformation("Imagem {ImageId} enviada por {UserId}, aguardando aprovação.", imageId, userId);

        return await EnrichDetailAsync(entity);
    }

    public async Task ReviewAsync(Guid id, Guid adminId, bool approve) {
        var image = await GetEntityOrThrow(id);

        image.Status = approve ? "approved" : "rejected";
        image.ApprovedAt = DateTime.UtcNow;
        image.ApprovedBy = adminId;

        await supabase.Client.From<ImageItem>().Update(image);
    }

    public async Task DeleteAsync(Guid id, Guid requesterId, bool requesterIsAdmin) {
        var image = await GetEntityOrThrow(id);

        if (!requesterIsAdmin && image.UploadedBy != requesterId)
            throw ApiException.Forbidden("Você só pode remover suas próprias imagens.");

        var bucket = supabase.Client.Storage.From(_supabaseOptions.StorageBucket);
        await bucket.Remove(new List<string> { image.OriginalPath, image.ThumbnailPath });

        await supabase.Client.From<ImageItem>().Filter("id", Constants.Operator.Equals, id.ToString()).Delete();
    }

    // ------------------------------------------------------------------
    // Helpers privados
    // ------------------------------------------------------------------

    private void ValidateFile(IFormFile file) {
        if (file.Length == 0)
            throw ApiException.BadRequest("Arquivo vazio.");

        if (file.Length > _uploadOptions.MaxFileSizeBytes)
            throw ApiException.BadRequest($"Arquivo excede o tamanho máximo de {_uploadOptions.MaxFileSizeBytes / 1024 / 1024}MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_uploadOptions.AllowedExtensions.Contains(ext))
            throw ApiException.BadRequest(
                $"Extensão '{ext}' não permitida. Use: {string.Join(", ", _uploadOptions.AllowedExtensions)}.");
    }

    private async Task<ImageItem> GetEntityOrThrow(Guid id) {
        var image = await supabase.Client.From<ImageItem>()
            .Filter("id", Constants.Operator.Equals, id.ToString())
            .Single();

        return image ?? throw ApiException.NotFound("Imagem não encontrada.");
    }

    private async Task<List<ImageListItemDto>> EnrichListAsync(List<ImageItem> images) {
        if (images.Count == 0)
            return new List<ImageListItemDto>();

        var categories = (await supabase.Client.From<Category>().Get()).Models.ToDictionary(c => c.Id);
        var subcategories = (await supabase.Client.From<Subcategory>().Get()).Models.ToDictionary(s => s.Id);

        return images.Select(img => new ImageListItemDto(
            img.Id,
            img.Title,
            BuildPublicPath(img.ThumbnailPath, thumbnail: true, img.Id),
            img.CreatedAt,
            categories.GetValueOrDefault(img.CategoryId)?.Name ?? "—",
            subcategories.GetValueOrDefault(img.SubcategoryId)?.Name ?? "—",
            img.Clicks,
            img.FileSizeBytes,
            img.Width,
            img.Height,
            img.Status
        )).ToList();
    }

    private async Task<ImageDetailDto> EnrichDetailAsync(ImageItem img) {
        var category = await supabase.Client.From<Category>()
            .Filter("id", Constants.Operator.Equals, img.CategoryId.ToString()).Single();
        var subcategory = await supabase.Client.From<Subcategory>()
            .Filter("id", Constants.Operator.Equals, img.SubcategoryId.ToString()).Single();
        var uploader = await supabase.Client.From<Profile>()
            .Filter("id", Constants.Operator.Equals, img.UploadedBy.ToString()).Single();

        return new ImageDetailDto(
            img.Id,
            img.Title,
            BuildPublicPath(img.ThumbnailPath, thumbnail: true, img.Id),
            BuildPublicPath(img.OriginalPath, thumbnail: false, img.Id),
            img.CreatedAt,
            img.CategoryId,
            category?.Name ?? "—",
            img.SubcategoryId,
            subcategory?.Name ?? "—",
            img.Clicks,
            img.FileSizeBytes,
            img.Width,
            img.Height,
            img.Status,
            uploader?.Username ?? "—"
        );
    }

    /// <summary>
    /// As imagens não são servidas direto do Storage: o frontend chama o
    /// endpoint do próprio backend (GET /api/images/{id}/file ou /thumbnail),
    /// que faz o streaming a partir do Supabase Storage. Isso mantém o
    /// controle de aprovação e a contagem de acesso centralizados na API.
    /// </summary>
    private static string BuildPublicPath(string _, bool thumbnail, Guid imageId) =>
        thumbnail ? $"/api/images/{imageId}/thumbnail" : $"/api/images/{imageId}/file";
}