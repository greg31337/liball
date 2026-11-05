namespace icc;

/// <summary>Represents one node in a bounded profile inspection tree.</summary>
public class IccInspectionNode
{
    /// <summary>Gets or sets the node name.</summary>
    public string Name;
    /// <summary>Gets or sets the formatted node value.</summary>
    public string Value;
    /// <summary>Gets or sets the semantic profile path.</summary>
    public string Path;
    /// <summary>Gets or sets the source byte location.</summary>
    public IccByteLocation Location;
    /// <summary>Gets or sets retained raw bytes.</summary>
    public byte[] RawBytes;
    /// <summary>Gets or sets ordered child nodes.</summary>
    public IccInspectionNode[] Children;
    /// <summary>Gets or sets whether child output was truncated.</summary>
    public bool IsTruncated;

    /// <summary>Initializes an empty leaf node.</summary>
    public IccInspectionNode()
    {
        Name = string.Empty;
        Value = string.Empty;
        Path = string.Empty;
        Location = new IccByteLocation();
        RawBytes = new byte[0];
        Children = new IccInspectionNode[0];
        IsTruncated = false;
    }
}
