using System.Text.Json;
using Forum.Core;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Forum.Discord.Tests;

public class DiscordWebhookPublisherTests
{
    [Fact]
    public async Task Publisher_create_post_sends_thread_name_and_persona()
    {
        using var server = WireMockServer.Start();
        server.StubWebhookExecute(DiscordWireMock.MessageJson(501, DiscordWireMock.PostId, "hello", authorId: 1));
        using var webhook = server.CreateWebhookClient();
        using var publisher = new DiscordWebhookPublisher(webhook);
        var persona = new Persona("OpsBot", "https://cdn.example.test/avatar.png");

        var id = await publisher.CreatePostAsync(
            DiscordWireMock.ForumChannelId,
            "new-thread",
            "hello",
            persona,
            appliedTagIds: null,
            CancellationToken.None);

        Assert.Equal(501ul, id);
        var request = Assert.Single(server.LogEntries, IsWebhookPost);
        using var body = JsonDocument.Parse(WireMockRequest.Body(request));
        Assert.Equal("new-thread", body.RootElement.GetProperty("thread_name").GetString());
        Assert.Equal("OpsBot", body.RootElement.GetProperty("username").GetString());
        Assert.Equal("https://cdn.example.test/avatar.png", body.RootElement.GetProperty("avatar_url").GetString());
    }

    [Fact]
    public async Task Publisher_reply_uses_thread_id()
    {
        using var server = WireMockServer.Start();
        server.StubWebhookExecute(DiscordWireMock.MessageJson(502, DiscordWireMock.PostId, "reply", authorId: 1));
        using var webhook = server.CreateWebhookClient();
        using var publisher = new DiscordWebhookPublisher(webhook);
        var persona = new Persona("OpsBot", "https://cdn.example.test/avatar.png");
        var post = new PostRef(DiscordWireMock.ForumChannelId, DiscordWireMock.PostId);

        await publisher.ReplyAsync(post, "reply", persona, CancellationToken.None);

        var request = Assert.Single(server.LogEntries, IsWebhookPost);
        Assert.Equal(DiscordWireMock.PostId.ToString(), WireMockRequest.QueryValue(request, "thread_id"));
    }

    [Fact]
    public async Task RateLimit_429_retries_once()
    {
        using var server = WireMockServer.Start();
        server
            .Given(Request.Create().WithPath($"/api/v10/webhooks/{DiscordWireMock.WebhookId}/{DiscordWireMock.WebhookToken}").UsingPost())
            .InScenario("rate-limit")
            .WillSetStateTo("retried")
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "0")
                .WithHeader("X-RateLimit-Reset-After", "0")
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"message":"You are being rate limited.","retry_after":0.001,"global":false}"""));
        server
            .Given(Request.Create().WithPath($"/api/v10/webhooks/{DiscordWireMock.WebhookId}/{DiscordWireMock.WebhookToken}").UsingPost())
            .InScenario("rate-limit")
            .WhenStateIs("retried")
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(DiscordWireMock.MessageJson(503, DiscordWireMock.PostId, "ok", authorId: 1)));

        using var webhook = server.CreateWebhookClient();
        using var publisher = new DiscordWebhookPublisher(webhook);
        var persona = new Persona("OpsBot", "https://cdn.example.test/avatar.png");

        await publisher.ReplyAsync(
            new PostRef(DiscordWireMock.ForumChannelId, DiscordWireMock.PostId),
            "ok",
            persona,
            CancellationToken.None);

        Assert.Equal(2, server.LogEntries.Count(IsWebhookPost));
    }

    private static bool IsWebhookPost(WireMock.Logging.ILogEntry entry)
        => WireMockRequest.IsPostTo(entry, $"/webhooks/{DiscordWireMock.WebhookId}/");
}
