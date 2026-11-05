using System;
using System.IO;
using System.Text;
using System.Threading;

namespace html;

/// <summary>Provides entry points for parsing, sanitizing, converting, and extracting HTML.</summary>
internal static class Html
{
    internal const int ReaderBufferCharacters = 4096;

    /// <summary>Parses HTML source text into a browser-compatible document tree.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <returns>The parsed HTML document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlDocument Parse(string html)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        return Parse(html, HtmlParseOptions.CreateDefault(),
            CancellationToken.None).GetDocument();
    }

    /// <summary>Parses HTML source text into a browser-compatible document tree.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <returns>The parsed HTML document.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    public static HtmlDocument Parse(ReadOnlySpan<char> html)
    {
        HtmlParser parser = HtmlParser.Create(HtmlParseOptions.CreateDefault(),
            CancellationToken.None);
        parser.Feed(html, true);
        return parser.Complete().GetDocument();
    }

    /// <summary>Parses HTML source data and returns the document, diagnostics, encoding, and completion state.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document, parsing status, detected encoding information, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult Parse(string html,
        HtmlParseOptions options, CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        HtmlParser parser = HtmlParser.Create(options, cancellationToken);
        parser.Feed(html.AsSpan(), true);
        return parser.Complete();
    }

    /// <summary>Parses HTML source data and returns the document, diagnostics, encoding, and completion state.</summary>
    /// <param name="html">The HTML source characters to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document, parsing status, and diagnostics.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    public static HtmlParseResult Parse(ReadOnlySpan<char> html,
        HtmlParseOptions options, CancellationToken cancellationToken)
    {
        HtmlParser parser = HtmlParser.Create(options, cancellationToken);
        parser.Feed(html, true);
        return parser.Complete();
    }

    /// <summary>Parses HTML source data and returns the document, diagnostics, encoding, and completion state.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document, parsing status, detected encoding information, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult Parse(byte[] html,
        HtmlParseOptions options, CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        return ParseBytes(new MemoryStream(html, false), true, options,
            cancellationToken);
    }

    /// <summary>Parses HTML source data and returns the document, diagnostics, encoding, and completion state.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document, parsing status, detected encoding information, and diagnostics.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    public static HtmlParseResult Parse(ReadOnlySpan<byte> html,
        HtmlParseOptions options, CancellationToken cancellationToken)
    {
        return ParseBytes(new MemoryStream(html.ToArray(), false), true,
            options, cancellationToken);
    }

    /// <summary>Parses HTML source data and returns the document, diagnostics, encoding, and completion state.</summary>
    /// <param name="stream">The readable stream containing encoded HTML bytes.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document, parsing status, detected encoding information, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult Parse(Stream stream,
        HtmlParseOptions options, CancellationToken cancellationToken)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        ValidateOptions(options);
        return ParseBytes(stream, options.GetLeaveOpen(), options,
            cancellationToken);
    }

    /// <summary>Parses HTML source data and returns the document, diagnostics, encoding, and completion state.</summary>
    /// <param name="reader">The text reader that supplies decoded HTML characters.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document, parsing status, detected encoding information, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the operation cannot complete safely.</exception>
    public static HtmlParseResult Parse(TextReader reader,
        HtmlParseOptions options, CancellationToken cancellationToken)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        ValidateOptions(options);
        HtmlParser parser = HtmlParser.Create(options, cancellationToken);
        return ParseReaderWithParser(reader, options, cancellationToken,
            parser);
    }

    /// <summary>Parses HTML source text using the supplied element context and returns the resulting fragment.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="contextTag">The HTML element name whose parsing context applies.</param>
    /// <returns>The document and fragment roots, parsing status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult ParseFragment(string html,
        string contextTag)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        HtmlDocument document = new HtmlDocument("", "",
            HtmlDocumentMode.NoQuirks, new HtmlDiagnostic[0],
            HtmlSourceSpan.CreateUnknown());
        HtmlNode context = document.CreateElement(contextTag);
        return ParseFragment(html, new HtmlFragmentContext(context), options,
            CancellationToken.None);
    }

    /// <summary>Parses HTML source text using the supplied element context and returns the resulting fragment.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="context">The context that selects the applicable HTML parsing or encoding rules.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document and fragment roots, parsing status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult ParseFragment(string html,
        HtmlFragmentContext context, HtmlParseOptions options,
        CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        HtmlParser parser = HtmlParser.CreateFragment(options,
            context.GetElement(), cancellationToken);
        parser.Feed(html.AsSpan(), true);
        return parser.Complete();
    }

    /// <summary>Parses HTML source characters using the supplied element context and returns the resulting fragment.</summary>
    /// <param name="html">The HTML source characters to process.</param>
    /// <param name="context">The context that selects the applicable HTML parsing rules.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document and fragment roots, parsing status, and diagnostics.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult ParseFragment(ReadOnlySpan<char> html,
        HtmlFragmentContext context, HtmlParseOptions options,
        CancellationToken cancellationToken)
    {
        ValidateContext(context);
        HtmlParser parser = HtmlParser.CreateFragment(options,
            context.GetElement(), cancellationToken);
        parser.Feed(html, true);
        return parser.Complete();
    }

    /// <summary>Parses encoded HTML bytes using the supplied element context and returns the resulting fragment.</summary>
    /// <param name="html">The encoded HTML bytes to process.</param>
    /// <param name="context">The context that selects the applicable HTML parsing and encoding rules.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document and fragment roots, decoding metadata, parsing status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult ParseFragment(byte[] html,
        HtmlFragmentContext context, HtmlParseOptions options,
        CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        ValidateContext(context);
        return ParseFragmentBytes(new MemoryStream(html, false), true,
            context, options, cancellationToken);
    }

    /// <summary>Parses encoded HTML bytes using the supplied element context and returns the resulting fragment.</summary>
    /// <param name="html">The encoded HTML bytes to process.</param>
    /// <param name="context">The context that selects the applicable HTML parsing and encoding rules.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document and fragment roots, decoding metadata, parsing status, and diagnostics.</returns>
    /// <remarks>The input span is read during this call and is not retained.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult ParseFragment(ReadOnlySpan<byte> html,
        HtmlFragmentContext context, HtmlParseOptions options,
        CancellationToken cancellationToken)
    {
        ValidateContext(context);
        return ParseFragmentBytes(new MemoryStream(html.ToArray(), false),
            true, context, options, cancellationToken);
    }

    /// <summary>Parses an encoded HTML stream using the supplied element context and returns the resulting fragment.</summary>
    /// <param name="stream">The readable stream containing encoded HTML bytes.</param>
    /// <param name="context">The context that selects the applicable HTML parsing and encoding rules.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document and fragment roots, decoding metadata, parsing status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlParseResult ParseFragment(Stream stream,
        HtmlFragmentContext context, HtmlParseOptions options,
        CancellationToken cancellationToken)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        ValidateContext(context);
        ValidateOptions(options);
        return ParseFragmentBytes(stream, options.GetLeaveOpen(), context,
            options, cancellationToken);
    }

    /// <summary>Parses decoded HTML characters using the supplied element context and returns the resulting fragment.</summary>
    /// <param name="reader">The text reader that supplies decoded HTML characters.</param>
    /// <param name="context">The context that selects the applicable HTML parsing rules.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The document and fragment roots, parsing status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when the operation cannot complete safely.</exception>
    public static HtmlParseResult ParseFragment(TextReader reader,
        HtmlFragmentContext context, HtmlParseOptions options,
        CancellationToken cancellationToken)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        ValidateContext(context);
        ValidateOptions(options);
        HtmlParser parser = HtmlParser.CreateFragment(options,
            context.GetElement(), cancellationToken);
        return ParseReaderWithParser(reader, options, cancellationToken,
            parser);
    }

    /// <summary>Sanitizes HTML by applying the compiled element, attribute, URL, and style rules.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <returns>The sanitized HTML text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static string Sanitize(string html)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        HtmlSanitizeResult result = HtmlSanitizer.Sanitize(html,
            HtmlSanitizerPolicy.CreateRichText().Compile(),
            HtmlParseOptions.CreateDefault(), CancellationToken.None);
        return result.GetHtml();
    }

    /// <summary>Sanitizes HTML by applying the compiled element, attribute, URL, and style rules.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="policy">The sanitizer policy whose rules are applied.</param>
    /// <param name="parseOptions">The options that control HTML parsing.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The sanitized HTML, completion status, diagnostics, and bounded change report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlSanitizeResult Sanitize(string html,
        HtmlSanitizerPolicy policy, HtmlParseOptions parseOptions,
        CancellationToken cancellationToken)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        return HtmlSanitizer.Sanitize(html, policy.Compile(), parseOptions,
            cancellationToken);
    }

    /// <summary>Converts HTML to Markdown while preserving supported document structure.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <returns>The generated Markdown text.</returns>
    public static string ToMarkdown(string html)
    {
        return HtmlMarkdownConverter.Convert(html).GetMarkdown();
    }

    /// <summary>Converts HTML to Markdown while preserving supported document structure.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The generated Markdown, completion status, and diagnostics.</returns>
    public static HtmlMarkdownResult ToMarkdown(string html,
        IHtmlMarkdownOptions options, CancellationToken cancellationToken)
    {
        return HtmlMarkdownConverter.Convert(html, options,
            cancellationToken);
    }

    /// <summary>Converts HTML to readable plain text.</summary>
    /// <param name="html">The HTML source text to process.</param>
    /// <returns>The generated plain text.</returns>
    public static string ToText(string html)
    {
        return HtmlTextConverter.Convert(html).GetText();
    }

    /// <summary>Converts HTML to readable plain text.</summary>
    /// <param name="html">The HTML source text to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The generated text, completion status, and diagnostics.</returns>
    public static HtmlTextResult ToText(string html,
        IHtmlTextOptions options, CancellationToken cancellationToken)
    {
        return HtmlTextConverter.Convert(html, options,
            cancellationToken);
    }

    /// <summary>Extracts normalized metadata from HTML source text.</summary>
    /// <param name="html">The HTML source text to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The extracted and precedence-resolved document metadata.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlMetadata ExtractMetadata(string html,
        IHtmlMetadataOptions options,
        CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        HtmlMetadataOptions snapshot = options.Copy();
        if (snapshot == null)
        {
            throw new ArgumentException(
                "Metadata options must produce a copy.", "options");
        }

        snapshot.Validate();
        HtmlParseOptions parseOptions = HtmlParseOptions.CreateDefault();
        parseOptions.SetLimits(snapshot.GetLimits());
        HtmlParseResult parsed = Parse(html, parseOptions,
            cancellationToken);
        return HtmlMetadataExtractor.ExtractCore(parsed.GetDocument(),
            snapshot, cancellationToken, null);
    }

    /// <summary>Extracts article content without sanitizing it.</summary>
    /// <remarks>Use <see cref="Process(string, IHtmlProcessingConfiguration,
    /// CancellationToken)"/> when renderable article HTML is required.
    /// </remarks>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The extracted article, completeness status, and diagnostics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlArticleResult ExtractArticle(string html,
        HtmlArticleOptions options, CancellationToken cancellationToken)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        HtmlParseOptions parseOptions = HtmlParseOptions.CreateDefault();
        parseOptions.SetLimits(options.GetLimits());
        HtmlParseResult parsed = Parse(html, parseOptions,
            cancellationToken);
        return HtmlArticleExtractor.ExtractParsed(parsed.GetDocument(),
            options, cancellationToken, parsed.IsSuccess(),
            parsed.IsComplete(), parsed.WasTruncated());
    }

    /// <summary>Runs the configured parsing, extraction, sanitization, serialization, text, and Markdown stages.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The requested pipeline outputs, per-stage status, timing, and diagnostics.</returns>
    public static HtmlProcessingResult Process(string html,
        IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlProcessor.Process(html, options, cancellationToken);
    }

    /// <summary>Runs the configured parsing, extraction, sanitization, serialization, text, and Markdown stages.</summary>
    /// <param name="html">The HTML source text or bytes to process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The requested pipeline outputs, per-stage status, timing, and diagnostics.</returns>
    public static HtmlProcessingResult Process(byte[] html,
        IHtmlProcessingConfiguration options,
        CancellationToken cancellationToken)
    {
        return HtmlProcessor.Process(html, options, cancellationToken);
    }

    internal static HtmlParseResult ParseBytes(Stream stream, bool leaveOpen,
        HtmlParseOptions options, CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        HtmlBudget budget = new HtmlBudget(options.GetLimits(),
            cancellationToken);
        HtmlParser parser = HtmlParser.Create(options, cancellationToken,
            budget, true);
        return ParseBytesWithParser(stream, leaveOpen, options, budget,
            parser);
    }

    internal static HtmlParseResult ParseFragmentBytes(Stream stream,
        bool leaveOpen, HtmlFragmentContext context,
        HtmlParseOptions options, CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        HtmlBudget budget = new HtmlBudget(options.GetLimits(),
            cancellationToken);
        HtmlParser parser = HtmlParser.CreateFragmentWithBudget(options,
            context.GetElement(), cancellationToken, budget, true);
        return ParseBytesWithParser(stream, leaveOpen, options, budget,
            parser);
    }

    internal static HtmlParseResult ParseBytesWithParser(Stream stream,
        bool leaveOpen, HtmlParseOptions options, HtmlBudget budget,
        HtmlParser parser)
    {
        HtmlDiagnosticCollector diagnostics =
            new HtmlDiagnosticCollector(budget);
        HtmlStreamDecoder decoder;
        if (options.HasEncoding())
        {
            decoder = HtmlStreamDecoder.CreateWithEncoding(stream,
                options.GetEncoding(), leaveOpen, budget, diagnostics);
        }
        else
        {
            decoder = HtmlStreamDecoder.Create(stream, leaveOpen,
                ReaderBufferCharacters,
                options.GetTransportEncodingLabel(),
                "windows-1252", budget, diagnostics);
        }

        HtmlDecodedInput? decoded = decoder.Load();
        HtmlEncodingDetectionResult? detection = decoder.GetDetection();
        string label = detection.GetEncodingName();
        Encoding? encoding = ResolveEncoding(label, options);
        parser.AddInputDiagnostics(diagnostics.GetDiagnostics());
        if (diagnostics.WasTruncated())
        {
            parser.SetInputDiagnosticsTruncated(
                diagnostics.GetDroppedCount());
        }

        parser.SetEncodingMetadata(encoding, label, detection);
        parser.FeedDecoded(decoded, true);
        if (decoder.WasLimitExceeded())
        {
            parser.SetInputLimitExceeded();
        }

        if (!decoded.IsSupported())
        {
            parser.SetInputFailed();
        }

        decoder.Close();
        return parser.Complete();
    }

    internal static HtmlParseResult ParseReaderWithParser(TextReader reader,
        HtmlParseOptions options, CancellationToken cancellationToken,
        HtmlParser parser)
    {
        char[] buffer = new char[ReaderBufferCharacters];
        int maximum = options.GetLimits().GetMaximumInputCharacters();
        int readCharacters = 0;
        bool ended = false;
        while (!ended && parser.CanAcceptInput())
        {
            cancellationToken.ThrowIfCancellationRequested();
            int remaining = maximum - readCharacters;
            int requested = remaining >= buffer.Length
                ? buffer.Length : remaining + 1;
            int read = reader.Read(buffer, 0, requested);
            cancellationToken.ThrowIfCancellationRequested();
            if (read < 0 || read > requested)
            {
                throw new InvalidDataException(
                    "TextReader returned an invalid character count.");
            }

            ended = read == 0;
            readCharacters = readCharacters + read;
            parser.Feed(new ReadOnlySpan<char>(buffer, 0, read), ended);
            DrainAvailable(parser);
        }

        if (!options.GetLeaveOpen())
        {
            reader.Dispose();
        }

        return parser.Complete();
    }

    internal static void DrainAvailable(HtmlParser parser)
    {
        bool reading = true;
        while (reading)
        {
            HtmlParseProgress progress = parser.Read();
            HtmlParseProgressStatus status = progress.GetStatus();
            reading = status == HtmlParseProgressStatus.TokenProcessed;
        }
    }

    internal static Encoding? ResolveEncoding(string label,
        HtmlParseOptions options)
    {
        if (label == "UTF-8")
        {
            return Encoding.UTF8;
        }

        if (label == "UTF-16LE")
        {
            return Encoding.Unicode;
        }

        if (label == "UTF-16BE")
        {
            return Encoding.BigEndianUnicode;
        }

        Encoding? configuredEncoding = options.GetEncoding();
        if (options.HasEncoding() && configuredEncoding != null
            && Ascii.EqualsIgnoreCase(configuredEncoding.WebName.AsSpan(),
                label.AsSpan()))
        {
            return configuredEncoding;
        }

        return null;
    }

    internal static void ValidateOptions(HtmlParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
    }

    internal static void ValidateContext(HtmlFragmentContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }
    }
}
