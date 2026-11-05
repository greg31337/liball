using System.Numerics;

namespace cbor;

internal class CddlNumericValue
{
    internal const int None = 0;
    internal const int Integer = 1;
    internal const int Float = 2;
    internal int Kind;
    internal BigInteger IntegerValue;
    internal double FloatValue;

    internal CddlNumericValue()
    {
    }
}
