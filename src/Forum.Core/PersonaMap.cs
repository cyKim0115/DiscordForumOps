using System.Text.Json;
using System.Text.Json.Serialization;

namespace Forum.Core;

public sealed class PersonaMapEntry
{
    public PersonaMapEntry(string name, string? agentId, string? cdnUrl, string? forumWebhookUrl)
    {
        Name = name;
        AgentId = agentId;
        CdnUrl = string.IsNullOrWhiteSpace(cdnUrl) ? null : cdnUrl.Trim();
        ForumWebhookUrl = string.IsNullOrWhiteSpace(forumWebhookUrl) ? null : forumWebhookUrl.Trim();
    }

    public string Name { get; }
    public string? AgentId { get; }
    public string? CdnUrl { get; }
    public string? ForumWebhookUrl { get; }
}

public sealed class PersonaMap
{
    public const string MapPathKey = "DISCORD_AVATAR_MAP_PATH";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly IReadOnlyList<PersonaMapEntry> _entries;

    private PersonaMap(IReadOnlyList<PersonaMapEntry> entries)
    {
        _entries = entries;
    }

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Sensitive",
        "avatar-cdn-map.json");

    public static PersonaMap Empty { get; } = new([]);

    public bool HasEntries => _entries.Count > 0;

    public static PersonaMap Load(string? path, Action<string>? warn = null)
    {
        var resolved = string.IsNullOrWhiteSpace(path) ? DefaultPath : path.Trim();
        if (!File.Exists(resolved))
        {
            warn?.Invoke("Avatar map file was not found; continuing without a mapped avatar.");
            return Empty;
        }

        IReadOnlyList<MapDto>? raw;
        try
        {
            var json = File.ReadAllText(resolved);
            raw = JsonSerializer.Deserialize<List<MapDto>>(json, JsonOptions);
        }
        catch (Exception)
        {
            warn?.Invoke("Avatar map file could not be read; continuing without a mapped avatar.");
            return Empty;
        }

        if (raw is null || raw.Count == 0)
        {
            warn?.Invoke("Avatar map file was empty; continuing without a mapped avatar.");
            return Empty;
        }

        var entries = new List<PersonaMapEntry>(raw.Count);
        foreach (var dto in raw)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                continue;
            }

            entries.Add(new PersonaMapEntry(dto.Name.Trim(), dto.AgentId, dto.CdnUrl, dto.ForumWebhookUrl));
        }

        if (entries.Count == 0)
        {
            warn?.Invoke("Avatar map file was empty; continuing without a mapped avatar.");
            return Empty;
        }

        return new PersonaMap(entries);
    }

    public PersonaMapEntry? Find(string personaName)
    {
        var name = personaName.Trim();
        foreach (var entry in _entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    private sealed class MapDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("agent_id")]
        public string? AgentId { get; set; }

        [JsonPropertyName("cdn_url")]
        public string? CdnUrl { get; set; }

        [JsonPropertyName("forum_webhook_url")]
        public string? ForumWebhookUrl { get; set; }
    }
}
