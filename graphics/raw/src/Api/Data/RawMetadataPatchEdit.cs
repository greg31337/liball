namespace raw;

/// <summary>Requests replacement of one existing TIFF tag with an exact same-type, same-count value.</summary>
public class RawMetadataPatchEdit
{
    /// <summary>The exact stable directory path containing the tag.</summary>
    public string IfdPath;
    /// <summary>The existing TIFF tag identifier.</summary>
    public ushort TagId;
    /// <summary>The exact replacement type, count, byte order, and encoded bytes.</summary>
    public RawTagValue Value;

    /// <summary>Creates one fixed-size TIFF metadata replacement.</summary>
    /// <param name="ifdPath">The exact stable directory path.</param>
    /// <param name="tagId">The existing tag identifier.</param>
    /// <param name="value">The exact replacement value.</param>
    public RawMetadataPatchEdit(string ifdPath, ushort tagId, RawTagValue value)
    {
        IfdPath = ifdPath == null ? string.Empty : ifdPath;
        TagId = tagId;
        Value = value;
    }
}
