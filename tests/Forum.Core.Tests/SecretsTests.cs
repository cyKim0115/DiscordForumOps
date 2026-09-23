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
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
