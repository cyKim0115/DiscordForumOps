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
        if (options.ForumChannelId == 0
            && ulong.TryParse(builder.Configuration[PollingOptions.ForumChannelIdKey], out var channelId))
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

    if (!ulong.TryParse(configuration[PollingOptions.BotUserIdKey], out var botUserId) || botUserId == 0)
    {
        return false;
    }

    if (!File.Exists(SecretsLoader.DefaultPath))
    {
        return false;
    }

    secrets = SecretsLoader.Load();
    var token = secrets.BotToken;
    readerFactory = () => DiscordRestForumReader.Connect(token, botUserId);
    return true;
}
