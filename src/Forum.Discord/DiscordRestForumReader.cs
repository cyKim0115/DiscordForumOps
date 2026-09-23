using Discord;
using Discord.Rest;
using Forum.Core;

namespace Forum.Discord;

public sealed class DiscordRestForumReader : IForumReader
{
    private const int MaxPageSize = 100;

    private readonly DiscordRestClient _client;
    private readonly ulong _botUserId;

    public DiscordRestForumReader(DiscordRestClient client, ulong botUserId)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _botUserId = botUserId;
    }

    public static DiscordRestForumReader Connect(string botToken, ulong botUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        var client = new DiscordRestClient();
        client.LoginAsync(TokenType.Bot, botToken).GetAwaiter().GetResult();
        return new DiscordRestForumReader(client, botUserId);
    }

    public async Task<(ulong Id, string Username)> GetCurrentUserAsync(CancellationToken ct)
    {
        var user = await _client.GetCurrentUserAsync(Options(ct)).ConfigureAwait(false);
        return (user.Id, user.Username);
    }

    public async Task<IReadOnlyList<PostRef>> ListActivePostsAsync(ulong forumChannelId, CancellationToken ct)
    {
        var options = Options(ct);
        var channel = await _client.GetChannelAsync(forumChannelId, options).ConfigureAwait(false);
        if (channel is not IForumChannel forum)
        {
            throw new InvalidOperationException("Forum channel was not found or is not a forum.");
        }

        var threads = await forum.GetActiveThreadsAsync(options).ConfigureAwait(false);
        return threads.Select(thread => new PostRef(forumChannelId, thread.Id)).ToArray();
    }

    public async Task<IReadOnlyList<ForumMessage>> GetMessagesAfterAsync(
        PostRef post, ulong? afterMessageId, int limit, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(post);

        var options = Options(ct);
        var channel = await _client.GetChannelAsync(post.PostId, options).ConfigureAwait(false);
        if (channel is not IMessageChannel messageChannel)
        {
            throw new InvalidOperationException("Forum post channel was not found or cannot receive messages.");
        }

        var pageSize = Math.Clamp(limit, 1, MaxPageSize);
        var after = afterMessageId ?? 0UL;
        var mapped = new List<ForumMessage>();
        await foreach (var batch in messageChannel
            .GetMessagesAsync(after, Direction.After, pageSize, CacheMode.AllowDownload, options)
            .WithCancellation(ct)
            .ConfigureAwait(false))
        {
            foreach (var message in batch)
            {
                mapped.Add(Map(post, message));
            }
        }

        return mapped;
    }

    private ForumMessage Map(PostRef post, IMessage message)
    {
        var mentionsBot = message.MentionedUserIds.Contains(_botUserId);
        return new ForumMessage(
            new MessageRef(message.Id, message.Channel.Id, message.Timestamp),
            post,
            message.Content ?? string.Empty,
            mentionsBot,
            message.Author.Id.ToString());
    }

    private static RequestOptions Options(CancellationToken ct) => new() { CancelToken = ct };
}
