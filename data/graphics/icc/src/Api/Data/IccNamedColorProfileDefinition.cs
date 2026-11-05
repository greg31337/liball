namespace icc;

/// <summary>Defines named colours and optional device-space metadata for a NamedColor profile.</summary>
public class IccNamedColorProfileDefinition
{
    /// <summary>Gets or sets the named-colour records, prefix, suffix, and optional device coordinates.</summary>
    public IccNamedColorSet NamedColors;
    /// <summary>Gets or sets an optional colorant table for the profile data space.</summary>
    public IccColorantSet? Colorants;
    /// <summary>Gets or sets whether version-2 legacy tint coordinates are emitted in addition to namedColor2 data.</summary>
    public bool IncludeLegacyTints;

    /// <summary>Initializes an empty named-colour profile definition.</summary>
    public IccNamedColorProfileDefinition()
    {
        NamedColors = new IccNamedColorSet();
        Colorants = null;
        IncludeLegacyTints = false;
    }
}
