using WireMock.Server;

namespace Forum.Discord.Tests;

public class DiscordWebhookPublisherCacheTests
{
    [Fact]
    public void Publisher_cache_reuses_one_instance_per_url()
    {
        using var server = WireMockServer.Start();
        using var webhook = server.CreateWebhookClient();
        using var cache = new DiscordWebhookPublisherCache(_ => new DiscordWebhookPublisher(webhook));

        var first = cache.GetOrCreate("https://cdn.example.test/hooks/alpha");
        var same = cache.GetOrCreate("https://cdn.example.test/hooks/alpha");
        var second = cache.GetOrCreate("https://cdn.example.test/hooks/bravo");

        Assert.Same(first, same);
        Assert.NotSame(first, second);
    }
}
