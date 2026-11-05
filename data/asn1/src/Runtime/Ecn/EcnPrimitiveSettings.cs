namespace asn1;

internal class EcnPrimitiveSettings
{
    internal int SizeBits = -1;
    internal int UnitBits = 1;
    internal int UnusedBits;
    internal bool HasUnusedBits;
    internal bool HasRange;
    internal bool HasAlphabet;
    internal System.Numerics.BigInteger LowerBound;
    internal System.Numerics.BigInteger UpperBound;
    internal string Alphabet = "";
    internal string Representation = "";
    internal string Signedness = "";
    internal string ByteOrder = "big-endian";
    internal string BitOrder = "most-significant-first";
    internal string BitReversal = "no-reversal";
    internal string Padding = "zero";
    internal string Justification = "right";
    internal bool ValueReversal;
    internal EcnBitField Pattern = new EcnBitField
    {
        Data = [0],
        BitCount = 1
    };
    internal EcnBitField TruePattern = new EcnBitField
    {
        Data = [128],
        BitCount = 1
    };
    internal EcnBitField FalsePattern = new EcnBitField
    {
        Data = [0],
        BitCount = 1
    };
    internal EcnBitField Terminator = new EcnBitField();
}
