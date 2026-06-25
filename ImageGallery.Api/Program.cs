using System.Text;
using ImageGallery.Api.Common;
using ImageGallery.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ----------------------- Configuração -----------------------
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.SectionName));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

var supabaseConfig = builder.Configuration.GetSection(SupabaseOptions.SectionName).Get<SupabaseOptions>()!;
var corsConfig = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

// ----------------------- Serviços -----------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Cria e inicializa o cliente Supabase (com a service role key) UMA vez,
// antes da aplicação subir — evita qualquer chamada bloqueante em runtime.
var supabaseClient = new Supabase.Client(supabaseConfig.Url, supabaseConfig.ServiceRoleKey, new Supabase.SupabaseOptions {
    AutoConnectRealtime = false,
    AutoRefreshToken = false
});
await supabaseClient.InitializeAsync();
builder.Services.AddSingleton(supabaseClient);
builder.Services.AddSingleton<ISupabaseClientProvider, SupabaseClientProvider>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<ISuggestionService, SuggestionService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----------------------- CORS (Angular dev server / produção) -----------------------
builder.Services.AddCors(options => {
    options.AddPolicy("Frontend", policy => {
        policy.WithOrigins(corsConfig.AllowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

//builder.Services.AddCors(options => {
//    options.AddPolicy("AllowFrontend", policy => {
//        policy.WithOrigins("https://galeriadev-frontend-git-dev-marcdevorg.vercel.app/")
//              .AllowAnyHeader()
//              .AllowAnyMethod();
//    });
//});

// ----------------------- Autenticação JWT (tokens emitidos pelo Supabase Auth) -----------------------
// Desde maio/2025 todo projeto novo do Supabase usa "JWT Signing Keys" assimétricas
// (RS256/ES256) por padrão, em vez do antigo segredo compartilhado (HS256). Por isso
// buscamos as chaves públicas no endpoint JWKS do projeto. Também mantemos o segredo
// simétrico (se configurado) como uma chave válida a mais — cobre projetos antigos
// que ainda usam o "Legacy JWT Secret".
var signingKeys = new List<SecurityKey>();

if (!string.IsNullOrWhiteSpace(supabaseConfig.JwtSecret)) {
    signingKeys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseConfig.JwtSecret)));
}

try {
    using var jwksHttpClient = new HttpClient();
    var jwksJson = await jwksHttpClient.GetStringAsync(
        $"{supabaseConfig.Url.TrimEnd('/')}/auth/v1/.well-known/jwks.json");
    var jwks = new JsonWebKeySet(jwksJson);
    signingKeys.AddRange(jwks.GetSigningKeys());
}
catch (Exception ex) {
    Console.WriteLine($"[Aviso] Não foi possível buscar o JWKS do Supabase ({ex.Message}). " +
                       "A validação de token vai depender só do Supabase:JwtSecret, se configurado.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseConfig.Url.TrimEnd('/')}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

//if (app.Environment.IsDevelopment()) {
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
//app.UseCors("AllowFrontend");
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();