namespace uspto;

/// <summary>
/// Reports a local dataset mirror planning, synchronization, verification, or repair failure.
/// </summary>
public class MirrorException : global::System.Exception
{
    /// <summary>The dataset family being mirrored.</summary>
    public DatasetFamily Family;

    /// <summary>The catalog product identifier, when known.</summary>
    public string? ProductIdentifier;

    /// <summary>The catalog or local file name, when known.</summary>
    public string? FileName;

    /// <summary>The planned mirror action at the time of failure.</summary>
    public MirrorAction Action;

    /// <summary>Initializes a mirror exception and its product context.</summary>
    /// <param name="message">The mirror failure message.</param>
    /// <param name="family">The dataset family being mirrored.</param>
    /// <param name="productIdentifier">The catalog product identifier.</param>
    /// <param name="fileName">The catalog or local file name.</param>
    /// <param name="action">The planned mirror action at failure.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public MirrorException(string message, DatasetFamily family, string? productIdentifier, string? fileName, MirrorAction action, global::System.Exception? innerException)
        : base(message, innerException)
    {
        Family = family;
        ProductIdentifier = productIdentifier;
        FileName = fileName;
        Action = action;
    }
}
