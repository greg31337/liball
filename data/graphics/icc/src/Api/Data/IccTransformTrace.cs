namespace icc;

/// <summary>Records input, output, and intermediate vectors for one transform execution.</summary>
public class IccTransformTrace
{
    /// <summary>Gets or sets the source vector.</summary>
    public IccColorVector Source;
    /// <summary>Gets or sets intermediate stage vectors.</summary>
    public IccColorVector[] Intermediates;
    /// <summary>Gets or sets the destination vector.</summary>
    public IccColorVector Destination;
    /// <summary>Gets or sets textual stage notes.</summary>
    public string[] Messages;

    /// <summary>Initializes an empty trace.</summary>
    public IccTransformTrace()
    {
        Source = new IccColorVector();
        Intermediates = new IccColorVector[0];
        Destination = new IccColorVector();
        Messages = new string[0];
    }
}
