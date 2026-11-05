namespace avif;

/// <summary>Configures one authored AV1 operating point.</summary>
public class Av1OperatingPointOptions
{
    /// <summary>The twelve-bit temporal and spatial layer inclusion mask.</summary>
    public int Idc;

    /// <summary>The AV1 sequence level index from zero through thirty-one.</summary>
    public int Level;

    /// <summary>The sequence tier, zero for main or one for high.</summary>
    public int Tier;

    /// <summary>Creates a main-tier operating point with automatic level selection.</summary>
    public Av1OperatingPointOptions()
    {
        Idc = 0;
        Level = -1;
        Tier = 0;
    }
}
