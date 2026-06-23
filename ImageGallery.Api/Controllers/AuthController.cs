using ImageGallery.Api.DTOs;
using ImageGallery.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageGallery.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Cadastro de novo usuário — entra com status "pending" até aprovação do admin.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request)
    {
        var user = await authService.RegisterAsync(request);
        return Ok(user);
    }

    /// <summary>Login — bloqueado caso o usuário ainda não tenha sido aprovado.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>Retorna os dados do usuário autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        var profile = await currentUser.GetProfileAsync();
        return Ok(new UserDto(profile.Id, profile.Username, profile.Email, profile.Role, profile.Status));
    }
}
