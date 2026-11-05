namespace qrcode;

internal class HybridBinarizerResult
{
    internal int[] Thresholds = new int[0];

    internal int BlockColumns;

    internal int BlockRows;

    internal bool UsedGlobalHistogram;

    internal QrRaster DarkOnLight = new QrRaster();

    internal QrRaster LightOnDark = new QrRaster();
}
