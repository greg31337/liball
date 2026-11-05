namespace websocket;

/// <summary>
/// Holds the public state of one extension negotiation result.
/// </summary>
public class WebSocketExtensionNegotiation
{
    /// <summary>
    /// Holds the extension token.
    /// </summary>
    public string Name = "";

    /// <summary>
    /// Identifies the extension negotiation state.
    /// </summary>
    public WebSocketExtensionStatus Status;

    /// <summary>
    /// Indicates whether <see cref="AcceptedConfiguration"/> contains the accepted response configuration.
    /// </summary>
    public bool HasAcceptedConfiguration;

    /// <summary>
    /// Holds the accepted extension configuration when <see cref="HasAcceptedConfiguration"/> is true.
    /// </summary>
    public WebSocketExtensionOffer AcceptedConfiguration = new WebSocketExtensionOffer();

    /// <summary>
    /// Indicates whether the extension claims the RSV1 frame bit.
    /// </summary>
    public bool ClaimsRsv1;

    /// <summary>
    /// Indicates whether the extension claims the RSV2 frame bit.
    /// </summary>
    public bool ClaimsRsv2;

    /// <summary>
    /// Indicates whether the extension claims the RSV3 frame bit.
    /// </summary>
    public bool ClaimsRsv3;

    /// <summary>
    /// Holds extension-claimed opcodes in registration order.
    /// </summary>
    public WebSocketOpcode[] ClaimedOpcodes = [];

    /// <summary>
    /// Indicates whether <see cref="Error"/> describes invalid negotiation.
    /// </summary>
    public bool HasError;

    /// <summary>
    /// Holds structured error information when <see cref="HasError"/> is true.
    /// </summary>
    public WebSocketError Error = new WebSocketError();
}
