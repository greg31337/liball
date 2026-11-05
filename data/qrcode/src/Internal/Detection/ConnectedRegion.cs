namespace qrcode;

internal class ConnectedRegion
{
    internal bool Complete;

    internal bool TouchesBoundary;

    internal int Area;

    internal int MinimumX = int.MaxValue;

    internal int MinimumY = int.MaxValue;

    internal int MaximumX = -1;

    internal int MaximumY = -1;

    internal int MinimumSum = int.MaxValue;

    internal int MinimumSumX;

    internal int MinimumSumY;

    internal int MaximumSum = int.MinValue;

    internal int MaximumSumX;

    internal int MaximumSumY;

    internal int MinimumDifference = int.MaxValue;

    internal int MinimumDifferenceX;

    internal int MinimumDifferenceY;

    internal int MaximumDifference = int.MinValue;

    internal int MaximumDifferenceX;

    internal int MaximumDifferenceY;
}
