namespace pop3;

/// <summary>Identifies the kind of a POP3 response line.</summary>
public enum Pop3ResponseKind
{
    /// <summary>A positive <c>+OK</c> response.</summary>
    Positive = 0,

    /// <summary>A negative <c>-ERR</c> response.</summary>
    Negative = 1,

    /// <summary>A SASL <c>+</c> continuation response.</summary>
    SaslContinuation = 2
}
