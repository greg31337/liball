using System.Collections.Generic;

namespace image;

/// <summary>Describes a non-fatal condition encountered while extracting metadata.</summary>
public class MetadataDiagnostic
{
    /// <summary>The condition severity and extraction impact.</summary>
    public MetadataSeverity Severity;
    /// <summary>The stable machine-readable diagnostic code.</summary>
    public string Code;
    /// <summary>The plain-English description of the condition.</summary>
    public string Message;
    /// <summary>The source block, segment, chunk, or structure involved.</summary>
    public string BlockType;
    /// <summary>The zero-based source byte offset, or negative one when no location is available.</summary>
    public long Offset;
    /// <summary>The number of source bytes involved, or zero when no range is available.</summary>
    public long Length;

    /// <summary>Creates an informational diagnostic with empty text and no source location.</summary>
    public MetadataDiagnostic()
    {
        Severity = MetadataSeverity.Info;
        Code = "";
        Message = "";
        BlockType = "";
        Offset = -1;
        Length = 0;
    }

    /// <summary>Creates a diagnostic without a source location.</summary>
    /// <param name="severity">The condition severity.</param>
    /// <param name="code">The stable machine-readable code.</param>
    /// <param name="message">The plain-English description.</param>
    /// <returns>A diagnostic initialized with the supplied severity, code, and message.</returns>
    internal static MetadataDiagnostic Create(MetadataSeverity severity, string code, string message)
    {
        MetadataDiagnostic diagnostic = new MetadataDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        return diagnostic;
    }

    internal static MetadataDiagnostic Create(MetadataSeverity severity, string code, string message, string blockType, long offset, long length)
    {
        MetadataDiagnostic diagnostic = Create(severity, code, message);
        diagnostic.BlockType = blockType;
        diagnostic.Offset = offset;
        diagnostic.Length = length;
        return diagnostic;
    }

    internal static MetadataDiagnostic Add(List<MetadataDiagnostic>? diagnostics, MetadataSeverity severity, string code, string message)
    {
        MetadataDiagnostic diagnostic = Create(severity, code, message);
        AddCreatedDiagnostic(diagnostics, diagnostic);
        return diagnostic;
    }

    internal static MetadataDiagnostic AddAt(List<MetadataDiagnostic>? diagnostics, MetadataSeverity severity, string code, string message, string blockType, long offset, long length)
    {
        MetadataDiagnostic diagnostic = Create(severity, code, message, blockType, offset, length);
        AddCreatedDiagnostic(diagnostics, diagnostic);
        return diagnostic;
    }

    internal static MetadataDiagnostic AddWarning(List<MetadataDiagnostic>? diagnostics, string code, string message)
    {
        return Add(diagnostics, MetadataSeverity.Warning, code, message);
    }

    internal static MetadataDiagnostic AddWarningAt(List<MetadataDiagnostic>? diagnostics, string code, string message, string blockType, long offset, long length)
    {
        return AddAt(diagnostics, MetadataSeverity.Warning, code, message, blockType, offset, length);
    }

    internal static MetadataDiagnostic AddRecoverableError(List<MetadataDiagnostic>? diagnostics, string code, string message)
    {
        return Add(diagnostics, MetadataSeverity.RecoverableError, code, message);
    }

    internal static MetadataDiagnostic AddRecoverableErrorAt(List<MetadataDiagnostic>? diagnostics, string code, string message, string blockType, long offset, long length)
    {
        return AddAt(diagnostics, MetadataSeverity.RecoverableError, code, message, blockType, offset, length);
    }

    internal static MetadataDiagnostic AddUnsupported(List<MetadataDiagnostic>? diagnostics, string code, string message, string blockType)
    {
        return AddAt(diagnostics, MetadataSeverity.Unsupported, code, message, blockType, -1, 0);
    }

    internal static MetadataDiagnostic AddTruncated(List<MetadataDiagnostic>? diagnostics, string code, string message, string blockType, long offset, long length)
    {
        return AddAt(diagnostics, MetadataSeverity.Truncated, code, message, blockType, offset, length);
    }

    internal static MetadataDiagnostic AddLimitExceeded(List<MetadataDiagnostic>? diagnostics, string code, string message, string blockType, long offset, long length)
    {
        return AddAt(diagnostics, MetadataSeverity.LimitExceeded, code, message, blockType, offset, length);
    }

    internal static bool HasSeverity(List<MetadataDiagnostic>? diagnostics, MetadataSeverity severity)
    {
        if (diagnostics == null)
        {
            return false;
        }

        foreach (MetadataDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null && diagnostic.Severity == severity)
            {
                return true;
            }
        }

        return false;
    }

    internal static bool HasWarningsOrErrors(List<MetadataDiagnostic>? diagnostics)
    {
        if (HasSeverity(diagnostics, MetadataSeverity.Warning))
        {
            return true;
        }

        if (HasSeverity(diagnostics, MetadataSeverity.RecoverableError))
        {
            return true;
        }

        if (HasSeverity(diagnostics, MetadataSeverity.Truncated))
        {
            return true;
        }

        if (HasSeverity(diagnostics, MetadataSeverity.LimitExceeded))
        {
            return true;
        }

        return false;
    }

    internal static int CountSeverity(List<MetadataDiagnostic>? diagnostics, MetadataSeverity severity)
    {
        int count = 0;

        if (diagnostics == null)
        {
            return count;
        }

        foreach (MetadataDiagnostic diagnostic in diagnostics)
        {
            if (diagnostic != null && diagnostic.Severity == severity)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static void AddCreatedDiagnostic(List<MetadataDiagnostic>? diagnostics, MetadataDiagnostic diagnostic)
    {
        if (diagnostics == null)
        {
            return;
        }

        diagnostics.Add(diagnostic);
    }
}
