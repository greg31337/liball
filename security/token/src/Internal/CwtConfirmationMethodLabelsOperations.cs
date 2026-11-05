using System;
namespace token;

/// <summary>Provides registered CWT confirmation-method labels and names.</summary>
internal static class CwtConfirmationMethodLabelsOperations
{
    /// <summary>A directly embedded COSE_Key.</summary>
    internal const int CoseKey = CwtConfirmationMethodLabels.CoseKey;

    /// <summary>A COSE_Encrypt or COSE_Encrypt0 containing a COSE_Key.</summary>
    internal const int EncryptedCoseKey = CwtConfirmationMethodLabels.EncryptedCoseKey;

    /// <summary>A binary key identifier.</summary>
    internal const int KeyId = CwtConfirmationMethodLabels.KeyId;

    /// <summary>OSCORE input material.</summary>
    internal const int OscoreInputMaterial = CwtConfirmationMethodLabels.OscoreInputMaterial;

    /// <summary>A SHA-256 COSE_Key thumbprint.</summary>
    internal const int CoseKeyThumbprint = CwtConfirmationMethodLabels.CoseKeyThumbprint;

    /// <summary>A SHA-256 JWK thumbprint encoded as a byte string.</summary>
    internal const int JwkThumbprint = CwtConfirmationMethodLabels.JwkThumbprint;

    static readonly int[] labels = new int[]
    {
        CoseKey,
        EncryptedCoseKey,
        KeyId,
        OscoreInputMaterial,
        CoseKeyThumbprint,
        JwkThumbprint
    };

    static readonly string[] names = new string[]
    {
        "COSE_Key",
        "Encrypted_COSE_Key",
        "kid",
        "osc",
        "ckt",
        "jkt"
    };

    /// <summary>Determines whether an integer CWT confirmation-method label is registered.</summary>
    /// <param name="label">The integer confirmation-method label.</param>
    /// <returns><c>true</c> when the label is registered; otherwise, <c>false</c>.</returns>
    internal static bool IsRegistered(int label)
    {
        return RegistryLookup.Contains(labels, label);
    }

    /// <summary>Gets the registered confirmation-method name for a label.</summary>
    /// <param name="label">The integer confirmation-method label.</param>
    /// <returns>The registered name, or <c>null</c> when the label is unknown.</returns>
    internal static string? GetName(int label)
    {
        int index = RegistryLookup.IndexOf(labels, label);
        if (index < 0)
        {
            return null;
        }

        return names[index];
    }

    /// <summary>Returns all registered integer confirmation-method labels.</summary>
    /// <returns>A new array containing the labels.</returns>
    internal static int[] GetAll()
    {
        return RegistryLookup.Copy(labels);
    }
}
