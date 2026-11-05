using System.IO;

namespace iso20022;

/// <summary>Reads generic and detected ISO 20022 XML messages from supported input forms.</summary>
public interface IXmlMessageReader
{
    /// <summary>Reads generic XML from bytes without requiring a known message identity.</summary>
    /// <param name="input">The encoded XML input.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadGenericBytes(byte[]? input, MessageReadOptions? options);
    /// <summary>Reads generic XML from a stream without requiring a known message identity.</summary>
    /// <param name="input">The input stream.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadGenericStream(Stream? input, MessageReadOptions? options);
    /// <summary>Reads generic XML from text without requiring a known message identity.</summary>
    /// <param name="input">The XML text.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadGenericText(string? input, MessageReadOptions? options);
    /// <summary>Detects and reads an ISO 20022 message from bytes.</summary>
    /// <param name="input">The encoded XML input.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadAutoBytes(byte[]? input, MessageReadOptions? options);
    /// <summary>Detects and reads an ISO 20022 message from a stream.</summary>
    /// <param name="input">The input stream.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadAutoStream(Stream? input, MessageReadOptions? options);
    /// <summary>Detects and reads an ISO 20022 message from text.</summary>
    /// <param name="input">The XML text.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadAutoText(string? input, MessageReadOptions? options);
    /// <summary>Reads bytes only when the detected payload has the exact requested identifier.</summary>
    /// <param name="input">The encoded XML input.</param><param name="identifier">The required identifier.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadExactBytes(byte[]? input, MessageIdentifier? identifier, MessageReadOptions? options);
    /// <summary>Reads a stream only when the detected payload has the exact requested identifier.</summary>
    /// <param name="input">The input stream.</param><param name="identifier">The required identifier.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadExactStream(Stream? input, MessageIdentifier? identifier, MessageReadOptions? options);
    /// <summary>Reads text only when the detected payload has the exact requested identifier.</summary>
    /// <param name="input">The XML text.</param><param name="identifier">The required identifier.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadExactText(string? input, MessageIdentifier? identifier, MessageReadOptions? options);
    /// <summary>Reads only the application header from bytes.</summary>
    /// <param name="input">The encoded XML input.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadHeaderBytes(byte[]? input, MessageReadOptions? options);
    /// <summary>Reads only the application header from a stream.</summary>
    /// <param name="input">The input stream.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadHeaderStream(Stream? input, MessageReadOptions? options);
    /// <summary>Reads only the application header from text.</summary>
    /// <param name="input">The XML text.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadHeaderText(string? input, MessageReadOptions? options);
    /// <summary>Reads only metadata from bytes.</summary>
    /// <param name="input">The encoded XML input.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadMetadataBytes(byte[]? input, MessageReadOptions? options);
    /// <summary>Reads only metadata from a stream.</summary>
    /// <param name="input">The input stream.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadMetadataStream(Stream? input, MessageReadOptions? options);
    /// <summary>Reads only metadata from text.</summary>
    /// <param name="input">The XML text.</param><param name="options">The read options.</param><returns>The read result.</returns>
    MessageReadResult ReadMetadataText(string? input, MessageReadOptions? options);
}
