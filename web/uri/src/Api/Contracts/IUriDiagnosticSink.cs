namespace uri;

/// <summary>
/// Receives diagnostics produced while processing identifier data.
/// </summary>
public interface IUriDiagnosticSink
{
    /// <summary>
    /// Reports a diagnostic to the receiver.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to report.</param>
    void ReportDiagnostic(UriDiagnostic diagnostic);
}
