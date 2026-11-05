namespace svg;

/// <summary>Represents one located library diagnostic.</summary>
public class SvgDiagnostic
{
    /// <summary>The diagnostic severity.</summary>
    public SvgDiagnosticSeverity Severity;

    /// <summary>The stable diagnostic code.</summary>
    public SvgDiagnosticCode Code;

    /// <summary>The human-readable diagnostic message.</summary>
    public string Message;

    /// <summary>The primary source span, or <c>null</c> when unavailable.</summary>
    public SvgSourceSpan? Source;

    /// <summary>Additional related source spans in discovery order.</summary>
    public System.Collections.Generic.List<SvgSourceSpan> RelatedSpans;

    /// <summary>The stable resource path, or an empty string when unavailable.</summary>
    public string ResourcePath;

    /// <summary>The resolved instance path, or an empty string when unavailable.</summary>
    public string InstancePath;

    /// <summary>The bag-assigned sequence, or <c>-1</c> before the diagnostic is added.</summary>
    public long Sequence;

    /// <summary>Creates a diagnostic.</summary>
    /// <param name="severity">The diagnostic severity.</param>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human-readable message.</param>
    /// <param name="source">The primary source span, or <c>null</c>.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>message</c> is <c>null</c>.</exception>
    public SvgDiagnostic(SvgDiagnosticSeverity severity, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        if (message == null)
        {
            throw new System.ArgumentNullException("message");
        }

        Severity = severity;
        Code = code;
        Message = message;
        Source = source;
        RelatedSpans = new System.Collections.Generic.List<SvgSourceSpan>();
        ResourcePath = source == null ? "" : source.Source.ResourcePath;
        InstancePath = "";
        Sequence = -1;
    }

    /// <summary>Adds a related source span.</summary>
    /// <param name="span">The related span.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>span</c> is <c>null</c>.</exception>
    internal void AddRelatedSpan(SvgSourceSpan span)
    {
        if (span == null)
        {
            throw new System.ArgumentNullException("span");
        }
        RelatedSpans.Add(span);
    }
}
