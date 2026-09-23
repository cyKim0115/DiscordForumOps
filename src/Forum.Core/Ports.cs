namespace Forum.Core;

public interface ICursorStore
{
    Task<ulong?> GetLastSeenAsync(PostRef post, CancellationToken ct);
    Task SetLastSeenAsync(PostRef post, ulong messageId, CancellationToken ct);
}

public interface IForumReader
{
    Task<IReadOnlyList<PostRef>> ListActivePostsAsync(ulong forumChannelId, CancellationToken ct);
    Task<IReadOnlyList<ForumMessage>> GetMessagesAfterAsync(
        PostRef post, ulong? afterMessageId, int limit, CancellationToken ct);
}

public interface IPersonaPublisher
{
    /// <summary>새 포럼 포스트. 반환 = 스타터 메시지/포스트 id (스모크로 id vs channel_id 확인).</summary>
    Task<ulong> CreatePostAsync(
        ulong forumChannelId,
        string threadName,
        string content,
        Persona persona,
        IReadOnlyList<ulong>? appliedTagIds,
        CancellationToken ct);

    Task ReplyAsync(
        PostRef post,
        string content,
        Persona persona,
        CancellationToken ct);
}

public interface IMentionHandler
{
    Task HandleAsync(ForumMessage message, CancellationToken ct);
}
