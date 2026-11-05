using System;
using System.Collections.Generic;

namespace diameter;

/// <summary>Sends, cancels, and inspects tracked Diameter requests.</summary>
public interface IDiameterRequestClient
{
    /// <summary>Attempts to send and track one Diameter request.</summary>
    /// <param name="request">The outbound request and delivery policy.</param>
    /// <param name="handler">The handler for terminal and requested late outcomes.</param>
    /// <returns>The immediate send-admission outcome.</returns>
    DiameterSendResult SendDiameterRequest(
        DiameterSendRequest? request,
        IDiameterAnswerHandler? handler);

    /// <summary>Cancels one pending Diameter request by its stable local identifier.</summary>
    /// <param name="requestIdentifier">The positive local request identifier.</param>
    /// <returns>The cancellation outcome.</returns>
    DiameterAnswerResult CancelDiameterRequest(long requestIdentifier);

    /// <summary>Reads independent snapshots of all pending requests.</summary>
    /// <returns>The pending request snapshots in creation order.</returns>
    List<DiameterPendingRequestSnapshot> ReadPendingDiameterRequests();
}

/// <summary>Receives terminal and requested late outcomes for outbound requests.</summary>
public interface IDiameterAnswerHandler
{
    /// <summary>Handles one owned answer, timeout, cancellation, or failure outcome.</summary>
    /// <param name="result">The request outcome to handle.</param>
    void HandleDiameterAnswer(DiameterAnswerResult result);
}

/// <summary>Detects duplicate requests and stores answers that are safe to replay.</summary>
public interface IDuplicateRequestStore
{
    /// <summary>Atomically observes a request and returns its duplicate metadata.</summary>
    /// <param name="request">The inbound Diameter request.</param>
    /// <param name="originHost">The normalized Origin-Host from the request.</param>
    /// <param name="receivedAt">The time at which the request was received.</param>
    /// <returns>The first-seen or duplicate outcome.</returns>
    DiameterDuplicateResult ObserveDiameterDuplicateRequest(
        DiameterMessage? request,
        DiameterIdentity? originHost,
        DateTimeOffset receivedAt);

    /// <summary>Caches an answer and its replay policy for an observed request.</summary>
    /// <param name="result">The observed request metadata and answer to cache.</param>
    void CacheDiameterDuplicateAnswer(DiameterDuplicateResult? result);
}
