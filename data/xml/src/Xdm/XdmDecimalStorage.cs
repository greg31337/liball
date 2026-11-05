using System.Numerics;

namespace xml;

internal class XdmDecimalStorage
{
    internal BigInteger Coefficient;
    internal int Scale;

    internal XdmDecimalStorage(BigInteger coefficient, int scale)
    {
        Coefficient = coefficient;
        Scale = scale;
    }
}
