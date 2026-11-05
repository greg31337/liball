namespace image;

internal class GzipDecompressionResult
{
    internal byte[] Data;
    internal bool Complete;

    internal GzipDecompressionResult()
    {
        Data = new byte[0];
        Complete = false;
    }
}
