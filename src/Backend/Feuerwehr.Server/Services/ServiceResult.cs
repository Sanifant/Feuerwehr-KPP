namespace de.openelp.feuerwehr.Server.Services;

/// <summary>Application outcome; MVC response construction belongs to the controller adapter.</summary>
public sealed record ServiceResult(int Code, object? Value = null)
{
    public string? ActionName { get; init; }
    public object? RouteValues { get; init; }
    public string? ProblemTitle { get; init; }
    public string? ProblemDetail { get; init; }
    public byte[]? Content { get; init; }
    public string? ContentType { get; init; }
    public string? DownloadName { get; init; }

    public static ServiceResult Ok(object? value) => new(200, value);
    public static ServiceResult NotFound(object? value = null) => new(404, value);
    public static ServiceResult BadRequest(object? value) => new(400, value);
    public static ServiceResult Unauthorized(object? value = null) => new(401, value);
    public static ServiceResult NoContent() => new(204);
    public static ServiceResult Forbid() => new(403);
    public static ServiceResult StatusCode(int statusCode, object? value = null) => new(statusCode, value);
    public static ServiceResult Problem(int statusCode, string? title, string? detail = null) =>
        new(statusCode) { ProblemTitle = title, ProblemDetail = detail };
    public static ServiceResult CreatedAtAction(string actionName, object routeValues, object value) =>
        new(201, value) { ActionName = actionName, RouteValues = routeValues };
    public static ServiceResult File(byte[] content, string contentType, string downloadName) =>
        new(200) { Content = content, ContentType = contentType, DownloadName = downloadName };
}
