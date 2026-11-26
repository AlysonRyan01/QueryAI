using Microsoft.Extensions.Options;
using QueryAI.Api.Options;
using QueryAI.Api.Setup;

namespace QueryAI.Api.Validations;

internal sealed class SecurityOptionsValidator : IValidateOptions<SecurityOptions>
{
    public ValidateOptionsResult Validate(string? name, SecurityOptions options)
    {
        var failures = new List<string>();
        if (options.SessionIdleHours is < 1 or > 720)
        {
            failures.Add("A variável de ambiente QUERYAI_SESSION_IDLE_HOURS deve estar entre 1 e 720.");
        }

        if (options.LoginRateLimitPerMinute < 1)
        {
            failures.Add("A variável de ambiente QUERYAI_LOGIN_RATE_LIMIT_PER_MINUTE deve ser maior que zero.");
        }

        try
        {
            _ = OptionsRegistration.ParseNetworks(options.TrustedProxies);
        }
        catch (FormatException)
        {
            failures.Add("A variável de ambiente QUERYAI_TRUSTED_PROXIES deve conter IPs ou CIDRs separados por vírgula.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
