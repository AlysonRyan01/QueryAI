using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace QueryAI.Api.Policies;

internal sealed class SensitiveDataPolicy : IDestructuringPolicy
{
    internal const string Mask = "***";

    private static readonly string[] SensitiveNames =
    [
        "Password", "NewPassword", "CurrentPassword", "TemporaryPassword", "PasswordHash",
        "Token", "Secret", "ConnectionString", "ApiKey",
    ];

    public static bool IsSensitive(string name) =>
        SensitiveNames.Any(s => name.Contains(s, StringComparison.OrdinalIgnoreCase));

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(propertyValueFactory);

        var type = value.GetType();
        if (type.IsPrimitive || value is string || type.IsEnum)
        {
            result = null!;
            return false;
        }

        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

        if (!properties.Any(p => IsSensitive(p.Name)))
        {
            result = null!;
            return false;
        }

        var logProperties = properties.Select(p => new LogEventProperty(
            p.Name,
            IsSensitive(p.Name)
                ? new ScalarValue(Mask)
                : propertyValueFactory.CreatePropertyValue(p.GetValue(value), destructureObjects: true)));

        result = new StructureValue(logProperties, type.Name);
        return true;
    }
}
