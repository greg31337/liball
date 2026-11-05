namespace tiff;

/// <summary>Represents image description metadata extracted from or prepared for a TIFF file.</summary>
public class TiffImageDescriptionMetadata
{
    /// <summary>Indicates whether the description is present.</summary>
    public bool HasDescription;
    /// <summary>Contains the text.</summary>
    public string Text;
    /// <summary>Contains the values.</summary>
    public string[] Values;
    /// <summary>Contains the original encoded bytes so the value can be preserved without semantic conversion.</summary>
    public byte[] RawBytes;
    /// <summary>Indicates whether the first non-whitespace character suggests a JSON object or array.</summary>
    public bool LooksLikeJson;
    /// <summary>Indicates whether the description contains a valid JSON object or array.</summary>
    public bool HasJson;
    /// <summary>Contains the structured JSON root when <c>HasJson</c> is true.</summary>
    public TiffJsonNode JsonRoot;
    /// <summary>Explains why JSON-shaped content could not be parsed, or is empty otherwise.</summary>
    public string JsonError;

    /// <summary>Creates a new instance of the TIFF Image Description Metadata class.</summary>
    public TiffImageDescriptionMetadata()
    {
        HasDescription = false;
        Text = "";
        Values = new string[0];
        RawBytes = new byte[0];
        LooksLikeJson = false;
        HasJson = false;
        JsonRoot = new TiffJsonNode();
        JsonError = "";
    }

    /// <summary>Creates a new instance of the TIFF Image Description Metadata class.</summary>
    /// <param name="hasDescription">The has description.</param>
    /// <param name="text">The text.</param>
    /// <param name="values">The values.</param>
    /// <param name="rawBytes">The raw bytes.</param>
    internal static TiffImageDescriptionMetadata Create(bool hasDescription, string text, string[] values, byte[] rawBytes)
    {
        TiffImageDescriptionMetadata value = new TiffImageDescriptionMetadata();
        value.Initialize(hasDescription, text, values, rawBytes);
        return value;
    }

    internal void Initialize(bool hasDescription, string text, string[] values, byte[] rawBytes)
    {
        HasDescription = hasDescription;
        if (text == null)
        {
            Text = "";
        }
        else
        {
            Text = text;
        }

        Values = TiffAscii.CopyStrings(values);
        RawBytes = TiffTagValue.CopyByteArray(rawBytes);
        TiffJsonDescriptionParseResult json = new TiffJsonDescriptionParser(Text).ParseDescription();
        LooksLikeJson = json.LooksLikeJson;
        HasJson = json.Success;
        JsonRoot = json.Root.Copy();
        JsonError = json.Error;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF image description metadata.</returns>
    internal TiffImageDescriptionMetadata Copy()
    {
        return TiffImageDescriptionMetadata.Create(HasDescription, Text, Values, RawBytes);
    }
}
