namespace qrcode;

/// <summary>Contains one payload or control segment without discarding its raw representation.</summary>
public class QrSegment
{
    /// <summary>The segment encoding or control mode.</summary>
    public QrEncodingMode Mode;

    /// <summary>The mode-specific character count written to or read from the symbol.</summary>
    public int CharacterCount;

    /// <summary>The raw bytes represented by the segment.</summary>
    public byte[] Data = new byte[0];

    /// <summary>The decoded or caller-supplied text, or <c>null</c> when unavailable.</summary>
    public string? Text;

    /// <summary>The ECI assignment, or -1 when no ECI assignment is associated with the segment.</summary>
    public int EciAssignment = -1;

    /// <summary>The FNC1 second-position application indicator, or -1 when it does not apply.</summary>
    public int Fnc1ApplicationIndicator = -1;

    /// <summary>The structured-append metadata, or <c>null</c> for other segment modes.</summary>
    public QrStructuredAppendInfo? StructuredAppend;

    /// <summary>Whether Hanzi compatibility mode was explicitly enabled for this segment.</summary>
    public bool IsHanziCompatibilityMode;
}
