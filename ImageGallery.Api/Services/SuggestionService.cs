using ImageGallery.Api.Common;
using ImageGallery.Api.DTOs;
using ImageGallery.Api.Models;
using Supabase.Postgrest;

namespace ImageGallery.Api.Services;

public interface ISuggestionService {
    Task<SuggestionDto> CreateCategorySuggestionAsync(Guid userId, CreateCategorySuggestionRequest request);
    Task<SuggestionDto> CreateSubcategorySuggestionAsync(Guid userId, CreateSubcategorySuggestionRequest request);
    Task<List<SuggestionDto>> GetPendingCategorySuggestionsAsync();
    Task<List<SuggestionDto>> GetPendingSubcategorySuggestionsAsync();
    Task ReviewCategorySuggestionAsync(Guid id, Guid adminId, bool approve);
    Task ReviewSubcategorySuggestionAsync(Guid id, Guid adminId, bool approve);
}

public class SuggestionService(ISupabaseClientProvider supabase, ICategoryService categoryService) : ISuggestionService {
    public async Task<SuggestionDto> CreateCategorySuggestionAsync(Guid userId, CreateCategorySuggestionRequest request) {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw ApiException.BadRequest("Informe o nome da categoria sugerida.");

        var entity = new CategorySuggestion {
            Id = Guid.NewGuid(),
            Name = name,
            SuggestedBy = userId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await supabase.Client.From<CategorySuggestion>().Insert(entity);

        var user = await GetUsername(userId);
        return new SuggestionDto(entity.Id, entity.Name, null, null, userId, user, entity.Status, entity.CreatedAt);
    }

    public async Task<SuggestionDto> CreateSubcategorySuggestionAsync(Guid userId, CreateSubcategorySuggestionRequest request) {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw ApiException.BadRequest("Informe o nome da subcategoria sugerida.");

        var category = await supabase.Client.From<Category>()
            .Filter("id", Constants.Operator.Equals, request.CategoryId.ToString()).Single()
            ?? throw ApiException.NotFound("Categoria não encontrada.");

        var entity = new SubcategorySuggestion {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = name,
            SuggestedBy = userId,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        await supabase.Client.From<SubcategorySuggestion>().Insert(entity);

        var user = await GetUsername(userId);
        return new SuggestionDto(entity.Id, entity.Name, category.Id, category.Name, userId, user, entity.Status, entity.CreatedAt);
    }

    public async Task<List<SuggestionDto>> GetPendingCategorySuggestionsAsync() {
        var result = await supabase.Client.From<CategorySuggestion>()
            .Filter("status", Constants.Operator.Equals, "pending")
            .Order("created_at", Constants.Ordering.Ascending)
            .Get();

        var list = new List<SuggestionDto>();
        foreach (var s in result.Models)
            list.Add(new SuggestionDto(s.Id, s.Name, null, null, s.SuggestedBy, await GetUsername(s.SuggestedBy), s.Status, s.CreatedAt));

        return list;
    }

    public async Task<List<SuggestionDto>> GetPendingSubcategorySuggestionsAsync() {
        var result = await supabase.Client.From<SubcategorySuggestion>()
            .Filter("status", Constants.Operator.Equals, "pending")
            .Order("created_at", Constants.Ordering.Ascending)
            .Get();

        var categories = (await supabase.Client.From<Category>().Get()).Models.ToDictionary(c => c.Id);

        var list = new List<SuggestionDto>();
        foreach (var s in result.Models) {
            categories.TryGetValue(s.CategoryId, out var category);
            list.Add(new SuggestionDto(s.Id, s.Name, s.CategoryId, category?.Name, s.SuggestedBy, await GetUsername(s.SuggestedBy), s.Status, s.CreatedAt));
        }

        return list;
    }

    public async Task ReviewCategorySuggestionAsync(Guid id, Guid adminId, bool approve) {
        var suggestion = await supabase.Client.From<CategorySuggestion>()
            .Filter("id", Constants.Operator.Equals, id.ToString()).Single()
            ?? throw ApiException.NotFound("Sugestão não encontrada.");

        if (approve)
            await categoryService.CreateAsync(new CreateCategoryRequest(suggestion.Name), adminId);

        suggestion.Status = approve ? "approved" : "rejected";
        suggestion.ReviewedAt = DateTime.UtcNow;
        suggestion.ReviewedBy = adminId;

        await supabase.Client.From<CategorySuggestion>().Update(suggestion);
    }

    public async Task ReviewSubcategorySuggestionAsync(Guid id, Guid adminId, bool approve) {
        var suggestion = await supabase.Client.From<SubcategorySuggestion>()
            .Filter("id", Constants.Operator.Equals, id.ToString()).Single()
            ?? throw ApiException.NotFound("Sugestão não encontrada.");

        if (approve)
            await categoryService.CreateSubcategoryAsync(
                new CreateSubcategoryRequest(suggestion.CategoryId, suggestion.Name), adminId);

        suggestion.Status = approve ? "approved" : "rejected";
        suggestion.ReviewedAt = DateTime.UtcNow;
        suggestion.ReviewedBy = adminId;

        await supabase.Client.From<SubcategorySuggestion>().Update(suggestion);
    }

    private async Task<string> GetUsername(Guid userId) {
        var profile = await supabase.Client.From<Profile>()
            .Filter("id", Constants.Operator.Equals, userId.ToString()).Single();
        return profile?.Username ?? "—";
    }
}