namespace sip;

/// <summary>Holds an application SIP Identity and PASSporT verification decision.</summary>
public class SipIdentityVerificationResult
{
    /// <summary>Indicates that an application verification policy was evaluated.</summary>
    public bool PolicyEvaluated;
    /// <summary>Indicates that the message may be accepted under application policy.</summary>
    public bool Accepted;
    /// <summary>Indicates that a cryptographic identity assertion was verified.</summary>
    public bool Verified;
    /// <summary>The identity established by successful verification.</summary>
    public string? Identity;
    /// <summary>The SIP rejection status, or zero when the application selected none.</summary>
    public int StatusCode;
    /// <summary>The application decision reason.</summary>
    public string? Reason;
    /// <summary>The detached metadata supplied to the verification policy.</summary>
    public SipIdentityMetadata? Metadata;
}
