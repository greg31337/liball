using System.Collections.Generic;

namespace iso20022;

internal class MessageGraphSelection
{
    internal bool Success;
    internal string ErrorCode = string.Empty;
    internal string ErrorMessage = string.Empty;
    internal MessageDefinitionMetadata? Message;
    internal HashSet<string> Identifiers = new HashSet<string>(System.StringComparer.Ordinal);
}
