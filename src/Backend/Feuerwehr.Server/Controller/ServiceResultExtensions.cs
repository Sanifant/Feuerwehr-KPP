using de.openelp.feuerwehr.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace de.openelp.feuerwehr.Server.Controller;

internal static class ServiceResultExtensions
{
    public static ActionResult ToActionResult(this ControllerBase controller, ServiceResult result)
    {
        if (result.Content is not null)
            return controller.File(result.Content, result.ContentType!, result.DownloadName);
        if (result.ProblemTitle is not null)
            return controller.Problem(statusCode: result.Code, title: result.ProblemTitle, detail: result.ProblemDetail);
        if (result.ActionName is not null)
            return controller.CreatedAtAction(result.ActionName, result.RouteValues, result.Value);

        return result.Code switch
        {
            200 => controller.Ok(result.Value),
            204 => controller.NoContent(),
            403 => controller.Forbid(),
            400 => controller.BadRequest(result.Value),
            401 => result.Value is null ? controller.Unauthorized() : controller.Unauthorized(result.Value),
            404 => result.Value is null ? controller.NotFound() : controller.NotFound(result.Value),
            _ => result.Value is null ? controller.StatusCode(result.Code) : controller.StatusCode(result.Code, result.Value),
        };
    }
}
