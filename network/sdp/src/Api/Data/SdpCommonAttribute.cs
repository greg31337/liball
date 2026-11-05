namespace sdp;

/// <summary>
/// Represents a common direction, textual, numeric, or obsolete SDP attribute.
/// </summary>
public class SdpCommonAttribute
{
    /// <summary>Contains the originating generic attribute when available.</summary>
    public SdpAttribute? SourceAttribute = null;

    /// <summary>Contains the canonical common attribute name.</summary>
    public string Name = string.Empty;

    /// <summary>Contains the original textual value for a value attribute.</summary>
    public string Value = string.Empty;

    /// <summary>Identifies a recognized media direction for direction attributes.</summary>
    public SdpDirection Direction = SdpDirection.Unknown;

    /// <summary>Indicates whether an integer value was projected.</summary>
    public bool HasIntegerValue = false;

    /// <summary>Contains the projected integer value.</summary>
    public int IntegerValue = 0;

    /// <summary>Indicates whether a finite numeric value was projected.</summary>
    public bool HasNumericValue = false;

    /// <summary>Contains the projected numeric value.</summary>
    public double NumericValue = 0;

    /// <summary>Indicates whether the attribute is obsolete under RFC 8866.</summary>
    public bool Obsolete = false;
}
