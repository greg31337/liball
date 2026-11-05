namespace tiff;

/// <summary>Represents one value in a JSON-shaped ImageDescription.</summary>
public class TiffJsonNode
{
    /// <summary>Contains the property name, or an empty string for a root or array element.</summary>
    public string Name;
    /// <summary>Identifies the JSON value kind.</summary>
    public TiffJsonValueKind Kind;
    /// <summary>Contains the decoded string value or exact number token.</summary>
    public string Text;
    /// <summary>Contains the Boolean value when <c>Kind</c> is <c>Boolean</c>.</summary>
    public bool BooleanValue;
    /// <summary>Contains the exact JSON token text for this node.</summary>
    public string RawText;
    /// <summary>Contains object properties or array elements in source order.</summary>
    public TiffJsonNode[] Children;

    /// <summary>Creates an empty JSON node.</summary>
    public TiffJsonNode()
    {
        Name = "";
        Kind = TiffJsonValueKind.None;
        Text = "";
        BooleanValue = false;
        RawText = "";
        Children = new TiffJsonNode[0];
    }

    internal TiffJsonNode Copy()
    {
        TiffJsonNode copy = new TiffJsonNode();
        copy.Name = Name;
        copy.Kind = Kind;
        copy.Text = Text;
        copy.BooleanValue = BooleanValue;
        copy.RawText = RawText;
        copy.Children = CopyNodes(Children);
        return copy;
    }

    internal static TiffJsonNode[] CopyNodes(TiffJsonNode[] nodes)
    {
        if (nodes == null)
        {
            return new TiffJsonNode[0];
        }

        TiffJsonNode[] copy = new TiffJsonNode[nodes.Length];
        int index = 0;
        while (index < nodes.Length)
        {
            copy[index] = nodes[index] == null ? new TiffJsonNode() : nodes[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
