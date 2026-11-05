namespace fake;

/// <summary>Selects the shape of generated comma-separated text.</summary>
public enum CsvTextKind
{
    /// <summary>Generates one RFC 4180-shaped record without a trailing record separator.</summary>
    Row = 0,

    /// <summary>Generates an RFC 4180-shaped document with a header and data records.</summary>
    Document = 1
}
