namespace rtp;

/// <summary>
/// Contains one RFC 3611 run-length or bit-vector chunk.
/// </summary>
public class RtcpRunLengthChunk
{
    /// <summary>Whether the chunk carries a fifteen-bit vector instead of a run.</summary>
    public bool IsBitVector;

    /// <summary>The receipt or duplication state represented by a run-length chunk.</summary>
    public bool PositiveRun;

    /// <summary>The number of events represented by a run-length chunk.</summary>
    public int RunLength;

    /// <summary>The fifteen-bit event vector represented by a bit-vector chunk.</summary>
    public ushort BitVector;

    internal RtcpRunLengthChunk Copy()
    {
        RtcpRunLengthChunk copy = new RtcpRunLengthChunk();
        copy.IsBitVector = IsBitVector;
        copy.PositiveRun = PositiveRun;
        copy.RunLength = RunLength;
        copy.BitVector = BitVector;
        return copy;
    }
}
