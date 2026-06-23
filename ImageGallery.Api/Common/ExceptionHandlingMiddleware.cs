using System.Text.Json;

namespace ImageGallery.Api.Common;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado ao processar {Path}", context.Request.Path);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Erro interno do servidor." }));
        }
    }
}
