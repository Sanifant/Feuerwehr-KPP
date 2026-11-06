namespace De.Openelp.Feuerwehr.Server.Services
{
    /// <summary>
    /// Application outcome; MVC response construction belongs to the controller adapter.
    /// </summary>
    /// <param name="Code">The HTTP status code of the application outcome.</param>
    /// <param name="Value">The response payload carried by the application outcome.</param>
    public sealed record ServiceResult(int Code, object? Value = null)
    {
        /// <summary>
        /// Gets the MVC action used to generate the created-resource location.
        /// </summary>
        public string? ActionName { get; init; }
        /// <summary>
        /// Gets the route values used to generate the created-resource location.
        /// </summary>
        public object? RouteValues { get; init; }
        /// <summary>
        /// Gets the short title of a problem response.
        /// </summary>
        public string? ProblemTitle { get; init; }
        /// <summary>
        /// Gets the detailed explanation of a problem response.
        /// </summary>
        public string? ProblemDetail { get; init; }
        /// <summary>
        /// Gets the bytes returned as a downloadable file.
        /// </summary>
        public byte[]? Content { get; init; }
        /// <summary>
        /// Gets the MIME type of the response content.
        /// </summary>
        public string? ContentType { get; init; }
        /// <summary>
        /// Gets the filename suggested for the download.
        /// </summary>
        public string? DownloadName { get; init; }

        /// <summary>
        /// Creates a successful application outcome with HTTP status 200 and the supplied payload.
        /// </summary>
        /// <param name="value">The response payload, when present.</param>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult Ok(object? value) => new (200, value);
        /// <summary>
        /// Creates an application outcome with HTTP status 404 and optional error information.
        /// </summary>
        /// <param name="value">The response payload, when present.</param>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult NotFound(object? value = null) => new (404, value);
        /// <summary>
        /// Creates an application outcome with HTTP status 400 and validation details.
        /// </summary>
        /// <param name="value">The response payload, when present.</param>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult BadRequest(object? value) => new (400, value);
        /// <summary>
        /// Creates an application outcome with HTTP status 401 and optional authentication details.
        /// </summary>
        /// <param name="value">The response payload, when present.</param>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult Unauthorized(object? value = null) => new (401, value);
        /// <summary>
        /// Creates an application outcome with HTTP status 204 and no response body.
        /// </summary>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult NoContent() => new (204);
        /// <summary>
        /// Creates an application outcome with HTTP status 403.
        /// </summary>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult Forbid() => new (403);
        /// <summary>
        /// Creates an application outcome with the requested HTTP status and optional payload.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="value">The response payload, when present.</param>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult StatusCode(int statusCode, object? value = null) => new (statusCode, value);
        /// <summary>
        /// Creates an application outcome containing the requested problem title, detail, and HTTP status.
        /// </summary>
        /// <param name="statusCode">The HTTP status code of the response.</param>
        /// <param name="title">The title to include in the response or exported document.</param>
        /// <param name="detail">The detailed explanation included in the problem response.</param>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult Problem(int statusCode, string? title, string? detail = null) =>
            new (statusCode) { ProblemTitle = title, ProblemDetail = detail };
        /// <summary>
        /// Creates an HTTP 201 outcome with route information for the new resource's location.
        /// </summary>
        /// <param name="actionName">The MVC action used to generate the created-resource location.</param>
        /// <param name="routeValues">The route values used to generate the created-resource location.</param>
        /// <param name="value">The response payload, when present.</param>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult CreatedAtAction(string actionName, object routeValues, object value) =>
            new (201, value) { ActionName = actionName, RouteValues = routeValues };
        /// <summary>
        /// Creates a downloadable-file outcome with its content, MIME type, and suggested filename.
        /// </summary>
        /// <param name="content">The content to validate or return in the generated response.</param>
        /// <param name="contentType">The MIME type of the response content.</param>
        /// <param name="downloadName">The filename suggested for the download.</param>
        /// <returns>An application result containing the outcome of the service result operation, or validation, authorization, or conflict details on failure.</returns>
        public static ServiceResult File(byte[] content, string contentType, string downloadName) =>
            new (200) { Content = content, ContentType = contentType, DownloadName = downloadName };
    }
}
