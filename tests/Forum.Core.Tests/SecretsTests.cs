namespace Forum.Core.Tests;

public class SecretsTests
{
    [Fact]
    public void Secrets_never_logged()
    {
        const string webhookUrl = "https://discord.com/api/webhooks/123456789012345678/FakeWebhookToken_UnitTestOnly";
        const string botToken = "AAAAAAAAAAAAAAAAAAAAAAAA.AAAAAA.AAAAAAAAAAAAAAAAAAAAAAAAAAA";
        var path = Path.Combine(Path.GetTempPath(), $"forum-secrets-{Guid.NewGuid():N}.env");

        try
        {
            File.WriteAllLines(path,
            [
                $"{SecretsLoader.BotTokenKey}={botToken}",
                $"{SecretsLoader.ForumWebhookUrlKey}={webhookUrl}",
            ]);

            var secrets = SecretsLoader.Load(path);
            var leaky = $"login {secrets.BotToken} hook {secrets.ForumWebhookUrl} Authorization: Bot {botToken}";
            var redacted = LogScrubber.Redact(leaky);
            var printed = secrets.ToString();

            Assert.Contains("/api/webhooks/***/***", redacted);
            Assert.Contains("Bot ***", redacted);
            Assert.DoesNotContain("Bot Bot ***", redacted);
            Assert.DoesNotContain(botToken, redacted);
            Assert.DoesNotContain("123456789012345678", redacted);
            Assert.DoesNotContain("FakeWebhookToken_UnitTestOnly", redacted);
            Assert.DoesNotContain(botToken, printed);
            Assert.DoesNotContain(webhookUrl, printed);
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void Secrets_optional_ids_load_from_file()
    {
        const string webhookUrl = "https://discord.com/api/webhooks/123456789012345678/FakeWebhookToken_UnitTestOnly";
        const string botToken = "AAAAAAAAAAAAAAAAAAAAAAAA.AAAAAA.AAAAAAAAAAAAAAAAAAAAAAAAAAA";
        var path = Path.Combine(Path.GetTempPath(), $"forum-secrets-ids-{Guid.NewGuid():N}.env");

        try
        {
            File.WriteAllLines(path,
            [
                $"{SecretsLoader.BotTokenKey}={botToken}",
                $"{SecretsLoader.ForumWebhookUrlKey}={webhookUrl}",
                $"{SecretsLoader.BotUserIdKey}=1559000000000000001",
                $"{SecretsLoader.ForumChannelIdKey}=1559000000000000002",
            ]);

            var secrets = SecretsLoader.Load(path);

            Assert.Equal(1559000000000000001UL, secrets.BotUserId);
            Assert.Equal(1559000000000000002UL, secrets.ForumChannelId);
            Assert.True(SecretsLoader.TryReadOptionalId(SecretsLoader.BotUserIdKey, out var botUserId, path));
            Assert.Equal(1559000000000000001UL, botUserId);
            Assert.True(SecretsLoader.TryReadOptionalId(SecretsLoader.ForumChannelIdKey, out var forumChannelId, path));
            Assert.Equal(1559000000000000002UL, forumChannelId);
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void Secrets_optional_ids_absent_do_not_fail_load()
    {
        const string webhookUrl = "https://discord.com/api/webhooks/123456789012345678/FakeWebhookToken_UnitTestOnly";
        const string botToken = "AAAAAAAAAAAAAAAAAAAAAAAA.AAAAAA.AAAAAAAAAAAAAAAAAAAAAAAAAAA";
        var path = Path.Combine(Path.GetTempPath(), $"forum-secrets-no-ids-{Guid.NewGuid():N}.env");

        try
        {
            File.WriteAllLines(path,
            [
                $"{SecretsLoader.BotTokenKey}={botToken}",
                $"{SecretsLoader.ForumWebhookUrlKey}={webhookUrl}",
            ]);

            var secrets = SecretsLoader.Load(path);

            Assert.Null(secrets.BotUserId);
            Assert.Null(secrets.ForumChannelId);
            Assert.False(SecretsLoader.TryReadOptionalId(SecretsLoader.BotUserIdKey, out _, path));
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void Secrets_config_overrides_file_ids()
    {
        Assert.True(SecretsLoader.TryResolveUInt64("99", 11, out var configured));
        Assert.Equal(99UL, configured);

        Assert.True(SecretsLoader.TryResolveUInt64(null, 11, out var fromSecrets));
        Assert.Equal(11UL, fromSecrets);

        Assert.True(SecretsLoader.TryResolveUInt64("0", 11, out var zeroConfig));
        Assert.Equal(11UL, zeroConfig);

        Assert.False(SecretsLoader.TryResolveUInt64(null, null, out var missing));
        Assert.Equal(0UL, missing);
    }

    [Fact]
    public void Secrets_optional_ids_missing_file_does_not_throw()
    {
        var path = Path.Combine(Path.GetTempPath(), $"forum-secrets-missing-{Guid.NewGuid():N}.env");

        Assert.False(SecretsLoader.TryReadOptionalId(SecretsLoader.BotUserIdKey, out var value, path));
        Assert.Equal(0UL, value);
    }

    private static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
