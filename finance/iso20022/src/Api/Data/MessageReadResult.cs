using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less outcome of reading one message document.</summary>
public class MessageReadResult
{
    /// <summary>Whether a message document was read successfully.</summary>
    public bool Success;
    /// <summary>The resulting document when successful or partially available.</summary>
    public MessageDocument? Document;
    /// <summary>The ordered repairs applied in lenient mode.</summary>
    public List<MessageRepair> Repairs = new List<MessageRepair>();
    /// <summary>The number of source bytes consumed when known.</summary>
    public long BytesConsumed;
    /// <summary>Whether the input ended after this result.</summary>
    public bool EndOfInput;
    /// <summary>A stable error code when reading failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when reading failed.</summary>
    public string ErrorMessage = string.Empty;
}
