using ImageGallery.Api.Common;
using ImageGallery.Api.DTOs;
using ImageGallery.Api.Models;
using Supabase.Postgrest;

namespace ImageGallery.Api.Services;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, Guid adminId);
    Task DeleteAsync(Guid id);

    Task<SubcategoryDto> CreateSubcategoryAsync(CreateSubcategoryRequest request, Guid adminId);
    Task DeleteSubcategoryAsync(Guid id);
}

public class CategoryService(ISupabaseClientProvider supabase) : ICategoryService
{
    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await supabase.Client.From<Category>().Order("name", Constants.Ordering.Ascending).Get();
        var subcategories = await supabase.Client.From<Subcategory>().Order("name", Constants.Ordering.Ascending).Get();

        return categories.Models.Select(c => new CategoryDto(
            c.Id,
            c.Name,
            c.Slug,
            subcategories.Models
                .Where(s => s.CategoryId == c.Id)
                .Select(s => new SubcategoryDto(s.Id, s.CategoryId, s.Name, s.Slug))
                .ToList()
        )).ToList();
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, Guid adminId)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw ApiException.BadRequest("Nome da categoria é obrigatório.");

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = Slugify(name),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminId
        };

        try
        {
            await supabase.Client.From<Category>().Insert(category);
        }
        catch (Exception)
        {
            throw ApiException.Conflict("Já existe uma categoria com esse nome.");
        }

        return new CategoryDto(category.Id, category.Name, category.Slug, new List<SubcategoryDto>());
    }

    public async Task DeleteAsync(Guid id)
    {
        await supabase.Client.From<Category>().Filter("id", Constants.Operator.Equals, id.ToString()).Delete();
    }

    public async Task<SubcategoryDto> CreateSubcategoryAsync(CreateSubcategoryRequest request, Guid adminId)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw ApiException.BadRequest("Nome da subcategoria é obrigatório.");

        var categoryExists = await supabase.Client
            .From<Category>()
            .Filter("id", Constants.Operator.Equals, request.CategoryId.ToString())
            .Single();

        if (categoryExists is null)
            throw ApiException.NotFound("Categoria não encontrada.");

        var subcategory = new Subcategory
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = name,
            Slug = Slugify(name),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminId
        };

        try
        {
            await supabase.Client.From<Subcategory>().Insert(subcategory);
        }
        catch (Exception)
        {
            throw ApiException.Conflict("Já existe uma subcategoria com esse nome nessa categoria.");
        }

        return new SubcategoryDto(subcategory.Id, subcategory.CategoryId, subcategory.Name, subcategory.Slug);
    }

    public async Task DeleteSubcategoryAsync(Guid id)
    {
        await supabase.Client.From<Subcategory>().Filter("id", Constants.Operator.Equals, id.ToString()).Delete();
    }

    public static string Slugify(string value)
    {
        var slug = value.Trim().ToLowerInvariant();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");
        return slug;
    }
}
