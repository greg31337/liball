namespace asn1;

/// <summary>
/// Describes the category of an ASN.1 useful or time value.
/// </summary>
public class AsnTimeValue
{
    /// <summary>
    /// Contains the time-value category.
    /// </summary>
    public AsnTimeKind Kind;

    /// <summary>
    /// Contains the normalized lexical value.
    /// </summary>
    public string Text = "";

    /// <summary>
    /// Indicates whether UtcOffsetMinutes contains an explicit UTC difference.
    /// </summary>
    public bool HasUtcOffsetMinutes;

    /// <summary>
    /// Contains the signed UTC difference in minutes when HasUtcOffsetMinutes is true.
    /// </summary>
    public int UtcOffsetMinutes;

    /// <summary>
    /// Indicates whether FractionalDigits contains an explicit fractional precision.
    /// </summary>
    public bool HasFractionalDigits;

    /// <summary>
    /// Contains the non-negative fractional precision when HasFractionalDigits is true.
    /// </summary>
    public int FractionalDigits;

    internal bool IsValid()
    {
        int kindValue = (int)this.Kind;
        if (kindValue < (int)AsnTimeKind.UtcTime || kindValue > (int)AsnTimeKind.Duration)
        {
            return false;
        }

        if (this.Text == null || this.Text.Length == 0)
        {
            return false;
        }

        if (this.HasUtcOffsetMinutes
            && (this.UtcOffsetMinutes < -840 || this.UtcOffsetMinutes > 840))
        {
            return false;
        }

        if (this.HasFractionalDigits && this.FractionalDigits < 0)
        {
            return false;
        }

        if (this.Kind == AsnTimeKind.Duration && this.HasUtcOffsetMinutes)
        {
            return false;
        }

        return true;
    }
}
