namespace Forum.Core;

public static class PersonaAvatarResolver
{
    public const string MapPathKey = PersonaMap.MapPathKey;

    public static string DefaultPath => PersonaMap.DefaultPath;

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

        var map = PersonaMap.Load(mapPath, warn);
        var entry = map.Find(personaName);
        if (!string.IsNullOrWhiteSpace(entry?.CdnUrl))
        {
            return entry.CdnUrl;
        }

        if (entry is null && map.HasEntries)
        {
            warn?.Invoke("No avatar map entry matched the persona name; continuing without a mapped avatar.");
        }

        return string.IsNullOrWhiteSpace(configuredDefaultAvatarUrl)
            ? null
            : configuredDefaultAvatarUrl.Trim();
    }
}
