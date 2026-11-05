namespace fake;

/// <summary>Selects a conventional five-field Unix cron expression shape.</summary>
public enum CronExpressionKind
{
    /// <summary>Runs every minute.</summary>
    EveryMinute = 0,

    /// <summary>Runs at minute zero of every hour.</summary>
    Hourly = 1,

    /// <summary>Runs at midnight every day.</summary>
    Daily = 2,

    /// <summary>Runs at midnight every Sunday.</summary>
    Weekly = 3,

    /// <summary>Runs at midnight on the first day of every month.</summary>
    Monthly = 4,

    /// <summary>Runs daily at a generated minute and hour.</summary>
    SpecificTime = 5,

    /// <summary>Runs at a generated minute interval.</summary>
    MinuteInterval = 6
}
