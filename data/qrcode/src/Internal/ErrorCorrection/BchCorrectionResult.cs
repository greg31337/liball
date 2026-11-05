namespace qrcode;

internal class BchCorrectionResult
{
    internal bool Success;

    internal int Data = -1;

    internal int Code = -1;

    internal int Distance = -1;

    internal int FirstDistance = -1;

    internal int SecondDistance = -1;

    internal int SourceCopy = -1;

    internal bool CopiesAgree;
}
