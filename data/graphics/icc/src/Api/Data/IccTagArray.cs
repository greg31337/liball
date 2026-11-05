namespace icc;

/// <summary>Represents an ICC.2 array of nested tag elements.</summary>
public class IccTagArray
{
    /// <summary>Gets or sets the array signature.</summary>
    public uint ArraySignature;
    /// <summary>Gets or sets ordered nested values.</summary>
    public IccTagData[] Values;

    /// <summary>Initializes an empty generic tag array.</summary>
    public IccTagArray()
    {
        ArraySignature = 0;
        Values = new IccTagData[0];
    }
}
