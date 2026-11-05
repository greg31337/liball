using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less outcome and optional buffered output of writing a message.</summary>
public class MessageWriteResult
{
    /// <summary>Whether the message was written successfully.</summary>
    public bool Success;
    /// <summary>The UTF-8 output for byte-buffer writing.</summary>
    public byte[] Bytes = [];
    /// <summary>The output for string writing.</summary>
    public string Text = string.Empty;
    /// <summary>The number of bytes written when known.</summary>
    public long BytesWritten;
    /// <summary>The ordered repairs applied before writing.</summary>
    public List<MessageRepair> Repairs = new List<MessageRepair>();
    /// <summary>A stable error code when writing failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when writing failed.</summary>
    public string ErrorMessage = string.Empty;
}
