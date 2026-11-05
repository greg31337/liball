namespace fix;

/// <summary>Describes the encoding and template metadata at a binary codec boundary.</summary>
public class BinaryMessageContext
{
    /// <summary>The logical encoding family.</summary>
    public BinaryEncodingType EncodingType;
    /// <summary>The SOFH encoding identifier.</summary>
    public ushort EncodingIdentifier;
    /// <summary>The binary template identifier.</summary>
    public int TemplateId;
    /// <summary>The binary schema identifier.</summary>
    public int SchemaId;
    /// <summary>The binary schema version.</summary>
    public int Version;
    /// <summary>The logical FIX MsgType when known.</summary>
    public string MsgType = "";
    /// <summary>The dictionary identity selected for mapping.</summary>
    public FixDictionaryIdentity DictionaryIdentity = new FixDictionaryIdentity();
}
