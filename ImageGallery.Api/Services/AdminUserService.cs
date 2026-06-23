using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ImageGallery.Api.Common;
using ImageGallery.Api.DTOs;
using ImageGallery.Api.Models;
using Microsoft.Extensions.Options;
using Supabase.Postgrest;

namespace ImageGallery.Api.Services;

public interface IAdminUserService {
    Task<List<AdminUserDto>> GetAllAsync(string? statusFilter);
    Task ApproveAsync(Guid id, Guid adminId);
    Task RejectAsync(Guid id, Guid adminId);
    Task DeleteAsync(Guid id);
    Task<AdminUserDto> CreateUserAsync(CreateUserByAdminRequest request, Guid adminId);
}

public class AdminUserService(
    ISupabaseClientProvider supabase,
    IHttpClientFactory httpClientFactory,
    IOptions<SupabaseOptions> options) : IAdminUserService {
    private readonly SupabaseOptions _options = options.Value;

    public async Task<List<AdminUserDto>> GetAllAsync(string? statusFilter) {
        var query = supabase.Client.From<Profile>();

        var result = string.IsNullOrEmpty(statusFilter)
            ? await query.Order("created_at", Constants.Ordering.Descending).Get()
            : await query.Filter("status", Constants.Operator.Equals, statusFilter)
                .Order("created_at", Constants.Ordering.Descending).Get();

        return result.Models.Select(p => new AdminUserDto(p.Id, p.Username, p.Email, p.Role, p.Status, p.CreatedAt)).ToList();
    }

    public async Task ApproveAsync(Guid id, Guid adminId) {
        var profile = await supabase.Client.From<Profile>()
            .Filter("id", Constants.Operator.Equals, id.ToString()).Single()
            ?? throw ApiException.NotFound("Usuário não encontrado.");

        profile.Status = "approved";
        profile.ApprovedAt = DateTime.UtcNow;
        profile.ApprovedBy = adminId;

        await supabase.Client.From<Profile>().Update(profile);
    }

    public async Task RejectAsync(Guid id, Guid adminId) {
        var profile = await supabase.Client.From<Profile>()
            .Filter("id", Constants.Operator.Equals, id.ToString()).Single()
            ?? throw ApiException.NotFound("Usuário não encontrado.");

        profile.Status = "rejected";
        profile.ApprovedAt = DateTime.UtcNow;
        profile.ApprovedBy = adminId;

        await supabase.Client.From<Profile>().Update(profile);
    }

    public async Task DeleteAsync(Guid id) {
        // Remove do Supabase Auth (o que cascateia para public.profiles via FK on delete cascade)
        var http = CreateServiceClient();
        var response = await http.DeleteAsync($"auth/v1/admin/users/{id}");
        if (!response.IsSuccessStatusCode)
            throw ApiException.BadRequest("Não foi possível remover o usuário.");
    }

    public async Task<AdminUserDto> CreateUserAsync(CreateUserByAdminRequest request, Guid adminId) {
        var http = CreateServiceClient();

        var response = await http.PostAsJsonAsync("auth/v1/admin/users", new {
            email = request.Email,
            password = request.Password,
            email_confirm = true,
            user_metadata = new { username = request.Username }
        });

        if (!response.IsSuccessStatusCode) {
            var error = await response.Content.ReadAsStringAsync();
            throw ApiException.BadRequest($"Não foi possível criar o usuário: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var userId = Guid.Parse(json.GetProperty("id").GetString()!);

        // Usuário criado pelo admin já entra aprovado e com o papel definido.
        await supabase.Client.From<Profile>()
            .Filter("id", Constants.Operator.Equals, userId.ToString())
            .Set(x => x.Role!, request.Role)
            .Set(x => x.Status!, "approved")
            .Set(x => x.ApprovedAt!, DateTime.UtcNow)
            .Set(x => x.ApprovedBy!, adminId)
            .Update();

        return new AdminUserDto(userId, request.Username, request.Email, request.Role, "approved", DateTime.UtcNow);
    }

    private HttpClient CreateServiceClient() {
        var http = httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(_options.Url.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Add("apikey", _options.ServiceRoleKey);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ServiceRoleKey);
        return http;
    }
}