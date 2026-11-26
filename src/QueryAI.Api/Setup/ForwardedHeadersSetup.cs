using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using QueryAI.Api.Options;

namespace QueryAI.Api.Setup;

internal static class ForwardedHeadersSetup
{
    public static IServiceCollection AddQueryAIForwardedHeaders(this IServiceCollection services)
    {
        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<SecurityOptions>>((options, security) =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = null;
                options.RequireHeaderSymmetry = false;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.0/8"));
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("::1/128"));
                foreach (var network in OptionsRegistration.ParseNetworks(security.Value.TrustedProxies))
                {
                    options.KnownIPNetworks.Add(network);
                }
            });
        return services;
    }
}
