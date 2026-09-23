namespace Forum.Core;

public sealed class PollingOptions
{
    public const string SectionName = "Polling";
    public const string ForumChannelIdKey = "DISCORD_FORUM_CHANNEL_ID";
    public const string BotUserIdKey = "DISCORD_BOT_USER_ID";

    public const int DefaultIdleSeconds = 60;
    public const int DefaultActiveSeconds = 10;
    public const int DefaultQuietTicksToExpand = 1;

    public ulong ForumChannelId { get; set; }

    public int IdleSeconds { get; set; } = DefaultIdleSeconds;

    public int ActiveSeconds { get; set; } = DefaultActiveSeconds;

    public int QuietTicksToExpand { get; set; } = DefaultQuietTicksToExpand;
}
