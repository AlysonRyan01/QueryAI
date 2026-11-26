using QueryAI.Application.Auth.Requests;
using QueryAI.Application.Auth.Services;
using QueryAI.Application.Tests.Fakes;
using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Tests.Auth;

public sealed class AuthServiceLoginTests
{
    private const string Password = "correct-horse-battery";
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly RecordingAuditWriter _audit = new();
    private readonly FakeClock _clock = new();
    private readonly User _user;

    public AuthServiceLoginTests()
    {
        _user = User.Create("Ana", "ana@example.com", _hasher.Hash(Password), Role.Analyst, false, _clock.UtcNow);
        _users.Add(_user);
    }

    private AuthService Service() => new(_users, _uow, _hasher, _audit, _clock);

    [Fact]
    public async Task Valid_credentials_return_session_and_audit_success()
    {
        var result = await Service().LoginAsync(new LoginRequest(" ANA@example.com ", Password), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.User.Id.ShouldBe(_user.Id);
        result.Value.SecurityStamp.ShouldBe(_user.SecurityStamp);
        _user.LastLoginAtUtc.ShouldBe(_clock.UtcNow);
        _audit.Events.ShouldContain(e => e.Type == SecurityEventType.LoginSucceeded && e.Target == _user.Id);
        _uow.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Unknown_email_and_wrong_password_return_same_error()
    {
        var unknown = await Service().LoginAsync(new LoginRequest("nobody@example.com", Password), TestContext.Current.CancellationToken);
        var wrong = await Service().LoginAsync(new LoginRequest("ana@example.com", "wrong-password-123"), TestContext.Current.CancellationToken);

        unknown.Error!.Code.ShouldBe("invalid_credentials");
        wrong.Error!.Code.ShouldBe("invalid_credentials");
        unknown.Error.Message.ShouldBe(wrong.Error.Message);
        _audit.Events.Count(e => e.Type == SecurityEventType.LoginFailed).ShouldBe(2);
    }

    [Fact]
    public async Task Inactive_user_cannot_login()
    {
        _user.Deactivate(_clock.UtcNow);

        var result = await Service().LoginAsync(new LoginRequest("ana@example.com", Password), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("invalid_credentials");
    }

    [Fact]
    public async Task Fifth_wrong_password_locks_account_and_audits_lockout()
    {
        for (var i = 0; i < 4; i++)
        {
            (await Service().LoginAsync(new LoginRequest("ana@example.com", "wrong-password-123"), TestContext.Current.CancellationToken))
                .Error!.Code.ShouldBe("invalid_credentials");
        }

        var fifth = await Service().LoginAsync(new LoginRequest("ana@example.com", "wrong-password-123"), TestContext.Current.CancellationToken);

        fifth.Error!.Code.ShouldBe("account_locked");
        fifth.Error.Metadata!["retryAfterSeconds"].ShouldBe(900);
        _audit.Events.ShouldContain(e => e.Type == SecurityEventType.AccountLockedOut);
    }

    [Fact]
    public async Task Locked_account_rejects_even_correct_password_until_expiry()
    {
        for (var i = 0; i < 5; i++)
        {
            await Service().LoginAsync(new LoginRequest("ana@example.com", "wrong-password-123"), TestContext.Current.CancellationToken);
        }

        (await Service().LoginAsync(new LoginRequest("ana@example.com", Password), TestContext.Current.CancellationToken))
            .Error!.Code.ShouldBe("account_locked");

        _clock.UtcNow = _clock.UtcNow.AddMinutes(15);
        (await Service().LoginAsync(new LoginRequest("ana@example.com", Password), TestContext.Current.CancellationToken))
            .IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Legacy_hash_is_upgraded_on_successful_login()
    {
        var legacy = User.Create("Leo", "leo@example.com", "old:" + Password, Role.Analyst, false, _clock.UtcNow);
        _users.Add(legacy);

        var result = await Service().LoginAsync(new LoginRequest("leo@example.com", Password), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        legacy.PasswordHash.ShouldBe(_hasher.Hash(Password));
    }

    [Fact]
    public async Task Empty_credentials_are_rejected_without_auditing()
    {
        var result = await Service().LoginAsync(new LoginRequest("", ""), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("validation_failed");
        _audit.Events.ShouldBeEmpty();
        _uow.SaveCount.ShouldBe(0);
    }
}
