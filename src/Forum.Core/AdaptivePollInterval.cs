namespace Forum.Core;

public sealed class AdaptivePollInterval
{
    private readonly TimeSpan _active;
    private readonly TimeSpan _idle;
    private readonly int _quietTicksToExpand;
    private TimeSpan _current;
    private int _quietStreak;

    public AdaptivePollInterval(TimeSpan active, TimeSpan idle, int quietTicksToExpand)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(active, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(idle, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quietTicksToExpand);
        _active = active;
        _idle = idle;
        _quietTicksToExpand = quietTicksToExpand;
        _current = idle;
    }

    public TimeSpan Current => _current;

    public static AdaptivePollInterval From(PollingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new AdaptivePollInterval(
            TimeSpan.FromSeconds(options.ActiveSeconds),
            TimeSpan.FromSeconds(options.IdleSeconds),
            options.QuietTicksToExpand);
    }

    public TimeSpan OnTick(bool hadNewMessages)
    {
        if (hadNewMessages)
        {
            _quietStreak = 0;
            _current = _active;
            return _current;
        }

        _quietStreak++;
        if (_quietStreak >= _quietTicksToExpand)
        {
            _quietStreak = 0;
            _current = ExpandTowardIdle(_current, _idle);
        }

        return _current;
    }

    private static TimeSpan ExpandTowardIdle(TimeSpan current, TimeSpan idle)
    {
        if (current >= idle)
        {
            return idle;
        }

        var nextTicks = current.Ticks <= TimeSpan.MaxValue.Ticks / 2
            ? current.Ticks * 2
            : TimeSpan.MaxValue.Ticks;
        var doubled = TimeSpan.FromTicks(nextTicks);
        return doubled < idle ? doubled : idle;
    }
}
