namespace svg;

/// <summary>Defines SVG animation timing specifier kinds.</summary>
public enum SvgTimingKind
{
    /// <summary>A simple signed clock offset.</summary>
    Offset = 0,
    /// <summary>A synchronization base such as <c>other.begin</c>.</summary>
    SyncBase = 1,
    /// <summary>An event base such as <c>button.click</c>.</summary>
    EventBase = 2,
    /// <summary>A repeat event such as <c>other.repeat(2)</c>.</summary>
    Repeat = 3,
    /// <summary>An access-key event.</summary>
    AccessKey = 4,
    /// <summary>A preserved wallclock value.</summary>
    Wallclock = 5,
    /// <summary>The <c>indefinite</c> keyword.</summary>
    Indefinite = 6
}

/// <summary>Defines stable timing microsyntax failures.</summary>
public enum SvgTimingParseError
{
    /// <summary>No parsing failure occurred.</summary>
    None = 0,
    /// <summary>An input or configured bound was invalid.</summary>
    InvalidArgument = 1,
    /// <summary>A nonempty value was required.</summary>
    ExpectedValue = 2,
    /// <summary>A clock value was malformed.</summary>
    InvalidClock = 3,
    /// <summary>A token exceeded its configured character bound.</summary>
    TokenTooLong = 4,
    /// <summary>A finite clock conversion overflowed.</summary>
    OutOfRange = 5,
    /// <summary>A timing specifier was malformed.</summary>
    InvalidSpecifier = 6,
    /// <summary>An identifier or event name was malformed.</summary>
    InvalidIdentifier = 7,
    /// <summary>A repeat specifier was malformed.</summary>
    InvalidRepeat = 8,
    /// <summary>An access-key specifier was malformed.</summary>
    InvalidAccessKey = 9,
    /// <summary>A wallclock specifier was malformed.</summary>
    InvalidWallclock = 10,
    /// <summary>A list separator was missing or trailing.</summary>
    InvalidSeparator = 11,
    /// <summary>The configured timing item count was exceeded.</summary>
    ItemLimitExceeded = 12,
    /// <summary>A key value was outside its permitted range.</summary>
    InvalidKeyValue = 13,
    /// <summary>A key-value list decreased where monotonic values were required.</summary>
    NonMonotonic = 14
}
