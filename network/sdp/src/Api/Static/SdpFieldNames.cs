namespace sdp;

/// <summary>
/// Defines the one-character field names used by SDP.
/// </summary>
public static class SdpFieldNames
{
    /// <summary>The protocol version field.</summary>
    public const char Version = 'v';

    /// <summary>The origin field.</summary>
    public const char Origin = 'o';

    /// <summary>The session name field.</summary>
    public const char SessionName = 's';

    /// <summary>The session or media information field.</summary>
    public const char Information = 'i';

    /// <summary>The URI field.</summary>
    public const char Uri = 'u';

    /// <summary>The email-address field.</summary>
    public const char Email = 'e';

    /// <summary>The phone-number field.</summary>
    public const char Phone = 'p';

    /// <summary>The connection-data field.</summary>
    public const char Connection = 'c';

    /// <summary>The bandwidth-information field.</summary>
    public const char Bandwidth = 'b';

    /// <summary>The timing field.</summary>
    public const char Timing = 't';

    /// <summary>The repeat-time field.</summary>
    public const char Repeat = 'r';

    /// <summary>The time-zone-adjustment field.</summary>
    public const char TimeZoneAdjustments = 'z';

    /// <summary>The obsolete encryption-key field.</summary>
    public const char EncryptionKey = 'k';

    /// <summary>The attribute field.</summary>
    public const char Attribute = 'a';

    /// <summary>The media-description field.</summary>
    public const char Media = 'm';
}
