namespace sip;

/// <summary>Coordinates registered transports, connections, flows, and target selection.</summary>
public interface ISipTransportManager
{
    /// <summary>Registers one transport implementation.</summary>
    /// <param name="transport">The transport implementation.</param>
    void RegisterTransport(ISipTransport transport);
    /// <summary>Registers one application-provided transport content coding.</summary>
    /// <param name="contentCoding">The content-coding implementation.</param>
    /// <returns>True when the implementation was registered.</returns>
    bool RegisterTransportContentCoding(ISipTransportContentCoding contentCoding);
    /// <summary>Removes one application-provided transport content coding.</summary>
    /// <param name="contentCoding">The content-coding implementation.</param>
    /// <returns>True when the implementation was removed.</returns>
    bool RemoveTransportContentCoding(ISipTransportContentCoding contentCoding);
    /// <summary>Releases one externally managed SigComp compartment.</summary>
    /// <param name="metadata">The coding and compartment metadata.</param>
    /// <returns>True when a registered implementation received the close operation.</returns>
    bool CloseTransportContentCodingCompartment(SipContentCodingMetadata metadata);
    /// <summary>Sends using transport selection and reuse policy.</summary>
    /// <param name="request">The send request.</param>
    /// <returns>The send result.</returns>
    SipSendResult SendManaged(SipSendRequest request);
    /// <summary>Polls the next managed transport event.</summary>
    /// <returns>The next event.</returns>
    SipTransportEvent PollManagedTransportEvent();
    /// <summary>Closes all managed transports.</summary>
    void CloseManagedTransports();
}
