namespace Forum.Core;

public sealed class ForumSecrets
{
    public ForumSecrets(
        string botToken,
        string forumWebhookUrl,
        ulong? botUserId = null,
        ulong? forumChannelId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(botToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(forumWebhookUrl);
        BotToken = botToken;
        ForumWebhookUrl = forumWebhookUrl;
        BotUserId = NormalizeId(botUserId);
        ForumChannelId = NormalizeId(forumChannelId);
    }

    public string BotToken { get; }
    public string ForumWebhookUrl { get; }
    public ulong? BotUserId { get; }
    public ulong? ForumChannelId { get; }

    public override string ToString() =>
        $"ForumSecrets {{ {SecretsLoader.BotTokenKey}, {SecretsLoader.ForumWebhookUrlKey}, {SecretsLoader.BotUserIdKey}, {SecretsLoader.ForumChannelIdKey} }}";

    private static ulong? NormalizeId(ulong? value) => value is > 0 ? value : null;
}

public static class SecretsLoader
{
    public const string BotTokenKey = "DISCORD_BOT_TOKEN";
    public const string ForumWebhookUrlKey = "DISCORD_FORUM_WEBHOOK_URL";
    public const string BotUserIdKey = "DISCORD_BOT_USER_ID";
    public const string ForumChannelIdKey = "DISCORD_FORUM_CHANNEL_ID";

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Sensitive",
        "mcp-secrets.env");

    public static ForumSecrets Load(string? path = null)
    {
        var values = ReadValues(path);
        return new ForumSecrets(
            Require(values, BotTokenKey),
            Require(values, ForumWebhookUrlKey),
            ParseId(values, BotUserIdKey),
            ParseId(values, ForumChannelIdKey));
    }

    public static bool TryResolveUInt64(string? configuredValue, ulong? secretsValue, out ulong value)
    {
        if (ulong.TryParse(configuredValue, out value) && value != 0)
        {
            return true;
        }

        if (secretsValue is > 0)
        {
            value = secretsValue.Value;
            return true;
        }

        value = 0;
        return false;
    }

    public static bool TryReadOptionalId(string key, out ulong value, string? path = null)
    {
        value = 0;
        try
        {
            var parsed = ParseId(ReadValues(path), key);
            if (parsed is not > 0)
            {
                return false;
            }

            value = parsed.Value;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static Dictionary<string, string> ReadValues(string? path)
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

        return values;
    }

    private static string Require(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required secret key '{key}' is missing.");
        }

        return value;
    }

    private static ulong? ParseId(IReadOnlyDictionary<string, string> values, string key)
    {
        if (values.TryGetValue(key, out var text)
            && ulong.TryParse(text, out var id)
            && id != 0)
        {
            return id;
        }

        return null;
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
