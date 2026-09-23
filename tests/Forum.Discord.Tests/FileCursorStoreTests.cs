using Forum.Core;

namespace Forum.Discord.Tests;

public class FileCursorStoreTests
{
    private static readonly PostRef Post = new(ForumChannelId: 10, PostId: 20);

    [Fact]
    public async Task Cursor_advances_only_forward()
    {
        var path = NewTempPath();
        try
        {
            var store = new FileCursorStore(path);

            await store.SetLastSeenAsync(Post, 100, CancellationToken.None);
            await store.SetLastSeenAsync(Post, 50, CancellationToken.None);
            Assert.Equal(100ul, await store.GetLastSeenAsync(Post, CancellationToken.None));

            await store.SetLastSeenAsync(Post, 100, CancellationToken.None);
            Assert.Equal(100ul, await store.GetLastSeenAsync(Post, CancellationToken.None));

            await store.SetLastSeenAsync(Post, 101, CancellationToken.None);
            Assert.Equal(101ul, await store.GetLastSeenAsync(Post, CancellationToken.None));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public async Task Cursor_survives_roundtrip()
    {
        var path = NewTempPath();
        try
        {
            var writer = new FileCursorStore(path);
            await writer.SetLastSeenAsync(Post, 42, CancellationToken.None);

            var reader = new FileCursorStore(path);
            Assert.Equal(42ul, await reader.GetLastSeenAsync(Post, CancellationToken.None));
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string NewTempPath()
        => Path.Combine(Path.GetTempPath(), $"forum-cursor-{Guid.NewGuid():N}.json");

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            var temp = path + ".tmp";
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }
        }
        catch (IOException)
        {
        }
    }
}
