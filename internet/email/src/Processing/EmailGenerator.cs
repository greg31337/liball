using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace email;
internal class EmailGeneratorWorker
{
    EmailGenerationOptions options;
    List<EmailGeneratedValue> generatedValues;
    HashSet<string> usedIdentifiers;
    HashSet<string> usedBoundaries;
    int identifierCounter;
    int boundaryCounter;

    internal EmailGeneratorWorker(EmailGenerationOptions options)
    {
        this.options = options;
        generatedValues = new List<EmailGeneratedValue>();
        usedIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        usedBoundaries = new HashSet<string>(StringComparer.Ordinal);
    }

    internal List<EmailGeneratedValue> GetGeneratedValues()
    {
        return new List<EmailGeneratedValue>(generatedValues);
    }

    internal EmailMessage GenerateMessage(EmailMessage source)
    {
        if (source.HasBody())
        {
            new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(source.GetBody(), options.GetMaximumDepth(), options.GetMaximumEntityCount());
        }

        CollectMessageIdentifiers(source);
        EmailMessage generated = GenerateMessageCore(source, new EmailPathOperations().CreateRoot(), false);
        return generatedValues.Count == 0 ? source.Clone() : generated;
    }

    internal MimeEntity GenerateEntity(MimeEntity source)
    {
        new EmailTraversal().DepthFirstWithRootAndMaximumDepthAndMaximumEntityCount(source, options.GetMaximumDepth(), options.GetMaximumEntityCount());
        CollectEntityIdentifiers(source);
        MimeEntity generated = GenerateEntityCore(source, new EmailPathOperations().CreateRoot(), 0);
        return generatedValues.Count == 0 ? source.Clone() : generated;
    }

    EmailMessage GenerateMessageCore(EmailMessage? source, EmailPath path, bool nested)
    {
        int initialGenerated = generatedValues.Count;
        EmailHeaders headers = ((source ?? throw new InvalidOperationException()).GetHeaders() ?? throw new InvalidOperationException()).Clone();
        if (options.GetGenerateDate() && !headers.Contains("Date"))
        {
            EmailDateTime? date = options.HasGenerationDate() ? options.GetGenerationDate() : options.GetDeterministicOutput() ? CreateDeterministicDate() : CreateCurrentDate();
            string value = Encoding.ASCII.GetString(new EmailDateFormatter().FormatWithValue(date).GetBytes());
            headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Date", Encoding.ASCII.GetBytes(value), EmailHeaderValueKind.Date));
            generatedValues.Add(new EmailGeneratedValue(EmailGenerationKind.Date, path, "Date", value));
        }

        if (options.GetGenerateMessageId() && !headers.Contains("Message-ID"))
        {
            EmailMessageId identifier = CreateIdentifier("message", path);
            string value = FormatIdentifier(identifier);
            headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Message-ID", Encoding.ASCII.GetBytes(value), EmailHeaderValueKind.MessageId));
            generatedValues.Add(new EmailGeneratedValue(EmailGenerationKind.MessageId, path, "Message-ID", value));
        }

        if (options.GetGenerateMimeVersion() && !headers.Contains("MIME-Version"))
        {
            headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValue("MIME-Version", Encoding.ASCII.GetBytes("1.0")));
            generatedValues.Add(new EmailGeneratedValue(EmailGenerationKind.MimeVersion, path, "MIME-Version", "1.0"));
        }

        EmailPath bodyPath = nested ? path.Append(0) : path;
        MimeEntity? body = source.HasBody() ? GenerateEntityCore(source.GetBody(), bodyPath, bodyPath.GetDepth()) : null;
        if (body != null)
        {
            SynchronizeContentHeaders(headers, body.GetHeaders());
        }

        EmailMessage generated = new EmailMessageOperations().CreateWithHeadersAndBodyAndEnvelopeAndSourceInfoAndIssues(headers, body, (source.GetEnvelope() ?? throw new InvalidOperationException()).Clone(), null, source.GetOwnIssues());
        if (generatedValues.Count > initialGenerated)
        {
            generated.MarkDirty();
        }

        return generated;
    }

    MimeEntity GenerateEntityCore(MimeEntity? source, EmailPath path, int depth)
    {
        if (depth > options.GetMaximumDepth())
        {
            throw new EmailLimitException("MaxTraversalDepth", options.GetMaximumDepth(), depth);
        }

        int initialGenerated = generatedValues.Count;
        EmailHeaders headers = ((source ?? throw new InvalidOperationException()).GetHeaders() ?? throw new InvalidOperationException()).Clone();
        EmailMediaType? mediaType = source.GetMediaType();
        EmailMessageId? contentId = source.HasContentId() ? source.GetContentId() : null;
        List<MimeEntity> children = new List<MimeEntity>();
        int childIndex = 0;
        while (childIndex < source.GetChildCount())
        {
            children.Add(GenerateEntityCore(source.GetChildAt(childIndex), path.Append(childIndex), depth + 1));
            childIndex++;
        }

        EmailMessage? nested = source.HasNestedMessage() ? GenerateMessageCore(source.GetNestedMessage(), path, true) : null;
        if (options.GetGenerateContentIds() && contentId == null && !headers.Contains("Content-ID"))
        {
            contentId = CreateIdentifier("content", path);
            string value = FormatIdentifier(contentId);
            headers.Add(new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind("Content-ID", Encoding.ASCII.GetBytes(value), EmailHeaderValueKind.ContentId));
            generatedValues.Add(new EmailGeneratedValue(EmailGenerationKind.ContentId, path, "Content-ID", value));
        }

        if (options.GetGenerateBoundaries() && source.IsContainer())
        {
            EmailParameter? boundary = (mediaType ?? throw new InvalidOperationException()).GetParameters().GetFirst("boundary");
            string? current = boundary == null ? null : Encoding.ASCII.GetString(boundary.GetValueBytes());
            if (!IsBoundaryValid(current))
            {
                string generatedBoundary = GenerateBoundary(source, path);
                mediaType = ReplaceBoundary(mediaType, generatedBoundary);
                ReplaceHeader(headers, "Content-Type", FormatMediaType(mediaType), EmailHeaderValueKind.MediaType);
                generatedValues.Add(new EmailGeneratedValue(EmailGenerationKind.Boundary, path, "Content-Type", generatedBoundary));
            }
            else
            {
                usedBoundaries.Add((current ?? throw new InvalidOperationException()));
            }
        }

        MimeEntity generated = new MimeEntityOperations().CreateWithKindAndHeadersAndMediaTypeAndDispositionAndTransferEncodingNameAndContentIdAndContentLocationAndContentLanguageAndContentDescriptionAndPartialMessageMetadataAndExternalBodyMetadataAndBodyIngestMetadataAndRawContentAndDecodedContentAndChildrenAndNestedMessageAndPreambleAndEpilogueAndSourceRangeAndIssues(source.GetKind(), headers, mediaType, source.HasDisposition() ? source.GetDisposition() : null, source.HasTransferEncoding() ? source.GetTransferEncodingName() : null, contentId, source.HasContentLocation() ? source.GetContentLocation() : null, source.HasContentLanguage() ? source.GetContentLanguage() : null, source.HasContentDescription() ? source.GetContentDescription() : null, source.HasPartialMessageMetadata() ? source.GetPartialMessageMetadata() : null, source.HasExternalBodyMetadata() ? source.GetExternalBodyMetadata() : null, source.HasBodyIngestMetadata() ? source.GetBodyIngestMetadata() : null, source.HasRawContent() ? source.GetRawContent() : null, source.HasDecodedContent() ? source.GetDecodedContent() : null, children, nested, source.HasPreamble() ? source.GetPreamble() : null, source.HasEpilogue() ? source.GetEpilogue() : null, source.HasSourceRange() ? source.GetSourceRange() : null, source.GetOwnIssues());
        if (generatedValues.Count > initialGenerated)
        {
            generated.MarkDirty();
        }

        return generated;
    }

    EmailMessageId CreateIdentifier(string purpose, EmailPath path)
    {
        int attempt = 0;
        while (attempt < 64)
        {
            byte[] random = CreateIdentifierBytes(purpose, path, identifierCounter, attempt);
            string left = "email." + ToHex(random);
            string key = left + "@" + options.GetIdentifierDomain();
            if (usedIdentifiers.Add(key))
            {
                identifierCounter++;
                return new EmailMessageId(left, options.GetIdentifierDomain(), false, new byte[0], new EmailSourcePosition(0, 1, 1), false, false);
            }

            attempt++;
        }

        throw new InvalidOperationException("A unique identifier could not be generated.");
    }

    byte[] CreateIdentifierBytes(string purpose, EmailPath path, int counter, int attempt)
    {
        if (!options.GetDeterministicOutput())
        {
            byte[] random = new byte[18];
            RandomNumberGenerator.Fill(random);
            return random;
        }

        byte[] seed = options.GetDeterministicSeed();
        byte[] context = Encoding.ASCII.GetBytes(purpose + ":" + path.GetDisplayText() + ":" + counter.ToString(CultureInfo.InvariantCulture) + ":" + attempt.ToString(CultureInfo.InvariantCulture));
        byte[] input = new byte[seed.Length + context.Length];
        seed.CopyTo(input, 0);
        context.CopyTo(input, seed.Length);
        byte[] hash = SHA256.HashData(input);
        byte[] output = new byte[18];
        Array.Copy(hash, output, output.Length);
        return output;
    }

    string GenerateBoundary(MimeEntity entity, EmailPath path)
    {
        List<EmailContent> contents = new List<EmailContent>();
        CollectRepeatableContent(entity, contents);
        int attempt = 0;
        while (attempt < EmailBoundaryGeneratorConstants.MaximumAttempts)
        {
            EmailWriteOptions writeOptions = new EmailWriteOptions();
            writeOptions.SetDeterministicOutput(options.GetDeterministicOutput());
            writeOptions.SetDeterministicSeed(CreateBoundarySeed(path, boundaryCounter, attempt));
            string boundary = new EmailBoundaryGenerator().GenerateWithContentsAndOptions(contents, writeOptions).GetBoundary();
            if (usedBoundaries.Add(boundary))
            {
                boundaryCounter++;
                return boundary;
            }

            attempt++;
        }

        throw new InvalidOperationException("A unique MIME boundary could not be generated.");
    }

    byte[] CreateBoundarySeed(EmailPath path, int counter, int attempt)
    {
        byte[] seed = options.GetDeterministicSeed();
        byte[] context = Encoding.ASCII.GetBytes("boundary:" + path.GetDisplayText() + ":" + counter.ToString(CultureInfo.InvariantCulture) + ":" + attempt.ToString(CultureInfo.InvariantCulture));
        byte[] combined = new byte[seed.Length + context.Length];
        seed.CopyTo(combined, 0);
        context.CopyTo(combined, seed.Length);
        return combined;
    }

    void CollectMessageIdentifiers(EmailMessage? message)
    {
        AddHeaderIdentifier(((message ?? throw new InvalidOperationException()).GetHeaders() ?? throw new InvalidOperationException()).GetFirst("Message-ID"), false);
        if (message.HasBody())
        {
            CollectEntityIdentifiers(message.GetBody());
        }
    }

    void CollectEntityIdentifiers(MimeEntity? entity)
    {
        if ((entity ?? throw new InvalidOperationException()).HasContentId())
        {
            AddUsedIdentifier(entity.GetContentId());
        }
        else
        {
            AddHeaderIdentifier((entity.GetHeaders() ?? throw new InvalidOperationException()).GetFirst("Content-ID"), true);
        }

        int index = 0;
        while (index < entity.GetChildCount())
        {
            CollectEntityIdentifiers(entity.GetChildAt(index));
            index++;
        }

        if (entity.HasNestedMessage())
        {
            CollectMessageIdentifiers(entity.GetNestedMessage());
        }
    }

    void AddHeaderIdentifier(EmailHeader? header, bool content)
    {
        if (header == null)
        {
            return;
        }

        EmailMessageIdParseResult parsed = content ? new EmailMessageIdParser().ParseContentIdWithInputAndMode(header.GetUnfoldedValue(), EmailMessageIdParseMode.Tolerant) : new EmailMessageIdParser().ParseMessageIdWithInputAndMode(header.GetUnfoldedValue(), EmailMessageIdParseMode.Tolerant);
        if (parsed.HasSingleIdentifier())
        {
            AddUsedIdentifier(parsed.GetIdentifier());
        }
    }

    void AddUsedIdentifier(EmailMessageId? identifier)
    {
        usedIdentifiers.Add((identifier ?? throw new InvalidOperationException()).GetIdLeft() + "@" + identifier.GetIdRight());
    }

    static EmailDateTime CreateCurrentDate()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new EmailDateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, true, false, true, (EmailDateDayOfWeek)now.DayOfWeek, true, EmailDateZoneKind.Numeric, "+0000", 0, true, false, 4, new byte[0], new EmailSourcePosition(0, 1, 1));
    }

    static EmailDateTime CreateDeterministicDate()
    {
        return new EmailDateTime(2000, 1, 1, 0, 0, 0, true, false, true, EmailDateDayOfWeek.Saturday, true, EmailDateZoneKind.Numeric, "+0000", 0, true, false, 4, new byte[0], new EmailSourcePosition(0, 1, 1));
    }

    static string FormatIdentifier(EmailMessageId identifier)
    {
        return Encoding.ASCII.GetString(new EmailMessageIdFormatter().FormatWithIdentifier(identifier).GetBytes());
    }

    static string FormatMediaType(EmailMediaType mediaType)
    {
        return Encoding.ASCII.GetString(new EmailValueWriter().WriteMediaTypeWithValue(mediaType).GetBytes());
    }

    static EmailMediaType ReplaceBoundary(EmailMediaType mediaType, string boundary)
    {
        List<EmailParameter> physical = new List<EmailParameter>();
        List<EmailRfc2231DecodedParameter> decoded = new List<EmailRfc2231DecodedParameter>();
        EmailParameterList original = mediaType.GetParameters();
        int index = 0;
        while (index < original.GetCount())
        {
            if (!string.Equals(original.GetAt(index).GetName(), "boundary", StringComparison.OrdinalIgnoreCase))
            {
                physical.Add(original.GetAt(index));
            }

            index++;
        }

        index = 0;
        while (index < original.GetDecodedCount())
        {
            if (!string.Equals(original.GetDecodedAt(index).GetName(), "boundary", StringComparison.OrdinalIgnoreCase))
            {
                decoded.Add(original.GetDecodedAt(index));
            }

            index++;
        }

        byte[] bytes = Encoding.ASCII.GetBytes(boundary);
        EmailSourcePosition position = new EmailSourcePosition(0, 1, 1);
        physical.Add(new EmailParameter("boundary", bytes, bytes, true, new byte[0], position));
        decoded.Add(new EmailRfc2231DecodedParameter("boundary", bytes, null, false, null, false, false, false, 1, new List<EmailRfc2231Parameter>()));
        return new EmailMediaType(mediaType.GetTopLevelType(), mediaType.GetSubtype(), new EmailParameterList(physical, decoded), new byte[0], mediaType.GetSourcePosition(), false);
    }

    static bool IsBoundaryValid(string? value)
    {
        if (value == null || value.Length < 1 || value.Length > 70 || value[value.Length - 1] == ' ')
        {
            return false;
        }

        const string allowed = "'()+_,-./:=? ";
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current > 127 || !char.IsLetterOrDigit(current) && allowed.IndexOf(current) < 0)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static void CollectRepeatableContent(MimeEntity? entity, List<EmailContent> contents)
    {
        if ((entity ?? throw new InvalidOperationException()).HasContent() && (entity.GetContent() ?? throw new InvalidOperationException()).IsRepeatable())
        {
            contents.Add((entity.GetContent() ?? throw new InvalidOperationException()));
        }

        int index = 0;
        while (index < entity.GetChildCount())
        {
            CollectRepeatableContent(entity.GetChildAt(index), contents);
            index++;
        }

        if (entity.HasNestedMessage() && (entity.GetNestedMessage() ?? throw new InvalidOperationException()).HasBody())
        {
            CollectRepeatableContent((entity.GetNestedMessage() ?? throw new InvalidOperationException()).GetBody(), contents);
        }
    }

    static void ReplaceHeader(EmailHeaders headers, string name, string value, EmailHeaderValueKind kind)
    {
        EmailHeader replacement = new EmailHeaderOperations().CreateWithNameAndRawValueAndValueKind(name, Encoding.ASCII.GetBytes(value), kind);
        if (headers.Contains(name))
        {
            headers.ReplaceAll(name, replacement);
        }
        else
        {
            headers.Add(replacement);
        }
    }

    static void SynchronizeContentHeaders(EmailHeaders messageHeaders, EmailHeaders? entityHeaders)
    {
        string[] names = new string[] { "Content-Type", "Content-ID" };
        int index = 0;
        while (index < names.Length)
        {
            List<EmailHeader> replacements = (entityHeaders ?? throw new InvalidOperationException()).GetAllNamed(names[index]);
            if (replacements.Count > 0)
            {
                if (messageHeaders.Contains(names[index]))
                {
                    messageHeaders.ReplaceAllWithHeaders(names[index], replacements);
                }
                else
                {
                    int replacementIndex = 0;
                    while (replacementIndex < replacements.Count)
                    {
                        messageHeaders.Add(replacements[replacementIndex]);
                        replacementIndex++;
                    }
                }
            }

            index++;
        }
    }

    static string ToHex(byte[] value)
    {
        char[] characters = new char[value.Length * 2];
        int index = 0;
        while (index < value.Length)
        {
            int current = value[index];
            characters[index * 2] = Hex(current >> 4);
            characters[index * 2 + 1] = Hex(current & 15);
            index++;
        }

        return new string(characters);
    }

    static char Hex(int value)
    {
        return (char)(value < 10 ? 48 + value : 87 + value);
    }
}
