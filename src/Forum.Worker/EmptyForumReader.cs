using Forum.Core;

namespace Forum.Worker;

internal sealed class EmptyForumReader : IForumReader
{
    public Task<IReadOnlyList<PostRef>> ListActivePostsAsync(ulong forumChannelId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _ = forumChannelId;
        return Task.FromResult<IReadOnlyList<PostRef>>([]);
    }

    public Task<IReadOnlyList<ForumMessage>> GetMessagesAfterAsync(
        PostRef post, ulong? afterMessageId, int limit, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(post);
        ct.ThrowIfCancellationRequested();
        _ = afterMessageId;
        _ = limit;
        return Task.FromResult<IReadOnlyList<ForumMessage>>([]);
    }
}
