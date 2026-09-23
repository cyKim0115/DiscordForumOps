using System.Text;
using Discord.Net.Rest;

namespace Forum.Discord.Tests;

internal sealed class RedirectRestClient : IRestClient
{
    private readonly HttpClient _http;
    private readonly Uri _baseUri;
    private readonly Dictionary<string, string?> _headers = new(StringComparer.OrdinalIgnoreCase);
    private CancellationToken _cancelToken = CancellationToken.None;
    private bool _disposed;

    public RedirectRestClient(string baseUrl)
    {
        _baseUri = new Uri(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute);
        _http = new HttpClient { BaseAddress = _baseUri };
    }

    public void SetHeader(string key, string value)
    {
        _headers[key] = value;
        _http.DefaultRequestHeaders.Remove(key);
        if (value is not null)
        {
            _http.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
        }
    }

    public void SetCancelToken(CancellationToken cancelToken) => _cancelToken = cancelToken;

    public Task<RestResponse> SendAsync(
        string method,
        string endpoint,
        CancellationToken cancelToken,
        bool headerOnly = false,
        string reason = null!,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> requestHeaders = null!)
        => SendCoreAsync(method, endpoint, content: null, cancelToken, headerOnly, reason, requestHeaders);

    public Task<RestResponse> SendAsync(
        string method,
        string endpoint,
        string json,
        CancellationToken cancelToken,
        bool headerOnly = false,
        string reason = null!,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> requestHeaders = null!)
    {
        HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
        return SendCoreAsync(method, endpoint, content, cancelToken, headerOnly, reason, requestHeaders);
    }

    public Task<RestResponse> SendAsync(
        string method,
        string endpoint,
        IReadOnlyDictionary<string, object> multipartParams,
        CancellationToken cancelToken,
        bool headerOnly = false,
        string reason = null!,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> requestHeaders = null!)
        => throw new NotSupportedException("Multipart Discord requests are not used by Forum.Discord MVP tests.");

    private async Task<RestResponse> SendCoreAsync(
        string method,
        string endpoint,
        HttpContent? content,
        CancellationToken cancelToken,
        bool headerOnly,
        string? reason,
        IEnumerable<KeyValuePair<string, IEnumerable<string>>>? requestHeaders)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cancelToken, cancelToken);
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(_baseUri, endpoint));
        if (reason is not null)
        {
            request.Headers.TryAddWithoutValidation("X-Audit-Log-Reason", Uri.EscapeDataString(reason));
        }

        if (requestHeaders is not null)
        {
            foreach (var header in requestHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        request.Content = content;
        using var response = await _http.SendAsync(request, linked.Token).ConfigureAwait(false);
        var headers = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(pair => pair.Key, pair => pair.Value.FirstOrDefault() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        Stream stream = Stream.Null;
        if (!headerOnly || !response.IsSuccessStatusCode)
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(linked.Token).ConfigureAwait(false);
            stream = new MemoryStream(bytes, writable: false);
        }

        return new RestResponse(response.StatusCode, headers, stream);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _http.Dispose();
        _disposed = true;
    }
}
