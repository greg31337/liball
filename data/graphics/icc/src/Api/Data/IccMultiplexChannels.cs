namespace icc;

/// <summary>Represents ICC.2 multiplex-channel names, defaults, and ordering.</summary>
public class IccMultiplexChannels
{
    /// <summary>Gets or sets channel names.</summary>
    public string[] Names;
    /// <summary>Gets or sets default channel values.</summary>
    public double[] DefaultValues;
    /// <summary>Gets or sets channel reordering indices.</summary>
    public int[] Order;

    /// <summary>Initializes an empty channel definition.</summary>
    public IccMultiplexChannels()
    {
        Names = new string[0];
        DefaultValues = new double[0];
        Order = new int[0];
    }
}
