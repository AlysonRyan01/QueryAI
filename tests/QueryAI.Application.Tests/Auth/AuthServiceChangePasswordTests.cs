using QueryAI.Application.Auth.Requests;
using QueryAI.Application.Auth.Services;
using QueryAI.Application.Auth.Validations;
using QueryAI.Application.Tests.Fakes;
using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Tests.Auth;

public sealed class AuthServiceChangePasswordTests
{
    private const string Current = "current-password-1";
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly RecordingAuditWriter _audit = new();
    private readonly FakeClock _clock = new();
    private readonly User _user;

    public AuthServiceChangePasswordTests()
    {
        _user = User.Create("Ana", "ana@example.com", _hasher.Hash(Current), Role.Admin, mustChangePassword: true, _clock.UtcNow);
        _users.Add(_user);
    }

    private AuthService Service() => new(_users, _uow, _hasher, _audit, _clock);

    [Fact]
    public async Task Changes_password_clears_flag_rotates_stamp_and_audits()
    {
        var stamp = _user.SecurityStamp;

        var result = await Service().ChangePasswordAsync(_user.Id, new ChangePasswordRequest(Current, "brand-new-password"), TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.User.MustChangePassword.ShouldBeFalse();
        result.Value.SecurityStamp.ShouldNotBe(stamp);
        _user.PasswordHash.ShouldBe(_hasher.Hash("brand-new-password"));
        _audit.Events.ShouldContain(e => e.Type == SecurityEventType.PasswordChanged);
    }

    [Fact]
    public async Task Wrong_current_password_is_rejected()
    {
        var result = await Service().ChangePasswordAsync(_user.Id, new ChangePasswordRequest("not-the-current", "brand-new-password"), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("invalid_current_password");
        _uow.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Same_password_is_rejected()
    {
        var result = await Service().ChangePasswordAsync(_user.Id, new ChangePasswordRequest(Current, Current), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("same_password");
    }

    [Fact]
    public async Task Validator_rejects_short_new_password()
    {
        var validation = await new ChangePasswordRequestValidator()
            .ValidateAsync(new ChangePasswordRequest(Current, "short"), TestContext.Current.CancellationToken);

        validation.IsValid.ShouldBeFalse();
        validation.Errors.ShouldContain(e => e.PropertyName == "NewPassword");
    }
}
