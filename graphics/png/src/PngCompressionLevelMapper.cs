using System.IO.Compression;

namespace png;

internal class PngCompressionLevelMapper
{
    internal const int MinimumCompressionLevel = 0;
    internal const int MaximumCompressionLevel = 9;

    internal static CompressionLevel Map(int compressionLevel)
    {
        if (compressionLevel < MinimumCompressionLevel || compressionLevel > MaximumCompressionLevel)
        {
            throw new ArgumentOutOfRangeException("CompressionLevel");
        }

        if (compressionLevel == 0)
        {
            return CompressionLevel.NoCompression;
        }

        if (compressionLevel < 4)
        {
            return CompressionLevel.Fastest;
        }

        if (compressionLevel < MaximumCompressionLevel)
        {
            return CompressionLevel.Optimal;
        }

        return CompressionLevel.SmallestSize;
    }
}
