namespace tiff;

/// <summary>Contains the values and diagnostics produced by a tag default operation.</summary>
public class TiffTagDefaultResult
{
    /// <summary>Indicates whether the requested tag or default value was found.</summary>
    public bool Found;
    /// <summary>Contains the used default.</summary>
    public bool UsedDefault;
    /// <summary>Contains the value.</summary>
    public TiffTagValue Value;

    /// <summary>Creates a new instance of the TIFF Tag Default Result class.</summary>
    public TiffTagDefaultResult()
    {
        Found = false;
        UsedDefault = false;
        Value = new TiffTagValue();
    }

    /// <summary>Creates a new instance of the TIFF Tag Default Result class.</summary>
    /// <param name="found">The found.</param>
    /// <param name="usedDefault">The used default.</param>
    /// <param name="value">The value.</param>
    internal static TiffTagDefaultResult Create(bool found, bool usedDefault, TiffTagValue value)
    {
        TiffTagDefaultResult result = new TiffTagDefaultResult();
        result.Initialize(found, usedDefault, value);
        return result;
    }

    internal void Initialize(bool found, bool usedDefault, TiffTagValue value)
    {
        Found = found;
        UsedDefault = usedDefault;
        if (value == null)
        {
            Value = new TiffTagValue();
        }
        else
        {
            Value = value.Copy();
        }
    }
}
