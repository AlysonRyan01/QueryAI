namespace QueryAI.Application.Users.Dtos;

public sealed record UserListDto(IReadOnlyList<UserDto> Items, int Total, int Page, int PageSize);
