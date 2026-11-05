namespace tiff;

/// <summary>Defines a mutable collection of TIFF diagnostics.</summary>
public interface ITiffDiagnostics
{
    /// <summary>Returns the item count.</summary>
    /// <returns>The item count.</returns>
    int Count();
    /// <summary>Returns one diagnostic.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The diagnostic.</returns>
    TiffDiagnostic Get(int index);
    /// <summary>Clears the collection.</summary>
    void Clear();
    /// <summary>Adds one diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic.</param>
    void Add(TiffDiagnostic diagnostic);
    /// <summary>Adds an error.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The message.</param>
    /// <returns>The created diagnostic.</returns>
    TiffDiagnostic AddError(TiffErrorCode code, string message);
    /// <summary>Adds a warning.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The message.</param>
    /// <returns>The created diagnostic.</returns>
    TiffDiagnostic AddWarning(TiffErrorCode code, string message);
    /// <summary>Adds an error at a byte offset.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The message.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The created diagnostic.</returns>
    TiffDiagnostic AddErrorAt(TiffErrorCode code, string message, long offset);
    /// <summary>Adds a warning at a byte offset.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The message.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The created diagnostic.</returns>
    TiffDiagnostic AddWarningAt(TiffErrorCode code, string message, long offset);
    /// <summary>Adds an error for an image tag.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The message.</param>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="tagId">The tag identifier.</param>
    /// <returns>The created diagnostic.</returns>
    TiffDiagnostic AddErrorForTag(TiffErrorCode code, string message, int imageIndex, int tagId);
    /// <summary>Adds a warning for an image tag.</summary>
    /// <param name="code">The error code.</param>
    /// <param name="message">The message.</param>
    /// <param name="imageIndex">The image index.</param>
    /// <param name="tagId">The tag identifier.</param>
    /// <returns>The created diagnostic.</returns>
    TiffDiagnostic AddWarningForTag(TiffErrorCode code, string message, int imageIndex, int tagId);
    /// <summary>Indicates whether errors are present.</summary>
    /// <returns><c>true</c> when errors are present.</returns>
    bool HasErrors();
    /// <summary>Indicates whether warnings are present.</summary>
    /// <returns><c>true</c> when warnings are present.</returns>
    bool HasWarnings();
    /// <summary>Returns the error count.</summary>
    /// <returns>The error count.</returns>
    int ErrorCount();
    /// <summary>Returns the warning count.</summary>
    /// <returns>The warning count.</returns>
    int WarningCount();
    /// <summary>Checks for one diagnostic code.</summary>
    /// <param name="code">The error code.</param>
    /// <returns><c>true</c> when the code is present.</returns>
    bool ContainsCode(TiffErrorCode code);
    /// <summary>Formats one diagnostic for display.</summary>
    /// <param name="diagnostic">The diagnostic.</param>
    /// <returns>The formatted diagnostic.</returns>
    string Format(TiffDiagnostic diagnostic);
    /// <summary>Merges another collection.</summary>
    /// <param name="diagnostics">The diagnostics to merge.</param>
    void Merge(TiffDiagnostics diagnostics);
    /// <summary>Returns the diagnostics as an array.</summary>
    /// <returns>A detached array.</returns>
    TiffDiagnostic[] ToArray();
}
