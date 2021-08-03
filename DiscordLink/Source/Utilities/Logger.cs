using Eco.Core.Utils;
using static Eco.EM.Framework.Utils.ConsoleColors;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class Logger
    {
        private static readonly NLogWrapper _pluginLog = NLogWriter.GetConcreteLogger("DiscordLink");
        private static readonly System.ConsoleColor _consoleColor = System.ConsoleColor.Cyan;
        private static readonly string _tag = "[DiscordLink]";

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
                PrintConsoleMultiColored(_tag, _consoleColor, $" DEBUG: {message}", System.ConsoleColor.Gray);
                _pluginLog.Info($"DEBUG: {message}"); // Verbose debug log messages are only written to the log file if enabled via configuration
            }
        }

        public static void Debug(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Debug)
                PrintConsoleMultiColored(_tag, _consoleColor, $" DEBUG: {message}", System.ConsoleColor.Gray);
            _pluginLog.Info($"DEBUG: {message}");
        }

        public static void Warning(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Warning)
                PrintConsoleMultiColored(_tag, _consoleColor, $" WARNING: {message}", System.ConsoleColor.Yellow);
            _pluginLog.Info($"WARNING: {message}");
        }

        public static void Info(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Information)
                PrintConsoleMultiColored(_tag, _consoleColor, $" {message}", System.ConsoleColor.White);
            _pluginLog.Info($"INFO: {message}");
        }

        public static void Error(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Error)
                PrintConsoleMultiColored(_tag, _consoleColor, $" ERROR: {message}", System.ConsoleColor.Red);
            _pluginLog.Info($"ERROR: {message}");
        }
    }
}
