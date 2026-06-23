using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImageGallery.Api.Common;
using ImageGallery.Api.DTOs;
using ImageGallery.Api.Models;
using Microsoft.Extensions.Options;
using Supabase.Postgrest;

namespace ImageGallery.Api.Services;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}

public class AuthService(
    IHttpClientFactory httpClientFactory,
    IOptions<SupabaseOptions> options,
    ISupabaseClientProvider supabase) : IAuthService
{
    private readonly SupabaseOptions _options = options.Value;

    public async Task<UserDto> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw ApiException.BadRequest("E-mail e senha são obrigatórios.");

        if (request.Password.Length < 8)
            throw ApiException.BadRequest("A senha deve ter no mínimo 8 caracteres.");

        var http = CreateAnonClient();

        var response = await http.PostAsJsonAsync("auth/v1/signup", new
        {
            email = request.Email,
            password = request.Password,
            data = new { username = request.Username }
        });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw ApiException.BadRequest($"Não foi possível cadastrar: {error}");
        }

        // O trigger on_auth_user_created (ver schema.sql) já cria a linha em
        // public.profiles com status = 'pending'. Buscamos para retornar ao cliente.
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var userId = json.GetProperty("id").GetString();

        var profile = await supabase.Client
            .From<Profile>()
            .Filter("id", Constants.Operator.Equals, userId)
            .Single();

        if (profile is null)
        {
            // fallback caso o trigger ainda não tenha processado: cria manualmente
            profile = new Profile
            {
                Id = Guid.Parse(userId!),
                Username = request.Username,
                Email = request.Email,
                Role = "user",
                Status = "pending",
                CreatedAt = DateTime.UtcNow
            };
            await supabase.Client.From<Profile>().Insert(profile);
        }

        return new UserDto(profile.Id, profile.Username, profile.Email, profile.Role, profile.Status);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var http = CreateAnonClient();

        var response = await http.PostAsJsonAsync("auth/v1/token?grant_type=password", new
        {
            email = request.Email,
            password = request.Password
        });

        if (!response.IsSuccessStatusCode)
            throw ApiException.Unauthorized("E-mail ou senha inválidos.");

        var tokenResult = await response.Content.ReadFromJsonAsync<SupabaseTokenResponse>()
            ?? throw ApiException.Unauthorized("Falha ao autenticar.");

        var profile = await supabase.Client
            .From<Profile>()
            .Filter("id", Constants.Operator.Equals, tokenResult.User.Id)
            .Single();

        if (profile is null)
            throw ApiException.NotFound("Perfil de usuário não encontrado.");

        if (profile.Status == "rejected")
            throw ApiException.Forbidden("Seu cadastro foi rejeitado. Entre em contato com o administrador.");

        if (profile.Status == "pending")
            throw ApiException.Forbidden("Seu cadastro está aguardando aprovação de um administrador.");

        var expiresAt = DateTime.UtcNow.AddSeconds(tokenResult.ExpiresIn);

        return new AuthResponse(
            tokenResult.AccessToken,
            expiresAt,
            new UserDto(profile.Id, profile.Username, profile.Email, profile.Role, profile.Status));
    }

    private HttpClient CreateAnonClient()
    {
        var http = httpClientFactory.CreateClient();
        http.BaseAddress = new Uri(_options.Url.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Add("apikey", _options.AnonKey);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AnonKey);
        return http;
    }

    private class SupabaseTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("user")]
        public SupabaseTokenUser User { get; set; } = new();
    }

    private class SupabaseTokenUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }
}
