namespace Forum.Core;

public sealed class MentionPollProcessor
{
    public const int DefaultMessageLimit = 100;

    private readonly IForumReader _reader;
    private readonly ICursorStore _cursor;
    private readonly IMentionHandler _handler;

    public MentionPollProcessor(IForumReader reader, ICursorStore cursor, IMentionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(cursor);
        ArgumentNullException.ThrowIfNull(handler);
        _reader = reader;
        _cursor = cursor;
        _handler = handler;
    }

    public async Task<int> PollOnceAsync(ulong forumChannelId, CancellationToken ct)
    {
        var newMessageCount = 0;
        var posts = await _reader.ListActivePostsAsync(forumChannelId, ct).ConfigureAwait(false);
        foreach (var post in posts)
        {
            var last = await _cursor.GetLastSeenAsync(post, ct).ConfigureAwait(false);
            var messages = await _reader
                .GetMessagesAfterAsync(post, last, DefaultMessageLimit, ct)
                .ConfigureAwait(false);

            foreach (var message in messages.OrderBy(m => m.Ref.MessageId))
            {
                newMessageCount++;
                if (message.MentionsBot)
                {
                    await _handler.HandleAsync(message, ct).ConfigureAwait(false);
                }

                await _cursor.SetLastSeenAsync(post, message.Ref.MessageId, ct).ConfigureAwait(false);
            }
        }

        return newMessageCount;
    }
}
