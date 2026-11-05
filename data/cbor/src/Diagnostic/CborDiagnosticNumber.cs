using System.Numerics;

namespace cbor;

internal class CborDiagnosticNumber
{
    internal bool Success;
    internal bool IsFloat;
    internal BigInteger IntegerValue;
    internal double FloatValue;
}
