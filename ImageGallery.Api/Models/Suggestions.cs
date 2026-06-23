using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ImageGallery.Api.Models;

[Table("category_suggestions")]
public class CategorySuggestion : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("suggested_by")]
    public Guid SuggestedBy { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("reviewed_by")]
    public Guid? ReviewedBy { get; set; }
}

[Table("subcategory_suggestions")]
public class SubcategorySuggestion : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("suggested_by")]
    public Guid SuggestedBy { get; set; }

    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("reviewed_at")]
    public DateTime? ReviewedAt { get; set; }

    [Column("reviewed_by")]
    public Guid? ReviewedBy { get; set; }
}
