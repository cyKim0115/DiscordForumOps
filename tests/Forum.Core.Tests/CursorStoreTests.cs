namespace Forum.Core.Tests;

public class CursorStoreTests
{
    private static readonly PostRef Post = new(forumChannelId: 10, postId: 20);

    [Fact]
    public async Task Cursor_advances_only_forward()
    {
        var store = new InMemoryCursorStore();

        await store.SetLastSeenAsync(Post, 100, CancellationToken.None);
        await store.SetLastSeenAsync(Post, 50, CancellationToken.None);
        Assert.Equal(100ul, await store.GetLastSeenAsync(Post, CancellationToken.None));

        await store.SetLastSeenAsync(Post, 100, CancellationToken.None);
        Assert.Equal(100ul, await store.GetLastSeenAsync(Post, CancellationToken.None));

        await store.SetLastSeenAsync(Post, 101, CancellationToken.None);
        Assert.Equal(101ul, await store.GetLastSeenAsync(Post, CancellationToken.None));
    }

    [Fact]
    public async Task Cursor_survives_roundtrip()
    {
        var storage = new Dictionary<PostRef, ulong>();
        var writer = new InMemoryCursorStore(storage);
        await writer.SetLastSeenAsync(Post, 42, CancellationToken.None);

        var reader = new InMemoryCursorStore(storage);
        Assert.Equal(42ul, await reader.GetLastSeenAsync(Post, CancellationToken.None));
    }
}
