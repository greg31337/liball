namespace tiff;

/// <summary>Contains one ImageJ channel display range.</summary>
public class TiffImageJDisplayRange
{
    /// <summary>Contains the zero-based channel index.</summary>
    public int ChannelIndex;
    /// <summary>Contains the display minimum.</summary>
    public double Minimum;
    /// <summary>Contains the display maximum.</summary>
    public double Maximum;

    /// <summary>Creates an empty ImageJ display range.</summary>
    public TiffImageJDisplayRange()
    {
        ChannelIndex = 0;
        Minimum = 0;
        Maximum = 0;
    }

    internal static TiffImageJDisplayRange Create(int channelIndex, double minimum, double maximum)
    {
        TiffImageJDisplayRange value = new TiffImageJDisplayRange();
        value.ChannelIndex = channelIndex;
        value.Minimum = minimum;
        value.Maximum = maximum;
        return value;
    }

    internal TiffImageJDisplayRange Copy()
    {
        return TiffImageJDisplayRange.Create(ChannelIndex, Minimum, Maximum);
    }
}
