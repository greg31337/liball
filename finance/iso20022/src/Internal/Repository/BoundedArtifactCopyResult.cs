namespace iso20022;

internal class BoundedArtifactCopyResult
{
    internal bool Success;
    internal byte[] Bytes = [];
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
