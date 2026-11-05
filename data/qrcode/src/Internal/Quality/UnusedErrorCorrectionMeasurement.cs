namespace qrcode;

internal class UnusedErrorCorrectionMeasurement
{
    internal int[] BlockCapacities = new int[0];
    internal int[] ConsumedCapacity = new int[0];
    internal double[] UnusedFractions = new double[0];
    internal int LimitingBlockIndex = -1;
    internal QrQualityMetric UnusedErrorCorrection = new QrQualityMetric { Name = "UnusedErrorCorrection", Unit = "fraction" };
}
