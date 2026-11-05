namespace htmlgui;

/// <summary>Provides a host-specific transport for versioned bridge envelopes.</summary>
public interface IUiMessageTransport
{
    /// <summary>Starts the underlying message transport.</summary>
    /// <returns>The exceptionless transport result.</returns>
    UiResult StartUiMessageTransport();

    /// <summary>Sends one detached bridge envelope.</summary>
    /// <param name="message">The validated outbound message.</param>
    /// <returns>The exceptionless transport result.</returns>
    UiResult SendUiMessage(UiBridgeEnvelope message);

    /// <summary>Stops the underlying message transport.</summary>
    /// <returns>The exceptionless transport result.</returns>
    UiResult StopUiMessageTransport();

    /// <summary>Reads the transport's currently available capabilities.</summary>
    /// <returns>The detached transport capabilities.</returns>
    UiTransportCapabilities GetUiMessageTransportCapabilities();
}
