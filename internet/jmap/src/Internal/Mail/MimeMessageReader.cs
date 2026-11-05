using System;
using System.Collections.Generic;
using System.Text;

namespace jmap;

internal class MimeReaderLimits
{
    internal int MaximumMessageBytes = SecurityLimits.MaximumMimeMessageBytes;
    internal int MaximumDepth = SecurityLimits.MaximumMimeDepth;
    internal int MaximumParts = SecurityLimits.MaximumMimeParts;
    internal int MaximumDecodedPartBytes = SecurityLimits.MaximumDecodedMimePartBytes;
    internal int MaximumTotalDecodedBytes = SecurityLimits.MaximumTotalDecodedMimeBytes;
}

internal class MimeMessageReadResult
{
    internal List<EmailHeader> Headers = new List<EmailHeader>();
    internal MimeEntity? Root;
    internal bool HasMalformedInput;
    internal bool IsLimitExceeded;
}

internal class MimeEntity
{
    internal List<EmailHeader> Headers = new List<EmailHeader>();
    internal string Type = "application/octet-stream";
    internal string? Charset;
    internal string? Disposition;
    internal string? Name;
    internal string? ContentId;
    internal List<string>? Language;
    internal string? Location;
    internal string TransferEncoding = "7bit";
    internal byte[] RawBody = new byte[0];
    internal byte[] DecodedBody = new byte[0];
    internal string? DecodedText;
    internal List<MimeEntity>? SubParts;
    internal MimeMessageReadResult? EmbeddedMessage;
    internal bool IsEncodingProblem;
    internal bool HasMetadataEncodingProblem;
    internal bool IsTruncated;
}

internal class MimeReaderContext
{
    internal int PartCount;
    internal int DecodedBytes;
    internal bool HasMalformedInput;
    internal bool IsLimitExceeded;
}

internal class MimePartRange
{
    internal int Start;
    internal int Length;
}

internal class MimeMultipartResult
{
    internal List<MimePartRange> Parts = new List<MimePartRange>();
    internal bool FoundBoundary;
    internal bool FoundClosingBoundary;
}

internal class MimeBoundaryLine
{
    internal int Start;
    internal int ContentEnd;
    internal int NextStart;
}

internal class MimeMessageReader
{
    internal MimeReaderLimits Limits = new MimeReaderLimits();
    internal HeaderParser HeaderReader = new HeaderParser();
    internal MimeHeaderValueParser HeaderValues = new MimeHeaderValueParser();
    internal MimeTransferDecoder Transfers = new MimeTransferDecoder();
    internal StructuredHeaderValueParser StructuredValues = new StructuredHeaderValueParser();
    internal HeaderValueParser TextValues = new HeaderValueParser();
    internal ValueValidator Values = new ValueValidator();
    internal Encoding Utf8 = new UTF8Encoding(false, false);

    internal MimeMessageReadResult ReadMessage(byte[]? message)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        ValidateLimits();
        MimeMessageReadResult result = new MimeMessageReadResult();
        if (message.Length > Limits.MaximumMessageBytes)
        {
            result.IsLimitExceeded = true;
            return result;
        }

        MimeReaderContext context = new MimeReaderContext();
        result.Root = ReadEntity(message, "text/plain", 0, context);
        if (result.Root != null)
        {
            result.Headers = result.Root.Headers;
        }

        result.HasMalformedInput = context.HasMalformedInput;
        result.IsLimitExceeded = context.IsLimitExceeded;
        return result;
    }

    internal MimeEntity? ReadEntity(byte[] message, string defaultType, int depth, MimeReaderContext context)
    {
        if (depth > Limits.MaximumDepth || context.PartCount >= Limits.MaximumParts)
        {
            context.IsLimitExceeded = true;
            return null;
        }

        context.PartCount = context.PartCount + 1;
        HeaderParseResult headers = HeaderReader.ParseHeaders(message);
        context.HasMalformedInput = context.HasMalformedInput || headers.HasMalformedInput;
        context.IsLimitExceeded = context.IsLimitExceeded || headers.IsLimitExceeded;

        MimeEntity entity = new MimeEntity();
        entity.Headers = headers.Headers;
        string? contentTypeRaw = FindLastHeader(headers.Headers, "Content-Type");
        MimeHeaderValue? contentType = null;
        if (contentTypeRaw != null)
        {
            contentType = HeaderValues.ParseHeaderValue(contentTypeRaw);
            context.HasMalformedInput = context.HasMalformedInput || contentType.IsMalformed;
        }

        string selectedType = contentType == null || contentType.MainValue == null ? defaultType : contentType.MainValue;
        if (!Values.IsMediaType(selectedType))
        {
            selectedType = "application/octet-stream";
            context.HasMalformedInput = true;
        }

        entity.Type = selectedType.ToLowerInvariant();
        if (contentType != null)
        {
            string? declaredCharset = HeaderValues.GetSimpleParameter(contentType, "charset");
            entity.Charset = NormalizeOptionalToken(declaredCharset);
            if (entity.Type.StartsWith("text/", StringComparison.Ordinal) &&
                (contentType.IsMalformed || declaredCharset != null && entity.Charset == null))
            {
                entity.IsEncodingProblem = true;
            }
        }

        ReadDispositionMetadata(entity, headers.Headers, contentType, context);
        ReadContentMetadata(entity, headers.Headers, context);

        string? transferRaw = FindLastHeader(headers.Headers, "Content-Transfer-Encoding");
        if (transferRaw != null)
        {
            MimeHeaderValue transferValue = HeaderValues.ParseHeaderValue(transferRaw);
            context.HasMalformedInput = context.HasMalformedInput || transferValue.IsMalformed;
            entity.TransferEncoding = transferValue.MainValue == null ? "7bit" : transferValue.MainValue;
            if (transferValue.IsMalformed || transferValue.Parameters.Count != 0)
            {
                entity.IsEncodingProblem = true;
                context.HasMalformedInput = true;
            }
        }

        int bodyStart = headers.BodyOffset;
        entity.RawBody = SliceBytes(message, bodyStart, message.Length - bodyStart);
        int remaining = Limits.MaximumTotalDecodedBytes - context.DecodedBytes;
        int allowance = remaining < Limits.MaximumDecodedPartBytes ? remaining : Limits.MaximumDecodedPartBytes;
        if (allowance < 0)
        {
            allowance = 0;
        }

        MimeByteDecodeResult decoded = Transfers.DecodeBody(entity.RawBody, entity.TransferEncoding, allowance);
        entity.DecodedBody = decoded.Bytes;
        entity.IsEncodingProblem = entity.IsEncodingProblem || decoded.IsMalformed;
        entity.IsTruncated = decoded.IsTruncated;
        context.DecodedBytes = context.DecodedBytes + decoded.Bytes.Length;
        context.HasMalformedInput = context.HasMalformedInput || decoded.IsMalformed;
        context.IsLimitExceeded = context.IsLimitExceeded || decoded.IsTruncated;

        if (entity.Type.StartsWith("multipart/", StringComparison.Ordinal))
        {
            ReadMultipart(entity, contentType, depth, context);
        }
        else if (entity.Type == "message/rfc822" || entity.Type == "message/global")
        {
            ReadEmbedded(entity, depth, context);
        }
        else if (entity.Type.StartsWith("text/", StringComparison.Ordinal))
        {
            ReadText(entity, context);
        }

        return entity;
    }

    internal void ReadDispositionMetadata(MimeEntity entity, List<EmailHeader> headers, MimeHeaderValue? contentType,
        MimeReaderContext context)
    {
        string? raw = FindLastHeader(headers, "Content-Disposition");
        MimeHeaderValue? disposition = null;
        if (raw != null)
        {
            disposition = HeaderValues.ParseHeaderValue(raw);
            context.HasMalformedInput = context.HasMalformedInput || disposition.IsMalformed;
            if (disposition.MainValue != null && HeaderValues.IsMimeToken(disposition.MainValue))
            {
                entity.Disposition = disposition.MainValue;
            }
            else
            {
                context.HasMalformedInput = true;
            }
        }

        MimeDecodedStringResult name = new MimeDecodedStringResult();
        if (disposition != null)
        {
            name = HeaderValues.GetDecodedParameter(disposition, "filename");
        }

        if (name.Value == null && contentType != null)
        {
            name = HeaderValues.GetDecodedParameter(contentType, "name");
        }

        entity.Name = name.Value;
        entity.HasMetadataEncodingProblem = name.IsMalformed;
        context.HasMalformedInput = context.HasMalformedInput || name.IsMalformed;
    }

    internal void ReadContentMetadata(MimeEntity entity, List<EmailHeader> headers, MimeReaderContext context)
    {
        string? contentId = FindLastHeader(headers, "Content-ID");
        if (contentId != null)
        {
            entity.ContentId = StructuredValues.ParseContentId(contentId);
            context.HasMalformedInput = context.HasMalformedInput || entity.ContentId == null;
        }

        string? location = FindLastHeader(headers, "Content-Location");
        if (location != null)
        {
            entity.Location = StructuredValues.ParseContentLocation(location);
            context.HasMalformedInput = context.HasMalformedInput || entity.Location == null;
        }

        string? language = FindLastHeader(headers, "Content-Language");
        if (language != null)
        {
            entity.Language = ParseLanguages(language, context);
        }
    }

    internal void ReadMultipart(MimeEntity entity, MimeHeaderValue? contentType, int depth, MimeReaderContext context)
    {
        string? boundary = contentType == null ? null : HeaderValues.GetSimpleParameter(contentType, "boundary");
        if (!IsValidBoundary(boundary))
        {
            context.HasMalformedInput = true;
            entity.IsEncodingProblem = true;
            return;
        }

        MimeMultipartResult split = SplitMultipart(entity.DecodedBody, boundary == null ? "" : boundary);
        if (!split.FoundBoundary || !split.FoundClosingBoundary)
        {
            context.HasMalformedInput = true;
            entity.IsEncodingProblem = true;
        }

        entity.SubParts = new List<MimeEntity>();
        string defaultType = entity.Type == "multipart/digest" ? "message/rfc822" : "text/plain";
        foreach (MimePartRange range in split.Parts)
        {
            byte[] bytes = SliceBytes(entity.DecodedBody, range.Start, range.Length);
            MimeEntity? child;
            if (defaultType == "message/rfc822" && !HasExplicitContentType(bytes))
            {
                child = ReadDigestPart(bytes, depth + 1, context);
            }
            else
            {
                child = ReadEntity(bytes, defaultType, depth + 1, context);
            }

            if (child != null)
            {
                entity.SubParts.Add(child);
            }
        }
    }

    internal MimeEntity? ReadDigestPart(byte[] message, int depth, MimeReaderContext context)
    {
        if (depth > Limits.MaximumDepth || context.PartCount >= Limits.MaximumParts)
        {
            context.IsLimitExceeded = true;
            return null;
        }

        context.PartCount = context.PartCount + 1;
        MimeEntity entity = new MimeEntity();
        entity.Type = "message/rfc822";
        entity.RawBody = message;
        int remaining = Limits.MaximumTotalDecodedBytes - context.DecodedBytes;
        int allowance = remaining < Limits.MaximumDecodedPartBytes ? remaining : Limits.MaximumDecodedPartBytes;
        if (allowance < 0)
        {
            allowance = 0;
        }

        MimeByteDecodeResult copied = Transfers.CopyIdentityBody(message, allowance);
        entity.DecodedBody = copied.Bytes;
        entity.IsTruncated = copied.IsTruncated;
        context.DecodedBytes = context.DecodedBytes + copied.Bytes.Length;
        context.IsLimitExceeded = context.IsLimitExceeded || copied.IsTruncated;
        ReadEmbedded(entity, depth, context);
        return entity;
    }

    internal bool HasExplicitContentType(byte[] message)
    {
        HeaderParseResult headers = HeaderReader.ParseHeaders(message);
        return FindLastHeader(headers.Headers, "Content-Type") != null;
    }

    internal void ReadEmbedded(MimeEntity entity, int depth, MimeReaderContext context)
    {
        MimeMessageReadResult embedded = new MimeMessageReadResult();
        embedded.Root = ReadEntity(entity.DecodedBody, "text/plain", depth + 1, context);
        if (embedded.Root != null)
        {
            embedded.Headers = embedded.Root.Headers;
        }

        embedded.HasMalformedInput = context.HasMalformedInput;
        embedded.IsLimitExceeded = context.IsLimitExceeded;
        entity.EmbeddedMessage = embedded;
    }

    internal void ReadText(MimeEntity entity, MimeReaderContext context)
    {
        string charset = entity.Charset == null || entity.Charset.Length == 0 ? "us-ascii" : entity.Charset;
        string? decoded = TextValues.DecodeCharset(entity.DecodedBody, charset);
        if (decoded == null)
        {
            decoded = Utf8.GetString(entity.DecodedBody);
            entity.IsEncodingProblem = true;
            context.HasMalformedInput = true;
        }
        else if (!IsValidCharsetBytes(entity.DecodedBody, charset))
        {
            entity.IsEncodingProblem = true;
            context.HasMalformedInput = true;
        }

        entity.DecodedText = decoded.Normalize(NormalizationForm.FormC);
    }

    internal MimeMultipartResult SplitMultipart(byte[] body, string boundary)
    {
        MimeMultipartResult result = new MimeMultipartResult();
        byte[] delimiter = Encoding.ASCII.GetBytes("--" + boundary);
        int partStart = -1;
        int offset = 0;
        while (offset < body.Length)
        {
            MimeBoundaryLine line = ReadBoundaryLine(body, offset);
            int kind = BoundaryKind(body, line, delimiter);
            if (kind != 0)
            {
                result.FoundBoundary = true;
                if (partStart >= 0)
                {
                    int end = StripPrecedingLineBreak(body, line.Start, partStart);
                    MimePartRange range = new MimePartRange();
                    range.Start = partStart;
                    range.Length = end - partStart;
                    result.Parts.Add(range);
                }

                if (kind == 2)
                {
                    result.FoundClosingBoundary = true;
                    return result;
                }

                partStart = line.NextStart;
            }

            offset = line.NextStart;
        }

        if (partStart >= 0)
        {
            MimePartRange final = new MimePartRange();
            final.Start = partStart;
            final.Length = body.Length - partStart;
            result.Parts.Add(final);
        }

        return result;
    }

    internal MimeBoundaryLine ReadBoundaryLine(byte[] body, int offset)
    {
        MimeBoundaryLine line = new MimeBoundaryLine();
        line.Start = offset;
        int index = offset;
        while (index < body.Length && body[index] != 13 && body[index] != 10)
        {
            index = index + 1;
        }

        line.ContentEnd = index;
        if (index < body.Length && body[index] == 13 && index + 1 < body.Length && body[index + 1] == 10)
        {
            line.NextStart = index + 2;
        }
        else if (index < body.Length)
        {
            line.NextStart = index + 1;
        }
        else
        {
            line.NextStart = index;
        }

        return line;
    }

    internal int BoundaryKind(byte[] body, MimeBoundaryLine line, byte[] delimiter)
    {
        int length = line.ContentEnd - line.Start;
        if (length < delimiter.Length)
        {
            return 0;
        }

        int index = 0;
        while (index < delimiter.Length)
        {
            if (body[line.Start + index] != delimiter[index])
            {
                return 0;
            }

            index = index + 1;
        }

        int cursor = line.Start + delimiter.Length;
        bool closing = false;
        if (cursor + 1 < line.ContentEnd && body[cursor] == 45 && body[cursor + 1] == 45)
        {
            closing = true;
            cursor = cursor + 2;
        }

        while (cursor < line.ContentEnd && (body[cursor] == 32 || body[cursor] == 9))
        {
            cursor = cursor + 1;
        }

        if (cursor != line.ContentEnd)
        {
            return 0;
        }

        return closing ? 2 : 1;
    }

    internal int StripPrecedingLineBreak(byte[] body, int end, int start)
    {
        if (end > start && body[end - 1] == 10)
        {
            end = end - 1;
            if (end > start && body[end - 1] == 13)
            {
                end = end - 1;
            }
        }
        else if (end > start && body[end - 1] == 13)
        {
            end = end - 1;
        }

        return end;
    }

    internal List<string>? ParseLanguages(string raw, MimeReaderContext context)
    {
        string unfolded = HeaderValues.Addresses.RemoveComments(TextValues.UnfoldValue(raw));
        string[] pieces = unfolded.Split(',');
        List<string> result = new List<string>();
        foreach (string piece in pieces)
        {
            string value = TrimWhitespace(piece);
            if (!IsLanguageTag(value))
            {
                context.HasMalformedInput = true;
            }
            else
            {
                result.Add(value);
            }
        }

        return result.Count == 0 ? null : result;
    }

    internal bool IsLanguageTag(string value)
    {
        if (value.Length == 0 || value[0] == '-' || value[value.Length - 1] == '-')
        {
            return false;
        }

        bool previousHyphen = false;
        foreach (char character in value)
        {
            bool hyphen = character == '-';
            bool alphaNumeric = character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' ||
                character >= '0' && character <= '9';
            if ((!alphaNumeric && !hyphen) || (hyphen && previousHyphen))
            {
                return false;
            }

            previousHyphen = hyphen;
        }

        return true;
    }

    internal bool IsValidBoundary(string? value)
    {
        if (value == null || value.Length == 0 || value.Length > 70 || value[value.Length - 1] == ' ')
        {
            return false;
        }

        foreach (char character in value)
        {
            if (character < 32 || character > 126)
            {
                return false;
            }
        }

        return true;
    }

    internal bool IsValidCharsetBytes(byte[] value, string charset)
    {
        string normalized = TextValues.CanonicalCharset(charset);
        if (normalized == "us-ascii" || normalized == "ascii")
        {
            foreach (byte item in value)
            {
                if (item > 127)
                {
                    return false;
                }
            }

            return true;
        }

        if (normalized == "utf-8" || normalized == "utf8")
        {
            return IsValidUtf8(value);
        }

        if (normalized == "utf-16" || normalized == "utf-16le" || normalized == "utf-16be")
        {
            return value.Length % 2 == 0;
        }

        return true;
    }

    internal bool IsValidUtf8(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int first = value[index];
            if (first <= 127)
            {
                index = index + 1;
                continue;
            }

            int needed;
            int minimumSecond = 128;
            int maximumSecond = 191;
            if (first >= 194 && first <= 223)
            {
                needed = 1;
            }
            else if (first >= 224 && first <= 239)
            {
                needed = 2;
                if (first == 224)
                {
                    minimumSecond = 160;
                }
                else if (first == 237)
                {
                    maximumSecond = 159;
                }
            }
            else if (first >= 240 && first <= 244)
            {
                needed = 3;
                if (first == 240)
                {
                    minimumSecond = 144;
                }
                else if (first == 244)
                {
                    maximumSecond = 143;
                }
            }
            else
            {
                return false;
            }

            if (index + needed >= value.Length || value[index + 1] < minimumSecond || value[index + 1] > maximumSecond)
            {
                return false;
            }

            int continuation = 2;
            while (continuation <= needed)
            {
                int item = value[index + continuation];
                if (item < 128 || item > 191)
                {
                    return false;
                }

                continuation = continuation + 1;
            }

            index = index + needed + 1;
        }

        return true;
    }

    internal string? FindLastHeader(List<EmailHeader> headers, string name)
    {
        int index = headers.Count - 1;
        while (index >= 0)
        {
            if (headers[index].Name != null && string.Equals(headers[index].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return headers[index].Value;
            }

            index = index - 1;
        }

        return null;
    }

    internal byte[] SliceBytes(byte[] value, int start, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = value[start + index];
            index = index + 1;
        }

        return result;
    }

    internal string? NormalizeOptionalToken(string? value)
    {
        if (value == null)
        {
            return null;
        }

        string normalized = TrimWhitespace(value).ToLowerInvariant();
        return normalized.Length == 0 ? null : normalized;
    }

    internal string TrimWhitespace(string value)
    {
        int start = 0;
        while (start < value.Length && (value[start] == ' ' || value[start] == '\t' || value[start] == '\r' || value[start] == '\n'))
        {
            start = start + 1;
        }

        int end = value.Length;
        while (end > start && (value[end - 1] == ' ' || value[end - 1] == '\t' || value[end - 1] == '\r' || value[end - 1] == '\n'))
        {
            end = end - 1;
        }

        return value.Substring(start, end - start);
    }

    internal void ValidateLimits()
    {
        if (Limits.MaximumMessageBytes <= 0 || Limits.MaximumDepth < 0 || Limits.MaximumParts <= 0 ||
            Limits.MaximumDecodedPartBytes <= 0 || Limits.MaximumTotalDecodedBytes <= 0)
        {
            throw new ArgumentOutOfRangeException("Limits");
        }
    }
}
