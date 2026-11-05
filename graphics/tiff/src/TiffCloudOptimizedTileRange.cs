namespace tiff;

internal class TiffCloudOptimizedTileRange
{
    internal bool HasData;
    internal ulong FirstOffset;
    internal ulong EndOffset;

    internal TiffCloudOptimizedTileRange()
    {
        HasData = false;
        FirstOffset = 0;
        EndOffset = 0;
    }
}
