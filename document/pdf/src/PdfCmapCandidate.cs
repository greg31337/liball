namespace pdf;

internal class PdfCmapCandidate
{
    internal int PlatformId;
    internal int EncodingId;
    internal int Format;
    internal int Offset;
    internal int Length;
    internal int Score;

    internal PdfCmapCandidate(int platformId, int encodingId, int format, int offset, int length, int score)
    {
        PlatformId = platformId;
        EncodingId = encodingId;
        Format = format;
        Offset = offset;
        Length = length;
        Score = score;
    }
}
