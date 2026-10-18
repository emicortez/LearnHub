using Microsoft.AspNetCore.OutputCaching;

namespace LearnHub.Catalog.Api.Caching;

/// <summary>
/// Evicts the cached course reads after a write endpoint succeeds. The handler's typed result already carries the
/// status, so a 2xx is known before the response is written and no client can read the stale entry afterwards.
/// </summary>
public sealed class EvictCoursesCacheFilter(IOutputCacheStore store) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var result = await next(context);

        if (StatusCodeOf(result) is >= 200 and < 300)
        {
            // Not tied to RequestAborted: the write already happened, so its stale reads must go regardless.
            await store.EvictByTagAsync(OutputCachingServiceCollectionExtensions.CoursesTag, CancellationToken.None);
        }

        return result;
    }

    // Results<T1, T2> wraps the result that actually runs, so the status is read from the innermost one.
    private static int? StatusCodeOf(object? result)
    {
        while (result is INestedHttpResult nested)
        {
            result = nested.Result;
        }

        return (result as IStatusCodeHttpResult)?.StatusCode;
    }
}
