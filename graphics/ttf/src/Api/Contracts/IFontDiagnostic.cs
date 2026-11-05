namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontDiagnostic"/>.</summary>
public interface IFontDiagnostic
{
    /// <summary>Gets the effect of the finding.</summary>
    /// <returns>The diagnostic severity.</returns>
    FontDiagnosticSeverity GetSeverity();

    /// <summary>Gets the stable machine-readable code.</summary>
    /// <returns>The diagnostic code.</returns>
    FontDiagnosticCode GetCode();

    /// <summary>Gets the human-readable message.</summary>
    /// <returns>The diagnostic message.</returns>
    string GetMessage();

    /// <summary>Gets the zero-based face index.</summary>
    /// <returns>The face index, or <c>-1</c> when no face applies.</returns>
    int GetFaceIndex();

    /// <summary>Gets the big-endian table tag.</summary>
    /// <returns>The table tag, or zero when no table applies.</returns>
    int GetTableTag();

    /// <summary>Gets the offset from the start of the original container.</summary>
    /// <returns>The absolute byte offset, or <c>-1</c> when no byte location applies.</returns>
    long GetAbsoluteOffset();

    /// <summary>Gets the stable parser-oriented logical path.</summary>
    /// <returns>The logical path, or an empty string when no path applies.</returns>
    string GetLogicalPath();
}
