namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgLoader"/>.</summary>
public interface ISvgLoader
{
    /// <summary>Stages an immutable character string.</summary>
    /// <param name="value">The SVG or XML characters.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    SvgLoadResult LoadWithStringAndSvgLoadOptions(string? value, SvgLoadOptions options);
    /// <summary>Copies and stages a character span.</summary>
    /// <param name="value">The SVG or XML characters.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    SvgLoadResult LoadWithReadOnlySpanOfCharAndSvgLoadOptions(System.ReadOnlySpan<char> value, SvgLoadOptions options);
    /// <summary>Stages a caller byte array, copying it when configured.</summary>
    /// <param name="value">The encoded SVG or SVGZ bytes.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    SvgLoadResult LoadWithByteArrayAndSvgLoadOptions(byte[]? value, SvgLoadOptions options);
    /// <summary>Copies and stages a byte span.</summary>
    /// <param name="value">The encoded SVG or SVGZ bytes.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    SvgLoadResult LoadWithReadOnlySpanOfByteAndSvgLoadOptions(System.ReadOnlySpan<byte> value, SvgLoadOptions options);
    /// <summary>Reads a caller-owned stream from its current position without closing it.</summary>
    /// <param name="stream">The readable encoded input stream.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    SvgLoadResult LoadWithStreamAndOptions(System.IO.Stream? stream, SvgLoadOptions options);
    /// <summary>Reads a caller-owned text reader without closing it.</summary>
    /// <param name="reader">The readable decoded character source.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    SvgLoadResult LoadWithTextReaderAndSvgLoadOptions(System.IO.TextReader? reader, SvgLoadOptions options);
    /// <summary>Retains a caller-owned XML reader without advancing or closing it.</summary>
    /// <param name="reader">The XML reader positioned by the caller.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A staged reader or diagnostics.</returns>
    SvgLoadResult LoadWithXmlReaderAndSvgLoadOptions(System.Xml.XmlReader? reader, SvgLoadOptions options);
    /// <summary>Opens and stages a filesystem path using direct file I/O.</summary>
    /// <param name="path">The filesystem path.</param>
    /// <param name="options">The caller-owned load options.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    SvgLoadResult LoadPath(string? path, SvgLoadOptions options);
    /// <summary>Requests and stages a root URI exclusively through the configured callback.</summary>
    /// <param name="uri">The caller-resolved root URI.</param>
    /// <param name="options">The caller-owned load options containing the callback.</param>
    /// <returns>A bounded staged source or diagnostics.</returns>
    SvgLoadResult LoadUri(string? uri, SvgLoadOptions options);
}
