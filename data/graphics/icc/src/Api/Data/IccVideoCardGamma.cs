namespace icc;

/// <summary>Represents decoded inspection metadata from an Apple <c>vcgt</c> payload.</summary>
public class IccVideoCardGamma
{
    /// <summary>Gets or sets whether the payload uses the formula representation.</summary>
    public bool IsFormula;
    /// <summary>Gets or sets the number of table channels.</summary>
    public int ChannelCount;
    /// <summary>Gets or sets the number of table entries per channel.</summary>
    public int EntryCount;
    /// <summary>Gets or sets the encoded byte size of each table entry.</summary>
    public int EntrySize;
    /// <summary>Gets or sets the encoded channel-major table bytes.</summary>
    public byte[] TableData;
    /// <summary>Gets or sets the red gamma, minimum, and maximum formula values.</summary>
    public double[] RedFormula;
    /// <summary>Gets or sets the green gamma, minimum, and maximum formula values.</summary>
    public double[] GreenFormula;
    /// <summary>Gets or sets the blue gamma, minimum, and maximum formula values.</summary>
    public double[] BlueFormula;

    /// <summary>Initializes empty video-card gamma metadata.</summary>
    public IccVideoCardGamma()
    {
        IsFormula = false;
        ChannelCount = 0;
        EntryCount = 0;
        EntrySize = 0;
        TableData = new byte[0];
        RedFormula = new double[0];
        GreenFormula = new double[0];
        BlueFormula = new double[0];
    }
}
