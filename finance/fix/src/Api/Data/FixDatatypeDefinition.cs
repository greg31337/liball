namespace fix;

/// <summary>Defines one named FIX datatype.</summary>
public class FixDatatypeDefinition
{
    /// <summary>The datatype name.</summary>
    public string Name = "";
    /// <summary>The canonical field type used by codecs.</summary>
    public FixFieldType FieldType;
    /// <summary>The parent datatype name when this datatype is an alias.</summary>
    public string BaseTypeName = "";
    /// <summary>A human-readable description.</summary>
    public string Description = "";
    /// <summary>The source repository or overlay that supplied the definition.</summary>
    public string Source = "";
    /// <summary>Whether this overlay definition removes the matching base definition.</summary>
    public bool IsRemoved;
}
