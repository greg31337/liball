namespace htmlgui;

/// <summary>Receives validated, ordered, detached inbound bridge messages.</summary>
public interface IUiBridgeMessageHandler
{
    /// <summary>Handles one inbound bridge message.</summary>
    /// <param name="message">The detached inbound envelope.</param>
    /// <returns>The exceptionless callback result; its value completes an inbound request.</returns>
    UiResult HandleUiBridgeMessage(UiBridgeEnvelope message);
}
