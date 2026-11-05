using System.Collections.Generic;
using System.IO;

namespace image;

/// <summary>Reads image structure and metadata without decoding the complete pixel payload.</summary>
internal static class ImageMetadataReaderCore
{
    internal const int BufferLength = 8192;

    /// <summary>Reads metadata from a file using default safety limits and parser options.</summary>
    /// <param name="path">The path of the image file to open, or null.</param>
    /// <returns>An extracted document. A null or empty path produces a document with a diagnostic instead of opening a file.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the file cannot be opened or read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when the caller cannot read the file.</exception>
    public static ImageMetadataDocument ReadFile(string? path)
    {
        return ReadFileWithOptions(path, new ImageMetadataReaderOptions());
    }

    /// <summary>Reads metadata from a file using caller-supplied safety limits and parser options.</summary>
    /// <remarks>A null options value selects the defaults. Malformed image structures normally become diagnostics in the returned document.</remarks>
    /// <param name="path">The path of the image file to open, or null.</param>
    /// <param name="options">The options to apply, or null to use defaults.</param>
    /// <returns>An extracted document. A null or empty path produces a document with a diagnostic instead of opening a file.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the file cannot be opened or read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown when the caller cannot read the file.</exception>
    public static ImageMetadataDocument ReadFileWithOptions(string? path, ImageMetadataReaderOptions? options)
    {
        if (path == null || path.Length == 0)
        {
            ImageMetadataDocument document = new ImageMetadataDocument();
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "reader.path.empty", "Image file path was empty.");
            return document;
        }

        using (FileStream stream = File.OpenRead(path))
        {
            if (HasXmpExtension(path))
            {
                return ReadXmpFileStreamWithOptions(stream, options);
            }

            return ReadImageFileStreamWithOptions(stream, path, options);
        }
    }

    /// <summary>Reads metadata from the current position of a stream using default options.</summary>
    /// <remarks>Seekable streams are parsed in place. Non-seekable streams are buffered up to the configured file limit.</remarks>
    /// <param name="stream">The readable image stream.</param>
    /// <returns>An extracted document. A null stream produces a document with a diagnostic.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the underlying stream reports a read or seek failure.</exception>
    public static ImageMetadataDocument ReadStream(Stream? stream)
    {
        return ReadStreamWithOptions(stream, new ImageMetadataReaderOptions());
    }

    /// <summary>Reads metadata from the current stream position using caller-supplied options.</summary>
    /// <remarks>The reader limits how much data parsers inspect or preserve. Reaching a limit is reported in diagnostics and can yield a partial document.</remarks>
    /// <param name="stream">The readable image stream.</param>
    /// <param name="options">The options to apply, or null to use defaults.</param>
    /// <returns>An extracted document. A null stream produces a document with a diagnostic.</returns>
    /// <exception cref="System.IO.IOException">Thrown when the underlying stream reports a read or seek failure.</exception>
    public static ImageMetadataDocument ReadStreamWithOptions(Stream? stream, ImageMetadataReaderOptions? options)
    {
        ImageMetadataReaderOptions actualOptions = EffectiveOptions(options);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();

        if (stream == null)
        {
            ImageMetadataDocument document = new ImageMetadataDocument();
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "reader.stream.null", "Image stream was null.");
            return document;
        }

        long limit = EffectiveFileLimit(actualOptions, diagnostics);

        if (!stream.CanSeek)
        {
            byte[] bytes = ReadNonSeekableBytes(stream, limit, diagnostics);
            using (MemoryStream memory = new MemoryStream(bytes))
            {
                return ReadSeekableStreamWithDiagnostics(memory, actualOptions, diagnostics, bytes.Length, limit);
            }
        }

        long length = stream.Length - stream.Position;
        return ReadSeekableStreamWithDiagnostics(stream, actualOptions, diagnostics, length, limit);
    }

    internal static ImageMetadataDocument ReadSeekableStreamWithDiagnostics(Stream stream, ImageMetadataReaderOptions options, List<MetadataDiagnostic> diagnostics, long length, long limit)
    {
        if (length > limit)
        {
            MetadataDiagnostic.AddLimitExceeded(diagnostics, "reader.file.limit", "Image stream exceeded the configured file byte limit.", "stream", 0, length);
        }

        ImageFormatInfo format = ImageFormatDetectorCore.DetectStream(stream);
        BoundedStreamReader reader = new BoundedStreamReader(stream, length, limit, diagnostics);
        ImageMetadataDocument document = ParserDispatch.Parse(reader, format, options);
        return ApplyDiagnosticVerbosity(document, options);
    }

    internal static ImageMetadataDocument ReadXmpFileStreamWithOptions(Stream stream, ImageMetadataReaderOptions? options)
    {
        ImageMetadataReaderOptions actualOptions = EffectiveOptions(options);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        long limit = EffectiveFileLimit(actualOptions, diagnostics);
        long length = stream.Length - stream.Position;
        if (length > limit)
        {
            MetadataDiagnostic.AddLimitExceeded(diagnostics, "reader.file.limit", "Image stream exceeded the configured file byte limit.", "stream", 0, length);
        }

        ImageFormatInfo format = ImageFormatDetectorCore.DetectStream(stream);
        if (format.Kind != ImageFormatKind.Xmp)
        {
            format = ImageFormatMapping.CreateInfo(ImageFormatKind.Xmp);
            format.Confidence = 60;
        }

        BoundedStreamReader reader = new BoundedStreamReader(stream, length, limit, diagnostics);
        ImageMetadataDocument document = ParserDispatch.Parse(reader, format, actualOptions);
        return ApplyDiagnosticVerbosity(document, actualOptions);
    }

    internal static ImageMetadataDocument ReadImageFileStreamWithOptions(Stream stream, string path, ImageMetadataReaderOptions? options)
    {
        ImageMetadataReaderOptions actualOptions = EffectiveOptions(options);
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        long limit = EffectiveFileLimit(actualOptions, diagnostics);
        long length = stream.Length - stream.Position;
        if (length > limit)
        {
            MetadataDiagnostic.AddLimitExceeded(diagnostics, "reader.file.limit", "Image stream exceeded the configured file byte limit.", "stream", 0, length);
        }

        ImageFormatInfo format = ImageFormatDetectorCore.DetectStreamWithHint(stream, path);

        BoundedStreamReader reader = new BoundedStreamReader(stream, length, limit, diagnostics);
        ImageMetadataDocument document = ParserDispatch.Parse(reader, format, actualOptions);
        return ApplyDiagnosticVerbosity(document, actualOptions);
    }

    internal static ImageMetadataDocument ApplyDiagnosticVerbosity(ImageMetadataDocument document, ImageMetadataReaderOptions options)
    {
        if (options.DiagnosticsVerbosity != MetadataDiagnosticsVerbosity.Quiet)
        {
            return document;
        }

        RemoveQuietDiagnostics(document.Diagnostics);
        if (document.Format != null)
        {
            RemoveQuietDiagnostics(document.Format.Diagnostics);
        }

        document.RefreshDiagnosticStatus();
        return document;
    }

    internal static void RemoveQuietDiagnostics(List<MetadataDiagnostic> diagnostics)
    {
        int index = diagnostics.Count - 1;
        while (index >= 0)
        {
            MetadataDiagnostic diagnostic = diagnostics[index];
            if (diagnostic != null && (diagnostic.Severity == MetadataSeverity.Info || diagnostic.Severity == MetadataSeverity.Unsupported))
            {
                diagnostics.RemoveAt(index);
            }

            index = index - 1;
        }
    }

    internal static bool HasXmpExtension(string path)
    {
        string extension = Path.GetExtension(path);
        return string.Equals(extension, ".xmp", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static ImageMetadataReaderOptions EffectiveOptions(ImageMetadataReaderOptions? options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions();
        }

        return options;
    }

    internal static long EffectiveFileLimit(ImageMetadataReaderOptions options, List<MetadataDiagnostic> diagnostics)
    {
        long limit = options.MaxFileBytes;

        if (limit < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "reader.file.limit.negative", "Configured file byte limit was negative.", "stream", 0, limit);
            return 0;
        }

        return limit;
    }

    internal static byte[] ReadNonSeekableBytes(Stream stream, long limit, List<MetadataDiagnostic> diagnostics)
    {
        if (limit < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(diagnostics, "reader.file.limit.negative", "Configured file byte limit was negative.", "stream", 0, limit);
            return new byte[0];
        }

        using (MemoryStream output = new MemoryStream())
        {
            byte[] buffer = new byte[BufferLength];

            while (true)
            {
                long remainingLimit = limit - output.Length;

                if (remainingLimit == 0)
                {
                    int extra = stream.Read(buffer, 0, 1);
                    if (extra > 0)
                    {
                        MetadataDiagnostic.AddLimitExceeded(diagnostics, "reader.file.limit", "Image stream exceeded the configured file byte limit.", "stream", 0, output.Length + extra);
                    }

                    return output.ToArray();
                }

                int request = BufferLength;
                if (remainingLimit < request)
                {
                    request = (int)remainingLimit;
                }

                int read = stream.Read(buffer, 0, request);
                if (read == 0)
                {
                    return output.ToArray();
                }

                output.Write(buffer, 0, read);
            }
        }
    }
}
