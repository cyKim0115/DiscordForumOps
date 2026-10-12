namespace Forum.Core.Tests;

public class PersonaAvatarResolverTests
{
    private const string ExplicitUrl = "https://cdn.example.test/explicit.png";
    private const string DefaultUrl = "https://cdn.example.test/default.png";
    private const string WeldUrl = "https://cdn.example.test/weld.png";

    [Fact]
    public void Avatar_resolver_uses_precedence_explicit_then_map_then_default()
    {
        var path = WriteMap();
        var warnings = new List<string>();

        try
        {
            var explicitWins = PersonaAvatarResolver.Resolve(
                "Weld",
                ExplicitUrl,
                DefaultUrl,
                path,
                warnings.Add);
            var mapWins = PersonaAvatarResolver.Resolve(
                "Weld",
                explicitAvatarUrl: null,
                DefaultUrl,
                path,
                warnings.Add);
            var caseInsensitive = PersonaAvatarResolver.Resolve(
                "weld",
                explicitAvatarUrl: null,
                DefaultUrl,
                path,
                warnings.Add);
            var defaultWins = PersonaAvatarResolver.Resolve(
                "Unknown",
                explicitAvatarUrl: null,
                DefaultUrl,
                path,
                warnings.Add);
            var none = PersonaAvatarResolver.Resolve(
                "Unknown",
                explicitAvatarUrl: null,
                configuredDefaultAvatarUrl: null,
                path,
                warnings.Add);

            Assert.Equal(ExplicitUrl, explicitWins);
            Assert.Equal(WeldUrl, mapWins);
            Assert.Equal(WeldUrl, caseInsensitive);
            Assert.Equal(DefaultUrl, defaultWins);
            Assert.Null(none);
            Assert.DoesNotContain(warnings, warning => warning.Contains("http", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(warnings, warning => warning.Contains("No avatar map entry", StringComparison.Ordinal));
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void Avatar_map_missing_file_warns_and_falls_back()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"forum-avatar-missing-{Guid.NewGuid():N}.json");
        var warnings = new List<string>();

        var fallback = PersonaAvatarResolver.Resolve(
            "Weld",
            explicitAvatarUrl: null,
            DefaultUrl,
            missingPath,
            warnings.Add);
        var none = PersonaAvatarResolver.Resolve(
            "Weld",
            explicitAvatarUrl: null,
            configuredDefaultAvatarUrl: null,
            missingPath,
            warnings.Add);

        Assert.Equal(DefaultUrl, fallback);
        Assert.Null(none);
        Assert.NotEmpty(warnings);
        Assert.All(warnings, warning =>
        {
            Assert.Contains("not found", warning, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("http", warning, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(DefaultUrl, warning, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Avatar_map_invalid_json_does_not_throw()
    {
        var path = Path.Combine(Path.GetTempPath(), $"forum-avatar-bad-{Guid.NewGuid():N}.json");
        var warnings = new List<string>();

        try
        {
            File.WriteAllText(path, "{not-json");
            var resolved = PersonaAvatarResolver.Resolve(
                "Weld",
                explicitAvatarUrl: null,
                DefaultUrl,
                path,
                warnings.Add);

            Assert.Equal(DefaultUrl, resolved);
            Assert.Contains(warnings, warning => warning.Contains("could not be read", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(warnings, warning => warning.Contains("http", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Delete(path);
        }
    }

    [Fact]
    public void Avatar_explicit_url_skips_map_and_does_not_warn()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"forum-avatar-skip-{Guid.NewGuid():N}.json");
        var warnings = new List<string>();

        var resolved = PersonaAvatarResolver.Resolve(
            "Weld",
            ExplicitUrl,
            DefaultUrl,
            missingPath,
            warnings.Add);

        Assert.Equal(ExplicitUrl, resolved);
        Assert.Empty(warnings);
    }

    private static string WriteMap()
    {
        var path = Path.Combine(Path.GetTempPath(), $"forum-avatar-map-{Guid.NewGuid():N}.json");
        File.WriteAllText(
            path,
            """
            [
              {"name": "Reed", "agent_id": "agent-reed", "cdn_url": "https://cdn.example.test/reed.png"},
              {"name": "Weld", "agent_id": "agent-weld", "cdn_url": "https://cdn.example.test/weld.png"}
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
