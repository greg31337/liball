namespace icc;

/// <summary>Represents an ordered ICC multi-process-element pipeline.</summary>
public class IccMultiProcessElements
{
    /// <summary>Gets or sets the input channel count.</summary>
    public int InputChannels;
    /// <summary>Gets or sets the output channel count.</summary>
    public int OutputChannels;
    /// <summary>Gets or sets input channel ranges.</summary>
    public IccEncodingRange[] InputRanges;
    /// <summary>Gets or sets output channel ranges.</summary>
    public IccEncodingRange[] OutputRanges;
    /// <summary>Gets or sets the ordered processing elements.</summary>
    public IccProcessingElement[] Elements;
    /// <summary>Gets or sets whether every processing element has executable semantics.</summary>
    public bool IsExecutable;

    /// <summary>Initializes an empty pipeline.</summary>
    public IccMultiProcessElements()
    {
        InputChannels = 0;
        OutputChannels = 0;
        InputRanges = new IccEncodingRange[0];
        OutputRanges = new IccEncodingRange[0];
        Elements = new IccProcessingElement[0];
        IsExecutable = false;
    }
}
