namespace pop3;

/// <summary>Classifies the result of authentication or maildrop acquisition.</summary>
public enum Pop3AuthenticationStatus
{
    /// <summary>Authentication and any required maildrop acquisition succeeded.</summary>
    Success = 0,

    /// <summary>The authentication attempt was rejected.</summary>
    Rejected = 1,

    /// <summary>The attempt occurred before the permitted login delay elapsed.</summary>
    LoginDelay = 2,

    /// <summary>The requested maildrop is already in use.</summary>
    InUse = 3,

    /// <summary>A temporary system failure prevented authentication.</summary>
    TemporaryFailure = 4,

    /// <summary>A permanent system failure prevented authentication.</summary>
    PermanentFailure = 5
}
