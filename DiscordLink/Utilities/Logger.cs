using Eco.Core.Utils.Logging;
using static Eco.EM.Framework.Utils.ConsoleColors;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class Logger
    {
        private static readonly NLogWriter _pluginLog = NLogManager.GetLogWriter("DiscordLink");
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
                PrintConsoleMultiColored(_tag, _consoleColor, $" {message}", System.ConsoleColor.Gray);
                _pluginLog.Write($"DEBUG {message}"); // Verbose debug log messages are only written to the log file if enabled via configuration
            }
        }

        public static void Debug(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Debug)
                PrintConsoleMultiColored(_tag, _consoleColor, $" {message}", System.ConsoleColor.Gray);
            _pluginLog.Write($"DEBUG: {message}");
        }

        public static void Warning(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Warning)
                PrintConsoleMultiColored(_tag, _consoleColor, $" {message}", System.ConsoleColor.Yellow);
            _pluginLog.WriteWarning(message);
        }

        public static void Info(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Information)
                PrintConsoleMultiColored(_tag, _consoleColor, $" {message}", System.ConsoleColor.White);
            _pluginLog.Write(message);
        }

        public static void Error(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.Error)
                PrintConsoleMultiColored(_tag, _consoleColor, $" {message}", System.ConsoleColor.Red);
            _pluginLog.WriteError(message);
        }
    }
}
