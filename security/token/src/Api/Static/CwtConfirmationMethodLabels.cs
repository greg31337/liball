using System;
namespace token;

/// <summary>Provides registered CWT confirmation-method labels.</summary>
public static class CwtConfirmationMethodLabels
{
    /// <summary>A directly embedded COSE_Key.</summary>
    public const int CoseKey = 1;

    /// <summary>A COSE_Encrypt or COSE_Encrypt0 containing a COSE_Key.</summary>
    public const int EncryptedCoseKey = 2;

    /// <summary>A binary key identifier.</summary>
    public const int KeyId = 3;

    /// <summary>OSCORE input material.</summary>
    public const int OscoreInputMaterial = 4;

    /// <summary>A SHA-256 COSE_Key thumbprint.</summary>
    public const int CoseKeyThumbprint = 5;

    /// <summary>A SHA-256 JWK thumbprint encoded as a byte string.</summary>
    public const int JwkThumbprint = 323;
}
