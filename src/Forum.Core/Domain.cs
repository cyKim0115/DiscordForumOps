namespace Forum.Core;

public sealed record Persona(string Name, string? AvatarUrl);

public sealed record PostRef(ulong ForumChannelId, ulong PostId);

public sealed record MessageRef(ulong MessageId, ulong ChannelId, DateTimeOffset Timestamp);

public sealed record ForumMessage(
    MessageRef Ref,
    PostRef Post,
    string Content,
    bool MentionsBot,
    string AuthorId);
