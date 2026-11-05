using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Parses MIME into library objects.
/// </summary>
/// <remarks>
/// Malformed email data is normally represented by status and issue information rather than by exceptions. Configured limits still bound the amount of work performed.
/// </remarks>
public class EmailMimeParser : IEmailMimeParserContract
{
    internal static EmailEntityParseResult ParseImplementationWithInputUsingValueArray(byte[] input)
    {
        return ParseImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEntityParseResult ParseWithInputUsingValueArray(byte[] input)
    {
        return EmailMimeParser.ParseImplementationWithInputUsingValueArray(input);
    }

    internal static EmailEntityParseResult ParseImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailParseOptions snapshot = options.Clone();
        EnsureLength(input.Length, snapshot);
        EmailContent source = new EmailContentOperations().CreateMemory(input, EmailContentOwnership.Borrowed, false);
        return ParseBuffered(input, source, snapshot);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailEntityParseResult ParseWithInputAndOptionsUsingValueArrayAndEmailParseOptions(byte[] input, EmailParseOptions options)
    {
        return EmailMimeParser.ParseImplementationWithInputAndOptionsUsingValueArrayAndEmailParseOptions(input, options);
    }

    internal static EmailEntityParseResult ParseImplementationWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return ParseImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, new EmailParseOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailEntityParseResult ParseWithInputUsingReadOnlyMemory(ReadOnlyMemory<byte> input)
    {
        return EmailMimeParser.ParseImplementationWithInputUsingReadOnlyMemory(input);
    }

    internal static EmailEntityParseResult ParseImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        EmailParseOptions snapshot = options.Clone();
        EnsureLength(input.Length, snapshot);
        byte[] bytes = input.ToArray();
        EmailContent source = new EmailContentOperations().CreateMemory(bytes, EmailContentOwnership.Owned, false);
        return ParseBuffered(bytes, source, snapshot);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailEntityParseResult ParseWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(ReadOnlyMemory<byte> input, EmailParseOptions options)
    {
        return EmailMimeParser.ParseImplementationWithInputAndOptionsUsingReadOnlyMemoryAndEmailParseOptions(input, options);
    }

    internal static MimeEntity? ParseBodyImplementation(EmailHeaders? headers, EmailContent? source, EmailRange? entityRange, EmailRange? contentRange, EmailParseOptions options)
    {
        return ParseBodyResultImplementation(headers, source, entityRange, contentRange, options).GetEntity();
    }

    /// <summary>
    /// Parses the supplied body data.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "source">The source content to read or copy from.</param>
    /// <param name = "entityRange">The entity range used by the operation.</param>
    /// <param name = "contentRange">The content range used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public MimeEntity? ParseBody(EmailHeaders? headers, EmailContent? source, EmailRange? entityRange, EmailRange? contentRange, EmailParseOptions options)
    {
        return EmailMimeParser.ParseBodyImplementation(headers, source, entityRange, contentRange, options);
    }

    internal static EmailEntityParseResult ParseBodyResultImplementation(EmailHeaders? headers, EmailContent? source, EmailRange? entityRange, EmailRange? contentRange, EmailParseOptions options)
    {
        if (headers == null || source == null || entityRange == null || contentRange == null || options == null)
        {
            throw new ArgumentNullException("headers");
        }

        EmailParseOptions snapshot = options.Clone();
        EmailMimeParseState state = new EmailMimeParseState(snapshot);
        MimeEntity entity = BuildEntity(headers, null, source, entityRange, contentRange, snapshot, state, 0, new EmailIssueList());
        return new EmailEntityParseResult(entity, entity.GetIssues(), entityRange.GetLength(), entityRange.GetLength(), state.IsComplete());
    }

    /// <summary>
    /// Parses the supplied body result data.
    /// </summary>
    /// <param name = "headers">The headers used by the operation.</param>
    /// <param name = "source">The source content to read or copy from.</param>
    /// <param name = "entityRange">The entity range used by the operation.</param>
    /// <param name = "contentRange">The content range used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailEntityParseResult ParseBodyResult(EmailHeaders? headers, EmailContent? source, EmailRange? entityRange, EmailRange? contentRange, EmailParseOptions options)
    {
        return EmailMimeParser.ParseBodyResultImplementation(headers, source, entityRange, contentRange, options);
    }

    static EmailEntityParseResult ParseBuffered(byte[] input, EmailContent source, EmailParseOptions options)
    {
        EmailHeaderBlockParserOptions headerOptions = new EmailHeaderBlockParserOptions();
        headerOptions.SetLimits(options.GetLimits().GetParsingLimits());
        headerOptions.SetMode(options.GetMode() == EmailParseMode.Strict ? EmailStructuredValueParseMode.Strict : EmailStructuredValueParseMode.Tolerant);
        headerOptions.SetCompatibilityOptions(options.GetCompatibilityOptions());
        EmailHeaderBlockParseResult headers = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(input, headerOptions, new EmailSourcePosition(0, 1, 1), true);
        bool complete = headers.IsComplete();
        MimeEntity? entity = null;
        if (complete || options.GetCompatibilityOptions().GetReturnPartialResults())
        {
            long bodyStart = complete ? headers.GetBodyStartOffset() : headers.GetConsumedByteCount();
            EmailRange entityRange = new EmailRange(0, input.Length);
            EmailRange contentRange = new EmailRange(bodyStart, input.Length - bodyStart);
            EmailMimeParseState state = new EmailMimeParseState(options);
            EmailIssueList initialIssues = complete ? new EmailIssueList() : headers.GetIssues();
            entity = BuildEntity(headers.GetHeaders(), null, source, entityRange, contentRange, options, state, 0, initialIssues);
            complete = complete && state.IsComplete();
        }

        EmailIssueList issues = entity == null ? headers.GetIssues() : entity.GetIssues();
        return new EmailEntityParseResult(entity, issues, input.Length, input.Length, complete);
    }

    static MimeEntity BuildEntity(EmailHeaders headers, EmailMediaType? parentMediaType, EmailContent source, EmailRange entityRange, EmailRange contentRange, EmailParseOptions options, EmailMimeParseState state, int depth, EmailIssueList initialIssues)
    {
        if (!state.EnsureDepth(depth, initialIssues, entityRange.GetStartOffset()))
        {
            return CreateOpaqueEntity(headers, source, entityRange, contentRange, initialIssues);
        }

        EmailIssueList ownIssues = new EmailIssueList();
        ownIssues.AddRange(initialIssues);
        EmailMediaType mediaType = ApplyMediaTypeCompatibility(GetMediaType(headers, parentMediaType), options, ownIssues);
        EmailDisposition? disposition = ApplyLegacyAttachmentCompatibility(GetDisposition(headers), mediaType, options, ownIssues);
        EmailMessageId? contentId = GetContentId(headers);
        EmailContentLanguage? language = GetContentLanguage(headers);
        string? location = GetText(headers, "Content-Location", false);
        string? description = GetText(headers, "Content-Description", true);
        string? transferEncoding = GetTransferEncoding(headers);
        if (transferEncoding != null && !IsToken(transferEncoding))
        {
            EmailIssueSeverity severity = options.GetMode() == EmailParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
            ownIssues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.TransferEncodingMalformed, "Content-Transfer-Encoding is not a single MIME token.", null, null, "Content-Transfer-Encoding", "The malformed transfer encoding was retained in the raw header and the entity uses the default transfer encoding."));
            transferEncoding = null;
        }

        MimeEntityKind kind = GetKind(mediaType, options, ownIssues);
        EmailContent? rawContent = null;
        EmailContent? decodedContent = null;
        EmailBodyIngestMetadata? bodyIngestMetadata = null;
        EmailPartialMessageMetadata? partialMessageMetadata = null;
        EmailExternalBodyMetadata? externalBodyMetadata = null;
        if (kind == MimeEntityKind.PartialMessage)
        {
            partialMessageMetadata = EmailSpecializedMessageMetadataParser.ParsePartial(mediaType, transferEncoding, options, ownIssues);
        }
        else if (kind == MimeEntityKind.ExternalBody)
        {
            externalBodyMetadata = EmailSpecializedMessageMetadataParser.ParseExternal(mediaType, transferEncoding, source, contentRange, options, ownIssues);
            if (!externalBodyMetadata.AreReferencedHeadersComplete())
            {
                state.MarkIncomplete();
            }
        }

        if (!IsContainer(kind) && kind != MimeEntityKind.Message)
        {
            EmailBodyIngestResult ingest = EmailBodyIngestor.Ingest(source, contentRange, transferEncoding, options, state);
            rawContent = ingest.GetRawContent();
            decodedContent = ingest.GetDecodedContent();
            bodyIngestMetadata = ingest.GetMetadata();
            AddRebasedIssues(ownIssues, ingest.GetIssues(), contentRange.GetStartOffset());
        }

        List<MimeEntity> children = new List<MimeEntity>();
        EmailMessage? nestedMessage = null;
        EmailContent? preamble = null;
        EmailContent? epilogue = null;
        if (kind == MimeEntityKind.Message)
        {
            nestedMessage = ParseNestedMessage(source, contentRange, options, state, depth + 1);
        }
        else if (IsContainer(kind))
        {
            EmailMultipartBody multipart = ParseMultipart(headers, kind, mediaType, source, contentRange, options, state, depth, ownIssues);
            children = multipart.GetChildren();
            preamble = multipart.GetPreamble();
            epilogue = multipart.GetEpilogue();
        }

        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndBodyIngestMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(kind, headers, mediaType, disposition, transferEncoding, contentId, location, language, description, partialMessageMetadata, externalBodyMetadata, bodyIngestMetadata, rawContent, decodedContent, children, nestedMessage, preamble, epilogue, entityRange, ownIssues);
        entity.RetainOriginalEntityContent(CreateSlice(source, entityRange.GetStartOffset(), entityRange.GetLength()));
        return entity;
    }

    static EmailMultipartBody ParseMultipart(EmailHeaders headers, MimeEntityKind kind, EmailMediaType mediaType, EmailContent source, EmailRange contentRange, EmailParseOptions options, EmailMimeParseState state, int depth, EmailIssueList ownIssues)
    {
        List<MimeEntity> children = new List<MimeEntity>();
        string? boundary = GetBoundary(mediaType);
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        bool compatibilityEnabled = options.GetMode() != EmailParseMode.Strict;
        bool genericRecovery = compatibilityEnabled && compatibility.GetAllowBrokenMimeBoundaries();
        bool exchangeRecovery = compatibilityEnabled && compatibility.GetAllowExchangeDeviations() && (options.GetExchangeCompatibilityContext() || HasHeaderPrefix(headers, "X-MS-Exchange-"));
        bool gmailRecovery = compatibilityEnabled && compatibility.GetAllowGmailDeviations() && (options.GetGmailCompatibilityContext() || HasHeaderPrefix(headers, "X-Google-") || HasHeaderPrefix(headers, "X-Gm-"));
        bool allowBoundaryRecovery = genericRecovery || exchangeRecovery || gmailRecovery;
        if (!IsUsableBoundary(boundary, allowBoundaryRecovery))
        {
            state.MarkIncomplete();
            AddMalformedIssue(ownIssues, options, "The multipart Content-Type has no usable boundary parameter.", contentRange.GetStartOffset(), "The body was retained as preamble and no delimiter split was attempted.");
            EmailContent unparsed = CreateSlice(source, contentRange.GetStartOffset(), contentRange.GetLength());
            NormalizeFixedArity(kind, children, ownIssues, options, state, contentRange.GetStartOffset());
            return new EmailMultipartBody(children, unparsed, null);
        }

        byte[] body = MaterializeRange(source, contentRange, options);
        EmailParseOptions scannerOptions = options.Clone();
        EmailCompatibilityOptions scannerCompatibility = scannerOptions.GetCompatibilityOptions();
        scannerCompatibility.SetAllowBrokenMimeBoundaries(allowBoundaryRecovery);
        scannerOptions.SetCompatibilityOptions(scannerCompatibility);
        EmailBoundaryScanner scanner = new EmailBoundaryScannerOperations().CreateWithBoundaryAndOptions(boundary, scannerOptions);
        scanner.Feed(body);
        EmailBoundaryScanResult scan = scanner.Complete();
        AddRebasedIssues(ownIssues, scan.GetIssues(), contentRange.GetStartOffset());
        AddBoundaryCompatibilityIssue(ownIssues, scan, genericRecovery, exchangeRecovery, gmailRecovery, contentRange.GetStartOffset());
        if (!scan.IsComplete())
        {
            state.MarkIncomplete();
        }

        EmailContent preamble = CreatePreamble(source, contentRange, scan);
        EmailContent? epilogue = CreateEpilogue(source, contentRange, scan);
        int delimiterIndex = 0;
        while (delimiterIndex < scan.GetDelimiterCount())
        {
            EmailBoundaryDelimiter current = scan.GetDelimiterAt(delimiterIndex);
            if (current.IsClosing())
            {
                break;
            }

            long childStart = current.GetSourceRange().GetEndOffsetExclusive();
            long childEnd = body.Length;
            if (delimiterIndex + 1 < scan.GetDelimiterCount())
            {
                EmailBoundaryDelimiter next = scan.GetDelimiterAt(delimiterIndex + 1);
                childEnd = next.GetSourceRange().GetStartOffset() - next.GetPrecedingLineEndingByteCount();
            }

            if (childEnd < childStart)
            {
                childEnd = childStart;
                state.MarkIncomplete();
                AddMalformedIssue(ownIssues, options, "Adjacent multipart delimiters overlap the preceding part terminator.", contentRange.GetStartOffset() + childStart, "An empty child part was retained.");
            }

            if (childEnd > body.Length)
            {
                childEnd = body.Length;
            }

            long childOffset = contentRange.GetStartOffset() + childStart;
            if (!state.AddPart(ownIssues, childOffset))
            {
                break;
            }

            MimeEntity child = ParseChild(body, childStart, childEnd, source, contentRange, mediaType, options, state, depth + 1);
            children.Add(child);
            delimiterIndex++;
        }

        if (children.Count == 0 && scan.HasClosingBoundary())
        {
            state.MarkIncomplete();
            long delimiterOffset = contentRange.GetStartOffset() + scan.GetDelimiterAt(0).GetSourceRange().GetStartOffset();
            AddMalformedIssue(ownIssues, options, "The multipart closing delimiter appeared before any child part.", delimiterOffset, "The empty multipart container, preamble, and epilogue were retained.");
        }

        NormalizeFixedArity(kind, children, ownIssues, options, state, contentRange.GetStartOffset());
        return new EmailMultipartBody(children, preamble, epilogue);
    }

    static MimeEntity ParseChild(byte[] multipartBody, long relativeStart, long relativeEnd, EmailContent source, EmailRange multipartContentRange, EmailMediaType parentMediaType, EmailParseOptions options, EmailMimeParseState state, int depth)
    {
        int start = (int)relativeStart;
        int length = (int)(relativeEnd - relativeStart);
        byte[] childBytes = new ReadOnlySpan<byte>(multipartBody, start, length).ToArray();
        long absoluteStart = multipartContentRange.GetStartOffset() + relativeStart;
        EmailHeaderBlockParserOptions headerOptions = CreateHeaderOptions(options);
        EmailHeaderBlockParseResult headerResult = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(childBytes, headerOptions, new EmailSourcePosition(absoluteStart, 1, 1), true);
        bool headerComplete = headerResult.IsComplete();
        if (!headerComplete)
        {
            state.MarkIncomplete();
        }

        long bodyStart = headerComplete ? headerResult.GetBodyStartOffset() : headerResult.GetConsumedByteCount();
        EmailRange entityRange = new EmailRange(absoluteStart, length);
        EmailRange contentRange = new EmailRange(absoluteStart + bodyStart, length - bodyStart);
        EmailIssueList initialIssues = headerComplete ? new EmailIssueList() : headerResult.GetIssues();
        return BuildEntity(headerResult.GetHeaders(), parentMediaType, source, entityRange, contentRange, options, state, depth, initialIssues);
    }

    static EmailMessage ParseNestedMessage(EmailContent source, EmailRange contentRange, EmailParseOptions options, EmailMimeParseState state, int depth)
    {
        EmailContent nestedSource = CreateSlice(source, contentRange.GetStartOffset(), contentRange.GetLength());
        EmailIssueList issues = new EmailIssueList();
        if (!state.EnsureDepth(depth, issues, contentRange.GetStartOffset()))
        {
            EmailRange partialRange = new EmailRange(0, contentRange.GetLength());
            EmailRange emptyHeaderRange = new EmailRange(0, 0);
            EmailSourceInfo partialSourceInfo = new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(nestedSource, partialRange, emptyHeaderRange, null, null, null, false);
            return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(new EmailHeadersOperations().CreateWith(), null, new EmailEnvelopeOperations().CreateWith(), partialSourceInfo, issues);
        }

        byte[] input = MaterializeRange(source, contentRange, options);
        EmailHeaderBlockParserOptions headerOptions = CreateHeaderOptions(options);
        EmailHeaderBlockParseResult headerResult = new EmailHeaderBlockParser().ParseWithInputAndOptionsAndInitialPositionAndEndOfInput(input, headerOptions, new EmailSourcePosition(0, 1, 1), true);
        bool headerComplete = headerResult.IsComplete();
        if (!headerComplete)
        {
            state.MarkIncomplete();
        }

        EmailSourceInfo sourceInfo = CreateNestedSourceInfo(nestedSource, input.Length, headerResult, headerComplete);
        MimeEntity? body = null;
        issues = headerComplete ? new EmailIssueList() : headerResult.GetIssues();
        if (headerComplete)
        {
            long bodyStart = headerResult.GetBodyStartOffset();
            EmailRange bodyRange = new EmailRange(bodyStart, input.Length - bodyStart);
            EmailHeaders contentHeaders = GetContentHeaders(headerResult.GetHeaders());
            if (state.AddPart(issues, contentRange.GetStartOffset() + bodyStart))
            {
                EmailParseOptions bodyOptions = options.Clone();
                bodyOptions.SetExchangeCompatibilityContext(options.GetExchangeCompatibilityContext() || HasHeaderPrefix(headerResult.GetHeaders(), "X-MS-Exchange-"));
                bodyOptions.SetGmailCompatibilityContext(options.GetGmailCompatibilityContext() || HasHeaderPrefix(headerResult.GetHeaders(), "X-Google-") || HasHeaderPrefix(headerResult.GetHeaders(), "X-Gm-"));
                body = BuildEntity(contentHeaders, null, nestedSource, bodyRange, bodyRange, bodyOptions, state, depth, new EmailIssueList());
            }
        }

        return new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headerResult.GetHeaders(), body, new EmailEnvelopeOperations().CreateWith(), sourceInfo, issues);
    }

    static EmailSourceInfo CreateNestedSourceInfo(EmailContent source, long sourceByteCount, EmailHeaderBlockParseResult headerResult, bool complete)
    {
        EmailRange messageRange = new EmailRange(0, sourceByteCount);
        if (!complete)
        {
            EmailRange incompleteHeaderRange = new EmailRange(0, headerResult.GetConsumedByteCount());
            return new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(source, messageRange, incompleteHeaderRange, null, null, null, false);
        }

        long bodyStart = headerResult.GetBodyStartOffset();
        int separatorLength = headerResult.HasSeparator() ? GetLineEndingLength(headerResult.GetSeparatorLineEnding()) : 0;
        EmailRange headerRange = new EmailRange(0, bodyStart - separatorLength);
        EmailRange? separatorRange = separatorLength == 0 ? null : new EmailRange(bodyStart - separatorLength, separatorLength);
        EmailRange bodyRange = new EmailRange(bodyStart, sourceByteCount - bodyStart);
        return new EmailSourceInfoOperations().CreateWithContentAndMessageRangeAndHeaderBlockRangeAndSeparatorRangeAndBodyRangeAndSourceNameAndComplete(source, messageRange, headerRange, separatorRange, bodyRange, null, true);
    }

    static EmailHeaders GetContentHeaders(EmailHeaders headers)
    {
        List<EmailHeader> contentHeaders = new List<EmailHeader>();
        int index = 0;
        while (index < headers.GetCount())
        {
            EmailHeader header = headers.GetAt(index);
            if (header.GetName().StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
            {
                contentHeaders.Add(header.Clone());
            }

            index++;
        }

        return new EmailHeadersOperations().CreateWithPreservedHeaders(contentHeaders);
    }

    static int GetLineEndingLength(EmailLineEnding ending)
    {
        return ending == EmailLineEnding.CrLf ? 2 : 1;
    }

    static EmailHeaderBlockParserOptions CreateHeaderOptions(EmailParseOptions options)
    {
        EmailHeaderBlockParserOptions headerOptions = new EmailHeaderBlockParserOptions();
        headerOptions.SetLimits(options.GetLimits().GetParsingLimits());
        headerOptions.SetMode(options.GetMode() == EmailParseMode.Strict ? EmailStructuredValueParseMode.Strict : EmailStructuredValueParseMode.Tolerant);
        headerOptions.SetCompatibilityOptions(options.GetCompatibilityOptions());
        return headerOptions;
    }

    static EmailContent CreatePreamble(EmailContent source, EmailRange contentRange, EmailBoundaryScanResult scan)
    {
        long length = contentRange.GetLength();
        if (scan.GetDelimiterCount() > 0)
        {
            EmailBoundaryDelimiter first = scan.GetDelimiterAt(0);
            length = first.GetSourceRange().GetStartOffset() - first.GetPrecedingLineEndingByteCount();
            if (length < 0)
            {
                length = 0;
            }
        }

        return CreateSlice(source, contentRange.GetStartOffset(), length);
    }

    static EmailContent? CreateEpilogue(EmailContent source, EmailRange contentRange, EmailBoundaryScanResult scan)
    {
        int index = 0;
        while (index < scan.GetDelimiterCount())
        {
            EmailBoundaryDelimiter delimiter = scan.GetDelimiterAt(index);
            if (delimiter.IsClosing())
            {
                long relativeStart = delimiter.GetSourceRange().GetEndOffsetExclusive();
                return CreateSlice(source, contentRange.GetStartOffset() + relativeStart, contentRange.GetLength() - relativeStart);
            }

            index++;
        }

        return null;
    }

    static EmailContent CreateSlice(EmailContent source, long start, long length)
    {
        return new EmailContentOperations().CreateSlice(source, start, length);
    }

    static byte[] MaterializeRange(EmailContent source, EmailRange range, EmailParseOptions options)
    {
        long maximum = options.GetLimits().GetParsingLimits().GetMaxMessageBytes();
        if (maximum > int.MaxValue)
        {
            maximum = int.MaxValue;
        }

        EmailContent slice = CreateSlice(source, range.GetStartOffset(), range.GetLength());
        return slice.Materialize(maximum);
    }

    static string? GetBoundary(EmailMediaType mediaType)
    {
        EmailRfc2231DecodedParameter? decoded = mediaType.GetParameters().GetDecoded("boundary");
        if (decoded != null)
        {
            return Encoding.Latin1.GetString(decoded.GetDecodedBytes());
        }

        EmailParameter? physical = mediaType.GetParameters().GetFirst("boundary");
        return physical == null ? null : Encoding.Latin1.GetString(physical.GetValueBytes());
    }

    static bool IsUsableBoundary(string? value, bool allowBroken)
    {
        if (value == null)
        {
            return false;
        }

        bool leading = value.Length > 0 && value[0] == '"';
        bool trailing = value.Length > 0 && value[value.Length - 1] == '"';
        if ((leading || trailing) && !allowBroken && leading != trailing)
        {
            return false;
        }

        int start = leading ? 1 : 0;
        int length = value.Length - start - (trailing ? 1 : 0);
        if (length < 1 || length > 70)
        {
            return false;
        }

        int index = start;
        int end = start + length;
        while (index < end)
        {
            char current = value[index];
            bool alphaNumeric = current >= '0' && current <= '9' || current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z';
            bool punctuation = current == '\'' || current == '(' || current == ')' || current == '+' || current == '_' || current == ',' || current == '-' || current == '.' || current == '/' || current == ':' || current == '=' || current == '?' || current == ' ';
            if (!alphaNumeric && !punctuation || index + 1 == end && current == ' ')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static void NormalizeFixedArity(MimeEntityKind kind, List<MimeEntity> children, EmailIssueList issues, EmailParseOptions options, EmailMimeParseState state, long offset)
    {
        if (kind != MimeEntityKind.Signed && kind != MimeEntityKind.Encrypted)
        {
            return;
        }

        if (children.Count != 2)
        {
            state.MarkIncomplete();
            AddMalformedIssue(issues, options, "A signed or encrypted multipart did not contain exactly two child parts.", offset, "Missing children were represented by empty placeholders and excess children were omitted.");
        }

        while (children.Count > 2)
        {
            children.RemoveAt(children.Count - 1);
        }

        while (children.Count < 2)
        {
            children.Add(CreatePlaceholder());
        }
    }

    static void AddMalformedIssue(EmailIssueList issues, EmailParseOptions options, string message, long offset, string recovery)
    {
        EmailIssueSeverity severity = options.GetMode() == EmailParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.MimeStructureMalformed, message, new EmailSourcePosition(offset, 1, 1), null, "Content-Type", recovery));
    }

    static void AddRebasedIssues(EmailIssueList destination, EmailIssueList source, long baseOffset)
    {
        int index = 0;
        while (index < source.GetCount())
        {
            EmailIssue issue = source.GetAt(index);
            EmailSourcePosition? position = null;
            if (issue.HasSourcePosition())
            {
                EmailSourcePosition? original = issue.GetSourcePosition();
                position = new EmailSourcePosition(baseOffset + (original ?? throw new InvalidOperationException()).GetByteOffset(), original.GetLineNumber(), original.GetColumnNumber());
            }

            EmailPath? path = issue.HasEntityPath() ? issue.GetEntityPath() : null;
            string? fieldName = issue.HasFieldName() ? issue.GetFieldName() : null;
            string? recovery = issue.HasRecoveryDescription() ? issue.GetRecoveryDescription() : null;
            destination.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(issue.GetSeverity(), issue.GetCode(), issue.GetDescription(), position, path, fieldName, recovery));
            index++;
        }
    }

    static EmailMediaType GetMediaType(EmailHeaders headers, EmailMediaType? parentMediaType)
    {
        EmailHeader? header = headers.GetFirst("Content-Type");
        if (header == null)
        {
            return new EmailMimeDefaults().CreateDefaultMediaTypeWithParentMediaType(parentMediaType);
        }

        object? value = header.GetStructuredValue();
        return value == null ? new EmailMimeDefaults().CreateDefaultMediaTypeWithParentMediaType(parentMediaType) : (EmailMediaType)value;
    }

    static EmailMediaType ApplyMediaTypeCompatibility(EmailMediaType mediaType, EmailParseOptions options, EmailIssueList issues)
    {
        EmailCompatibilityOptions compatibility = options.GetCompatibilityOptions();
        bool compatibilityEnabled = options.GetMode() != EmailParseMode.Strict;
        if (mediaType.IsType("application", "x-tnef") && (!compatibilityEnabled || !compatibility.GetAllowOutlookDeviations()))
        {
            return new EmailMediaType("application", "octet-stream", mediaType.GetParameters(), mediaType.GetRawBytes(), mediaType.GetSourcePosition(), mediaType.WasDefaulted());
        }

        if (mediaType.IsType("application", "vnd.ms-tnef") && (!compatibilityEnabled || !compatibility.GetAllowExchangeDeviations()))
        {
            return new EmailMediaType("application", "octet-stream", mediaType.GetParameters(), mediaType.GetRawBytes(), mediaType.GetSourcePosition(), mediaType.WasDefaulted());
        }

        if (!compatibilityEnabled || !compatibility.GetAllowCommonMimeTypeAliases())
        {
            return mediaType;
        }

        string? type = null;
        string? subtype = null;
        if (mediaType.IsType("image", "jpg"))
        {
            type = "image";
            subtype = "jpeg";
        }
        else if (mediaType.IsType("application", "x-pdf"))
        {
            type = "application";
            subtype = "pdf";
        }
        else if (mediaType.IsType("application", "x-gzip"))
        {
            type = "application";
            subtype = "gzip";
        }
        else if (mediaType.IsType("audio", "x-wav"))
        {
            type = "audio";
            subtype = "wav";
        }
        else if (mediaType.IsType("text", "x-vcard"))
        {
            type = "text";
            subtype = "vcard";
        }

        if (type == null || subtype == null)
        {
            return mediaType;
        }

        issues.Add(CreateCompatibilityIssue(EmailIssueCode.MimeTypeAliasRecovery, "A common nonstandard MIME type alias was normalized.", "Content-Type", "The effective media type uses the registered canonical MIME type while the raw header remains unchanged."));
        return new EmailMediaType(type, subtype, mediaType.GetParameters(), mediaType.GetRawBytes(), mediaType.GetSourcePosition(), mediaType.WasDefaulted());
    }

    static EmailDisposition? ApplyLegacyAttachmentCompatibility(EmailDisposition? disposition, EmailMediaType mediaType, EmailParseOptions options, EmailIssueList issues)
    {
        if (disposition != null || options.GetMode() == EmailParseMode.Strict || !options.GetCompatibilityOptions().GetAllowLegacyAttachmentRecovery() || !mediaType.IsType("application", "octet-stream"))
        {
            return disposition;
        }

        EmailRfc2231DecodedParameter? decodedName = mediaType.GetParameters().GetDecoded("name");
        EmailParameter? physicalName = mediaType.GetParameters().GetFirst("name");
        if (decodedName == null && physicalName == null)
        {
            return null;
        }

        issues.Add(CreateCompatibilityIssue(EmailIssueCode.LegacyAttachmentRecovery, "A legacy octet-stream name parameter was treated as attachment metadata.", "Content-Type", "The entity has an effective attachment disposition while the original Content-Type header remains unchanged."));
        return new EmailDisposition("attachment", mediaType.GetParameters(), new byte[0], mediaType.GetSourcePosition());
    }

    static EmailDisposition? GetDisposition(EmailHeaders headers)
    {
        EmailHeader? header = headers.GetFirst("Content-Disposition");
        if (header == null)
        {
            return null;
        }

        return (EmailDisposition? )header.GetStructuredValue();
    }

    static EmailMessageId? GetContentId(EmailHeaders headers)
    {
        EmailHeader? header = headers.GetFirst("Content-ID");
        if (header == null)
        {
            return null;
        }

        return (EmailMessageId? )header.GetStructuredValue();
    }

    static EmailContentLanguage? GetContentLanguage(EmailHeaders headers)
    {
        EmailHeader? header = headers.GetFirst("Content-Language");
        if (header == null)
        {
            return null;
        }

        return (EmailContentLanguage? )header.GetStructuredValue();
    }

    static string? GetText(EmailHeaders headers, string name, bool decoded)
    {
        EmailHeader? header = headers.GetFirst(name);
        if (header == null)
        {
            return null;
        }

        string? value = decoded ? header.GetDecodedValue() : Encoding.UTF8.GetString(header.GetUnfoldedValue()).Trim();
        return (value ?? throw new InvalidOperationException()).Length == 0 ? null : value;
    }

    static string? GetTransferEncoding(EmailHeaders headers)
    {
        EmailHeader? header = headers.GetFirst("Content-Transfer-Encoding");
        if (header == null)
        {
            return null;
        }

        string value = Encoding.ASCII.GetString(header.GetUnfoldedValue()).Trim();
        return value.Length == 0 ? null : value;
    }

    static MimeEntityKind GetKind(EmailMediaType mediaType, EmailParseOptions options, EmailIssueList issues)
    {
        if (string.Equals(mediaType.GetTopLevelType(), "multipart", StringComparison.OrdinalIgnoreCase))
        {
            if (mediaType.IsType("multipart", "signed"))
            {
                return MimeEntityKind.Signed;
            }

            if (mediaType.IsType("multipart", "encrypted"))
            {
                return MimeEntityKind.Encrypted;
            }

            return MimeEntityKind.Multipart;
        }

        if (mediaType.IsType("message", "rfc822") || mediaType.IsType("message", "global"))
        {
            return MimeEntityKind.Message;
        }

        if (mediaType.IsType("message", "partial"))
        {
            return MimeEntityKind.PartialMessage;
        }

        if (mediaType.IsType("message", "external-body"))
        {
            return MimeEntityKind.ExternalBody;
        }

        if (mediaType.IsType("message", "delivery-status") || mediaType.IsType("message", "global-delivery-status"))
        {
            return MimeEntityKind.DeliveryStatus;
        }

        if (mediaType.IsType("message", "disposition-notification") || mediaType.IsType("message", "global-disposition-notification"))
        {
            return MimeEntityKind.DispositionNotification;
        }

        if (mediaType.IsType("message", "feedback-report"))
        {
            return MimeEntityKind.FeedbackReport;
        }

        if (mediaType.IsType("text", "calendar"))
        {
            return MimeEntityKind.Calendar;
        }

        if (mediaType.IsType("application", "ms-tnef"))
        {
            return MimeEntityKind.Tnef;
        }

        if (mediaType.IsType("application", "x-tnef") && options.GetMode() != EmailParseMode.Strict && options.GetCompatibilityOptions().GetAllowOutlookDeviations())
        {
            issues.Add(CreateCompatibilityIssue(EmailIssueCode.OutlookCompatibilityRecovery, "An Outlook TNEF MIME type alias was recognized.", "Content-Type", "The entity was classified as TNEF while the original media type remains available in the header."));
            return MimeEntityKind.Tnef;
        }

        if (mediaType.IsType("application", "vnd.ms-tnef") && options.GetMode() != EmailParseMode.Strict && options.GetCompatibilityOptions().GetAllowExchangeDeviations())
        {
            issues.Add(CreateCompatibilityIssue(EmailIssueCode.ExchangeCompatibilityRecovery, "An Exchange TNEF MIME type alias was recognized.", "Content-Type", "The entity was classified as TNEF while the original media type remains available in the header."));
            return MimeEntityKind.Tnef;
        }

        return MimeEntityKind.Leaf;
    }

    static bool HasHeaderPrefix(EmailHeaders headers, string prefix)
    {
        int index = 0;
        while (index < headers.GetCount())
        {
            if (headers.GetAt(index).GetName().StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    static void AddBoundaryCompatibilityIssue(EmailIssueList issues, EmailBoundaryScanResult scan, bool genericRecovery, bool exchangeRecovery, bool gmailRecovery, long offset)
    {
        bool recovered = false;
        int index = 0;
        while (index < scan.GetDelimiterCount())
        {
            if (scan.GetDelimiterAt(index).WasRecovered())
            {
                recovered = true;
            }

            index++;
        }

        if (!recovered)
        {
            return;
        }

        EmailIssueCode code = EmailIssueCode.MimeBoundaryCompatibilityRecovery;
        string description = "A malformed MIME boundary was accepted by an explicitly enabled compatibility rule.";
        if (!genericRecovery && exchangeRecovery)
        {
            code = EmailIssueCode.ExchangeCompatibilityRecovery;
            description = "An Exchange MIME boundary deviation was accepted by an explicitly enabled compatibility rule.";
        }
        else if (!genericRecovery && gmailRecovery)
        {
            code = EmailIssueCode.GmailCompatibilityRecovery;
            description = "A Gmail MIME boundary deviation was accepted by an explicitly enabled compatibility rule.";
        }

        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, code, description, new EmailSourcePosition(offset, 1, 1), null, "Content-Type", "The recovered delimiter ranges are retained and marked as noncanonical."));
    }

    static EmailIssue CreateCompatibilityIssue(EmailIssueCode code, string description, string fieldName, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, code, description, null, null, fieldName, recovery);
    }

    static bool IsContainer(MimeEntityKind kind)
    {
        return kind == MimeEntityKind.Multipart || kind == MimeEntityKind.Signed || kind == MimeEntityKind.Encrypted;
    }

    static MimeEntity CreatePlaceholder()
    {
        EmailContent empty = new EmailContentOperations().CreateMemory(new byte[0], EmailContentOwnership.Borrowed, false);
        return new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndContentAndChildrenAndNestedMessage(MimeEntityKind.Leaf, new EmailHeadersOperations().CreateWith(), new EmailMimeDefaults().CreateDefaultMediaTypeWith(), empty, new List<MimeEntity>(), null);
    }

    static MimeEntity CreateOpaqueEntity(EmailHeaders headers, EmailContent source, EmailRange entityRange, EmailRange contentRange, EmailIssueList issues)
    {
        EmailContent raw = CreateSlice(source, contentRange.GetStartOffset(), contentRange.GetLength());
        MimeEntity entity = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(MimeEntityKind.Leaf, headers, new EmailMimeDefaults().CreateDefaultMediaTypeWith(), null, null, null, null, null, null, raw, null, new List<MimeEntity>(), null, null, null, entityRange, issues);
        entity.RetainOriginalEntityContent(CreateSlice(source, entityRange.GetStartOffset(), entityRange.GetLength()));
        return entity;
    }

    static bool IsToken(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == '(' || current == ')' || current == '<' || current == '>' || current == '@' || current == ',' || current == ';' || current == ':' || current == '\\' || current == '"' || current == '/' || current == '[' || current == ']' || current == '?' || current == '=')
            {
                return false;
            }

            index++;
        }

        return value.Length > 0;
    }

    static void EnsureLength(long length, EmailParseOptions options)
    {
        long maximum = options.GetLimits().GetParsingLimits().GetMaxMessageBytes();
        if (length > maximum)
        {
            throw new EmailLimitException("MaxMessageBytes", maximum, length);
        }
    }
}
