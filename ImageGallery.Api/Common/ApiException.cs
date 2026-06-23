namespace ImageGallery.Api.Common;

/// <summary>
/// Exceção lançada pelos services para erros de negócio esperados
/// (ex: "categoria não encontrada", "usuário não aprovado").
/// O middleware global converte isso em uma resposta HTTP apropriada.
/// </summary>
public class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ApiException NotFound(string message) => new(404, message);
    public static ApiException BadRequest(string message) => new(400, message);
    public static ApiException Forbidden(string message) => new(403, message);
    public static ApiException Unauthorized(string message) => new(401, message);
    public static ApiException Conflict(string message) => new(409, message);
}
