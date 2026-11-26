using QueryAI.Application.Tests.Fakes;
using QueryAI.Application.Users.Requests;
using QueryAI.Application.Users.Services;
using QueryAI.Domain.Auditing.Enums;
using QueryAI.Domain.Users.Entities;
using QueryAI.Domain.Users.Enums;

namespace QueryAI.Application.Tests.Users;

public sealed class UserServiceAdminInvariantTests
{
    private readonly InMemoryUserRepository _users = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly RecordingAuditWriter _audit = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeClock _clock = new();

    private User AddUser(Role role)
    {
        var user = User.Create("Pessoa " + role, Guid.NewGuid() + "@example.com", "h", role, false, _clock.UtcNow);
        _users.Add(user);
        return user;
    }

    private UserService Service() => new(_users, _uow, new FakePasswordHasher(), _audit, _currentUser, _clock);

    [Fact]
    public async Task Cannot_deactivate_the_last_active_admin()
    {
        var admin = AddUser(Role.Admin);

        var result = await Service().DeactivateAsync(admin.Id, TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("last_admin");
        admin.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task Can_deactivate_an_admin_when_another_active_admin_exists()
    {
        var admin = AddUser(Role.Admin);
        AddUser(Role.Admin);

        var result = await Service().DeactivateAsync(admin.Id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        admin.IsActive.ShouldBeFalse();
        _audit.Events.ShouldContain(e => e.Type == SecurityEventType.UserDeactivated && e.Target == admin.Id);
    }

    [Fact]
    public async Task Deactivating_an_analyst_needs_no_admin_check()
    {
        AddUser(Role.Admin);
        var analyst = AddUser(Role.Analyst);

        (await Service().DeactivateAsync(analyst.Id, TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Cannot_demote_the_last_active_admin()
    {
        var admin = AddUser(Role.Admin);

        var result = await Service().UpdateAsync(admin.Id, new UpdateUserRequest("Novo Nome", Role.Analyst), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("last_admin");
        admin.Role.ShouldBe(Role.Admin);
    }

    [Fact]
    public async Task Role_change_is_audited_with_previous_and_new_role()
    {
        AddUser(Role.Admin);
        var analyst = AddUser(Role.Analyst);

        await Service().UpdateAsync(analyst.Id, new UpdateUserRequest("Novo Nome", Role.Admin), TestContext.Current.CancellationToken);

        var roleChange = _audit.Events.Single(e => e.Type == SecurityEventType.UserRoleChanged);
        roleChange.Details!["from"].ShouldBe("Analyst");
        roleChange.Details["to"].ShouldBe("Admin");
    }

    [Fact]
    public async Task Unknown_user_returns_not_found()
    {
        var result = await Service().DeactivateAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Error!.Code.ShouldBe("not_found");
    }
}
