namespace sdp;

/// <summary>
/// Defines timestamp reference-clock and media-clock tokens.
/// </summary>
public static class SdpClockTokens
{
    /// <summary>The Network Time Protocol clock-source token.</summary>
    public const string NetworkTimeProtocol = "ntp";

    /// <summary>The Precision Time Protocol clock-source token.</summary>
    public const string PrecisionTimeProtocol = "ptp";

    /// <summary>The Global Positioning System clock-source token.</summary>
    public const string GlobalPositioningSystem = "gps";

    /// <summary>The Galileo clock-source token.</summary>
    public const string Galileo = "gal";

    /// <summary>The GLONASS clock-source token.</summary>
    public const string Glonass = "glonass";

    /// <summary>The local clock-source token.</summary>
    public const string Local = "local";

    /// <summary>The private clock-source token.</summary>
    public const string Private = "private";

    /// <summary>The traceable clock-source marker.</summary>
    public const string Traceable = "traceable";

    /// <summary>The original IEEE 1588 clock-version token.</summary>
    public const string Ieee1588_2002 = "IEEE1588-2002";

    /// <summary>The revised IEEE 1588 clock-version token.</summary>
    public const string Ieee1588_2008 = "IEEE1588-2008";

    /// <summary>The IEEE 802.1AS clock-version token.</summary>
    public const string Ieee8021As_2011 = "IEEE802.1AS-2011";

    /// <summary>The asynchronous sender media-clock token.</summary>
    public const string Sender = "sender";

    /// <summary>The direct-reference media-clock token.</summary>
    public const string Direct = "direct";

    /// <summary>The media-clock rate-modifier token.</summary>
    public const string Rate = "rate";
}
