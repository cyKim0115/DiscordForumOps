using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forum.Core;

public static class PersonaAvatarResolver
{
    public const string MapPathKey = "DISCORD_AVATAR_MAP_PATH";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Sensitive",
        "avatar-cdn-map.json");

    public static string? Resolve(
        string personaName,
        string? explicitAvatarUrl,
        string? configuredDefaultAvatarUrl,
        string? mapPath = null,
        Action<string>? warn = null)
    {
        if (!string.IsNullOrWhiteSpace(explicitAvatarUrl))
        {
            return explicitAvatarUrl.Trim();
        }

        var mapped = TryReadMappedAvatar(personaName, mapPath, warn);
        if (!string.IsNullOrWhiteSpace(mapped))
        {
            return mapped;
        }

        return string.IsNullOrWhiteSpace(configuredDefaultAvatarUrl)
            ? null
            : configuredDefaultAvatarUrl.Trim();
    }

    private static string? TryReadMappedAvatar(string personaName, string? mapPath, Action<string>? warn)
    {
        var path = string.IsNullOrWhiteSpace(mapPath) ? DefaultPath : mapPath.Trim();
        if (!File.Exists(path))
        {
            Warn(warn, "Avatar map file was not found; continuing without a mapped avatar.");
            return null;
        }

        IReadOnlyList<AvatarMapEntry>? entries;
        try
        {
            var json = File.ReadAllText(path);
            entries = JsonSerializer.Deserialize<List<AvatarMapEntry>>(json, JsonOptions);
        }
        catch (Exception)
        {
            Warn(warn, "Avatar map file could not be read; continuing without a mapped avatar.");
            return null;
        }

        if (entries is null || entries.Count == 0)
        {
            Warn(warn, "Avatar map file was empty; continuing without a mapped avatar.");
            return null;
        }

        var name = personaName.Trim();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || string.IsNullOrWhiteSpace(entry.CdnUrl))
            {
                continue;
            }

            if (string.Equals(entry.Name.Trim(), name, StringComparison.OrdinalIgnoreCase))
            {
                return entry.CdnUrl.Trim();
            }
        }

        Warn(warn, "No avatar map entry matched the persona name; continuing without a mapped avatar.");
        return null;
    }

    private static void Warn(Action<string>? warn, string message) => warn?.Invoke(message);

    private sealed class AvatarMapEntry
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("agent_id")]
        public string? AgentId { get; set; }

        [JsonPropertyName("cdn_url")]
        public string? CdnUrl { get; set; }
    }
}
