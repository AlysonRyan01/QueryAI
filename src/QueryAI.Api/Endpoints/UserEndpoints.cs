using QueryAI.Api.Extensions;
using QueryAI.Api.Setup;
using QueryAI.Application.Users.Dtos;
using QueryAI.Application.Users.Requests;
using QueryAI.Application.Users.Services;

namespace QueryAI.Api.Endpoints;

internal static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Usuários")
            .RequireAuthorization(AuthorizationSetup.AdminOnly)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/", ListAsync).WithName("ListUsers")
            .Produces<UserListDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest);
        group.MapPost("/", CreateAsync).WithName("CreateUser")
            .Produces<UserDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapGet("/{id:guid}", GetAsync).WithName("GetUser")
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateUser")
            .Produces<UserDto>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/reset-password", ResetPasswordAsync).WithName("ResetUserPassword")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/{id:guid}/deactivate", DeactivateAsync).WithName("DeactivateUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPost("/{id:guid}/activate", ActivateAsync).WithName("ActivateUser")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        [AsParameters] ListUsersRequest request, HttpContext httpContext, UserService users, CancellationToken cancellationToken) =>
        (await users.ListAsync(request, cancellationToken)).ToHttpResult(httpContext, TypedResults.Ok);

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request, HttpContext httpContext, UserService users, CancellationToken cancellationToken) =>
        (await users.CreateAsync(request, cancellationToken))
            .ToHttpResult(httpContext, user => TypedResults.Created($"/api/users/{user.Id}", user));

    private static async Task<IResult> GetAsync(
        Guid id, HttpContext httpContext, UserService users, CancellationToken cancellationToken) =>
        (await users.GetAsync(id, cancellationToken)).ToHttpResult(httpContext, TypedResults.Ok);

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateUserRequest request, HttpContext httpContext, UserService users, CancellationToken cancellationToken) =>
        (await users.UpdateAsync(id, request, cancellationToken)).ToHttpResult(httpContext, TypedResults.Ok);

    private static async Task<IResult> ResetPasswordAsync(
        Guid id, ResetPasswordRequest request, HttpContext httpContext, UserService users, CancellationToken cancellationToken) =>
        (await users.ResetPasswordAsync(id, request, cancellationToken)).ToHttpResult(httpContext, TypedResults.NoContent);

    private static async Task<IResult> DeactivateAsync(
        Guid id, HttpContext httpContext, UserService users, CancellationToken cancellationToken) =>
        (await users.DeactivateAsync(id, cancellationToken)).ToHttpResult(httpContext, TypedResults.NoContent);

    private static async Task<IResult> ActivateAsync(
        Guid id, HttpContext httpContext, UserService users, CancellationToken cancellationToken) =>
        (await users.ActivateAsync(id, cancellationToken)).ToHttpResult(httpContext, TypedResults.NoContent);
}
