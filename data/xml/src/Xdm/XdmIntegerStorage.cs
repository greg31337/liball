using System.Numerics;

namespace xml;

internal class XdmIntegerStorage
{
    internal BigInteger Value;

    internal XdmIntegerStorage(BigInteger value)
    {
        Value = value;
    }
}
