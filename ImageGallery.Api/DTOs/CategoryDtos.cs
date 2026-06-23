namespace ImageGallery.Api.DTOs;

public record CategoryDto(Guid Id, string Name, string Slug, List<SubcategoryDto> Subcategories);

public record SubcategoryDto(Guid Id, Guid CategoryId, string Name, string Slug);

public record CreateCategoryRequest(string Name);

public record CreateSubcategoryRequest(Guid CategoryId, string Name);

public record SuggestionDto(
    Guid Id,
    string Name,
    Guid? CategoryId,
    string? CategoryName,
    Guid SuggestedBy,
    string SuggestedByUsername,
    string Status,
    DateTime CreatedAt);

public record CreateCategorySuggestionRequest(string Name);

public record CreateSubcategorySuggestionRequest(Guid CategoryId, string Name);

public record ReviewSuggestionRequest(bool Approve);
