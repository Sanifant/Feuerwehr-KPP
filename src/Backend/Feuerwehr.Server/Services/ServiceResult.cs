namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>Application outcome; MVC response construction belongs to the controller adapter.</summary>
    /// <param name="Code">The code.</param>
    /// <param name="Value">The value.</param>
    public sealed record ServiceResult(int Code, object? Value = null)
    {
        public string? ActionName { get; init; }
        public object? RouteValues { get; init; }
        public string? ProblemTitle { get; init; }
        public string? ProblemDetail { get; init; }
        public byte[]? Content { get; init; }
        public string? ContentType { get; init; }
        public string? DownloadName { get; init; }

        /// <summary>
        /// Executes ok.
        /// </summary>
        public static ServiceResult Ok(object? value) => new(200, value);
        /// <summary>
        /// Executes not Found.
        /// </summary>
        /// <param name="value">The value.</param>
        public static ServiceResult NotFound(object? value = null) => new(404, value);
        /// <summary>
        /// Executes bad Request.
        /// </summary>
        public static ServiceResult BadRequest(object? value) => new(400, value);
        /// <summary>
        /// Executes unauthorized.
        /// </summary>
        /// <param name="value">The value.</param>
        public static ServiceResult Unauthorized(object? value = null) => new(401, value);
        /// <summary>
        /// Executes no Content.
        /// </summary>
        public static ServiceResult NoContent() => new(204);
        /// <summary>
        /// Executes forbid.
        /// </summary>
        public static ServiceResult Forbid() => new(403);
        /// <summary>
        /// Executes status Code.
        /// </summary>
        /// <param name="statusCode">The status Code.</param>
        /// <param name="value">The value.</param>
        public static ServiceResult StatusCode(int statusCode, object? value = null) => new(statusCode, value);
        /// <summary>
        /// Executes problem.
        /// </summary>
        /// <param name="statusCode">The status Code.</param>
        /// <param name="title">The title.</param>
        /// <param name="detail">The detail.</param>
        public static ServiceResult Problem(int statusCode, string? title, string? detail = null) =>
            new(statusCode) { ProblemTitle = title, ProblemDetail = detail };
        /// <summary>
        /// Executes created At Action.
        /// </summary>
        /// <param name="actionName">The action Name.</param>
        /// <param name="routeValues">The route Values.</param>
        /// <param name="value">The value.</param>
        public static ServiceResult CreatedAtAction(string actionName, object routeValues, object value) =>
            new(201, value) { ActionName = actionName, RouteValues = routeValues };
        /// <summary>
        /// Executes file.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="contentType">The content Type.</param>
        /// <param name="downloadName">The download Name.</param>
        public static ServiceResult File(byte[] content, string contentType, string downloadName) =>
            new(200) { Content = content, ContentType = contentType, DownloadName = downloadName };
    }

}
