using System.IO;

namespace iso20022;

/// <summary>Detects ISO 20022 message identity and envelope layout without losing the input.</summary>
public interface IMessageDetector
{
    /// <summary>Detects a message from a byte array.</summary>
    /// <param name="input">The complete encoded input.</param>
    /// <param name="options">The read and wrapper options.</param>
    /// <returns>The detection result and replayable input.</returns>
    MessageDetectionResult DetectBytes(byte[]? input, MessageReadOptions? options);

    /// <summary>Detects a message from the current position of a stream.</summary>
    /// <param name="input">The input stream.</param>
    /// <param name="options">The read and wrapper options.</param>
    /// <returns>The detection result and original or replacement replay stream.</returns>
    MessageDetectionResult DetectStream(Stream? input, MessageReadOptions? options);

    /// <summary>Detects a message from text encoded to UTF-8 for replay.</summary>
    /// <param name="input">The XML text.</param>
    /// <param name="options">The read and wrapper options.</param>
    /// <returns>The detection result and replayable UTF-8 input.</returns>
    MessageDetectionResult DetectText(string? input, MessageReadOptions? options);
}
