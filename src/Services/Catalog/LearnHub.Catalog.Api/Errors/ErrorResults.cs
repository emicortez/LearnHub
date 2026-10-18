using LearnHub.SharedKernel;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;

namespace LearnHub.Catalog.Api.Errors;

/// <summary>
/// The single translation of a business <see cref="Error"/> into an RFC 9457 Problem Details response.
/// </summary>
public static class ErrorResults
{
    public static ProblemHttpResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, "Unknown error type."),
        };

        // The title describes the status; the stable error code goes in its own extension so clients can branch on it.
        return TypedResults.Problem(
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: error.Description,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = error.Code });
    }
}
