using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Users.Requests;

public sealed record UpdateUserRequest(string Name, Role Role);
