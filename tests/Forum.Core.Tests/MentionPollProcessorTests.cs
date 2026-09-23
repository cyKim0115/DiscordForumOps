namespace Forum.Core.Tests;

public class MentionPollProcessorTests
{
    private const ulong ForumChannelId = 99;
    private static readonly PostRef Post = new(ForumChannelId, postId: 7);

    [Fact]
    public async Task Poll_skips_non_mention()
    {
        var message = CreateMessage(messageId: 11, mentionsBot: false);
        var reader = new FakeForumReader(Post, message);
        var cursor = new InMemoryCursorStore();
        var handler = new RecordingMentionHandler();
        var processor = new MentionPollProcessor(reader, cursor, handler);

        await processor.PollOnceAsync(ForumChannelId, CancellationToken.None);

        Assert.Empty(handler.Handled);
        Assert.Equal(11ul, await cursor.GetLastSeenAsync(Post, CancellationToken.None));
    }

    [Fact]
    public async Task Poll_invokes_handler_on_mention()
    {
        var message = CreateMessage(messageId: 22, mentionsBot: true);
        var reader = new FakeForumReader(Post, message);
        var cursor = new InMemoryCursorStore();
        var handler = new RecordingMentionHandler();
        var processor = new MentionPollProcessor(reader, cursor, handler);

        await processor.PollOnceAsync(ForumChannelId, CancellationToken.None);

        var handled = Assert.Single(handler.Handled);
        Assert.Same(message, handled);
        Assert.Equal(22ul, await cursor.GetLastSeenAsync(Post, CancellationToken.None));
    }

    private static ForumMessage CreateMessage(ulong messageId, bool mentionsBot)
    {
        return new ForumMessage(
            new MessageRef(messageId, Post.PostId, DateTimeOffset.UnixEpoch),
            Post,
            mentionsBot ? "hello @bot" : "plain",
            mentionsBot,
            AuthorId: "author-1");
    }

    private sealed class FakeForumReader : IForumReader
    {
        private readonly IReadOnlyList<PostRef> _posts;
        private readonly IReadOnlyList<ForumMessage> _messages;

        public FakeForumReader(PostRef post, params ForumMessage[] messages)
        {
            _posts = [post];
            _messages = messages;
        }

        public Task<IReadOnlyList<PostRef>> ListActivePostsAsync(ulong forumChannelId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<PostRef> posts = _posts.Where(post => post.ForumChannelId == forumChannelId).ToList();
            return Task.FromResult(posts);
        }

        public Task<IReadOnlyList<ForumMessage>> GetMessagesAfterAsync(
            PostRef post, ulong? afterMessageId, int limit, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<ForumMessage> filtered = _messages
                .Where(message => message.Post == post)
                .Where(message => afterMessageId is null || message.Ref.MessageId > afterMessageId)
                .Take(limit)
                .ToList();
            return Task.FromResult(filtered);
        }
    }

    private sealed class RecordingMentionHandler : IMentionHandler
    {
        public List<ForumMessage> Handled { get; } = [];

        public Task HandleAsync(ForumMessage message, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Handled.Add(message);
            return Task.CompletedTask;
        }
    }
}
