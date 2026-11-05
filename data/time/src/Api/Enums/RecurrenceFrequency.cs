namespace time;

/// <summary>Defines the base unit by which a recurrence advances.</summary>
public enum RecurrenceFrequency
{
    /// <summary>Advances in fixed one-second calendar steps.</summary>
    Secondly = 1,
    /// <summary>Advances in fixed one-minute calendar steps.</summary>
    Minutely = 2,
    /// <summary>Advances in fixed one-hour calendar steps.</summary>
    Hourly = 3,
    /// <summary>Advances by local calendar days.</summary>
    Daily = 4,
    /// <summary>Advances by seven-day calendar weeks.</summary>
    Weekly = 5,
    /// <summary>Advances by local calendar months, constraining invalid month days.</summary>
    Monthly = 6,
    /// <summary>Advances by local calendar years, constraining invalid leap days.</summary>
    Yearly = 7
}
