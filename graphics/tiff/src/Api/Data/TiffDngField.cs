namespace tiff;

/// <summary>Contains one decoded DNG field together with its original encoded bytes.</summary>
public class TiffDngField
{
    /// <summary>Identifies the DNG TIFF tag.</summary>
    public int TagId;
    /// <summary>Contains the standard DNG field name.</summary>
    public string Name;
    /// <summary>Contains the decoded TIFF value.</summary>
    public TiffTagValue Value;
    /// <summary>Contains the exact encoded field bytes.</summary>
    public byte[] EncodedBytes;

    /// <summary>Creates an empty DNG field.</summary>
    public TiffDngField()
    {
        TagId = 0;
        Name = "";
        Value = new TiffTagValue();
        EncodedBytes = new byte[0];
    }

    internal static TiffDngField Create(int tagId, string name, TiffTagValue value, byte[] encodedBytes)
    {
        TiffDngField field = new TiffDngField();
        field.TagId = tagId;
        if (name != null)
        {
            field.Name = name;
        }

        if (value != null)
        {
            field.Value = value.Copy();
        }

        field.EncodedBytes = TiffTagValue.CopyByteArray(encodedBytes);
        return field;
    }

    internal TiffDngField Copy()
    {
        return TiffDngField.Create(TagId, Name, Value, EncodedBytes);
    }
}
