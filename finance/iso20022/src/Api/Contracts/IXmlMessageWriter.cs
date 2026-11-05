using System.Buffers;
using System.IO;

namespace iso20022;

/// <summary>Writes generic ISO 20022 message documents to supported output forms.</summary>
public interface IXmlMessageWriter
{
    /// <summary>Writes a generic message to a byte stream.</summary>
    /// <param name="document">The message document.</param><param name="output">The output stream.</param><param name="options">The write options.</param><returns>The write result.</returns>
    MessageWriteResult WriteGenericStream(MessageDocument? document, Stream? output, MessageWriteOptions? options);
    /// <summary>Writes a generic message through a text writer.</summary>
    /// <param name="document">The message document.</param><param name="output">The text writer.</param><param name="options">The write options.</param><returns>The write result.</returns>
    MessageWriteResult WriteGenericTextWriter(MessageDocument? document, TextWriter? output, MessageWriteOptions? options);
    /// <summary>Writes a generic message to a byte buffer writer.</summary>
    /// <param name="document">The message document.</param><param name="output">The byte buffer writer.</param><param name="options">The write options.</param><returns>The write result.</returns>
    MessageWriteResult WriteGenericBuffer(MessageDocument? document, IBufferWriter<byte>? output, MessageWriteOptions? options);
    /// <summary>Writes a generic message to a returned string.</summary>
    /// <param name="document">The message document.</param><param name="options">The write options.</param><returns>The write result.</returns>
    MessageWriteResult WriteGenericString(MessageDocument? document, MessageWriteOptions? options);
}
