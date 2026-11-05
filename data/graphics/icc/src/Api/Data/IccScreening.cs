namespace icc;

/// <summary>Represents legacy device screening parameters.</summary>
public class IccScreening
{
    /// <summary>Gets or sets whether printer-default screens are used.</summary>
    public bool UsesPrinterDefaultScreens;
    /// <summary>Gets or sets whether frequencies are expressed in lines per inch instead of lines per centimetre.</summary>
    public bool FrequencyIsLinesPerInch;
    /// <summary>Gets or sets the ordered per-channel screening records.</summary>
    public IccScreeningChannel[] Channels;

    /// <summary>Initializes empty screening parameters.</summary>
    public IccScreening()
    {
        UsesPrinterDefaultScreens = false;
        FrequencyIsLinesPerInch = false;
        Channels = new IccScreeningChannel[0];
    }
}
