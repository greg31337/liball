using System;

namespace diameter;

/// <summary>Contains an outbound Diameter request and its delivery policy.</summary>
public class DiameterSendRequest
{
    /// <summary>The request message to send.</summary>
    public DiameterMessage Request = new DiameterMessage();

    /// <summary>The preferred peer, or <c>null</c> to select a peer by routing.</summary>
    public DiameterIdentity? PreferredPeer;

    /// <summary>The positive time allowed for a terminal request outcome.</summary>
    public int TimeoutMilliseconds = 30000;

    /// <summary>Allows an eligible request to move to an alternate peer after failure.</summary>
    public bool AllowFailover = true;

    /// <summary>Allows an eligible request to be retransmitted after delivery uncertainty.</summary>
    public bool AllowRetransmission = true;

    /// <summary>Requests notification when a correlated answer arrives after completion.</summary>
    public bool ReportLateAnswers = true;

    /// <summary>Contains caller-defined text returned with request outcomes.</summary>
    public string CorrelationText = string.Empty;

    internal void ValidateSendRequest()
    {
        if (Request == null || Request.Avps == null)
        {
            throw new ArgumentException("A Diameter request message is required.");
        }

        if (Request.Version != DiameterProtocolConstants.Version ||
            Request.CommandCode == 0 ||
            Request.CommandCode > DiameterProtocolConstants.MaximumCommandCode ||
            (Request.Flags & DiameterMessageFlags.Request) ==
                DiameterMessageFlags.None ||
            (Request.Flags & DiameterMessageFlags.Error) !=
                DiameterMessageFlags.None ||
            (Request.Flags & DiameterMessageFlags.PotentiallyRetransmitted) !=
                DiameterMessageFlags.None)
        {
            throw new ArgumentException("The outbound message must be a valid request header.");
        }

        if (PreferredPeer != null &&
            ValueCodecs.NormalizeDiameterIdentity(PreferredPeer).Status !=
                DiameterCodecStatus.Success)
        {
            throw new ArgumentException("The preferred peer identity is invalid.");
        }

        if (TimeoutMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException("TimeoutMilliseconds");
        }

        if (CorrelationText == null)
        {
            throw new ArgumentException("Correlation text must not be null.");
        }
    }
}

/// <summary>Contains the immediate outcome of accepting an outbound request.</summary>
public class DiameterSendResult
{
    /// <summary>The send admission status.</summary>
    public DiameterSendStatus Status;

    /// <summary>The accepted pending request, or <c>null</c> when sending was rejected.</summary>
    public DiameterPendingRequestSnapshot? PendingRequest;

    /// <summary>The peer selected for delivery, or <c>null</c> when none was selected.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>Indicates that the request was accepted into pending tracking.</summary>
    public bool Accepted;

    /// <summary>Indicates that the request was delivered to a peer transport.</summary>
    public bool Delivered;

    /// <summary>The stable machine-readable send outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable send outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains one terminal or late outcome for an outbound request.</summary>
public class DiameterAnswerResult
{
    /// <summary>The stable local request identifier.</summary>
    public long RequestIdentifier;

    /// <summary>The pending request state associated with the outcome.</summary>
    public DiameterPendingRequestSnapshot? PendingRequest;

    /// <summary>The received answer, or <c>null</c> for a non-answer outcome.</summary>
    public DiameterMessage? Answer;

    /// <summary>The peer from which the outcome originated, or <c>null</c> when absent.</summary>
    public DiameterIdentity? PeerIdentity;

    /// <summary>Indicates that a Diameter answer was received.</summary>
    public bool AnswerReceived;

    /// <summary>Indicates that the answer matched the pending request.</summary>
    public bool Correlated;

    /// <summary>Indicates that the answer represents application success.</summary>
    public bool Successful;

    /// <summary>Indicates that the answer carries the Diameter error bit.</summary>
    public bool ErrorAnswer;

    /// <summary>Indicates that the request deadline expired.</summary>
    public bool TimedOut;

    /// <summary>Indicates that the request was cancelled by its caller.</summary>
    public bool Cancelled;

    /// <summary>Indicates that peer or transport failure produced the outcome.</summary>
    public bool PeerFailed;

    /// <summary>Indicates that the answer arrived after the request completed.</summary>
    public bool Late;

    /// <summary>The standard Diameter Result-Code, or zero when unavailable.</summary>
    public uint ResultCode;

    /// <summary>The vendor-specific result, or <c>null</c> when unavailable.</summary>
    public DiameterExperimentalResult? ExperimentalResult;

    /// <summary>The class of the standard or experimental result code.</summary>
    public DiameterResultClass ResultClass;

    /// <summary>Indicates that protocol-error processing produced the answer.</summary>
    public bool ProtocolError;

    /// <summary>Indicates that transient or permanent application failure occurred.</summary>
    public bool ApplicationError;

    /// <summary>The stable machine-readable answer outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable answer outcome description.</summary>
    public string Message = string.Empty;
}

/// <summary>Contains an owned operational snapshot of one pending request.</summary>
public class DiameterPendingRequestSnapshot
{
    /// <summary>The stable local request identifier.</summary>
    public long RequestIdentifier;

    /// <summary>The original send request and caller policy.</summary>
    public DiameterSendRequest SendRequest = new DiameterSendRequest();

    /// <summary>The message used for the current delivery attempt.</summary>
    public DiameterMessage OutboundMessage = new DiameterMessage();

    /// <summary>The peer used for the current delivery attempt.</summary>
    public DiameterIdentity PeerIdentity = new DiameterIdentity();

    /// <summary>The Hop-by-Hop Identifier assigned to the first delivery attempt.</summary>
    public uint OriginalHopByHopIdentifier;

    /// <summary>The Hop-by-Hop Identifier assigned to the current delivery attempt.</summary>
    public uint HopByHopIdentifier;

    /// <summary>The End-to-End Identifier retained across delivery attempts.</summary>
    public uint EndToEndIdentifier;

    /// <summary>The time at which pending tracking began.</summary>
    public DateTimeOffset CreatedAt;

    /// <summary>The time of the most recent delivery attempt.</summary>
    public DateTimeOffset LastSentAt;

    /// <summary>The absolute request completion deadline.</summary>
    public DateTimeOffset DeadlineAt;

    /// <summary>The number of delivery attempts made for the request.</summary>
    public int AttemptCount;

    /// <summary>Indicates that at least one attempt reached a peer transport.</summary>
    public bool Delivered;

    /// <summary>Indicates that the request remains eligible for failover.</summary>
    public bool FailoverEligible;

    /// <summary>Indicates that the request remains eligible for retransmission.</summary>
    public bool RetransmissionEligible;
}

/// <summary>Contains duplicate-request detection and cached-answer replay metadata.</summary>
public class DiameterDuplicateResult
{
    /// <summary>The Origin-Host used by the duplicate key.</summary>
    public DiameterIdentity OriginHost = new DiameterIdentity();

    /// <summary>The End-to-End Identifier used by the duplicate key.</summary>
    public uint EndToEndIdentifier;

    /// <summary>The command code of the observed request.</summary>
    public uint CommandCode;

    /// <summary>The application identifier of the observed request.</summary>
    public uint ApplicationId;

    /// <summary>The most recently observed request.</summary>
    public DiameterMessage? Request;

    /// <summary>The cached answer available for replay, or <c>null</c>.</summary>
    public DiameterMessage? CachedAnswer;

    /// <summary>Indicates that the key was observed previously.</summary>
    public bool Duplicate;

    /// <summary>Indicates that the observed request carries the retransmitted bit.</summary>
    public bool Retransmission;

    /// <summary>Indicates that a cached answer is available.</summary>
    public bool CachedAnswerAvailable;

    /// <summary>Indicates that replaying the cached answer is safe for this request.</summary>
    public bool ReplaySafe;

    /// <summary>The number of requests observed for the duplicate key.</summary>
    public int ObservationCount;

    /// <summary>The time at which the duplicate key was first observed.</summary>
    public DateTimeOffset FirstSeenAt;

    /// <summary>The time at which the duplicate key was most recently observed.</summary>
    public DateTimeOffset LastSeenAt;

    /// <summary>The absolute expiry of the duplicate entry.</summary>
    public DateTimeOffset ExpiresAt;

    /// <summary>The stable machine-readable duplicate outcome code.</summary>
    public string Code = string.Empty;

    /// <summary>The human-readable duplicate outcome description.</summary>
    public string Message = string.Empty;
}
