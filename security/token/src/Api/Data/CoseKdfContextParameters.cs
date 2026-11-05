using System;
namespace token;

/// <summary>Defines the inputs used to construct an RFC 9053 COSE_KDF_Context.</summary>
public class CoseKdfContextParameters
{
    /// <summary>Contains the integer or text algorithm identifier.</summary>
    public CborItem AlgorithmIdentifier;

    /// <summary>Contains information about PartyU.</summary>
    public CoseKdfPartyInfo PartyUInfo;

    /// <summary>Contains information about PartyV.</summary>
    public CoseKdfPartyInfo PartyVInfo;

    /// <summary>Contains the desired derived-key length in bits.</summary>
    public ulong KeyDataLength;

    /// <summary>Contains the exact encoded protected header map, or an empty array.</summary>
    public byte[] ProtectedParameters;

    /// <summary>Contains optional supplemental public bytes, or <c>null</c> to omit the field.</summary>
    public byte[]? SupplementalPublicOther;

    /// <summary>Contains optional supplemental private bytes, or <c>null</c> to omit the field.</summary>
    public byte[]? SupplementalPrivateInfo;

    /// <summary>Initializes a context for an integer algorithm identifier.</summary>
    /// <param name="algorithmIdentifier">The COSE algorithm identifier.</param>
    /// <param name="keyDataLength">The desired derived-key length in bits.</param>
    public CoseKdfContextParameters(
        int algorithmIdentifier,
        ulong keyDataLength)
    {
        AlgorithmIdentifier = Integer(algorithmIdentifier);
        PartyUInfo = new CoseKdfPartyInfo();
        PartyVInfo = new CoseKdfPartyInfo();
        KeyDataLength = keyDataLength;
        ProtectedParameters = Array.Empty<byte>();
        SupplementalPublicOther = null;
        SupplementalPrivateInfo = null;
    }

    /// <summary>Creates a context for a private text algorithm identifier.</summary>
    /// <param name="algorithmIdentifier">The private COSE algorithm identifier.</param>
    /// <param name="keyDataLength">The desired derived-key length in bits.</param>
    /// <returns>A context containing the text algorithm identifier.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>algorithmIdentifier</c> is null.</exception>
    internal static CoseKdfContextParameters Create(
        string algorithmIdentifier,
        ulong keyDataLength)
    {
        if (algorithmIdentifier == null)
        {
            throw new ArgumentNullException("algorithmIdentifier");
        }

        CoseKdfContextParameters result = new CoseKdfContextParameters(0, keyDataLength);
        result.AlgorithmIdentifier = Text(algorithmIdentifier);
        return result;
    }

    internal static CborItem Integer(int value)
    {
        CborItem item = new CborItem();
        if (value >= 0)
        {
            item.Kind = TokenCborValueKind.UnsignedInteger;
            item.IntegerArgument = (ulong)value;
        }
        else
        {
            item.Kind = TokenCborValueKind.NegativeInteger;
            item.IntegerArgument = (ulong)(-(long)value - 1L);
        }

        return item;
    }

    internal static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }
}
