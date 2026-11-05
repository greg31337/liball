namespace xml;

/// <summary>
/// Provides caller-controlled synchronous cancellation polling.
/// </summary>
public interface IXmlCancellation
{
    /// <summary>
    /// Tests whether cancellation was requested.
    /// </summary>
    /// <returns>True when processing should stop.</returns>
    bool IsXmlCancellationRequested();
}

