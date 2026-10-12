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
        var secretsLoaded = TryLoadSecrets(out var secrets, out var secretsError);

        builder.Services
            .AddOptions<PollingOptions>()
            .Bind(builder.Configuration.GetSection(PollingOptions.SectionName))
            .PostConfigure(options =>
            {
                if (options.ForumChannelId != 0)
                {
                    return;
                }

                if (TryResolveForumChannelId(builder.Configuration, secretsLoaded ? secrets : null, out var channelId))
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

        builder.Services.AddSingleton<DiscordWebhookPublisherCache>();

        if (secretsLoaded)
        {
            builder.Services.AddSingleton(secrets);
            wiring = new CliWiring
            {
                PublisherAvailable = true,
                SecretsError = null,
            };

            if (TryReadBotUserId(builder.Configuration, secrets, out var botUserId))
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
        return TryReadBotUserId(configuration, secrets: null, out botUserId);
    }

    public static bool TryReadBotUserId(IConfiguration configuration, ForumSecrets? secrets, out ulong botUserId)
    {
        if (SecretsLoader.TryResolveUInt64(
                configuration[PollingOptions.BotUserIdKey],
                secrets?.BotUserId,
                out botUserId))
        {
            return true;
        }

        return SecretsLoader.TryReadOptionalId(PollingOptions.BotUserIdKey, out botUserId);
    }

    public static bool TryResolveForumChannelId(
        IConfiguration configuration,
        ForumSecrets? secrets,
        out ulong forumChannelId)
    {
        if (SecretsLoader.TryResolveUInt64(
                configuration[PollingOptions.ForumChannelIdKey],
                secrets?.ForumChannelId,
                out forumChannelId))
        {
            return true;
        }

        return SecretsLoader.TryReadOptionalId(PollingOptions.ForumChannelIdKey, out forumChannelId);
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

    public static Persona ResolvePersona(
        IServiceProvider services,
        string? personaNameOverride,
        string? avatarUrlOverride = null)
    {
        return ResolvePublishTarget(services, personaNameOverride, avatarUrlOverride).Persona;
    }

    public static PublishTarget ResolvePublishTarget(
        IServiceProvider services,
        string? personaNameOverride,
        string? avatarUrlOverride = null)
    {
        var options = services.GetRequiredService<IOptions<PersonaOptions>>().Value;
        var configuration = services.GetRequiredService<IConfiguration>();
        var name = string.IsNullOrWhiteSpace(personaNameOverride) ? options.Name : personaNameOverride.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "ForumOpsSmoke";
        }

        var mapPath = configuration[PersonaMap.MapPathKey];
        var avatarUrl = PersonaAvatarResolver.Resolve(
            name,
            avatarUrlOverride,
            options.AvatarUrl,
            mapPath,
            static message => Console.Error.WriteLine(message));
        var personaWebhookUrl = PersonaWebhookResolver.ResolveUrl(name, mapPath);
        var webhookSource = string.IsNullOrWhiteSpace(personaWebhookUrl)
            ? PersonaWebhookResolver.SharedSource
            : PersonaWebhookResolver.PersonaSource;
        return new PublishTarget(new Persona(name, avatarUrl), webhookSource, personaWebhookUrl);
    }

    public static IPersonaPublisher ResolvePublisher(IServiceProvider services, PublishTarget target)
    {
        var url = ResolveWebhookUrl(services, target);
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException(
                $"Live Discord requires secrets key {SecretsLoader.ForumWebhookUrlKey} or a persona map forum_webhook_url.");
        }

        return services.GetRequiredService<DiscordWebhookPublisherCache>().GetOrCreate(url);
    }

    public static string? ResolveWebhookUrl(IServiceProvider services, PublishTarget target)
    {
        if (!string.IsNullOrWhiteSpace(target.PersonaWebhookUrl))
        {
            return target.PersonaWebhookUrl;
        }

        return services.GetService<ForumSecrets>()?.ForumWebhookUrl;
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

internal sealed record PublishTarget(Persona Persona, string WebhookSource, string? PersonaWebhookUrl);

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
