namespace icc;

/// <summary>Represents legacy PostScript product and colour-rendering-dictionary names.</summary>
public class IccCrdInfo
{
    /// <summary>Gets or sets the PostScript product name.</summary>
    public string ProductName;
    /// <summary>Gets or sets CRD names for rendering intents zero through three.</summary>
    public string[] RenderingIntentCrdNames;

    /// <summary>Initializes empty legacy CRD information.</summary>
    public IccCrdInfo()
    {
        ProductName = string.Empty;
        RenderingIntentCrdNames = new string[4];
        int index = 0;
        while (index < RenderingIntentCrdNames.Length)
        {
            RenderingIntentCrdNames[index] = string.Empty;
            index = index + 1;
        }
    }
}
