using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Domain.Tests.Users;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 12, 0, 0, TimeSpan.Zero);

    private static User NewUser(Role role = Role.Analyst) =>
        User.Create("  Maria Silva ", " Maria@Example.com ", "hash", role, mustChangePassword: true, Now);

    [Fact]
    public void Create_normalizes_name_and_email_and_starts_active()
    {
        var user = NewUser();

        user.Name.ShouldBe("Maria Silva");
        user.Email.ShouldBe("Maria@Example.com");
        user.NormalizedEmail.ShouldBe("MARIA@EXAMPLE.COM");
        user.IsActive.ShouldBeTrue();
        user.MustChangePassword.ShouldBeTrue();
        user.SecurityStamp.ShouldNotBe(Guid.Empty);
        user.Id.ShouldNotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("A")]
    [InlineData(" ")]
    public void Create_rejects_invalid_name(string name) =>
        Should.Throw<ArgumentException>(() => User.Create(name, "a@b.com", "hash", Role.Analyst, false, Now));

    [Fact]
    public void Create_rejects_name_longer_than_120() =>
        Should.Throw<ArgumentException>(() => User.Create(new string('a', 121), "a@b.com", "hash", Role.Analyst, false, Now));

    [Fact]
    public void Four_failures_do_not_lock_the_account()
    {
        var user = NewUser();

        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(Now).ShouldBeFalse();
        }

        user.IsLockedOut(Now).ShouldBeFalse();
        user.FailedLoginCount.ShouldBe(4);
    }

    [Fact]
    public void Fifth_failure_locks_for_15_minutes_and_resets_counter()
    {
        var user = NewUser();
        for (var i = 0; i < 4; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        user.RegisterFailedLogin(Now).ShouldBeTrue();

        user.IsLockedOut(Now).ShouldBeTrue();
        user.IsLockedOut(Now.AddMinutes(14).AddSeconds(59)).ShouldBeTrue();
        user.IsLockedOut(Now.AddMinutes(15)).ShouldBeFalse();
        user.LockoutRemaining(Now).ShouldBe(TimeSpan.FromMinutes(15));
        user.FailedLoginCount.ShouldBe(0);
    }

    [Fact]
    public void Successful_login_clears_failures_and_records_last_login()
    {
        var user = NewUser();
        user.RegisterFailedLogin(Now);

        user.RegisterSuccessfulLogin(Now);

        user.FailedLoginCount.ShouldBe(0);
        user.LockoutEndUtc.ShouldBeNull();
        user.LastLoginAtUtc.ShouldBe(Now);
    }

    [Fact]
    public void ChangePassword_clears_flag_and_rotates_stamp()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.ChangePassword("new-hash", Now);

        user.PasswordHash.ShouldBe("new-hash");
        user.MustChangePassword.ShouldBeFalse();
        user.SecurityStamp.ShouldNotBe(stamp);
    }

    [Fact]
    public void ResetPassword_forces_change_unlocks_and_rotates_stamp()
    {
        var user = NewUser();
        user.ChangePassword("h2", Now);
        for (var i = 0; i < 5; i++)
        {
            user.RegisterFailedLogin(Now);
        }

        var stamp = user.SecurityStamp;

        user.ResetPassword("temp-hash", Now);

        user.MustChangePassword.ShouldBeTrue();
        user.IsLockedOut(Now).ShouldBeFalse();
        user.SecurityStamp.ShouldNotBe(stamp);
    }

    [Fact]
    public void ChangeRole_rotates_stamp_only_when_role_changes()
    {
        var user = NewUser(Role.Admin);
        var stamp = user.SecurityStamp;

        user.ChangeRole(Role.Admin, Now);
        user.SecurityStamp.ShouldBe(stamp);

        user.ChangeRole(Role.Analyst, Now);
        user.Role.ShouldBe(Role.Analyst);
        user.SecurityStamp.ShouldNotBe(stamp);
    }

    [Fact]
    public void Deactivate_rotates_stamp_and_Activate_restores()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.Deactivate(Now);
        user.IsActive.ShouldBeFalse();
        user.SecurityStamp.ShouldNotBe(stamp);

        user.Activate(Now);
        user.IsActive.ShouldBeTrue();
    }
}
