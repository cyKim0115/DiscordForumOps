using System.Text.RegularExpressions;

namespace Forum.Core;

public static partial class LogScrubber
{
    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        var redacted = WebhookPath().Replace(text, "/api/webhooks/***/***");
        return BotToken().Replace(redacted, "Bot ***");
    }

    [GeneratedRegex(@"/api/webhooks/\d+/[\w-]+", RegexOptions.CultureInvariant)]
    private static partial Regex WebhookPath();

    [GeneratedRegex(@"(?:Bot\s+)?[A-Za-z0-9_-]{24,30}\.[A-Za-z0-9_-]{6}\.[A-Za-z0-9_-]{27,40}", RegexOptions.CultureInvariant)]
    private static partial Regex BotToken();
}
