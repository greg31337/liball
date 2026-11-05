namespace uri;

/// <summary>
/// Stores the generic components and delimiter-presence state of an RFC 8141 URN.
/// </summary>
public class Urn
{
    /// <summary>The scheme spelling from the input.</summary>
    public string Scheme;

    /// <summary>The namespace identifier.</summary>
    public string NamespaceIdentifier;

    /// <summary>The namespace-specific string.</summary>
    public string NamespaceSpecificString;

    /// <summary>The complete scheme, namespace identifier, and namespace-specific string.</summary>
    public string AssignedName;

    /// <summary>Whether the URN contains an r-component delimiter.</summary>
    public bool HasRComponent;

    /// <summary>The r-component text without its delimiter.</summary>
    public string RComponent;

    /// <summary>Whether the URN contains a q-component delimiter.</summary>
    public bool HasQComponent;

    /// <summary>The q-component text without its delimiter.</summary>
    public string QComponent;

    /// <summary>Whether the URN contains an f-component delimiter.</summary>
    public bool HasFComponent;

    /// <summary>The f-component text without its delimiter.</summary>
    public string FComponent;

    /// <summary>The complete original URN text.</summary>
    public string OriginalText;

    /// <summary>Initializes an empty URN model with no optional components.</summary>
    public Urn()
    {
        Scheme = "";
        NamespaceIdentifier = "";
        NamespaceSpecificString = "";
        AssignedName = "";
        HasRComponent = false;
        RComponent = "";
        HasQComponent = false;
        QComponent = "";
        HasFComponent = false;
        FComponent = "";
        OriginalText = "";
    }
}
