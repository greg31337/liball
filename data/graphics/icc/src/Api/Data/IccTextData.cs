namespace icc;

/// <summary>Represents decoded text or XML and its lossless encoded form.</summary>
public class IccTextData
{
    /// <summary>Gets or sets the payload type signature.</summary>
    public uint TypeSignature;
    /// <summary>Gets or sets decoded text.</summary>
    public string Text;
    /// <summary>Gets or sets the invariant ASCII text from a legacy text description.</summary>
    public string AsciiText;
    /// <summary>Gets or sets the Unicode text from a legacy text description.</summary>
    public string UnicodeText;
    /// <summary>Gets or sets the Unicode language code from a legacy text description.</summary>
    public uint UnicodeLanguageCode;
    /// <summary>Gets or sets the Macintosh ScriptCode text from a legacy text description.</summary>
    public string ScriptCodeText;
    /// <summary>Gets or sets the Macintosh ScriptCode identifier from a legacy text description.</summary>
    public int ScriptCode;
    /// <summary>Gets or sets the original encoded bytes.</summary>
    public byte[] Bytes;
    /// <summary>Gets or sets whether the encoded form is compressed.</summary>
    public bool IsCompressed;
    /// <summary>Gets or sets whether the content is XML.</summary>
    public bool IsXml;

    /// <summary>Initializes empty plain text.</summary>
    public IccTextData()
    {
        TypeSignature = 0;
        Text = string.Empty;
        AsciiText = string.Empty;
        UnicodeText = string.Empty;
        UnicodeLanguageCode = 0;
        ScriptCodeText = string.Empty;
        ScriptCode = 0;
        Bytes = new byte[0];
        IsCompressed = false;
        IsXml = false;
    }
}
