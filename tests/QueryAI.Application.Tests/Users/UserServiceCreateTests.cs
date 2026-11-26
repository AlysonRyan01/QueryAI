using QueryAI.Application.Abstractions.Persistence.Exceptions;
using QueryAI.Application.Tests.Fakes;
using QueryAI.Application.Users.Requests;
using QueryAI.Application.Users.Services;
using QueryAI.Application.Users.Validations;
using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Tests.Users;

public sealed class UserServiceCreateTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly RecordingAuditWriter _audit = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeClock _clock = new();

    private UserService Service() => new(_users, _uow, _hasher, _audit, _currentUser, _clock);

    [Fact]
    public async Task Creates_user_with_must_change_password_and_audits()
    {
        var result = await Service().CreateAsync(
            new CreateUserRequest("Bruno", "bruno@example.com", Role.Analyst, "temporary-pass-1"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MustChangePassword.ShouldBeTrue();
        result.Value.Role.ShouldBe(Role.Analyst);
        _users.Users.Single().PasswordHash.ShouldBe("hash:temporary-pass-1");
        _audit.Events.ShouldContain(e => e.Type == SecurityEventType.UserCreated && e.Actor == _currentUser.UserId);
    }

    [Fact]
    public async Task Duplicate_email_is_case_insensitive()
    {
        _users.Add(User.Create("Bruno", "Bruno@Example.com", "h", Role.Analyst, false, _clock.UtcNow));

        var result = await Service().CreateAsync(
            new CreateUserRequest("Outro", "bruno@example.COM", Role.Analyst, "temporary-pass-1"), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("email_already_exists");
    }

    [Fact]
    public async Task Race_on_unique_index_maps_to_conflict()
    {
        _uow.ThrowOnSave = new DuplicateKeyException("dup", new InvalidOperationException());

        var result = await Service().CreateAsync(
            new CreateUserRequest("Bruno", "bruno@example.com", Role.Analyst, "temporary-pass-1"), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("email_already_exists");
    }

    [Theory]
    [InlineData("B", "b@example.com", "temporary-pass-1", "name")]
    [InlineData("Bruno", "not-an-email", "temporary-pass-1", "email")]
    [InlineData("Bruno", "b@example.com", "short", "temporaryPassword")]
    public async Task Validator_rejects_invalid_input(string name, string email, string password, string field)
    {
        var validation = await new CreateUserRequestValidator()
            .ValidateAsync(new CreateUserRequest(name, email, Role.Analyst, password), TestContext.Current.CancellationToken);

        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain(e => string.Equals(e.PropertyName, field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Invalid_request_is_rejected_before_touching_the_repository()
    {
        var result = await Service().CreateAsync(
            new CreateUserRequest("B", "invalid", Role.Analyst, "short"), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("validation_failed");
        result.Error.FieldErrors!.Keys.ShouldBe(["name", "email", "temporaryPassword"], ignoreOrder: true);
        _users.Users.ShouldBeEmpty();
        _uow.SaveCount.ShouldBe(0);
    }
}
