using System.Collections.Generic;

namespace molecule;


internal class BondOrderCoordinateSet
{
    internal List<double> X;
    internal List<double> Y;
    internal List<double> Z;
    internal List<bool> IsSet;

    internal BondOrderCoordinateSet()
    {
        X = new List<double>();
        Y = new List<double>();
        Z = new List<double>();
        IsSet = new List<bool>();
    }
}

internal class BondOrderProposal
{
    internal BondOrder Order;
    internal double RelativeError;
    internal double AlternativeGap;

    internal BondOrderProposal()
    {
        Order = BondOrder.Unknown;
        RelativeError = 1000.0;
        AlternativeGap = 0.0;
    }
}
