using WireMock;
using WireMock.Logging;

namespace Forum.Discord.Tests;

internal static class WireMockRequest
{
    public static string Body(ILogEntry entry)
    {
        var body = Request(entry).Body;
        Assert.False(string.IsNullOrWhiteSpace(body));
        return body;
    }

    public static string QueryValue(ILogEntry entry, string name)
    {
        var values = Request(entry).GetParameter(name, ignoreCase: true);
        Assert.NotNull(values);
        var value = Assert.Single(values);
        Assert.False(string.IsNullOrWhiteSpace(value));
        return value;
    }

    public static bool IsPostTo(ILogEntry entry, string pathFragment)
    {
        var request = entry.RequestMessage;
        return request is not null
               && string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase)
               && request.Path?.Contains(pathFragment, StringComparison.Ordinal) == true;
    }

    public static bool PathContains(ILogEntry entry, string pathFragment)
    {
        var request = entry.RequestMessage;
        return request?.Path?.Contains(pathFragment, StringComparison.Ordinal) == true;
    }

    private static IRequestMessage Request(ILogEntry entry)
    {
        Assert.NotNull(entry.RequestMessage);
        return entry.RequestMessage;
    }
}
