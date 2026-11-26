namespace QueryAI.Application.Users.Requests;

public sealed record ListUsersRequest(string? Search = null, int Page = 1, int PageSize = 20);
