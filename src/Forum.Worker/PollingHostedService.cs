using Forum.Core;
using Microsoft.Extensions.Options;

namespace Forum.Worker;

public sealed class PollingHostedService : BackgroundService
{
    private readonly MentionPollProcessor _processor;
    private readonly AdaptivePollInterval _interval;
    private readonly PollingOptions _options;
    private readonly ILogger<PollingHostedService> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public PollingHostedService(
        MentionPollProcessor processor,
        AdaptivePollInterval interval,
        IOptions<PollingOptions> options,
        ILogger<PollingHostedService> logger)
        : this(processor, interval, options, logger, delay: null)
    {
    }

    public PollingHostedService(
        MentionPollProcessor processor,
        AdaptivePollInterval interval,
        IOptions<PollingOptions> options,
        ILogger<PollingHostedService> logger,
        Func<TimeSpan, CancellationToken, Task>? delay)
    {
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(interval);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _processor = processor;
        _interval = interval;
        _options = options.Value;
        _logger = logger;
        _delay = delay ?? Task.Delay;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Forum worker started. ChannelId configured={Configured} IdleSeconds={Idle} ActiveSeconds={Active} QuietTicksToExpand={Quiet}",
            _options.ForumChannelId != 0,
            _options.IdleSeconds,
            _options.ActiveSeconds,
            _options.QuietTicksToExpand);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
                await _delay(_interval.Current, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poll tick failed: {Message}", LogScrubber.Redact(ex.Message));
                try
                {
                    await _delay(_interval.Current, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    internal async Task<int> TickAsync(CancellationToken ct)
    {
        if (_options.ForumChannelId == 0)
        {
            _logger.LogWarning("Skipping poll tick. Set {Key} or Polling:ForumChannelId.", PollingOptions.ForumChannelIdKey);
            _interval.OnTick(hadNewMessages: false);
            return 0;
        }

        var newMessageCount = await _processor.PollOnceAsync(_options.ForumChannelId, ct).ConfigureAwait(false);
        var next = _interval.OnTick(newMessageCount > 0);
        _logger.LogInformation(
            "Poll tick newMessages={Count} nextIntervalSeconds={Seconds}",
            newMessageCount,
            next.TotalSeconds);
        return newMessageCount;
    }
}
