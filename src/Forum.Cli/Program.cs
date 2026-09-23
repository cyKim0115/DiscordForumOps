using System.CommandLine;
using System.CommandLine.Invocation;
using Forum.Cli;
using Forum.Core;
using Forum.Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var dryRunOption = new Option<bool>("--dry-run", "Validate wiring without sending to Discord");
var threadNameOption = new Option<string>("--thread-name", () => "cli-smoke", "Forum post title");
var contentOption = new Option<string>("--content", () => "Forum.Cli smoke", "Message content");
var personaNameOption = new Option<string?>("--persona-name", "Webhook persona username override");
var postIdOption = new Option<ulong?>("--post-id", "Existing forum post id");
var forumChannelIdOption = new Option<ulong?>("--forum-channel-id", "Forum channel id override");

var whoami = new Command("whoami", "Check bot REST identity via the Forum.Discord reader token path");
whoami.SetHandler(async (InvocationContext context) =>
{
    context.ExitCode = await RunAsync(context, WhoAmIAsync);
});

var smokePost = new Command("smoke-post", "Create a forum post via IPersonaPublisher");
smokePost.AddOption(dryRunOption);
smokePost.AddOption(threadNameOption);
smokePost.AddOption(contentOption);
smokePost.AddOption(personaNameOption);
smokePost.AddOption(forumChannelIdOption);
smokePost.SetHandler(async (InvocationContext context) =>
{
    var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
    var threadName = context.ParseResult.GetValueForOption(threadNameOption) ?? "cli-smoke";
    var content = context.ParseResult.GetValueForOption(contentOption) ?? "Forum.Cli smoke";
    var personaName = context.ParseResult.GetValueForOption(personaNameOption);
    var forumChannelId = context.ParseResult.GetValueForOption(forumChannelIdOption);
    context.ExitCode = await RunAsync(
        context,
        (host, ct) => SmokePostAsync(host, dryRun, threadName, content, personaName, forumChannelId, ct));
});

var smokeReply = new Command("smoke-reply", "Reply into an existing forum post via IPersonaPublisher");
smokeReply.AddOption(dryRunOption);
smokeReply.AddOption(postIdOption);
smokeReply.AddOption(contentOption);
smokeReply.AddOption(personaNameOption);
smokeReply.AddOption(forumChannelIdOption);
smokeReply.SetHandler(async (InvocationContext context) =>
{
    var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
    var postId = context.ParseResult.GetValueForOption(postIdOption);
    var content = context.ParseResult.GetValueForOption(contentOption) ?? "Forum.Cli smoke";
    var personaName = context.ParseResult.GetValueForOption(personaNameOption);
    var forumChannelId = context.ParseResult.GetValueForOption(forumChannelIdOption);
    context.ExitCode = await RunAsync(
        context,
        (host, ct) => SmokeReplyAsync(host, dryRun, postId, content, personaName, forumChannelId, ct));
});

var pollOnce = new Command("poll-once", "Run one MentionPollProcessor tick and exit");
pollOnce.AddOption(forumChannelIdOption);
pollOnce.SetHandler(async (InvocationContext context) =>
{
    var forumChannelId = context.ParseResult.GetValueForOption(forumChannelIdOption);
    context.ExitCode = await RunAsync(context, (host, ct) => PollOnceAsync(host, forumChannelId, ct));
});

var root = new RootCommand("Forum.Cli smoke commands for DiscordForumOps");
root.AddCommand(whoami);
root.AddCommand(smokePost);
root.AddCommand(smokeReply);
root.AddCommand(pollOnce);

return await root.InvokeAsync(args);

static async Task<int> RunAsync(InvocationContext context, Func<IHost, CancellationToken, Task<int>> action)
{
    try
    {
        using var host = CliComposition.BuildHost([]);
        return await action(host, context.GetCancellationToken()).ConfigureAwait(false);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(LogScrubber.Redact(ex.Message));
        return 1;
    }
}

static async Task<int> WhoAmIAsync(IHost host, CancellationToken ct)
{
    var wiring = host.Services.GetRequiredService<CliWiring>();
    if (!wiring.ReaderAvailable)
    {
        WriteLiveUnavailable(wiring, needsBotUserId: true);
        return 1;
    }

    var reader = host.Services.GetRequiredService<DiscordRestForumReader>();
    var (id, username) = await reader.GetCurrentUserAsync(ct).ConfigureAwait(false);
    CliComposition.TryReadBotUserId(host.Services.GetRequiredService<IConfiguration>(), out var configuredId);

    Console.WriteLine($"id={id}");
    Console.WriteLine($"username={username}");
    Console.WriteLine($"configured {PollingOptions.BotUserIdKey} matches={configuredId == id}");
    return 0;
}

static async Task<int> SmokePostAsync(
    IHost host,
    bool dryRun,
    string threadName,
    string content,
    string? personaName,
    ulong? forumChannelIdOverride,
    CancellationToken ct)
{
    var persona = CliComposition.ResolvePersona(host.Services, personaName);
    var forumChannelId = CliComposition.ResolveForumChannelId(host.Services, forumChannelIdOverride);
    var wiring = host.Services.GetRequiredService<CliWiring>();

    if (dryRun)
    {
        WriteDryRun(
            "smoke-post",
            forumChannelId,
            threadName: threadName,
            postId: null,
            content: content,
            persona: persona,
            wiring: wiring);
        return 0;
    }

    if (!wiring.PublisherAvailable)
    {
        WriteLiveUnavailable(wiring, needsBotUserId: false);
        return 1;
    }

    if (forumChannelId == 0)
    {
        Console.Error.WriteLine($"Missing {PollingOptions.ForumChannelIdKey} (env, appsettings, or --forum-channel-id).");
        return 1;
    }

    var publisher = host.Services.GetRequiredService<IPersonaPublisher>();
    var id = await publisher
        .CreatePostAsync(forumChannelId, threadName, content, persona, appliedTagIds: null, ct)
        .ConfigureAwait(false);
    Console.WriteLine($"created post id={id}");
    return 0;
}

static async Task<int> SmokeReplyAsync(
    IHost host,
    bool dryRun,
    ulong? postId,
    string content,
    string? personaName,
    ulong? forumChannelIdOverride,
    CancellationToken ct)
{
    var persona = CliComposition.ResolvePersona(host.Services, personaName);
    var forumChannelId = CliComposition.ResolveForumChannelId(host.Services, forumChannelIdOverride);
    var wiring = host.Services.GetRequiredService<CliWiring>();

    if (dryRun)
    {
        WriteDryRun(
            "smoke-reply",
            forumChannelId,
            threadName: null,
            postId: postId,
            content: content,
            persona: persona,
            wiring: wiring);
        return 0;
    }

    if (!wiring.PublisherAvailable)
    {
        WriteLiveUnavailable(wiring, needsBotUserId: false);
        return 1;
    }

    if (postId is null or 0)
    {
        Console.Error.WriteLine("smoke-reply live requires --post-id.");
        return 1;
    }

    if (forumChannelId == 0)
    {
        Console.Error.WriteLine($"Missing {PollingOptions.ForumChannelIdKey} (env, appsettings, or --forum-channel-id).");
        return 1;
    }

    var publisher = host.Services.GetRequiredService<IPersonaPublisher>();
    var post = new PostRef(forumChannelId, postId.Value);
    await publisher.ReplyAsync(post, content, persona, ct).ConfigureAwait(false);
    Console.WriteLine($"replied post={post.PostId}");
    return 0;
}

static async Task<int> PollOnceAsync(IHost host, ulong? forumChannelIdOverride, CancellationToken ct)
{
    var wiring = host.Services.GetRequiredService<CliWiring>();
    if (!wiring.ReaderAvailable)
    {
        WriteLiveUnavailable(wiring, needsBotUserId: true);
        return 1;
    }

    var forumChannelId = CliComposition.ResolveForumChannelId(host.Services, forumChannelIdOverride);
    if (forumChannelId == 0)
    {
        Console.Error.WriteLine($"Missing {PollingOptions.ForumChannelIdKey} (env, appsettings, or --forum-channel-id).");
        return 1;
    }

    var processor = host.Services.GetRequiredService<MentionPollProcessor>();
    var newMessages = await processor.PollOnceAsync(forumChannelId, ct).ConfigureAwait(false);
    Console.WriteLine($"poll-once newMessages={newMessages}");
    return 0;
}

static void WriteDryRun(
    string command,
    ulong forumChannelId,
    string? threadName,
    ulong? postId,
    string content,
    Persona persona,
    CliWiring wiring)
{
    Console.WriteLine($"dry-run: {command}");
    Console.WriteLine($"forumChannelId={(forumChannelId == 0 ? "unset" : forumChannelId.ToString())}");
    if (threadName is not null)
    {
        Console.WriteLine($"threadName={threadName}");
    }

    if (postId is > 0)
    {
        Console.WriteLine($"postId={postId}");
    }
    else if (command == "smoke-reply")
    {
        Console.WriteLine("postId=unset");
    }

    Console.WriteLine($"content={LogScrubber.Redact(content)}");
    Console.WriteLine($"persona={persona.Name}");
    Console.WriteLine($"avatarConfigured={(!string.IsNullOrWhiteSpace(persona.AvatarUrl)).ToString().ToLowerInvariant()}");
    Console.WriteLine($"secretsConfigured={wiring.PublisherAvailable.ToString().ToLowerInvariant()}");
    Console.WriteLine($"readerConfigured={wiring.ReaderAvailable.ToString().ToLowerInvariant()}");
    Console.WriteLine("publisher: not invoked");
}

static void WriteLiveUnavailable(CliWiring wiring, bool needsBotUserId)
{
    if (!string.IsNullOrWhiteSpace(wiring.SecretsError))
    {
        Console.Error.WriteLine(wiring.SecretsError);
    }
    else if (!wiring.PublisherAvailable)
    {
        Console.Error.WriteLine(
            $"Live Discord requires secrets keys {SecretsLoader.BotTokenKey} and {SecretsLoader.ForumWebhookUrlKey}.");
    }

    if (needsBotUserId && !wiring.ReaderAvailable)
    {
        Console.Error.WriteLine($"Live reader also requires {PollingOptions.BotUserIdKey}.");
    }
}
