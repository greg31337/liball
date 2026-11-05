namespace html;

/// <summary>Defines inspection operations for a bounded HTML serialization result.</summary>
public interface IHtmlSerializationResult
{
    /// <summary>Gets the explicit completion status.</summary>
    public HtmlSerializationStatus GetStatus();
    /// <summary>Reports whether all requested markup was written.</summary>
    public bool IsComplete();
    /// <summary>Reports whether the output-character limit stopped serialization.</summary>
    public bool WasLimitExceeded();
    /// <summary>Reports whether incomplete serialization retained a safe output prefix.</summary>
    public bool IsPartial();
    /// <summary>Reports whether this result came from a string-producing operation.</summary>
    public bool HasString();
    /// <summary>Gets the serialized string when the operation produced one.</summary>
    public string? GetString();
    /// <summary>Gets the number of characters written by this operation.</summary>
    public int GetCharactersWritten();
    /// <summary>Gets the stable code explaining incomplete serialization.</summary>
    public HtmlDiagnosticCode GetFailureCode();
}
