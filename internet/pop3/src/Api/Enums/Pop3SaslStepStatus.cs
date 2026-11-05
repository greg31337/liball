namespace pop3;

/// <summary>Identifies the outcome of one step in a SASL exchange.</summary>
public enum Pop3SaslStepStatus
{
    /// <summary>The exchange requires another challenge or response.</summary>
    Continue = 0,

    /// <summary>The exchange completed successfully.</summary>
    Success = 1,

    /// <summary>The exchange was rejected.</summary>
    Rejected = 2,

    /// <summary>The exchange was cancelled.</summary>
    Cancelled = 3
}
