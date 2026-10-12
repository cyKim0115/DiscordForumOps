namespace Forum.Core.Tests;

public class PersonaWebhookResolverTests
{
    private const string PersonaWebhook = "https://discord.com/api/webhooks/111111111111111111/FakePersonaWeld_UnitTestOnly";
    private const string SharedWebhook = "https://discord.com/api/webhooks/222222222222222222/FakeSharedHook_UnitTestOnly";

    [Fact]
    public void Webhook_map_entry_wins_then_falls_back_to_shared()
    {
        var path = WriteMap();

        try
        {
            var weld = PersonaWebhookResolver.ResolveUrl("Weld", path);
            var reed = PersonaWebhookResolver.ResolveUrl("Reed", path);
            var caseInsensitive = PersonaWebhookResolver.ResolveUrl("weld", path);
            var unknown = PersonaWebhookResolver.ResolveUrl("Unknown", path);
            var emptyField = PersonaWebhookResolver.ResolveUrl("Keel", path);

            Assert.Equal(PersonaWebhook, weld);
            Assert.Null(reed);
            Assert.Equal(PersonaWebhook, caseInsensitive);
            Assert.Null(unknown);
            Assert.Null(emptyField);

            Assert.Equal(PersonaWebhookResolver.PersonaSource, PersonaWebhookResolver.ResolveSource("Weld", path));
            Assert.Equal(PersonaWebhookResolver.SharedSource, PersonaWebhookResolver.ResolveSource("Reed", path));
            Assert.Equal(PersonaWebhookResolver.SharedSource, PersonaWebhookResolver.ResolveSource("Unknown", path));
            Assert.Equal(PersonaWebhookResolver.SharedSource, PersonaWebhookResolver.ResolveSource("Keel", path));

            var sharedFallback = weld ?? SharedWebhook;
            var reedFallback = reed ?? SharedWebhook;
            Assert.Equal(PersonaWebhook, sharedFallback);
            Assert.Equal(SharedWebhook, reedFallback);
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void Webhook_missing_file_falls_back_without_throwing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"forum-webhook-missing-{Guid.NewGuid():N}.json");

        var url = PersonaWebhookResolver.ResolveUrl("Weld", missingPath);
        var source = PersonaWebhookResolver.ResolveSource("Weld", missingPath);

        Assert.Null(url);
        Assert.Equal(PersonaWebhookResolver.SharedSource, source);
    }

    [Fact]
    public void Webhook_invalid_json_falls_back_without_throwing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"forum-webhook-bad-{Guid.NewGuid():N}.json");

        try
        {
            File.WriteAllText(path, "{not-json");
            var url = PersonaWebhookResolver.ResolveUrl("Weld", path);
            var source = PersonaWebhookResolver.ResolveSource("Weld", path);

            Assert.Null(url);
            Assert.Equal(PersonaWebhookResolver.SharedSource, source);
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void Webhook_source_labels_never_contain_urls()
    {
        var path = WriteMap();

        try
        {
            var persona = PersonaWebhookResolver.ResolveSource("Weld", path);
            var shared = PersonaWebhookResolver.ResolveSource("Reed", path);

            Assert.Equal("persona", persona);
            Assert.Equal("shared", shared);
            Assert.DoesNotContain("http", persona, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("http", shared, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(PersonaWebhook, persona, StringComparison.Ordinal);
            Assert.DoesNotContain(PersonaWebhook, shared, StringComparison.Ordinal);
        }
        finally
        {
            Delete(path);
        }
    }

    private static string WriteMap()
    {
        var path = Path.Combine(Path.GetTempPath(), $"forum-webhook-map-{Guid.NewGuid():N}.json");
        File.WriteAllText(
            path,
            """
            [
              {"name": "Reed", "agent_id": "agent-reed", "cdn_url": "https://cdn.example.test/reed.png"},
              {"name": "Weld", "agent_id": "agent-weld", "cdn_url": "https://cdn.example.test/weld.png", "forum_webhook_url": "https://discord.com/api/webhooks/111111111111111111/FakePersonaWeld_UnitTestOnly"},
              {"name": "Keel", "agent_id": "agent-keel", "cdn_url": "https://cdn.example.test/keel.png", "forum_webhook_url": ""}
            ]
            """);
        return path;
    }

    private static void Delete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
