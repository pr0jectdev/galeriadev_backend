using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ImageGallery.Api.Models;

[Table("images")]
public class ImageItem : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("title")]
    public string? Title { get; set; }

    [Column("original_path")]
    public string OriginalPath { get; set; } = string.Empty;

    [Column("thumbnail_path")]
    public string ThumbnailPath { get; set; } = string.Empty;

    [Column("original_ext")]
    public string OriginalExt { get; set; } = string.Empty;

    [Column("mime_type")]
    public string MimeType { get; set; } = string.Empty;

    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; }

    [Column("width")]
    public int Width { get; set; }

    [Column("height")]
    public int Height { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("subcategory_id")]
    public Guid SubcategoryId { get; set; }

    [Column("uploaded_by")]
    public Guid UploadedBy { get; set; }

    /// <summary>"pending", "approved" ou "rejected"</summary>
    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("clicks")]
    public int Clicks { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("approved_by")]
    public Guid? ApprovedBy { get; set; }
}
