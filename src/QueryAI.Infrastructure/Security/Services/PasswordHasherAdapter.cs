using Microsoft.AspNetCore.Identity;
using QueryAI.Application.Abstractions.Security.Enums;

namespace QueryAI.Infrastructure.Security.Services;

internal sealed class PasswordHasherAdapter : QueryAI.Application.Abstractions.Security.Interfaces.IPasswordHasher
{
    private static readonly object HashSubject = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashSubject, password);

    public PasswordVerification Verify(string hashedPassword, string providedPassword) =>
        _hasher.VerifyHashedPassword(HashSubject, hashedPassword, providedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordVerification.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerification.SuccessRehashNeeded,
            _ => PasswordVerification.Failed,
        };
}
