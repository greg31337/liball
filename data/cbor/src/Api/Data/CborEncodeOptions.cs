using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Holds policies applied while encoding CBOR data.
/// </summary>
public class CborEncodeOptions
{
    /// <summary>The serialization profile.</summary>
    public CborEncodingProfile EncodingProfile = CborEncodingProfile.Basic;
    /// <summary>The map-key ordering policy.</summary>
    public CborMapKeyOrder MapKeyOrder = CborMapKeyOrder.Preserve;
    /// <summary>Indicates whether valid original representation metadata is honored.</summary>
    public bool PreserveRepresentation;
    /// <summary>Indicates whether preferred encoding reduces exactly representable floating-point values to the shortest width.</summary>
    public bool ReduceFloatWidth = true;
    /// <summary>Indicates whether indefinite-length strings and containers may be emitted.</summary>
    public bool AllowIndefiniteLength = true;
    /// <summary>The policy for string references and shared values.</summary>
    public CborSharedValuePolicy SharedValuePolicy = CborSharedValuePolicy.Disabled;
    /// <summary>Indicates whether a constructor-supplied output stream remains open when its writer is disposed.</summary>
    public bool LeaveOpen;
    /// <summary>The maximum nested array, map, and tag depth.</summary>
    public int MaxNestingDepth = 64;
    /// <summary>The maximum total number of encoded items.</summary>
    public int MaxTotalItems = 1000000;
    /// <summary>The maximum encoded bytes produced by one operation.</summary>
    public long MaxOutputBytes = 67108864;
    /// <summary>The maximum number of top-level items in one CBOR Sequence operation.</summary>
    public int MaxSequenceItems = 1000000;
    /// <summary>The maximum encoded bytes in one CBOR Sequence operation.</summary>
    public long MaxSequenceBytes = 67108864;
    /// <summary>The maximum number of tracked shared values.</summary>
    public int MaxSharedValues = 100000;
    /// <summary>The tag handlers available to this operation.</summary>
    public List<CborTagRegistration> TagRegistrations = new List<CborTagRegistration>();
}
