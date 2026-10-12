using Forum.Core;
using Forum.Discord;
using Forum.Worker;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<PollingOptions>()
    .Bind(builder.Configuration.GetSection(PollingOptions.SectionName))
    .PostConfigure(options =>
    {
        if (options.ForumChannelId != 0)
        {
            return;
        }

        ulong? secretsChannelId = null;
        if (SecretsLoader.TryReadOptionalId(PollingOptions.ForumChannelIdKey, out var fromSecrets))
        {
            secretsChannelId = fromSecrets;
        }

        if (SecretsLoader.TryResolveUInt64(
                builder.Configuration[PollingOptions.ForumChannelIdKey],
                secretsChannelId,
                out var channelId))
        {
            options.ForumChannelId = channelId;
        }
    })
    .Validate(
        options => options.IdleSeconds > 0 && options.ActiveSeconds > 0 && options.QuietTicksToExpand > 0,
        "Polling IdleSeconds, ActiveSeconds, and QuietTicksToExpand must be positive.")
    .ValidateOnStart();

builder.Services.AddSingleton<ICursorStore>(_ =>
{
    var directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DiscordForumOps");
    Directory.CreateDirectory(directory);
    return new FileCursorStore(Path.Combine(directory, "cursor.json"));
});

if (TryLoadDiscordReader(builder.Configuration, out var secrets, out var readerFactory))
{
    builder.Services.AddSingleton(secrets);
    builder.Services.AddSingleton<IForumReader>(_ => readerFactory());
}
else
{
    builder.Services.AddSingleton<IForumReader, EmptyForumReader>();
}

builder.Services.AddSingleton<IMentionHandler, LoggingMentionHandler>();
builder.Services.AddSingleton<MentionPollProcessor>();
builder.Services.AddSingleton(sp => AdaptivePollInterval.From(sp.GetRequiredService<IOptions<PollingOptions>>().Value));
builder.Services.AddHostedService<PollingHostedService>();

var host = builder.Build();
host.Run();

static bool TryLoadDiscordReader(
    IConfiguration configuration,
    out ForumSecrets secrets,
    out Func<IForumReader> readerFactory)
{
    secrets = null!;
    readerFactory = null!;

    if (!File.Exists(SecretsLoader.DefaultPath))
    {
        return false;
    }

    secrets = SecretsLoader.Load();
    if (!SecretsLoader.TryResolveUInt64(
            configuration[PollingOptions.BotUserIdKey],
            secrets.BotUserId,
            out var botUserId))
    {
        return false;
    }

    var token = secrets.BotToken;
    readerFactory = () => DiscordRestForumReader.Connect(token, botUserId);
    return true;
}
