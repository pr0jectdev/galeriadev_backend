namespace ImageGallery.Api.Common;

public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string AnonKey { get; set; } = string.Empty;
    public string ServiceRoleKey { get; set; } = string.Empty;
    public string JwtSecret { get; set; } = string.Empty;
    public string StorageBucket { get; set; } = "gallery-images";
}

public class UploadOptions
{
    public const string SectionName = "Upload";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;
    public List<string> AllowedExtensions { get; set; } = new() { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    public int ThumbnailMaxWidth { get; set; } = 480;
    public int ThumbnailMaxHeight { get; set; } = 480;
}

public class CorsOptions
{
    public const string SectionName = "Cors";

    public List<string> AllowedOrigins { get; set; } = new();
}
