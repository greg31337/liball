using System;

namespace fake;

/// <summary>
/// Describes a bundled IANA time zone resolved at one instant by the runtime time-zone database.
/// </summary>
public class TimeZoneDetails
{
    /// <summary>
    /// The canonical IANA identifier from the bundled metadata.
    /// </summary>
    public string IanaId = string.Empty;

    /// <summary>
    /// The identifier exposed by the runtime mapping.
    /// </summary>
    public string RuntimeId = string.Empty;

    /// <summary>
    /// The ISO 3166 alpha-2 country codes associated with the bundled zone record.
    /// </summary>
    public string[] CountryCodes = new string[0];

    /// <summary>
    /// The compact zone1970.tab representative coordinates.
    /// </summary>
    public string Coordinates = string.Empty;

    /// <summary>
    /// The optional short zone1970.tab area comment, or null when absent.
    /// </summary>
    public string? Comment;

    /// <summary>
    /// The instant at which the runtime offset was evaluated.
    /// </summary>
    public DateTimeOffset Instant;

    /// <summary>
    /// The runtime UTC offset at the evaluated instant.
    /// </summary>
    public TimeSpan Offset;

    /// <summary>
    /// The evaluated instant represented in the selected time zone.
    /// </summary>
    public DateTimeOffset LocalTime;

    /// <summary>
    /// Whether the runtime zone contains daylight-saving adjustment rules.
    /// </summary>
    public bool SupportsDaylightSavingTime;

    internal TimeZoneDetails()
    {
        IanaId = string.Empty;
        RuntimeId = string.Empty;
        CountryCodes = new string[0];
        Coordinates = string.Empty;
        Comment = null;
        Instant = DateTimeOffset.MinValue;
        Offset = TimeSpan.Zero;
        LocalTime = DateTimeOffset.MinValue;
        SupportsDaylightSavingTime = false;
    }
}
