using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ImageGallery.Api.Models;

[Table("categories")]
public class Category : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }
}
