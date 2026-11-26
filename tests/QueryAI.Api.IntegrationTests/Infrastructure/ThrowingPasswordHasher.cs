using Microsoft.AspNetCore.Identity;
using QueryAI.Application.Abstractions.Security.Enums;

namespace QueryAI.Api.IntegrationTests.Infrastructure;

public sealed class ThrowingPasswordHasher : QueryAI.Application.Abstractions.Security.Interfaces.IPasswordHasher
{
    public const string InternalDetail = "boom-internal-detail-do-not-leak";

    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(new object(), password);

    public PasswordVerification Verify(string hashedPassword, string providedPassword) =>
        throw new InvalidOperationException(InternalDetail);
}
