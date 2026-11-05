namespace fake;

/// <summary>
/// Represents a standard HTTP status code and reason phrase.
/// </summary>
public class HttpStatusRecord
{
    /// <summary>The three-digit HTTP status code.</summary>
    public int Code;

    /// <summary>The conventional English reason phrase.</summary>
    public string ReasonPhrase = string.Empty;

    /// <summary>The numeric status class from one through five.</summary>
    public int StatusClass;

    /// <summary>Whether the status belongs to the client-error or server-error class.</summary>
    public bool IsError;

    /// <summary>The code followed by the conventional reason phrase.</summary>
    public string Text = string.Empty;

    internal HttpStatusRecord()
    {
    }
}
