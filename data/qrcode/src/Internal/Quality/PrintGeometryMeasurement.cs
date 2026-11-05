namespace qrcode;

internal class PrintGeometryMeasurement
{
    internal QrQuietZoneMeasurement QuietZone = new QrQuietZoneMeasurement();
    internal QrQualityMetric HorizontalPrintGrowth = new QrQualityMetric { Name = "HorizontalPrintGrowth", Unit = "modules" };
    internal QrQualityMetric VerticalPrintGrowth = new QrQualityMetric { Name = "VerticalPrintGrowth", Unit = "modules" };
    internal int HorizontalRunCount;
    internal int VerticalRunCount;
    internal int RejectedHorizontalRunCount;
    internal int RejectedVerticalRunCount;
    internal int SampleCount;
}
