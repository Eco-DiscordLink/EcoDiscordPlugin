using Eco.Moose.Tools.Logger;
using Microsoft.Extensions.Logging;

namespace Eco.Plugins.DiscordLink;

/**
 * Factory Class for Loggers, which redirect their Output to the MightyMoose Implementation. This will also save Trace-Logs to File when enabled.
 */
public class DSharpPlusLogWrapperFactory : ILoggerFactory
{
    private readonly bool _saveTraceToFile;
    private LogLevel MinimumLevel { get; }

    public DSharpPlusLogWrapperFactory(LogLevel minimumLevel, bool saveTraceToFile)
    {
        _saveTraceToFile = saveTraceToFile;
        MinimumLevel = minimumLevel;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new DSharpPlusLogWrapper(MinimumLevel, _saveTraceToFile);
    }

    public void AddProvider(ILoggerProvider provider)
    {
        // we don't really need that for our simple use case
        Logger.Warning("Tried to add a Log-Provider, which is not Supported right now.");
    }
}