namespace avif;

/// <summary>Describes one detected AVIF feature and the operations available for it.</summary>
public class AvifFeatureCapability
{
    /// <summary>The detected feature.</summary>
    public AvifFeature Feature;

    /// <summary>Whether encoded image data for the feature can be decoded.</summary>
    public bool Decodable;

    /// <summary>Whether the feature can be reconstructed or rendered as pixels.</summary>
    public bool Renderable;

    /// <summary>Whether new instances of the feature can be authored.</summary>
    public bool Writable;

    /// <summary>Whether the feature can participate in a structural rewrite.</summary>
    public bool Rewritable;

    /// <summary>Whether the feature is unsupported except for opaque byte preservation.</summary>
    public bool PreservableOnly;

    /// <summary>Creates one feature capability description.</summary>
    /// <param name="feature">The detected feature.</param>
    /// <param name="decodable">Whether encoded data can be decoded.</param>
    /// <param name="renderable">Whether the feature can be rendered.</param>
    /// <param name="writable">Whether the feature can be authored.</param>
    /// <param name="rewritable">Whether the feature can be structurally rewritten.</param>
    /// <param name="preservableOnly">Whether only opaque preservation is supported.</param>
    public AvifFeatureCapability(
        AvifFeature feature,
        bool decodable,
        bool renderable,
        bool writable,
        bool rewritable,
        bool preservableOnly)
    {
        Feature = feature;
        Decodable = decodable;
        Renderable = renderable;
        Writable = writable;
        Rewritable = rewritable;
        PreservableOnly = preservableOnly;
    }
}
