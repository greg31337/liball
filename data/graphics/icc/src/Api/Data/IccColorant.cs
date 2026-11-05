namespace icc;

/// <summary>Represents one named input or output colorant.</summary>
public class IccColorant
{
    /// <summary>Gets or sets the colorant name.</summary>
    public string Name;
    /// <summary>Gets or sets PCS coordinates.</summary>
    public IccColorVector PcsColor;
    /// <summary>Gets or sets optional device coordinates.</summary>
    public IccColorVector DeviceColor;

    /// <summary>Initializes an unnamed colorant.</summary>
    public IccColorant()
    {
        Name = string.Empty;
        PcsColor = new IccColorVector();
        DeviceColor = new IccColorVector();
    }
}
