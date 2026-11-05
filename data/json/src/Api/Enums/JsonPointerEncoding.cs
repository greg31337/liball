namespace json;

/// <summary>Identifies an RFC 6901 JSON Pointer representation.</summary>
public enum JsonPointerEncoding
{
    /// <summary>Uses the JSON Pointer string representation.</summary>
    String = 0,

    /// <summary>Uses the URI fragment identifier representation.</summary>
    UriFragment = 1
}
