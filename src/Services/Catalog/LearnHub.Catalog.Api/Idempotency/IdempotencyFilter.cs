using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LearnHub.Catalog.Api.Authentication;
using LearnHub.Catalog.Api.Errors;
using LearnHub.SharedKernel;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.WebUtilities;

namespace LearnHub.Catalog.Api.Idempotency;

/// <summary>
/// Makes an endpoint safe to retry: a request carrying an <c>Idempotency-Key</c> runs the handler once,
/// and retries with the same key and body replay the stored response. Without the header nothing changes.
/// </summary>
/// <typeparam name="TRequest">The bound request body, which identifies what the key was used for.</typeparam>
public sealed class IdempotencyFilter<TRequest>(IIdempotencyStore store) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";
    public const string ReplayedHeaderName = "Idempotent-Replayed";
    private const int MaxKeyLength = 255;

    // An HTTP concern, not a business rule, so the error lives in the Api rather than in a domain errors class.
    public static readonly Error InvalidKey = Error.Validation(
        "catalog.idempotency_key.invalid", $"The {HeaderName} header must be between 1 and {MaxKeyLength} characters.");

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var httpContext = context.HttpContext;
        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            return await next(context);
        }

        var key = values.Count == 1 ? values[0] : null;
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaxKeyLength)
        {
            return InvalidKey.ToProblem();
        }

        // The endpoint requires a signed-in user, so the key is always scoped to who sent it.
        key = IdempotencyKeys.ForUser(httpContext.User.GetUserId(), key);
        var fingerprint = Fingerprint(httpContext.Request, context.Arguments.OfType<TRequest>().Single());
        var existing = await store.TryBeginAsync(key, fingerprint, httpContext.RequestAborted);

        if (existing is not null)
        {
            // While in flight the first request may still fail and release the key, so the client is told to wait
            // whatever the body; once completed, a different body is a misuse of the key.
            if (existing.Response is null)
            {
                return Problem(StatusCodes.Status409Conflict, "catalog.idempotency_key.in_progress",
                    "A request with this idempotency key is still being processed. Retry later.");
            }

            if (existing.Fingerprint != fingerprint)
            {
                return Problem(StatusCodes.Status422UnprocessableEntity, "catalog.idempotency_key.reused",
                    "This idempotency key was already used with a different request.");
            }

            return Replay(httpContext, existing.Response);
        }

        object? result;
        try
        {
            result = await next(context);
        }
        catch
        {
            await store.ReleaseAsync(key, CancellationToken.None);
            throw;
        }

        if (result is not IResult inner)
        {
            await store.ReleaseAsync(key, CancellationToken.None);
            throw new InvalidOperationException($"{nameof(IdempotencyFilter<TRequest>)} only supports endpoints that return IResult.");
        }

        return new SnapshotResult(inner, store, key);
    }

    private static string Fingerprint(HttpRequest request, TRequest body)
    {
        var text = $"{request.Method}\n{request.Path}\n{JsonSerializer.Serialize(body)}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    // 409 and 422 here are HTTP-protocol errors about the key, not business errors, so they are written directly
    // instead of going through Error.ToProblem(); they keep the same shape: a standard title and a 'code' extension.
    private static ProblemHttpResult Problem(int statusCode, string code, string detail) =>
        TypedResults.Problem(
            title: ReasonPhrases.GetReasonPhrase(statusCode),
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["code"] = code });

    private static IResult Replay(HttpContext httpContext, IdempotentResponse response)
    {
        var headers = httpContext.Response.Headers;
        headers[ReplayedHeaderName] = "true";
        if (response.Location is not null)
        {
            headers.Location = response.Location;
        }

        return response.Body is null
            ? TypedResults.StatusCode(response.StatusCode)
            : TypedResults.Text(response.Body, response.ContentType, Encoding.UTF8, response.StatusCode);
    }

    /// <summary>
    /// Runs the handler's result into a buffer, so the exact status, Location and body sent to the client
    /// are what gets stored. Only a 2xx is stored; anything else releases the key for a corrected retry.
    /// </summary>
    private sealed class SnapshotResult(IResult inner, IIdempotencyStore store, string key) : IResult
    {
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            var response = httpContext.Response;
            var original = response.Body;
            using var buffer = new MemoryStream();
            response.Body = buffer;
            try
            {
                await inner.ExecuteAsync(httpContext);
            }
            catch
            {
                await store.ReleaseAsync(key, CancellationToken.None);
                throw;
            }
            finally
            {
                response.Body = original;
            }

            // Not tied to RequestAborted: the work already happened, so the key must still be settled.
            if (response.StatusCode is >= 200 and < 300)
            {
                var snapshot = new IdempotentResponse(
                    response.StatusCode,
                    response.Headers.Location.FirstOrDefault(),
                    response.ContentType,
                    buffer.Length == 0 ? null : Encoding.UTF8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length));
                await store.CompleteAsync(key, snapshot, CancellationToken.None);
            }
            else
            {
                await store.ReleaseAsync(key, CancellationToken.None);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(original, httpContext.RequestAborted);
        }
    }
}
