using System.Collections.Immutable;
using Usm.Shared.Patterns.Scheduler.Abstractions;

namespace Usm.Shared.Patterns.Scheduler;

/// <summary>
/// Runs once after a delay.
/// </summary>
public sealed class DelaySchedule : ISchedule
{
    private readonly TimeSpan _delay;

    /// <summary>Creates a one-shot delayed schedule.</summary>
    public DelaySchedule(TimeSpan delay)
    {
        _delay = delay >= TimeSpan.Zero ? delay : throw new ArgumentOutOfRangeException(nameof(delay));
    }

    /// <inheritdoc />
    public bool IsRecurring => false;

    /// <inheritdoc />
    public DateTimeOffset? GetNextRun(DateTimeOffset utcNow) => utcNow.Add(_delay);
}

/// <summary>
/// Runs repeatedly on a fixed interval.
/// </summary>
public sealed class IntervalSchedule : ISchedule
{
    private readonly TimeSpan _interval;

    /// <summary>Creates a recurring interval schedule.</summary>
    public IntervalSchedule(TimeSpan interval)
    {
        _interval = interval > TimeSpan.Zero ? interval : throw new ArgumentOutOfRangeException(nameof(interval));
    }

    /// <inheritdoc />
    public bool IsRecurring => true;

    /// <inheritdoc />
    public DateTimeOffset? GetNextRun(DateTimeOffset utcNow) => utcNow.Add(_interval);
}

/// <summary>
/// Runs according to a cron expression.
/// </summary>
public sealed class CronSchedule : ISchedule
{
    private readonly CronField _minutes;
    private readonly CronField _hours;
    private readonly CronField _daysOfMonth;
    private readonly CronField _months;
    private readonly CronField _daysOfWeek;

    private CronSchedule(CronField minutes, CronField hours, CronField daysOfMonth, CronField months, CronField daysOfWeek)
    {
        _minutes = minutes;
        _hours = hours;
        _daysOfMonth = daysOfMonth;
        _months = months;
        _daysOfWeek = daysOfWeek;
    }

    /// <inheritdoc />
    public bool IsRecurring => true;

    /// <summary>Parses a cron expression in five-field minute/hour/day/month/day-of-week form.</summary>
    public static CronSchedule Parse(string expression)
    {
        if (!TryParse(expression, out var schedule) || schedule is null)
            throw new FormatException("Invalid cron expression.");

        return schedule;
    }

    /// <summary>Attempts to parse a cron expression.</summary>
    public static bool TryParse(string expression, out CronSchedule? schedule)
    {
        schedule = null;
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5)
            return false;

        if (!CronField.TryParse(parts[0], 0, 59, out var minutes) ||
            !CronField.TryParse(parts[1], 0, 23, out var hours) ||
            !CronField.TryParse(parts[2], 1, 31, out var dom) ||
            !CronField.TryParse(parts[3], 1, 12, out var months) ||
            !CronField.TryParse(parts[4], 0, 7, out var dow))
        {
            return false;
        }

        schedule = new CronSchedule(minutes, hours, dom, months, dow);
        return true;
    }

    /// <inheritdoc />
    public DateTimeOffset? GetNextRun(DateTimeOffset utcNow)
    {
        var candidate = utcNow.AddMinutes(1);
        candidate = new DateTimeOffset(candidate.Year, candidate.Month, candidate.Day, candidate.Hour, candidate.Minute, 0, TimeSpan.Zero);

        for (var i = 0; i < 525600; i++)
        {
            if (_months.Contains(candidate.Month) &&
                _daysOfMonth.Contains(candidate.Day) &&
                _hours.Contains(candidate.Hour) &&
                _minutes.Contains(candidate.Minute) &&
                _daysOfWeek.Contains((int)candidate.DayOfWeek))
            {
                return candidate;
            }

            candidate = candidate.AddMinutes(1);
        }

        return null;
    }

    private sealed class CronField
    {
        private readonly ImmutableHashSet<int> _values;

        private CronField(ImmutableHashSet<int> values)
        {
            _values = values;
        }

        public bool Contains(int value) => value == 7 ? _values.Contains(0) || _values.Contains(7) : _values.Contains(value);

        public static bool TryParse(string text, int min, int max, out CronField field)
        {
            var builder = ImmutableHashSet.CreateBuilder<int>();
            field = null!;

            foreach (var token in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (token == "*")
                {
                    for (var i = min; i <= max; i++)
                        builder.Add(i);

                    continue;
                }

                if (token.StartsWith("*/", StringComparison.Ordinal))
                {
                    if (!int.TryParse(token[2..], out var step) || step <= 0)
                        return false;

                    for (var i = min; i <= max; i += step)
                        builder.Add(i);

                    continue;
                }

                var range = token.Split('-', 2, StringSplitOptions.TrimEntries);
                if (range.Length == 2)
                {
                    if (!int.TryParse(range[0], out var start) || !int.TryParse(range[1], out var end) || start > end)
                        return false;

                    for (var i = start; i <= end; i++)
                        builder.Add(i);

                    continue;
                }

                if (!int.TryParse(token, out var value) || value < min || value > max)
                    return false;

                builder.Add(value);
            }

            field = new CronField(builder.ToImmutable());
            return field._values.Count > 0;
        }
    }
}
