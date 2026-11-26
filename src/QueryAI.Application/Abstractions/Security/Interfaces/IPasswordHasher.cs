using QueryAI.Application.Abstractions.Security.Enums;

namespace QueryAI.Application.Abstractions.Security.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerification Verify(string hashedPassword, string providedPassword);
}
