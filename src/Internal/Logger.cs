using System;

using Microsoft.Extensions.Logging;

namespace PayOS.Internal;

/// <summary>
/// Simple console logger implementation for PayOS when no external logger is provided
/// </summary>
internal class Logger(string categoryName, LogLevel minLevel) : ILogger
{
    private readonly string _categoryName = categoryName;
    private readonly LogLevel _minLevel = minLevel;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _minLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var level = GetLogLevelString(logLevel);
        var message = formatter(state, exception);

        Console.WriteLine($"[{timestamp}] [{level}] {_categoryName}: {message}");

        if (exception != null)
        {
            Console.WriteLine($"Exception: {exception}");
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "CRIT",
            LogLevel.None => "NONE",
            _ => "UNKN"
        };
    }

    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();
        public void Dispose() { }
    }
}