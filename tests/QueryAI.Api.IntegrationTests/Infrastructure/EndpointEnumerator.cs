using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace QueryAI.Api.IntegrationTests.Infrastructure;

internal static class EndpointEnumerator
{
    public static IReadOnlyList<EndpointInfo> List(IServiceProvider services) =>
        services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? ["GET"])
                .Select(m => new EndpointInfo(
                    m,
                    "/" + (e.RoutePattern.RawText ?? string.Empty).TrimStart('/').TrimEnd('/'),
                    e.Metadata.ToList())))
            .ToList();
}
