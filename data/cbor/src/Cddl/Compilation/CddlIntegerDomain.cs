using System.Numerics;

namespace cbor;

internal class CddlIntegerDomain
{
    internal bool IsKnown;
    internal BigInteger Minimum;
    internal BigInteger Maximum;
}
