using System.IO;

namespace iso20022;

internal class CatalogueContentReadResult
{
    internal bool Success;
    internal MemoryStream Content = new MemoryStream();
    internal long ByteCount;
    internal byte[] Hash = [];
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
}
