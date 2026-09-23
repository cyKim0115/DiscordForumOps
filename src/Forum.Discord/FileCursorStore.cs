using System.Collections.Concurrent;
using System.Text.Json;
using Forum.Core;

namespace Forum.Discord;

public sealed class FileCursorStore : ICursorStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _filePath;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public FileCursorStore(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public async Task<ulong?> GetLastSeenAsync(PostRef post, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(post);
        ct.ThrowIfCancellationRequested();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var store = await LoadAsync(ct).ConfigureAwait(false);
            return store.TryGetValue(Key(post), out var messageId) ? messageId : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetLastSeenAsync(PostRef post, ulong messageId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(post);
        ct.ThrowIfCancellationRequested();

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var store = await LoadAsync(ct).ConfigureAwait(false);
            var key = Key(post);
            if (!store.TryGetValue(key, out var current) || messageId > current)
            {
                store[key] = messageId;
                await SaveAsync(store, ct).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ConcurrentDictionary<string, ulong>> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
        {
            return new ConcurrentDictionary<string, ulong>(StringComparer.Ordinal);
        }

        await using var stream = File.OpenRead(_filePath);
        var loaded = await JsonSerializer
            .DeserializeAsync<Dictionary<string, ulong>>(stream, JsonOptions, ct)
            .ConfigureAwait(false);
        return new ConcurrentDictionary<string, ulong>(
            loaded ?? new Dictionary<string, ulong>(),
            StringComparer.Ordinal);
    }

    private async Task SaveAsync(IDictionary<string, ulong> store, CancellationToken ct)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = _filePath + ".tmp";
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, store, JsonOptions, ct).ConfigureAwait(false);
        }

        File.Move(tempPath, _filePath, overwrite: true);
    }

    private static string Key(PostRef post) => $"{post.ForumChannelId}:{post.PostId}";
}
