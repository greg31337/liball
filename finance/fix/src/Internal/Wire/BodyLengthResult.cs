namespace fix;

internal class BodyLengthResult
{
    internal bool Success;
    internal bool Matches;
    internal int DeclaredLength;
    internal int CalculatedLength;
    internal int BodyStart;
    internal int ChecksumStart;
    internal int ErrorIndex = -1;
    internal string ErrorCode = "";
}
