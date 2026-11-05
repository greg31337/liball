namespace uri;

/// <summary>
/// Stores an undefined, scalar, list, or ordered map URI Template value.
/// </summary>
public class UriTemplateValue
{
    /// <summary>The active value shape.</summary>
    public UriTemplateValueKind Kind;

    /// <summary>The scalar string; ignored for other value shapes.</summary>
    public string Scalar;

    /// <summary>The ordered list members; ignored for other value shapes.</summary>
    public string[] List;

    /// <summary>The ordered associative members; ignored for other value shapes.</summary>
    public UriTemplateMapEntry[] Map;

    /// <summary>Initializes an undefined value with empty scalar and composite storage.</summary>
    public UriTemplateValue()
    {
        Kind = UriTemplateValueKind.Undefined;
        Scalar = "";
        List = new string[0];
        Map = new UriTemplateMapEntry[0];
    }
}
