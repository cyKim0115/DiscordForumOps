using Forum.Core;
using Forum.Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Forum.Cli;

internal sealed class PersonaOptions
{
    public const string SectionName = "Persona";

    public string Name { get; set; } = "ForumOpsSmoke";

    public string? AvatarUrl { get; set; }
}

internal sealed class CliWiring
{
    public bool ReaderAvailable { get; init; }

    public bool PublisherAvailable { get; init; }

    public string? SecretsError { get; init; }
}

internal static class CliComposition
{
    public static IHost BuildHost(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var wiring = new CliWiring();

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
            });

        builder.Services
            .AddOptions<PersonaOptions>()
            .Bind(builder.Configuration.GetSection(PersonaOptions.SectionName));

        builder.Services.AddSingleton<ICursorStore>(_ =>
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DiscordForumOps");
            Directory.CreateDirectory(directory);
            return new FileCursorStore(Path.Combine(directory, "cursor.json"));
        });

        if (TryLoadSecrets(out var secrets, out var secretsError))
        {
            builder.Services.AddSingleton(secrets);
            builder.Services.AddSingleton<IPersonaPublisher>(_ => new DiscordWebhookPublisher(secrets.ForumWebhookUrl));
            wiring = new CliWiring
            {
                PublisherAvailable = true,
                SecretsError = null,
            };

            if (TryReadBotUserId(builder.Configuration, out var botUserId))
            {
                var token = secrets.BotToken;
                builder.Services.AddSingleton(_ => DiscordRestForumReader.Connect(token, botUserId));
                builder.Services.AddSingleton<IForumReader>(sp => sp.GetRequiredService<DiscordRestForumReader>());
                wiring = new CliWiring
                {
                    ReaderAvailable = true,
                    PublisherAvailable = true,
                };
            }
        }
        else
        {
            wiring = new CliWiring { SecretsError = secretsError };
        }

        builder.Services.AddSingleton(wiring);
        builder.Services.AddSingleton<IMentionHandler, ConsoleMentionHandler>();
        builder.Services.AddSingleton<MentionPollProcessor>();

        return builder.Build();
    }

    public static bool TryReadBotUserId(IConfiguration configuration, out ulong botUserId)
    {
        return ulong.TryParse(configuration[PollingOptions.BotUserIdKey], out botUserId) && botUserId != 0;
    }

    public static bool TryLoadSecrets(out ForumSecrets secrets, out string? error)
    {
        secrets = null!;
        error = null;

        if (!File.Exists(SecretsLoader.DefaultPath))
        {
            error =
                $"Secrets file was not found. Expected keys: {SecretsLoader.BotTokenKey}, {SecretsLoader.ForumWebhookUrlKey}.";
            return false;
        }

        try
        {
            secrets = SecretsLoader.Load();
            return true;
        }
        catch (Exception ex)
        {
            error = LogScrubber.Redact(ex.Message);
            return false;
        }
    }

    public static Persona ResolvePersona(IServiceProvider services, string? personaNameOverride)
    {
        var options = services.GetRequiredService<IOptions<PersonaOptions>>().Value;
        var name = string.IsNullOrWhiteSpace(personaNameOverride) ? options.Name : personaNameOverride.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "ForumOpsSmoke";
        }

        return new Persona(name, options.AvatarUrl);
    }

    public static ulong ResolveForumChannelId(IServiceProvider services, ulong? overrideId)
    {
        if (overrideId is > 0)
        {
            return overrideId.Value;
        }

        return services.GetRequiredService<IOptions<PollingOptions>>().Value.ForumChannelId;
    }
}

internal sealed class ConsoleMentionHandler : IMentionHandler
{
    public Task HandleAsync(ForumMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ct.ThrowIfCancellationRequested();
        Console.WriteLine(
            $"mention post={message.Post.PostId} message={message.Ref.MessageId} author={message.AuthorId} content={LogScrubber.Redact(message.Content)}");
        return Task.CompletedTask;
    }
}
