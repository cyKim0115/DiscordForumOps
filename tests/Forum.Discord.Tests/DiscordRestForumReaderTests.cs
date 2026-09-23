using Forum.Core;
using WireMock.Server;

namespace Forum.Discord.Tests;

public class DiscordRestForumReaderTests
{
    [Fact]
    public async Task Reader_uses_after_cursor()
    {
        using var server = WireMockServer.Start();
        server.StubThreadChannel();
        server.StubMessagesAfter(
            DiscordWireMock.PostId,
            DiscordWireMock.AfterMessageId,
            $"[{DiscordWireMock.MessageJson(301, DiscordWireMock.PostId, "<@42> hi", authorId: 7, DiscordWireMock.BotUserId)}]");

        await using var rest = await server.LoginRestClientAsync();
        var reader = new DiscordRestForumReader(rest, DiscordWireMock.BotUserId);
        var post = new PostRef(DiscordWireMock.ForumChannelId, DiscordWireMock.PostId);

        var messages = await reader.GetMessagesAfterAsync(
            post,
            DiscordWireMock.AfterMessageId,
            limit: 50,
            CancellationToken.None);

        var request = Assert.Single(
            server.LogEntries,
            entry => WireMockRequest.PathContains(entry, $"/channels/{DiscordWireMock.PostId}/messages"));
        var after = WireMockRequest.QueryValue(request, "after");
        var limit = WireMockRequest.QueryValue(request, "limit");
        Assert.Equal(DiscordWireMock.AfterMessageId.ToString(), after);
        Assert.True(int.Parse(limit) <= 100);

        var message = Assert.Single(messages);
        Assert.Equal(301ul, message.Ref.MessageId);
        Assert.True(message.MentionsBot);
    }
}
