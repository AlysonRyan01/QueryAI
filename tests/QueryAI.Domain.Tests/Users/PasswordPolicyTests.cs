using QueryAI.Domain.Users.Policies;

namespace QueryAI.Domain.Tests.Users;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(11, false)]
    [InlineData(12, true)]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Validates_length_between_12_and_128(int length, bool valid)
    {
        var result = PasswordPolicy.Validate(new string('x', length));

        result.IsSuccess.ShouldBe(valid);
        if (!valid)
        {
            result.Error!.Code.ShouldBe("validation_failed");
        }
    }

    [Fact]
    public void Null_is_invalid() => PasswordPolicy.Validate(null).IsFailure.ShouldBeTrue();
}
