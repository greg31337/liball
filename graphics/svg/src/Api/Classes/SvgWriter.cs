namespace svg;

/// <summary>Provides namespace-repairing SVG XML serialization entry points.</summary>
public class SvgWriter : ISvgWriter
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgWriter()
    {
    }

    /// <summary>Writes a document using deterministic XML defaults.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The complete XML text and write metadata.</returns>
    public SvgWriteResult WriteWithDocument(SvgDocument document)
    {
        return new SvgWriter().WriteWithDocumentAndOptions(document, new SvgWriteOptions());
    }

    /// <summary>Writes a document using explicit namespace and formatting options.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The complete XML text or an atomic failure.</returns>
    public SvgWriteResult WriteWithDocumentAndOptions(SvgDocument document, SvgWriteOptions options)
    {
        return SvgXmlWriter.WriteDocument(document, options);
    }

    /// <summary>Writes one element or content node as a standalone XML fragment.</summary>
    /// <param name="node">The element or content node.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The complete fragment text or an atomic failure.</returns>
    public SvgWriteResult WriteWithNodeAndOptions(SvgNode node, SvgWriteOptions options)
    {
        return SvgXmlWriter.WriteNode(node, options);
    }

    /// <summary>Writes a document using explicit namespace and formatting options.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The complete XML text or an atomic failure.</returns>
    public SvgWriteResult WriteXml(SvgDocument document, SvgWriteOptions options)
    {
        return new SvgWriter().WriteWithDocumentAndOptions(document, options);
    }

    /// <summary>Writes complete XML with deterministic defaults to a caller-owned text writer.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-owned text writer.</param>
    /// <returns>The XML serialization result.</returns>
    public SvgWriteResult WriteWithSvgDocumentAndTextWriter(SvgDocument document, System.IO.TextWriter destination)
    {
        return new SvgWriter().WriteWithSvgDocumentAndTextWriterAndSvgWriteOptions(document, destination, new SvgWriteOptions());
    }

    /// <summary>Writes complete XML to a caller-owned text writer without flushing or disposing it.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-owned text writer.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The XML serialization result.</returns>
    public SvgWriteResult WriteWithSvgDocumentAndTextWriterAndSvgWriteOptions(SvgDocument document, System.IO.TextWriter? destination, SvgWriteOptions options)
    {
        if (destination == null) return new SvgWriter().InvalidWrite("A text writer destination is required.");
        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, options);
        if (result.Success) destination.Write(result.Text);
        return result;
    }

    /// <summary>Writes UTF-8 XML with deterministic defaults to a caller-owned stream.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <returns>The XML serialization result.</returns>
    public SvgWriteResult WriteWithSvgDocumentAndStream(SvgDocument document, System.IO.Stream destination)
    {
        return new SvgWriter().WriteWithSvgDocumentAndStreamAndSvgWriteOptions(document, destination, new SvgWriteOptions());
    }

    /// <summary>Writes UTF-8 XML to a caller-owned stream without flushing or disposing it.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The XML serialization result.</returns>
    public SvgWriteResult WriteWithSvgDocumentAndStreamAndSvgWriteOptions(SvgDocument document, System.IO.Stream? destination, SvgWriteOptions options)
    {
        if (destination == null || !destination.CanWrite) return new SvgWriter().InvalidWrite("A writable stream destination is required.");
        SvgWriteBytesResult bytes = new SvgWriter().WriteUtf8WithDocumentAndOptions(document, options);
        if (bytes.Success) destination.Write(bytes.Bytes, 0, bytes.Bytes.Length);
        return bytes.WriteResult;
    }

    /// <summary>Encodes complete XML with deterministic defaults as UTF-8 without a byte-order mark.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The UTF-8 bytes and XML write metadata.</returns>
    public SvgWriteBytesResult WriteUtf8WithDocument(SvgDocument document)
    {
        return new SvgWriter().WriteUtf8WithDocumentAndOptions(document, new SvgWriteOptions());
    }

    /// <summary>Encodes complete XML as UTF-8 without a byte-order mark.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The UTF-8 bytes and XML write metadata.</returns>
    public SvgWriteBytesResult WriteUtf8WithDocumentAndOptions(SvgDocument document, SvgWriteOptions options)
    {
        SvgWriteOptions? prepared = new SvgWriter().PrepareUtf8Options(document, options);
        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, (prepared ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!result.Success) return new SvgWriteBytesResult(result, System.Array.Empty<byte>(), false);
        byte[] bytes = new System.Text.UTF8Encoding(false, true).GetBytes(result.Text);
        return new SvgWriteBytesResult(result, bytes, false);
    }

    /// <summary>Copies default deterministic UTF-8 XML into a caller span without partial writes.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-provided UTF-8 byte span.</param>
    /// <returns>The required and written byte counts.</returns>
    public SvgWriteBufferResult WriteUtf8WithDocumentAndDestination(SvgDocument document, System.Span<byte> destination)
    {
        return new SvgWriter().WriteUtf8WithDocumentAndDestinationAndOptions(document, destination, new SvgWriteOptions());
    }

    /// <summary>Copies complete UTF-8 XML into a caller span without partial writes.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-provided UTF-8 byte span.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The required and written byte counts.</returns>
    public SvgWriteBufferResult WriteUtf8WithDocumentAndDestinationAndOptions(SvgDocument document, System.Span<byte> destination, SvgWriteOptions options)
    {
        SvgWriteBytesResult bytes = new SvgWriter().WriteUtf8WithDocumentAndOptions(document, options);
        if (!bytes.Success) return new SvgWriteBufferResult(bytes.WriteResult, SvgWriteBufferKind.Utf8Bytes, 0, 0, bytes.WriteResult.Error, bytes.WriteResult.Message);
        if (destination.Length < bytes.Bytes.Length) return new SvgWriteBufferResult(bytes.WriteResult, SvgWriteBufferKind.Utf8Bytes, bytes.Bytes.Length, 0, SvgWriteError.DestinationTooSmall, "The UTF-8 destination span is too small.");
        int index = 0;
        while (index < bytes.Bytes.Length)
        {
            destination[index] = bytes.Bytes[index];
            index = index + 1;
        }
        return new SvgWriteBufferResult(bytes.WriteResult, SvgWriteBufferKind.Utf8Bytes, bytes.Bytes.Length, bytes.Bytes.Length, SvgWriteError.None, "");
    }

    /// <summary>Copies default deterministic XML into a caller UTF-16 span without partial writes.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-provided UTF-16 character span.</param>
    /// <returns>The required and written character counts.</returns>
    public SvgWriteBufferResult WriteWithSvgDocumentAndSpanOfChar(SvgDocument document, System.Span<char> destination)
    {
        return new SvgWriter().WriteWithSvgDocumentAndSpanOfCharAndSvgWriteOptions(document, destination, new SvgWriteOptions());
    }

    /// <summary>Copies complete XML into a caller UTF-16 span without partial writes.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The caller-provided UTF-16 character span.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The required and written character counts.</returns>
    public SvgWriteBufferResult WriteWithSvgDocumentAndSpanOfCharAndSvgWriteOptions(SvgDocument document, System.Span<char> destination, SvgWriteOptions options)
    {
        SvgWriteResult result = new SvgWriter().WriteWithDocumentAndOptions(document, options);
        if (!result.Success) return new SvgWriteBufferResult(result, SvgWriteBufferKind.Utf16Characters, 0, 0, result.Error, result.Message);
        if (destination.Length < result.Text.Length) return new SvgWriteBufferResult(result, SvgWriteBufferKind.Utf16Characters, result.Text.Length, 0, SvgWriteError.DestinationTooSmall, "The UTF-16 destination span is too small.");
        int index = 0;
        while (index < result.Text.Length)
        {
            destination[index] = result.Text[index];
            index = index + 1;
        }
        return new SvgWriteBufferResult(result, SvgWriteBufferKind.Utf16Characters, result.Text.Length, result.Text.Length, SvgWriteError.None, "");
    }

    /// <summary>Encodes default deterministic UTF-8 XML in an SVGZ gzip container.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <returns>The SVGZ bytes and XML write metadata.</returns>
    public SvgWriteBytesResult WriteSvgzWithDocument(SvgDocument document)
    {
        return new SvgWriter().WriteSvgzWithDocumentAndOptions(document, new SvgWriteOptions());
    }

    /// <summary>Encodes complete UTF-8 XML in a deterministic SVGZ gzip container.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The SVGZ bytes and XML write metadata.</returns>
    public SvgWriteBytesResult WriteSvgzWithDocumentAndOptions(SvgDocument document, SvgWriteOptions options)
    {
        SvgWriteBytesResult utf8 = new SvgWriter().WriteUtf8WithDocumentAndOptions(document, options);
        if (!utf8.Success) return new SvgWriteBytesResult(utf8.WriteResult, System.Array.Empty<byte>(), true);
        using (System.IO.MemoryStream output = new System.IO.MemoryStream())
        {
            using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionLevel.SmallestSize, true))
            {
                gzip.Write(utf8.Bytes, 0, utf8.Bytes.Length);
            }
            return new SvgWriteBytesResult(utf8.WriteResult, output.ToArray(), true);
        }
    }

    /// <summary>Writes default deterministic SVGZ bytes to a caller-owned stream.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <returns>The SVGZ bytes and XML write metadata.</returns>
    public SvgWriteBytesResult WriteSvgzWithDocumentAndDestination(SvgDocument document, System.IO.Stream destination)
    {
        return new SvgWriter().WriteSvgzWithDocumentAndDestinationAndOptions(document, destination, new SvgWriteOptions());
    }

    /// <summary>Writes deterministic SVGZ bytes to a caller-owned stream without flushing or disposing it.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="destination">The writable caller-owned stream.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The SVGZ bytes and XML write metadata.</returns>
    public SvgWriteBytesResult WriteSvgzWithDocumentAndDestinationAndOptions(SvgDocument document, System.IO.Stream? destination, SvgWriteOptions options)
    {
        if (destination == null || !destination.CanWrite) return new SvgWriteBytesResult(new SvgWriter().InvalidWrite("A writable stream destination is required."), System.Array.Empty<byte>(), true);
        SvgWriteBytesResult result = new SvgWriter().WriteSvgzWithDocumentAndOptions(document, options);
        if (result.Success) destination.Write(result.Bytes, 0, result.Bytes.Length);
        return result;
    }

    /// <summary>Writes a default deterministic raw, percent-encoded, or base64 SVG data URL.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="encoding">The data URL payload encoding.</param>
    /// <returns>The data URL and XML write metadata.</returns>
    public SvgWriteDataUrlResult WriteDataUrlWithDocumentAndEncoding(SvgDocument document, SvgDataUrlWriteEncoding encoding)
    {
        return new SvgWriter().WriteDataUrlWithDocumentAndEncodingAndOptions(document, encoding, new SvgWriteOptions());
    }

    /// <summary>Writes a raw, percent-encoded, or base64 SVG data URL.</summary>
    /// <param name="document">The editable SVG document.</param>
    /// <param name="encoding">The data URL payload encoding.</param>
    /// <param name="options">The namespace, policy, formatting, and limit options.</param>
    /// <returns>The data URL and XML write metadata.</returns>
    public SvgWriteDataUrlResult WriteDataUrlWithDocumentAndEncodingAndOptions(SvgDocument document, SvgDataUrlWriteEncoding encoding, SvgWriteOptions options)
    {
        if (encoding < SvgDataUrlWriteEncoding.Unencoded || encoding > SvgDataUrlWriteEncoding.Base64)
        {
            SvgWriteResult invalid = new SvgWriter().InvalidWrite("The data URL encoding is invalid.");
            return new SvgWriteDataUrlResult(invalid, "", encoding, 0, invalid.Error, invalid.Message);
        }
        SvgWriteBytesResult utf8 = new SvgWriter().WriteUtf8WithDocumentAndOptions(document, options);
        if (!utf8.Success) return new SvgWriteDataUrlResult(utf8.WriteResult, "", encoding, 0, utf8.WriteResult.Error, utf8.WriteResult.Message);
        if (encoding == SvgDataUrlWriteEncoding.Unencoded)
        {
            string raw = "data:image/svg+xml;charset=utf-8," + utf8.WriteResult.Text;
            return new SvgWriteDataUrlResult(utf8.WriteResult, raw, encoding, utf8.ByteCount, SvgWriteError.None, "");
        }
        SvgDataUrlEncodeResult encoded = new SvgDataUrl().EncodeWithBytesAndMediaTypeAndUseBase64AndMaxDecodedBytes(utf8.Bytes, "image/svg+xml;charset=utf-8", encoding == SvgDataUrlWriteEncoding.Base64, utf8.Bytes.LongLength);
        if (!encoded.Success)
        {
            SvgWriteResult invalid = new SvgWriter().InvalidWrite("SVG data URL encoding failed.");
            return new SvgWriteDataUrlResult(invalid, "", encoding, 0, invalid.Error, invalid.Message);
        }
        return new SvgWriteDataUrlResult(utf8.WriteResult, encoded.DataUrl, encoding, utf8.ByteCount, SvgWriteError.None, "");
    }
SvgWriteOptions? PrepareUtf8Options(SvgDocument document, SvgWriteOptions? options)
    {
        if (options == null) return null;
        SvgWriteOptions prepared = options.Copy();
        if (prepared.DeclarationPolicy == SvgXmlDeclarationPolicy.Include) prepared.DeclarationEncoding = "utf-8";
        else if (prepared.DeclarationPolicy == SvgXmlDeclarationPolicy.Preserve && document != null && document.SourceText != null && document.SourceText.StartsWith("<?xml", System.StringComparison.Ordinal))
        {
            prepared.DeclarationPolicy = SvgXmlDeclarationPolicy.Include;
            prepared.DeclarationEncoding = "utf-8";
        }
        return prepared;
    }
SvgWriteResult InvalidWrite(string message)
    {
        SvgWriteResult result = new SvgWriteResult();
        result.Error = SvgWriteError.InvalidArgument;
        result.Message = message;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidArgument, message, null));
        result.Success = false;
        return result;
    }
}
