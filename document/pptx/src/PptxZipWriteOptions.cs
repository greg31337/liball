using System.IO.Compression;

namespace pptx;

internal class PptxZipWriteOptions
{
    internal CompressionLevel CompressionLevel;

    internal PptxZipWriteOptions()
    {
        CompressionLevel = CompressionLevel.NoCompression;
    }

    internal static PptxZipWriteOptions CreateDefault()
    {
        return new PptxZipWriteOptions();
    }
}
