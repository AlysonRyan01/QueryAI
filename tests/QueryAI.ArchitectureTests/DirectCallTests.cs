using NetArchTest.Rules;

namespace QueryAI.ArchitectureTests;

public sealed class DirectCallTests
{
    private static readonly System.Reflection.Assembly[] Assemblies =
    [
        typeof(QueryAI.Domain.Users.Entities.User).Assembly,
        typeof(Application.DependencyInjection).Assembly,
        typeof(Infrastructure.DependencyInjection).Assembly,
        typeof(Program).Assembly,
    ];

    [Fact]
    public void No_command_query_handler_or_mediator_types()
    {
        var result = Types.InAssemblies(Assemblies)
            .That()
            .HaveNameEndingWith("Command")
            .Or().HaveNameEndingWith("Query")
            .Or().HaveNameEndingWith("CommandHandler")
            .Or().HaveNameEndingWith("QueryHandler")
            .Or().HaveNameEndingWith("Mediator")
            .GetTypes()
            .Select(t => t.FullName)
            .ToList();

        result.ShouldBeEmpty();
    }

    [Fact]
    public void No_dependency_on_mediator_libraries()
    {
        var result = Types.InAssemblies(Assemblies)
            .ShouldNot()
            .HaveDependencyOnAny("MediatR", "Mediator", "Wolverine", "MassTransit")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
