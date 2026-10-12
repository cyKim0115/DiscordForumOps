using System.Collections.Concurrent;
using Forum.Core;

namespace Forum.Discord;

public sealed class DiscordWebhookPublisherCache : IDisposable
{
    private readonly ConcurrentDictionary<string, DiscordWebhookPublisher> _publishers =
        new(StringComparer.Ordinal);
    private readonly Func<string, DiscordWebhookPublisher> _factory;

    public DiscordWebhookPublisherCache()
        : this(static url => new DiscordWebhookPublisher(url))
    {
    }

    public DiscordWebhookPublisherCache(Func<string, DiscordWebhookPublisher> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _factory = factory;
    }

    public IPersonaPublisher GetOrCreate(string webhookUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(webhookUrl);
        return _publishers.GetOrAdd(webhookUrl.Trim(), url => _factory(url));
    }

    public void Dispose()
    {
        foreach (var publisher in _publishers.Values)
        {
            publisher.Dispose();
        }

        _publishers.Clear();
    }
}
