using Forum.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Forum.Core.Tests;

public class PollingHostedServiceTests
{
    private const ulong ForumChannelId = 99;
    private static readonly PostRef Post = new(ForumChannelId, PostId: 7);

    [Fact]
    public async Task Poll_tick_skips_non_mention_and_advances_cursor()
    {
        var message = CreateMessage(messageId: 11, mentionsBot: false);
        var reader = new FakeForumReader(Post, message);
        var cursor = new InMemoryCursorStore();
        var handler = new RecordingMentionHandler();
        var (service, interval) = CreateService(reader, cursor, handler);

        var newCount = await service.TickAsync(CancellationToken.None);

        Assert.Equal(1, newCount);
        Assert.Empty(handler.Handled);
        Assert.Equal(11ul, await cursor.GetLastSeenAsync(Post, CancellationToken.None));
        Assert.Equal(TimeSpan.FromSeconds(10), interval.Current);
    }

    [Fact]
    public async Task Poll_tick_invokes_handler_on_mention()
    {
        var message = CreateMessage(messageId: 22, mentionsBot: true);
        var reader = new FakeForumReader(Post, message);
        var cursor = new InMemoryCursorStore();
        var handler = new RecordingMentionHandler();
        var (service, interval) = CreateService(reader, cursor, handler);

        var newCount = await service.TickAsync(CancellationToken.None);

        Assert.Equal(1, newCount);
        var handled = Assert.Single(handler.Handled);
        Assert.Same(message, handled);
        Assert.Equal(22ul, await cursor.GetLastSeenAsync(Post, CancellationToken.None));
        Assert.Equal(TimeSpan.FromSeconds(10), interval.Current);
    }

    [Fact]
    public async Task Poll_loop_uses_active_then_expands_after_quiet_ticks()
    {
        var message = CreateMessage(messageId: 33, mentionsBot: true);
        var reader = new FakeForumReader(Post, message);
        var cursor = new InMemoryCursorStore();
        var handler = new RecordingMentionHandler();
        var delays = new List<TimeSpan>();
        using var cts = new CancellationTokenSource();
        var (service, _) = CreateService(reader, cursor, handler, (delay, _) =>
        {
            delays.Add(delay);
            if (delays.Count >= 3)
            {
                cts.Cancel();
            }

            return Task.CompletedTask;
        });

        await service.StartAsync(cts.Token);
        if (service.ExecuteTask is not null)
        {
            await service.ExecuteTask;
        }

        Assert.Single(handler.Handled);
        Assert.Equal(33ul, await cursor.GetLastSeenAsync(Post, CancellationToken.None));
        Assert.Equal(3, delays.Count);
        Assert.Equal(TimeSpan.FromSeconds(10), delays[0]);
        Assert.Equal(TimeSpan.FromSeconds(20), delays[1]);
        Assert.Equal(TimeSpan.FromSeconds(40), delays[2]);
    }

    private static (PollingHostedService Service, AdaptivePollInterval Interval) CreateService(
        IForumReader reader,
        ICursorStore cursor,
        IMentionHandler handler,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        var options = new PollingOptions
        {
            ForumChannelId = ForumChannelId,
            IdleSeconds = 60,
            ActiveSeconds = 10,
            QuietTicksToExpand = 1,
        };
        var interval = AdaptivePollInterval.From(options);
        var processor = new MentionPollProcessor(reader, cursor, handler);
        var service = new PollingHostedService(
            processor,
            interval,
            Options.Create(options),
            NullLogger<PollingHostedService>.Instance,
            delay);
        return (service, interval);
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
