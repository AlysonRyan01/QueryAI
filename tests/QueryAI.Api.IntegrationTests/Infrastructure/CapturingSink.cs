using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace QueryAI.Api.IntegrationTests.Infrastructure;

public sealed class CapturingSink : ILogEventSink
{
    private readonly ConcurrentQueue<string> _lines = new();
    private readonly RenderedCompactJsonFormatter _formatter = new();

    public IReadOnlyCollection<string> Lines => _lines;

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        _lines.Enqueue(writer.ToString());
    }

    public string AllText => string.Join('\n', _lines);
}
