namespace odf;

/// <summary>Describes one document edit session and its staged operations.</summary>
public class OdfEditSession
{
    /// <summary>The caller-supplied session identifier.</summary>
    public string Id = "";
    /// <summary>The document modified when the session is committed.</summary>
    public OdfDocument? Document;
    /// <summary>The maximum number of operations accepted by the session.</summary>
    public int MaximumOperations;
    /// <summary>An independent snapshot of the staged operations.</summary>
    public OdfOperation[] StagedOperations = [];
    /// <summary>Whether the session has been committed or discarded.</summary>
    public bool IsFinished;
    /// <summary>Whether the finished session was discarded.</summary>
    public bool IsDiscarded;
    internal EditSessionState? State;
}
