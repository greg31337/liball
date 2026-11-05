using System;
namespace token;

/// <summary>Identifies the semantic role of a header in a token.</summary>
public enum TokenHeaderRole
{
    /// <summary>The header applies to the message as a whole.</summary>
    Message = 0,

    /// <summary>The header applies to a signature or MAC entry.</summary>
    Signature = 1,

    /// <summary>The header applies to an encryption or key-distribution recipient.</summary>
    Recipient = 2,

    /// <summary>The header applies to an SD-JWT key-binding JWT.</summary>
    KeyBinding = 3
}
