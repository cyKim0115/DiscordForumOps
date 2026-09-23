using Discord;
using Discord.Rest;
using Discord.Webhook;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Forum.Discord.Tests;

internal static class DiscordWireMock
{
    public const ulong BotUserId = 42;
    public const ulong GuildId = 10;
    public const ulong ForumChannelId = 100;
    public const ulong PostId = 200;
    public const ulong AfterMessageId = 300;
    public const ulong WebhookId = 400;
    public const string WebhookToken = "test_webhook_token";
    public const string BotToken = "test.bot.token";

    public static string ApiBaseUrl(this WireMockServer server)
        => server.Url!.TrimEnd('/') + "/api/v10/";

    public static DiscordRestConfig RestConfig(this WireMockServer server)
        => new()
        {
            RestClientProvider = _ => new RedirectRestClient(server.ApiBaseUrl()),
        };

    public static void StubBotLogin(this WireMockServer server)
    {
        server
            .Given(Request.Create().WithPath("/api/v10/users/@me").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{\"id\":\"{BotUserId}\",\"username\":\"forum-bot\",\"discriminator\":\"0\",\"bot\":true}}"));

        server
            .Given(Request.Create().WithPath("/api/v10/oauth2/applications/@me").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(
                    """
                    {"id":"1","name":"forum-app","description":"","icon":null,"verify_key":"verify","hook":false,"is_monetized":false}
                    """));
    }

    public static void StubThreadChannel(this WireMockServer server, ulong channelId = PostId)
    {
        server
            .Given(Request.Create().WithPath($"/api/v10/channels/{channelId}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(ThreadChannelJson(channelId)));
    }

    public static void StubMessagesAfter(
        this WireMockServer server,
        ulong channelId,
        ulong afterMessageId,
        string body)
    {
        server
            .Given(Request.Create()
                .WithPath($"/api/v10/channels/{channelId}/messages")
                .WithParam("after", afterMessageId.ToString())
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    public static void StubWebhookGet(this WireMockServer server)
    {
        server
            .Given(Request.Create().WithPath($"/api/v10/webhooks/{WebhookId}/{WebhookToken}").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{\"id\":\"{WebhookId}\",\"type\":1,\"guild_id\":\"{GuildId}\",\"channel_id\":\"{ForumChannelId}\",\"name\":\"forum-hook\",\"avatar\":null,\"token\":\"{WebhookToken}\",\"application_id\":null}}"));
    }

    public static void StubWebhookExecute(this WireMockServer server, string body, int statusCode = 200)
    {
        server
            .Given(Request.Create().WithPath($"/api/v10/webhooks/{WebhookId}/{WebhookToken}").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(statusCode)
                .WithHeader("Content-Type", "application/json")
                .WithBody(body));
    }

    public static async Task<DiscordRestClient> LoginRestClientAsync(this WireMockServer server)
    {
        server.StubBotLogin();
        var client = new DiscordRestClient(server.RestConfig());
        await client.LoginAsync(TokenType.Bot, BotToken);
        return client;
    }

    public static DiscordWebhookClient CreateWebhookClient(this WireMockServer server)
    {
        server.StubWebhookGet();
        return new DiscordWebhookClient(WebhookId, WebhookToken, server.RestConfig());
    }

    public static string MessageJson(
        ulong messageId,
        ulong channelId,
        string content,
        ulong authorId,
        params ulong[] mentionedUserIds)
    {
        var mentions = string.Join(
            ",",
            mentionedUserIds.Select(id => $"{{\"id\":\"{id}\",\"username\":\"user{id}\",\"discriminator\":\"0\"}}"));
        return $"{{\"id\":\"{messageId}\",\"type\":0,\"channel_id\":\"{channelId}\",\"content\":\"{content}\",\"author\":{{\"id\":\"{authorId}\",\"username\":\"author\",\"discriminator\":\"0\"}},\"timestamp\":\"2026-01-01T00:00:00.000000+00:00\",\"tts\":false,\"mention_everyone\":false,\"mentions\":[{mentions}],\"mention_roles\":[],\"attachments\":[],\"embeds\":[],\"pinned\":false}}";
    }

    public static string ThreadChannelJson(ulong channelId)
        => $"{{\"id\":\"{channelId}\",\"type\":11,\"guild_id\":\"{GuildId}\",\"parent_id\":\"{ForumChannelId}\",\"name\":\"post\",\"thread_metadata\":{{\"archived\":false,\"auto_archive_duration\":1440,\"archive_timestamp\":\"2026-01-01T00:00:00.000000+00:00\",\"locked\":false}}}}";
}
