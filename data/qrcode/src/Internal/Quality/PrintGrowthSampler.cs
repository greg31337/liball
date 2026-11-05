namespace qrcode;

internal class PrintGrowthSampler
{
    internal QrRaster Raster = new QrRaster();
    internal PerspectiveTransform Transform = new PerspectiveTransform();
    internal QrPoint Mapped = new QrPoint();
    internal int MaximumSamples;
    internal int SampleCount;

    internal QrModuleValue Read(double position, double line, bool horizontal)
    {
        if (SampleCount >= MaximumSamples)
        {
            throw new QrResourceLimitException("Print-growth sampling exceeds the configured limit.",
                "MaximumModuleSamples", SampleCount == int.MaxValue ? int.MaxValue : SampleCount + 1, MaximumSamples);
        }
        SampleCount++;
        if (!Transform.TryMapPoint(horizontal ? position : line, horizontal ? line : position, Mapped)
            || Mapped.X < 0 || Mapped.Y < 0 || Mapped.X >= Raster.Width || Mapped.Y >= Raster.Height)
        {
            return QrModuleValue.Unknown;
        }
        return Raster.Pixels[(int)Mapped.Y * Raster.Stride + (int)Mapped.X] == 0 ? QrModuleValue.Dark : QrModuleValue.Light;
    }

    // Endpoints must already bracket the intended transition. Precision is
    // bounded to 1/4096 module; binary pixel resolution remains the main limit.
    internal double FindBoundary(double start, double end, double line, bool horizontal, bool darkAtStart)
    {
        for (int iteration = 0; iteration < 12; iteration++)
        {
            double middle = (start + end) / 2;
            QrModuleValue value = Read(middle, line, horizontal);
            if (value == QrModuleValue.Unknown) { return double.NaN; }
            if ((value == QrModuleValue.Dark) == darkAtStart) { start = middle; }
            else { end = middle; }
        }
        return (start + end) / 2;
    }
}
