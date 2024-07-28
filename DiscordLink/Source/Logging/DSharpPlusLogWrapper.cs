using Eco.Moose.Tools.Logger;
using Microsoft.Extensions.Logging;
using Nito.Disposables;
using System;

namespace Eco.Plugins.DiscordLink;

/**
 * Implements the Microsoft Logging interface to call the MightyMoose Logger.
 */
public class DSharpPlusLogWrapper : ILogger
{
    private readonly LogLevel _minimumLevel;
    private readonly bool _saveTraceToFile;

    public DSharpPlusLogWrapper(LogLevel minLevel, bool saveTraceToFile)
    {
        _minimumLevel = minLevel;
        _saveTraceToFile = saveTraceToFile;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);
        if (exception != null)
        {
            Console.WriteLine(exception);
        }

        if (message == null || !IsEnabled(logLevel))
        {
            return;
        }

        var eventName = eventId.Name;
        eventName = eventName?.Length > 12 ? eventName?.Substring(0, 12) : eventName;
        var msgPrefix = $"[/{eventName,-12}]";

        switch (logLevel)
        {
            case LogLevel.Trace:
                Logger.Trace($"{msgPrefix} {message}");

                if (_saveTraceToFile)
                    Logger.Silent($"{msgPrefix} {message}");
                break;
            case LogLevel.Debug:
                Logger.Debug($"{msgPrefix} {message}");
                break;
            case LogLevel.Information:
                Logger.Info($"{msgPrefix} {message}");
                break;
            case LogLevel.Warning:
                Logger.Warning($"{msgPrefix} {message}");
                break;
            case LogLevel.Error:
                Logger.Error($"{msgPrefix} {message}");
                break;
            case LogLevel.Critical:
                Logger.Error($"{msgPrefix} {message}");
                break;
            case LogLevel.None:
                break;

            default:
                Logger.Info($"{msgPrefix} {message}");
                break;
        }
    }

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= _minimumLevel;

    /**
     * Nothing to dispose
     */
    public IDisposable BeginScope<TState>(TState state)
    {
        return new Disposable(null);
    }
}