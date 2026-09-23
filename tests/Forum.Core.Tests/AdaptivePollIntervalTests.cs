namespace Forum.Core.Tests;

public class AdaptivePollIntervalTests
{
    [Fact]
    public void Interval_starts_at_idle()
    {
        var interval = Create(activeSeconds: 10, idleSeconds: 60, quietTicksToExpand: 1);

        Assert.Equal(TimeSpan.FromSeconds(60), interval.Current);
    }

    [Fact]
    public void Interval_switches_to_active_on_new_message()
    {
        var interval = Create(activeSeconds: 10, idleSeconds: 60, quietTicksToExpand: 1);

        var next = interval.OnTick(hadNewMessages: true);

        Assert.Equal(TimeSpan.FromSeconds(10), next);
        Assert.Equal(TimeSpan.FromSeconds(10), interval.Current);
    }

    [Fact]
    public void Interval_expands_toward_idle_after_quiet_ticks()
    {
        var interval = Create(activeSeconds: 10, idleSeconds: 60, quietTicksToExpand: 1);
        interval.OnTick(hadNewMessages: true);

        Assert.Equal(TimeSpan.FromSeconds(20), interval.OnTick(hadNewMessages: false));
        Assert.Equal(TimeSpan.FromSeconds(40), interval.OnTick(hadNewMessages: false));
        Assert.Equal(TimeSpan.FromSeconds(60), interval.OnTick(hadNewMessages: false));
        Assert.Equal(TimeSpan.FromSeconds(60), interval.OnTick(hadNewMessages: false));
    }

    [Fact]
    public void Interval_waits_n_quiet_ticks_before_expanding()
    {
        var interval = Create(activeSeconds: 10, idleSeconds: 60, quietTicksToExpand: 2);
        interval.OnTick(hadNewMessages: true);

        Assert.Equal(TimeSpan.FromSeconds(10), interval.OnTick(hadNewMessages: false));
        Assert.Equal(TimeSpan.FromSeconds(20), interval.OnTick(hadNewMessages: false));
        Assert.Equal(TimeSpan.FromSeconds(20), interval.OnTick(hadNewMessages: false));
        Assert.Equal(TimeSpan.FromSeconds(40), interval.OnTick(hadNewMessages: false));
    }

    [Fact]
    public void Interval_resets_to_active_after_quiet_stretch()
    {
        var interval = Create(activeSeconds: 10, idleSeconds: 60, quietTicksToExpand: 1);
        interval.OnTick(hadNewMessages: true);
        interval.OnTick(hadNewMessages: false);
        interval.OnTick(hadNewMessages: false);

        var next = interval.OnTick(hadNewMessages: true);

        Assert.Equal(TimeSpan.FromSeconds(10), next);
    }

    [Fact]
    public void From_options_uses_configured_seconds()
    {
        var interval = AdaptivePollInterval.From(new PollingOptions
        {
            ActiveSeconds = 8,
            IdleSeconds = 32,
            QuietTicksToExpand = 1,
        });

        Assert.Equal(TimeSpan.FromSeconds(32), interval.Current);
        Assert.Equal(TimeSpan.FromSeconds(8), interval.OnTick(hadNewMessages: true));
        Assert.Equal(TimeSpan.FromSeconds(16), interval.OnTick(hadNewMessages: false));
        Assert.Equal(TimeSpan.FromSeconds(32), interval.OnTick(hadNewMessages: false));
    }

    private static AdaptivePollInterval Create(int activeSeconds, int idleSeconds, int quietTicksToExpand)
    {
        return new AdaptivePollInterval(
            TimeSpan.FromSeconds(activeSeconds),
            TimeSpan.FromSeconds(idleSeconds),
            quietTicksToExpand);
    }
}
