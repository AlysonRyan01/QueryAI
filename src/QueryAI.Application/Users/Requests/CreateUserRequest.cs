using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Users.Requests;

public sealed record CreateUserRequest(string Name, string Email, Role Role, string TemporaryPassword);
