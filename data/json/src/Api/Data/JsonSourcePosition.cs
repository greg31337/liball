namespace json;

/// <summary>Identifies one position in UTF-8 JSON input.</summary>
public class JsonSourcePosition
{
    /// <summary>The zero-based absolute UTF-8 byte offset.</summary>
    public long ByteOffset;

    /// <summary>The one-based line number when line tracking is enabled.</summary>
    public int Line;

    /// <summary>The one-based UTF-8 byte column when line tracking is enabled.</summary>
    public int ByteColumn;

    /// <summary>The one-based Unicode-scalar column when scalar tracking is enabled.</summary>
    public int ScalarColumn;
}
