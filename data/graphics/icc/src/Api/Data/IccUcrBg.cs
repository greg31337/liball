namespace icc;

/// <summary>Represents legacy under-colour-removal and black-generation curves.</summary>
public class IccUcrBg
{
    /// <summary>Gets or sets encoded under-colour-removal curve values.</summary>
    public ushort[] UnderColorRemovalValues;
    /// <summary>Gets or sets encoded black-generation curve values.</summary>
    public ushort[] BlackGenerationValues;
    /// <summary>Gets or sets the 7-bit ASCII method description.</summary>
    public string Description;

    /// <summary>Initializes empty UCR/BG curve data.</summary>
    public IccUcrBg()
    {
        UnderColorRemovalValues = new ushort[0];
        BlackGenerationValues = new ushort[0];
        Description = string.Empty;
    }
}
