using System;

namespace iso20022;

internal class ProcessingExecutionState
{
    internal ProcessingRequest Request = new ProcessingRequest();
    internal ProcessingResult Result = new ProcessingResult();
    internal MessageDocument? Document;
    internal object? Model;
    internal byte[] Xml = Array.Empty<byte>();
    internal MessageIdentifier? CurrentIdentifier;
    internal string CurrentNamespaceUri = string.Empty;
    internal bool TargetProduced;
}
