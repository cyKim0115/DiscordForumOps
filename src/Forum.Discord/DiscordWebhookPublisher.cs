using Discord;
using Discord.Webhook;
using Forum.Core;

namespace Forum.Discord;

public sealed class DiscordWebhookPublisher : IPersonaPublisher, IDisposable
{
    private readonly DiscordWebhookClient _client;
    private readonly bool _ownsClient;

    public DiscordWebhookPublisher(DiscordWebhookClient client)
        : this(client, ownsClient: false)
    {
    }

    public DiscordWebhookPublisher(string webhookUrl)
        : this(new DiscordWebhookClient(webhookUrl), ownsClient: true)
    {
    }

    private DiscordWebhookPublisher(DiscordWebhookClient client, bool ownsClient)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _ownsClient = ownsClient;
    }

    public async Task<ulong> CreatePostAsync(
        ulong forumChannelId,
        string threadName,
        string content,
        Persona persona,
        IReadOnlyList<ulong>? appliedTagIds,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(threadName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(persona);
        _ = forumChannelId;

        return await _client.SendMessageAsync(
            text: content,
            username: persona.Name,
            avatarUrl: persona.AvatarUrl,
            options: Options(ct),
            threadName: threadName,
            appliedTags: appliedTagIds?.ToArray()).ConfigureAwait(false);
    }

    public async Task ReplyAsync(
        PostRef post,
        string content,
        Persona persona,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(post);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(persona);

        await _client.SendMessageAsync(
            text: content,
            username: persona.Name,
            avatarUrl: persona.AvatarUrl,
            options: Options(ct),
            threadId: post.PostId).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _client.Dispose();
        }
    }

    private static RequestOptions Options(CancellationToken ct) => new() { CancelToken = ct };
}
