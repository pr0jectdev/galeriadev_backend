namespace ImageGallery.Api.DTOs;

public record ImageListItemDto(
    Guid Id,
    string? Title,
    string ThumbnailUrl,
    DateTime CreatedAt,
    string CategoryName,
    string SubcategoryName,
    int Clicks,
    long FileSizeBytes,
    int Width,
    int Height,
    string Status);

public record ImageDetailDto(
    Guid Id,
    string? Title,
    string ThumbnailUrl,
    string OriginalUrl,
    DateTime CreatedAt,
    Guid CategoryId,
    string CategoryName,
    Guid SubcategoryId,
    string SubcategoryName,
    int Clicks,
    long FileSizeBytes,
    int Width,
    int Height,
    string Status,
    string UploadedByUsername);

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount);

public record ReviewImageRequest(bool Approve);
