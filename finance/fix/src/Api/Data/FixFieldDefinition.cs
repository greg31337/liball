namespace fix;

/// <summary>Defines one FIX field in a canonical dictionary.</summary>
public class FixFieldDefinition
{
    /// <summary>The numeric FIX tag.</summary>
    public int Tag;
    /// <summary>The canonical field name.</summary>
    public string Name = "";
    /// <summary>The abbreviated field name used by compact encodings.</summary>
    public string Abbreviation = "";
    /// <summary>The referenced named datatype.</summary>
    public string DatatypeName = "";
    /// <summary>The resolved canonical field type.</summary>
    public FixFieldType FieldType;
    /// <summary>The referenced code-set name, or an empty string when unrestricted.</summary>
    public string CodeSetName = "";
    /// <summary>Whether another field selects this field's value domain.</summary>
    public bool HasDiscriminatorTag;
    /// <summary>The field tag that selects this field's value domain.</summary>
    public int DiscriminatorTag;
    /// <summary>Whether this data field has an associated length tag.</summary>
    public bool HasLengthTag;
    /// <summary>The associated length tag.</summary>
    public int LengthTag;
    /// <summary>Whether this length field has an associated data tag.</summary>
    public bool HasDataTag;
    /// <summary>The associated data tag.</summary>
    public int DataTag;
    /// <summary>A human-readable description.</summary>
    public string Description = "";
    /// <summary>The source repository or overlay that supplied the definition.</summary>
    public string Source = "";
    /// <summary>Whether this overlay definition removes the matching base definition.</summary>
    public bool IsRemoved;
}
