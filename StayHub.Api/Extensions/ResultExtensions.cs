using Microsoft.AspNetCore.Mvc;
using StayHub.Domain.Abstractions;

namespace StayHub.Api.Extensions;

public static class ResultExtensions
{
    public static ObjectResult ToProblemDetails(this Result result, ControllerBase controller)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Cannot convert a successful result to a problem.");
        }

        return controller.Problem(
            type: GetType(result.Error.Type),
            title: GetTitle(result.Error.Type),
            detail: result.Error.Message,
            statusCode: GetStatusCode(result.Error.Type),
            extensions: new Dictionary<string, object?>
            {
                { "errorCode", result.Error.Code }
            });
    }

    private static int GetStatusCode(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status403Forbidden,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };
    }

    private static string GetTitle(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.NotFound => "Resource not found",
            ErrorType.Conflict => "Conflict",
            ErrorType.Unauthorized => "Forbidden",
            ErrorType.Validation => "Validation error",
            _ => "Bad request"
        };
    }

    private static string GetType(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            ErrorType.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            ErrorType.Validation => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            _ => "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        };
    }
}