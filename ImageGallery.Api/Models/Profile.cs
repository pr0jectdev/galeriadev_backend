using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace ImageGallery.Api.Models;

[Table("profiles")]
public class Profile : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>"user" ou "admin"</summary>
    [Column("role")]
    public string Role { get; set; } = "user";

    /// <summary>"pending", "approved" ou "rejected"</summary>
    [Column("status")]
    public string Status { get; set; } = "pending";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("approved_by")]
    public Guid? ApprovedBy { get; set; }
}
