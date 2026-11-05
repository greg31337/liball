namespace ttf;

/// <summary>
/// Describes one structured finding produced while processing a font.
/// </summary>
/// <remarks>
/// A face index of <c>-1</c> identifies a package- or container-level finding.
/// A table tag of zero identifies no particular table. An absolute offset of
/// <c>-1</c> identifies a finding without a meaningful byte location. Logical
/// paths use stable parser-oriented names rather than localized display text.
/// </remarks>
public class FontDiagnostic : IFontDiagnostic
{
    FontDiagnosticSeverity severity;
    FontDiagnosticCode code;
    string message;
    int faceIndex;
    int tableTag;
    long absoluteOffset;
    string logicalPath;

    /// <summary>Creates a structured font diagnostic.</summary>
    /// <param name="severity">The effect of the finding.</param>
    /// <param name="code">The stable machine-readable diagnostic code.</param>
    /// <param name="message">The human-readable diagnostic message.</param>
    /// <param name="faceIndex">The zero-based face index, or <c>-1</c> for no face.</param>
    /// <param name="tableTag">The big-endian table tag, or zero for no table.</param>
    /// <param name="absoluteOffset">
    /// The byte offset from the start of the original container, or <c>-1</c> when
    /// no byte location applies.
    /// </param>
    /// <param name="logicalPath">The stable logical parser path, or an empty string.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <c>message</c> or <c>logicalPath</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// The severity or code is undefined, the code is <c>None</c>, the face index
    /// is less than <c>-1</c>, or the absolute offset is less than <c>-1</c>.
    /// </exception>
    public FontDiagnostic(
        FontDiagnosticSeverity severity,
        FontDiagnosticCode code,
        string message,
        int faceIndex,
        int tableTag,
        long absoluteOffset,
        string logicalPath)
    {
        if (severity < FontDiagnosticSeverity.Information ||
            severity > FontDiagnosticSeverity.Fatal)
        {
            throw new System.ArgumentOutOfRangeException("severity");
        }

        if (code == FontDiagnosticCode.None || !System.Enum.IsDefined(code))
        {
            throw new System.ArgumentOutOfRangeException("code");
        }

        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }

        if (faceIndex < -1)
        {
            throw new System.ArgumentOutOfRangeException("faceIndex");
        }

        if (absoluteOffset < -1)
        {
            throw new System.ArgumentOutOfRangeException("absoluteOffset");
        }

        if (logicalPath == null)
        {
            throw new System.ArgumentNullException("logicalPath");
        }

        this.severity = severity;
        this.code = code;
        this.message = message;
        this.faceIndex = faceIndex;
        this.tableTag = tableTag;
        this.absoluteOffset = absoluteOffset;
        this.logicalPath = logicalPath;
    }

    /// <summary>Gets the effect of the finding.</summary>
    /// <returns>The diagnostic severity.</returns>
    public FontDiagnosticSeverity GetSeverity()
    {
        return severity;
    }

    /// <summary>Gets the stable machine-readable code.</summary>
    /// <returns>The diagnostic code.</returns>
    public FontDiagnosticCode GetCode()
    {
        return code;
    }

    /// <summary>Gets the human-readable message.</summary>
    /// <returns>The diagnostic message.</returns>
    public string GetMessage()
    {
        return message;
    }

    /// <summary>Gets the zero-based face index.</summary>
    /// <returns>The face index, or <c>-1</c> when no face applies.</returns>
    public int GetFaceIndex()
    {
        return faceIndex;
    }

    /// <summary>Gets the big-endian table tag.</summary>
    /// <returns>The table tag, or zero when no table applies.</returns>
    public int GetTableTag()
    {
        return tableTag;
    }

    /// <summary>Gets the offset from the start of the original container.</summary>
    /// <returns>The absolute byte offset, or <c>-1</c> when no byte location applies.</returns>
    public long GetAbsoluteOffset()
    {
        return absoluteOffset;
    }

    /// <summary>Gets the stable parser-oriented logical path.</summary>
    /// <returns>The logical path, or an empty string when no path applies.</returns>
    public string GetLogicalPath()
    {
        return logicalPath;
    }
}
