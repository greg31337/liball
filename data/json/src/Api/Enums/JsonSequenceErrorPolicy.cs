namespace json;

/// <summary>Selects error handling for malformed JSON text sequence records.</summary>
public enum JsonSequenceErrorPolicy
{
    /// <summary>Stops processing at the first malformed record.</summary>
    Stop = 0,

    /// <summary>Searches for the next record separator after a malformed record.</summary>
    Resynchronize = 1
}
