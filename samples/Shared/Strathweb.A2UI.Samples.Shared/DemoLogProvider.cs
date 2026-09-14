using Microsoft.Extensions.Logging;

namespace Strathweb.A2UI.Samples;

/// <summary>
/// Captures what the A2UI library logs so a demo page can show it beside the wire log: surfaces
/// created, blocks the model got wrong, repair rounds, dropped data.
/// </summary>
public sealed class DemoLogProvider : ILoggerProvider
{
    private readonly List<WireLogEntry> entries = [];

    /// <summary>Raised when a line was logged.</summary>
    public event Action<WireLogEntry>? Logged;

    /// <summary>The captured lines, newest first.</summary>
    public IReadOnlyList<WireLogEntry> Entries
    {
        get
        {
            lock (entries)
            {
                return [.. entries];
            }
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        ArgumentNullException.ThrowIfNull(categoryName);
        return categoryName.StartsWith("Strathweb.A2UI", StringComparison.Ordinal)
            ? new Logger(this)
            : Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private void Add(LogLevel level, string message)
    {
        var entry = new WireLogEntry(level >= LogLevel.Warning ? "err" : "log", message, DateTimeOffset.Now);
        lock (entries)
        {
            entries.Insert(0, entry);
            if (entries.Count > 60)
            {
                entries.RemoveAt(entries.Count - 1);
            }
        }

        Logged?.Invoke(entry);
    }

    private sealed class Logger(DemoLogProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            owner.Add(logLevel, formatter(state, exception));
    }
}
