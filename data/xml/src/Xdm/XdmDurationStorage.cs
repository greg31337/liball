using System.Numerics;

namespace xml;

internal class XdmDurationStorage
{
    internal BigInteger Months;
    internal BigInteger SecondsCoefficient;
    internal int SecondsScale;

    internal XdmDurationStorage(BigInteger months, BigInteger secondsCoefficient, int secondsScale)
    {
        Months = months;
        SecondsCoefficient = secondsCoefficient;
        SecondsScale = secondsScale;
    }
}
