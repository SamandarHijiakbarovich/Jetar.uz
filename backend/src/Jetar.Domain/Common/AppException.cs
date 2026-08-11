namespace Jetar.Domain.Common;

/// <summary>
/// Biznes qoidasi buzilganda tashlanadi. Middleware buni HTTP status kodiga aylantiradi.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public AppException(string message, int statusCode = 400, string code = "bad_request")
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public static AppException NotFound(string what) => new($"{what} topilmadi.", 404, "not_found");
    public static AppException Forbidden(string message = "Bu amal uchun ruxsat yo'q.") => new(message, 403, "forbidden");
    public static AppException Conflict(string message) => new(message, 409, "conflict");
    public static AppException Unauthorized(string message = "Avtorizatsiya talab qilinadi.") => new(message, 401, "unauthorized");
}
