using System.Threading;
using System.IO;
using System;
using Eco.EW.Tools;

namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class SystemUtils
    {
        public static void StopAndDestroyTimer(ref Timer timer)
        {
            if (timer != null)
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                timer = null;
            }
        }

        public static void EnsurePathExists(string path)
        {
            string directoryPath = Path.GetDirectoryName(path);
            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (Exception e)
            {
                Logger.Exception($"Failed to create directory at path \"{path}\"", e);
            }
        }
    }
}
