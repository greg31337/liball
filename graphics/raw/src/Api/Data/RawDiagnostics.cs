namespace raw;

/// <summary>Describes a deterministic condition encountered during an operation.</summary>
public class RawDiagnostic
{
    /// <summary>The machine-readable diagnostic identifier.</summary>
    public RawDiagnosticCode Code;
    /// <summary>The diagnostic severity.</summary>
    public RawSeverity Severity;
    /// <summary>The format being processed when the condition occurred.</summary>
    public RawFormat Format;
    /// <summary>The zero-based file offset, or <c>-1</c> when no offset applies.</summary>
    public long Offset;
    /// <summary>The stable IFD path, or an empty string when no IFD applies.</summary>
    public string IfdPath;
    /// <summary>The numeric tag identifier, or <c>-1</c> when no tag applies.</summary>
    public int Tag;
    /// <summary>The parser, codec, processing, or writer stage.</summary>
    public string Stage;
    /// <summary>A human-readable description that excludes unrelated private metadata.</summary>
    public string Message;
    /// <summary>Whether a named recovery rule allowed processing to continue.</summary>
    public bool Recovered;

    /// <summary>Creates a diagnostic with complete location and recovery context.</summary>
    /// <param name="code">The machine-readable identifier.</param>
    /// <param name="severity">The severity.</param>
    /// <param name="format">The associated format.</param>
    /// <param name="offset">The file offset, or <c>-1</c>.</param>
    /// <param name="ifdPath">The IFD path, or an empty string.</param>
    /// <param name="tag">The tag identifier, or <c>-1</c>.</param>
    /// <param name="stage">The operation stage.</param>
    /// <param name="message">The human-readable description.</param>
    /// <param name="recovered">Whether recovery was applied.</param>
    public RawDiagnostic(RawDiagnosticCode code, RawSeverity severity, RawFormat format, long offset, string? ifdPath, int tag, string? stage, string? message, bool recovered)
    {
        Code = code;
        Severity = severity;
        Format = format;
        Offset = offset;
        IfdPath = ifdPath == null ? string.Empty : ifdPath;
        Tag = tag;
        Stage = stage == null ? string.Empty : stage;
        Message = message == null ? string.Empty : message;
        Recovered = recovered;
    }

    /// <summary>Returns whether the diagnostic identifies a file offset.</summary>
    /// <returns><c>true</c> when <see cref="Offset"/> is nonnegative; otherwise <c>false</c>.</returns>
    internal bool HasOffset()
    {
        return Offset >= 0;
    }

    /// <summary>Returns whether the diagnostic identifies a TIFF tag.</summary>
    /// <returns><c>true</c> when <see cref="Tag"/> is nonnegative; otherwise <c>false</c>.</returns>
    internal bool HasTag()
    {
        return Tag >= 0;
    }
}

/// <summary>Contains the value and diagnostics produced by a public operation.</summary>
public class RawOperationResult
{
    /// <summary>The operation-specific value, or <c>null</c> when no value was produced.</summary>
    public object? Value;
    /// <summary>All diagnostics in deterministic production order.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates an operation result.</summary>
    /// <param name="value">The operation-specific value.</param>
    /// <param name="diagnostics">The diagnostics, or <c>null</c> for an empty set.</param>
    public RawOperationResult(object? value, RawDiagnostic[]? diagnostics)
    {
        Value = value;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }

    /// <summary>Returns whether the operation has no error diagnostics.</summary>
    /// <returns><c>true</c> when no diagnostic has error severity; otherwise <c>false</c>.</returns>
    internal bool Succeeded()
    {
        int index = 0;
        while (index < Diagnostics.Length)
        {
            RawDiagnostic diagnostic = Diagnostics[index];
            if (diagnostic != null && diagnostic.Severity == RawSeverity.Error)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    /// <summary>Returns whether the operation produced at least one warning.</summary>
    /// <returns><c>true</c> when a warning is present; otherwise <c>false</c>.</returns>
    internal bool HasWarnings()
    {
        int index = 0;
        while (index < Diagnostics.Length)
        {
            RawDiagnostic diagnostic = Diagnostics[index];
            if (diagnostic != null && diagnostic.Severity == RawSeverity.Warning)
            {
                return true;
            }
            index++;
        }
        return false;
    }
}
