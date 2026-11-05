namespace qrcode;

internal class GridGeometryMeasurement
{
    internal int SampleCount;
    internal double HorizontalPitch;
    internal double VerticalPitch;
    internal double RootMeanSquareDisplacement;
    internal double[] ModuleDisplacements = new double[0];
    internal QrQualityMetric AxialNonUniformity = new QrQualityMetric { Name = "AxialNonUniformity", Unit = "fraction" };
    internal QrQualityMetric GridNonUniformity = new QrQualityMetric { Name = "GridNonUniformity", Unit = "modules" };
    internal QrQualityMetric GeometricDistortion = new QrQualityMetric { Name = "GeometricDistortion", Unit = "degrees" };
}
