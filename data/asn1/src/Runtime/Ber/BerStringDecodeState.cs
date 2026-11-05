namespace asn1;

internal class BerStringDecodeState
{
    internal ByteAccumulator Bytes = new ByteAccumulator(0);
    internal bool SawFragment;
    internal int LastUnusedBits;
    internal bool Failed;
    internal bool LimitExceeded;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
