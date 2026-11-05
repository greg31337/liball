namespace avif;

/// <summary>Configures one increasing-resolution AV1 spatial layer.</summary>
public class Av1SpatialLayerOptions
{
    /// <summary>The positive coded width.</summary>
    public int Width;

    /// <summary>The positive coded height.</summary>
    public int Height;

    /// <summary>The layer quality from zero through one hundred, or negative one to inherit.</summary>
    public int Quality;

    /// <summary>Creates an unset layer that inherits the encoder quality.</summary>
    public Av1SpatialLayerOptions()
    {
        Width = 0;
        Height = 0;
        Quality = -1;
    }
}
