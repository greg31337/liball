namespace xml;

/// <summary>
/// Receives normalized XML events with backpressure control.
/// </summary>
public interface IXmlEventHandler
{
    /// <summary>
    /// Handles one normalized XML event.
    /// </summary>
    /// <param name="value">The event to handle.</param>
    /// <returns>The action requested by the handler.</returns>
    XmlHandlerAction HandleXmlEvent(XmlEvent value);
}

