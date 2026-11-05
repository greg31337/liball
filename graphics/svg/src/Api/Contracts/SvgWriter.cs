namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgWriter"/>.</summary>
public interface ISvgWriter
{
    /// <summary>Writes a document using deterministic XML defaults.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The complete XML text and write metadata.</returns>
    SvgWriteResult WriteWithDocument(SvgDocument document);
    /// <summary>Writes a document using explicit namespace and formatting options.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The complete XML text or an atomic failure.</returns>
    SvgWriteResult WriteWithDocumentAndOptions(SvgDocument document, SvgWriteOptions options);
    /// <summary>Writes one element or content node as a standalone XML fragment.</summary>
    /// <param name="node">The element or content node.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The complete fragment text or an atomic failure.</returns>
    SvgWriteResult WriteWithNodeAndOptions(SvgNode node, SvgWriteOptions options);
    /// <summary>Writes a document using explicit namespace and formatting options.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The complete XML text or an atomic failure.</returns>
    SvgWriteResult WriteXml(SvgDocument document, SvgWriteOptions options);
    /// <summary>Writes complete XML with deterministic defaults to a caller-owned text writer.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-owned text writer.</param>
    /// <returns>The XML serialization result.</returns>
    SvgWriteResult WriteWithSvgDocumentAndTextWriter(SvgDocument document, System.IO.TextWriter destination);
    /// <summary>Writes complete XML to a caller-owned text writer without flushing or disposing it.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-owned text writer.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The XML serialization result.</returns>
    SvgWriteResult WriteWithSvgDocumentAndTextWriterAndSvgWriteOptions(SvgDocument document, System.IO.TextWriter? destination, SvgWriteOptions options);
    /// <summary>Writes UTF-8 XML with deterministic defaults to a caller-owned stream.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <returns>The XML serialization result.</returns>
    SvgWriteResult WriteWithSvgDocumentAndStream(SvgDocument document, System.IO.Stream destination);
    /// <summary>Writes UTF-8 XML to a caller-owned stream without flushing or disposing it.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The XML serialization result.</returns>
    SvgWriteResult WriteWithSvgDocumentAndStreamAndSvgWriteOptions(SvgDocument document, System.IO.Stream? destination, SvgWriteOptions options);
    /// <summary>Encodes complete XML with deterministic defaults as UTF-8 without a byte-order mark.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The UTF-8 bytes and XML write metadata.</returns>
    SvgWriteBytesResult WriteUtf8WithDocument(SvgDocument document);
    /// <summary>Encodes complete XML as UTF-8 without a byte-order mark.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The UTF-8 bytes and XML write metadata.</returns>
    SvgWriteBytesResult WriteUtf8WithDocumentAndOptions(SvgDocument document, SvgWriteOptions options);
    /// <summary>Copies default deterministic UTF-8 XML into a caller span without partial writes.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-provided UTF-8 byte span.</param>
    /// <returns>The required and written byte counts.</returns>
    SvgWriteBufferResult WriteUtf8WithDocumentAndDestination(SvgDocument document, System.Span<byte> destination);
    /// <summary>Copies complete UTF-8 XML into a caller span without partial writes.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-provided UTF-8 byte span.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The required and written byte counts.</returns>
    SvgWriteBufferResult WriteUtf8WithDocumentAndDestinationAndOptions(SvgDocument document, System.Span<byte> destination, SvgWriteOptions options);
    /// <summary>Copies default deterministic XML into a caller UTF-16 span without partial writes.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-provided UTF-16 character span.</param>
    /// <returns>The required and written character counts.</returns>
    SvgWriteBufferResult WriteWithSvgDocumentAndSpanOfChar(SvgDocument document, System.Span<char> destination);
    /// <summary>Copies complete XML into a caller UTF-16 span without partial writes.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-provided UTF-16 character span.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The required and written character counts.</returns>
    SvgWriteBufferResult WriteWithSvgDocumentAndSpanOfCharAndSvgWriteOptions(SvgDocument document, System.Span<char> destination, SvgWriteOptions options);
    /// <summary>Encodes default deterministic UTF-8 XML in an SVGZ gzip container.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The SVGZ bytes and XML write metadata.</returns>
    SvgWriteBytesResult WriteSvgzWithDocument(SvgDocument document);
    /// <summary>Encodes complete UTF-8 XML in a deterministic SVGZ gzip container.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The SVGZ bytes and XML write metadata.</returns>
    SvgWriteBytesResult WriteSvgzWithDocumentAndOptions(SvgDocument document, SvgWriteOptions options);
    /// <summary>Writes default deterministic SVGZ bytes to a caller-owned stream.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <returns>The SVGZ bytes and XML write metadata.</returns>
    SvgWriteBytesResult WriteSvgzWithDocumentAndDestination(SvgDocument document, System.IO.Stream destination);
    /// <summary>Writes deterministic SVGZ bytes to a caller-owned stream without flushing or disposing it.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The SVGZ bytes and XML write metadata.</returns>
    SvgWriteBytesResult WriteSvgzWithDocumentAndDestinationAndOptions(SvgDocument document, System.IO.Stream? destination, SvgWriteOptions options);
    /// <summary>Writes a default deterministic raw, percent-encoded, or base64 SVG data URL.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="encoding">The data URL payload encoding.</param>
    /// <returns>The data URL and XML write metadata.</returns>
    SvgWriteDataUrlResult WriteDataUrlWithDocumentAndEncoding(SvgDocument document, SvgDataUrlWriteEncoding encoding);
    /// <summary>Writes a raw, percent-encoded, or base64 SVG data URL.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="encoding">The data URL payload encoding.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The data URL and XML write metadata.</returns>
    SvgWriteDataUrlResult WriteDataUrlWithDocumentAndEncodingAndOptions(SvgDocument document, SvgDataUrlWriteEncoding encoding, SvgWriteOptions options);
}
