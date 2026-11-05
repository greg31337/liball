using System;
namespace token;

/// <summary>Identifies the integrity-protection bucket containing a header.</summary>
public enum TokenHeaderProtection
{
    /// <summary>The native header combines protected and unprotected buckets.</summary>
    Combined = 0,

    /// <summary>The header is integrity protected by the token operation.</summary>
    Protected = 1,

    /// <summary>The header is not integrity protected by the token operation.</summary>
    Unprotected = 2
}
