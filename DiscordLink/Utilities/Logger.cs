using Eco.Shared.Localization;
using Eco.Shared.Utils;
using System;
using System.IO;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class Logger
    {
        private static readonly string PluginLogPath = DiscordLink.BasePath + "Pluginlog.txt";
        private static readonly object LockObject = new object();

        private static StreamWriter _writer = null;

        public static void Initialize()
        {
            try
            {
                lock (LockObject)
                {
                    SystemUtil.EnsurePathExists(PluginLogPath);
                    _writer = new StreamWriter(PluginLogPath, append: true)
                    {
                        AutoFlush = true,
                    };
                }
            }
            catch (Exception e)
            {
                Error("Error occurred while attempting to initialize the plugin log. Error message: " + e);
            }
        }

        public static void Shutdown()
        {
            try
            {
                lock (LockObject)
                {
                    _writer.Flush();
                    _writer.Close();
                }
            }
            catch (Exception e)
            {
                Error("Error occurred while attempting to close the plugin log file writer. Error message: " + e);
            }
        }

        public static void DebugVerbose(string message)
        {
            if (DLConfig.Data.LogLevel <= LogLevel.DebugVerbose)
            {
                Log.Write(new LocString("[DiscordLink] DEBUG: " + message + "\n"));
            }
        }

        public static void Debug(string message)
        {
            string fullMessage = "[DiscordLink] DEBUG: " + message + "\n";
            if (DLConfig.Data.LogLevel <= LogLevel.Debug)
            {
                Log.Write(new LocString(fullMessage));
            }
            WriteToPluginLog(fullMessage);
        }

        public static void Warning(string message)
        {
            string fullMessage = "[DiscordLink] WARNING: " + message + "\n";
            if (DLConfig.Data.LogLevel <= LogLevel.Warning)
            {
                Log.Write(new LocString(fullMessage));   
            }
            WriteToPluginLog(fullMessage);
        }

        public static void Info(string message)
        {
            string fullMessage = "[DiscordLink] " + message + "\n";
            if (DLConfig.Data.LogLevel <= LogLevel.Information)
            {
                Log.Write(new LocString(fullMessage));   
            }
            WriteToPluginLog(fullMessage);
        }

        public static void Error(string message)
        {
            string fullMessage = "[DiscordLink] ERROR: " + message + "\n";
            if (DLConfig.Data.LogLevel <= LogLevel.Error)
            {
                Log.Write(new LocString(fullMessage));   
            }
            WriteToPluginLog(fullMessage);
        }

        private static void WriteToPluginLog(string message)
        {
            lock (LockObject)
            {
                _writer?.Write(message);
            }
        }
    }
}
