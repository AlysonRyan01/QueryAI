namespace QueryAI.Application.Auth.Dtos;

public sealed record AuthenticatedSession(CurrentUserDto User, Guid SecurityStamp);
