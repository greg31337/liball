using System;
namespace token;

/// <summary>Contains one unauthenticated header discovered during safe inspection.</summary>
public class TokenHeaderInspection
{
    /// <summary>Identifies how the header is used by the token.</summary>
    public TokenHeaderRole Role;

    /// <summary>Identifies the header protection bucket.</summary>
    public TokenHeaderProtection Protection;

    /// <summary>Contains a JOSE header, or <c>null</c> for a COSE header.</summary>
    public JoseHeader? JoseHeader;

    /// <summary>Contains a COSE header, or <c>null</c> for a JOSE header.</summary>
    public CoseHeader? CoseHeader;

    /// <summary>Initializes an empty message-level header entry.</summary>
    public TokenHeaderInspection()
    {
        Role = TokenHeaderRole.Message;
        Protection = TokenHeaderProtection.Combined;
        JoseHeader = null;
        CoseHeader = null;
    }
}
