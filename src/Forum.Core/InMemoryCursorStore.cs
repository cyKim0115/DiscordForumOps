namespace Forum.Core;

public sealed class InMemoryCursorStore : ICursorStore
{
    private readonly IDictionary<PostRef, ulong> _lastSeen;
    private readonly object _gate = new();

    public InMemoryCursorStore()
        : this(new Dictionary<PostRef, ulong>())
    {
    }

    public InMemoryCursorStore(IDictionary<PostRef, ulong> storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _lastSeen = storage;
    }

    public Task<ulong?> GetLastSeenAsync(PostRef post, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(post);
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return Task.FromResult(_lastSeen.TryGetValue(post, out var messageId) ? messageId : (ulong?)null);
        }
    }

    public Task SetLastSeenAsync(PostRef post, ulong messageId, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(post);
        ct.ThrowIfCancellationRequested();

        lock (_gate)
        {
            if (!_lastSeen.TryGetValue(post, out var current) || messageId > current)
            {
                _lastSeen[post] = messageId;
            }
        }

        return Task.CompletedTask;
    }
}
