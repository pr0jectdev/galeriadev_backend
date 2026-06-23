using System.Security.Claims;
using ImageGallery.Api.Common;
using ImageGallery.Api.Models;
using Supabase.Postgrest;

namespace ImageGallery.Api.Services;

public interface ICurrentUserService
{
    Guid UserId { get; }
    bool IsAuthenticated { get; }
    Task<Profile> GetProfileAsync();
    Task EnsureApprovedAsync();
    Task EnsureAdminAsync();
}

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    ISupabaseClientProvider supabase) : ICurrentUserService
{
    private Profile? _cachedProfile;

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var sub = httpContextAccessor.HttpContext?.User.FindFirstValue("sub")
                      ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(sub) || !Guid.TryParse(sub, out var id))
                throw ApiException.Unauthorized("Usuário não autenticado.");

            return id;
        }
    }

    public async Task<Profile> GetProfileAsync()
    {
        if (_cachedProfile is not null)
            return _cachedProfile;

        var userId = UserId;
        var result = await supabase.Client
            .From<Profile>()
            .Filter("id", Constants.Operator.Equals, userId.ToString())
            .Single();

        _cachedProfile = result ?? throw ApiException.NotFound("Perfil não encontrado.");
        return _cachedProfile;
    }

    public async Task EnsureApprovedAsync()
    {
        var profile = await GetProfileAsync();
        if (profile.Status != "approved")
            throw ApiException.Forbidden("Sua conta ainda não foi aprovada por um administrador.");
    }

    public async Task EnsureAdminAsync()
    {
        var profile = await GetProfileAsync();
        if (profile.Role != "admin")
            throw ApiException.Forbidden("Acesso restrito a administradores.");
        if (profile.Status != "approved")
            throw ApiException.Forbidden("Sua conta ainda não foi aprovada.");
    }
}
