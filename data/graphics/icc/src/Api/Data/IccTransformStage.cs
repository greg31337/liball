namespace icc;

/// <summary>Describes one compiled transform stage.</summary>
public class IccTransformStage
{
    /// <summary>Gets or sets the stage name.</summary>
    public string Name;
    /// <summary>Gets or sets the processing-element kind.</summary>
    public IccProcessingElementKind Kind;
    /// <summary>Gets or sets the selected tag signature.</summary>
    public uint TagSignature;
    /// <summary>Gets or sets the input channel count.</summary>
    public int InputChannels;
    /// <summary>Gets or sets the output channel count.</summary>
    public int OutputChannels;
    /// <summary>Gets or sets the underlying processing element when available.</summary>
    public IccProcessingElement? Element;

    /// <summary>Initializes an unnamed unknown stage.</summary>
    public IccTransformStage()
    {
        Name = string.Empty;
        Kind = IccProcessingElementKind.Unknown;
        TagSignature = 0;
        InputChannels = 0;
        OutputChannels = 0;
        Element = null;
    }
}
