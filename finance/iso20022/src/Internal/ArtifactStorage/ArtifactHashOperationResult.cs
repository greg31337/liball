namespace iso20022;

internal class ArtifactHashOperationResult
{
    internal bool Success;
    internal bool Cancelled;
    internal long ByteCount;
    internal byte[] Hash = [];
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
