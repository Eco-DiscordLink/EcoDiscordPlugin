using Eco.Core.Utils;
using Eco.Shared.Localization;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class Logger
    {
        private static NLogWrapper _pluginLog = NLogWriter.GetConcreteLogger("DiscordLink");

        public enum LogLevel
        {
            DebugVerbose,
            Debug,
            Warning,
            Information,
            Error,
            Silent,
        }

        public static void DebugVerbose(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.DebugVerbose)
            {
                string fullMessage = $"DEBUG: {message}";
                Log.WriteLine(new LocString($"[DiscordLink] {fullMessage}"));
                _pluginLog.Info(fullMessage); // Verbose debug log messages are only written to the log file if enabled via configuration
            }
        }

        public static void Debug(string message)
        {
            string fullMessage = $"DEBUG: {message}";
            if (DLConfig.Data.LogLevel <= LogLevel.Debug)
                Log.WriteLine(new LocString($"[DiscordLink] {fullMessage}"));
            _pluginLog.Info(fullMessage);
        }

        public static void Warning(string message)
        {
            string fullMessage = $"WARNING: {message}";
            if (DLConfig.Data.LogLevel <= LogLevel.Warning)
                Log.WriteLine(new LocString($"[DiscordLink] {fullMessage}"));
            _pluginLog.Info(fullMessage);
        }

        public static void Info(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Information)
                Log.WriteLine(new LocString($"[DiscordLink] {message}"));
            _pluginLog.Info($"INFO: {message}");
        }

        public static void Error(string message)
        {
            string fullMessage = $"ERROR: {message}";
            if (DLConfig.Data.LogLevel <= LogLevel.Error)
                Log.WriteLine(new LocString($"[DiscordLink] {fullMessage}"));
            _pluginLog.Info(fullMessage);
        }
    }
}
