namespace icc;

/// <summary>Defines an exact ICC.2 channel-count constraint for a tag pipeline or processing element.</summary>
public class IccValidationChannelRule
{
    /// <summary>Gets or sets the enclosing tag signature, or zero to match every tag.</summary>
    public uint TagSignature;
    /// <summary>Gets or sets the processing-element signature, or zero to constrain the enclosing pipeline.</summary>
    public uint ProcessingElementSignature;
    /// <summary>Gets or sets the required input channel count, or zero for no input constraint.</summary>
    public int InputChannels;
    /// <summary>Gets or sets the required output channel count, or zero for no output constraint.</summary>
    public int OutputChannels;

    /// <summary>Initializes a rule with wildcard signatures and unconstrained channel counts.</summary>
    public IccValidationChannelRule()
    {
        TagSignature = 0;
        ProcessingElementSignature = 0;
        InputChannels = 0;
        OutputChannels = 0;
    }
}
