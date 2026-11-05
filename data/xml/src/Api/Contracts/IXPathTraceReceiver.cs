namespace xml;

/// <summary>
/// Receives XPath trace values without changing evaluation semantics.
/// </summary>
public interface IXPathTraceReceiver
{
    /// <summary>
    /// Receives one labeled trace value.
    /// </summary>
    /// <param name="label">The trace label.</param>
    /// <param name="value">The traced sequence.</param>
    /// <param name="range">The originating expression range, or null.</param>
    void ReceiveXPathTrace(string label, XdmSequence value, SourceRange? range);
}
