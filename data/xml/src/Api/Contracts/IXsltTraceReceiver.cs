namespace xml;

/// <summary>
/// Receives XSLT execution trace events.
/// </summary>
public interface IXsltTraceReceiver
{
    /// <summary>
    /// Receives one trace event.
    /// </summary>
    /// <param name="value">The trace event data.</param>
    void ReceiveXsltTrace(XsltTraceEvent value);
}
