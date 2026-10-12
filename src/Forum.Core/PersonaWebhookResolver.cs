namespace Forum.Core;

public static class PersonaWebhookResolver
{
    public const string PersonaSource = "persona";
    public const string SharedSource = "shared";

    public static string? ResolveUrl(string personaName, string? mapPath = null)
    {
        var map = PersonaMap.Load(mapPath, warn: null);
        return map.Find(personaName)?.ForumWebhookUrl;
    }

    public static string ResolveSource(string personaName, string? mapPath = null)
    {
        return string.IsNullOrWhiteSpace(ResolveUrl(personaName, mapPath))
            ? SharedSource
            : PersonaSource;
    }
}
