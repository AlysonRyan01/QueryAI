using QueryAI.Application.Abstractions.Security.Enums;
using QueryAI.Application.Abstractions.Security.Interfaces;

namespace QueryAI.Application.Tests.Fakes;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;

    public PasswordVerification Verify(string hashedPassword, string providedPassword) =>
        hashedPassword == Hash(providedPassword) ? PasswordVerification.Success
        : hashedPassword == "old:" + providedPassword ? PasswordVerification.SuccessRehashNeeded
        : PasswordVerification.Failed;
}
