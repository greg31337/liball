namespace qrcode;

internal class GlobalHistogramResult
{
    internal int Threshold;

    internal bool LowContrast;

    internal QrRaster DarkOnLight = new QrRaster();

    internal QrRaster LightOnDark = new QrRaster();
}
