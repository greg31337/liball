namespace raw;

/// <summary>Reports a validated destination-path write and any recoverable staged file.</summary>
public class RawPathWriteResult
{
    /// <summary>The normalized destination path.</summary>
    public string DestinationPath;
    /// <summary>The recoverable sibling temporary path, or an empty string after commit.</summary>
    public string TemporaryPath;
    /// <summary>Whether the staged file passed byte and container validation.</summary>
    public bool Validated;
    /// <summary>Whether the staged file was atomically moved over or into the destination.</summary>
    public bool Committed;
    /// <summary>Diagnostics produced by staging and validation.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a destination-path write result.</summary>
    /// <param name="destinationPath">The normalized destination path.</param>
    /// <param name="temporaryPath">The recoverable temporary path, or an empty string.</param>
    /// <param name="validated">Whether staged validation passed.</param>
    /// <param name="committed">Whether atomic commit completed.</param>
    /// <param name="diagnostics">The diagnostics, or <c>null</c>.</param>
    public RawPathWriteResult(string destinationPath, string temporaryPath, bool validated, bool committed, RawDiagnostic[]? diagnostics)
    {
        DestinationPath = destinationPath == null ? string.Empty : destinationPath;
        TemporaryPath = temporaryPath == null ? string.Empty : temporaryPath;
        Validated = validated;
        Committed = committed;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }

    /// <summary>Returns whether a staged file remains available for recovery or inspection.</summary>
    /// <returns><c>true</c> when validation failed before commit and the sibling temporary file remains.</returns>
    internal bool HasRecoverableTemporaryFile()
    {
        return !Committed && TemporaryPath.Length != 0;
    }
}
