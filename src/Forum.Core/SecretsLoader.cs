namespace Forum.Core;

public sealed class ForumSecrets
{
    public ForumSecrets(string botToken, string forumWebhookUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(forumWebhookUrl);
        BotToken = botToken;
        ForumWebhookUrl = forumWebhookUrl;
    }

    public string BotToken { get; }
    public string ForumWebhookUrl { get; }

    public override string ToString() =>
        $"ForumSecrets {{ {SecretsLoader.BotTokenKey}, {SecretsLoader.ForumWebhookUrlKey} }}";
}

public static class SecretsLoader
{
    public const string BotTokenKey = "DISCORD_BOT_TOKEN";
    public const string ForumWebhookUrlKey = "DISCORD_FORUM_WEBHOOK_URL";

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Sensitive",
        "mcp-secrets.env");

    public static ForumSecrets Load(string? path = null)
    {
        var resolved = string.IsNullOrWhiteSpace(path) ? DefaultPath : path;
        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException(
                $"Secrets file was not found. Expected keys: {BotTokenKey}, {ForumWebhookUrlKey}.",
                resolved);
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rawLine in File.ReadAllLines(resolved))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            const string exportPrefix = "export ";
            if (line.StartsWith(exportPrefix, StringComparison.Ordinal))
            {
                line = line[exportPrefix.Length..].TrimStart();
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            if (key.Length == 0)
            {
                continue;
            }

            values[key] = Unquote(line[(separator + 1)..].Trim());
        }

        return new ForumSecrets(Require(values, BotTokenKey), Require(values, ForumWebhookUrlKey));
    }

    private static string Require(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required secret key '{key}' is missing.");
        }

        return value;
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2
            && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }
}
