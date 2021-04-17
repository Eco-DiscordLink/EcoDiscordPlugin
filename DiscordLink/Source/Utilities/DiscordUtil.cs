namespace Eco.Plugins.DiscordLink.Utilities
{
    public static class DiscordUtil
    {
        public static bool TryParseSnowflakeID(string nameOrID, out ulong ID)
        {
            return ulong.TryParse(nameOrID, out ID) && ID > 0xFFFFFFFFFFFFFUL;
        }
    }
}
