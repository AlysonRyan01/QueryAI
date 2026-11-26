namespace QueryAI.Api.Options;

internal sealed class SecurityOptions
{
    public const string DefaultTrustedProxies = "172.16.0.0/12";

    public string TrustedProxies { get; set; } = DefaultTrustedProxies;

    public int SessionIdleHours { get; set; } = 8;

    public bool AllowInsecureHttp { get; set; }

    public int LoginRateLimitPerMinute { get; set; } = 10;
}
