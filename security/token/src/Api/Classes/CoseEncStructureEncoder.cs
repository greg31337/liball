using System;
namespace token;

/// <summary>Encodes deterministic RFC 9052 Enc_structure values.</summary>
public class CoseEncStructureEncoder : ICoseEncStructureEncoder
{
    /// <summary>Initializes an Enc_structure encoder.</summary>
    public CoseEncStructureEncoder()
    {
    }

    /// <summary>Encodes a size-limited Enc_structure value.</summary>
    /// <param name="context">The encryption operation context.</param>
    /// <param name="protectedParameters">The exact encoded protected header map, or an empty array.</param>
    /// <param name="externalAuthenticatedData">The application-supplied authenticated bytes, or an empty array.</param>
    /// <param name="policy">The output size and CBOR depth limits.</param>
    /// <returns>The encoded Enc_structure, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an array or policy argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>context</c> is not defined.</exception>
    public TokenBytesResult Encode(
        CoseEncStructureContext context,
        byte[] protectedParameters,
        byte[] externalAuthenticatedData,
        TokenPolicy policy)
    {
        return CoseEncStructure.Create(
            context,
            protectedParameters,
            externalAuthenticatedData,
            policy);
    }
}
