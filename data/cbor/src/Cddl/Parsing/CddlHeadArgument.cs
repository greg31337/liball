using System.Numerics;

namespace cbor;

internal class CddlHeadArgument
{
    internal BigInteger Value;
    internal bool HasValue;
    internal CddlNode? Type;
    internal CddlSourceSpan Span = new CddlSourceSpan();
}
