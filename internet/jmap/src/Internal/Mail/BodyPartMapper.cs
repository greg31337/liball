using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace jmap;

internal class BodyPartMappingResult
{
    internal EmailBodyPart? BodyStructure;
    internal Dictionary<string, EmailBodyValue> BodyValues = new Dictionary<string, EmailBodyValue>();
    internal List<EmailBodyPart> TextBody = new List<EmailBodyPart>();
    internal List<EmailBodyPart> HtmlBody = new List<EmailBodyPart>();
    internal List<EmailBodyPart> Attachments = new List<EmailBodyPart>();
    internal Dictionary<string, string> PreviewValues = new Dictionary<string, string>();
    internal bool HasAttachment;
    internal string Preview = "";
}

internal class BodyTextTruncationResult
{
    internal string Value = "";
    internal bool IsTruncated;
}

internal class HtmlEntityDecodeResult
{
    internal string? Value;
    internal int Length;
}

internal class BodyPartMapper
{
    internal BodyPartMappingResult MapMessage(MimeMessageReadResult? message, long maximumBodyValueBytes)
    {
        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (maximumBodyValueBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumBodyValueBytes");
        }

        BodyPartMappingResult result = new BodyPartMappingResult();
        if (message.Root == null)
        {
            return result;
        }

        result.BodyStructure = MapEntity(message.Root, "1", true, result, maximumBodyValueBytes);
        List<EmailBodyPart> roots = new List<EmailBodyPart>();
        roots.Add(result.BodyStructure);
        ParseStructure(roots, "mixed", false, result.HtmlBody, result.TextBody, result.Attachments);
        foreach (EmailBodyPart attachment in result.Attachments)
        {
            if (!string.Equals(attachment.Disposition, "inline", StringComparison.OrdinalIgnoreCase))
            {
                result.HasAttachment = true;
                break;
            }
        }

        result.Preview = BuildPreview(result);
        return result;
    }

    internal EmailBodyPart MapEntity(MimeEntity entity, string path, bool isRoot, BodyPartMappingResult result,
        long maximumBodyValueBytes)
    {
        EmailBodyPart mapped = new EmailBodyPart();
        mapped.Headers = CopyHeaders(entity.Headers);
        mapped.Name = entity.Name;
        mapped.Type = entity.Type;
        mapped.Charset = CharsetForPart(entity);
        mapped.Disposition = entity.Disposition;
        mapped.Cid = entity.ContentId;
        mapped.Language = CopyStrings(entity.Language);
        mapped.Location = entity.Location;
        mapped.Size = entity.DecodedBody.Length;
        mapped.HasSize = true;

        if (entity.Type.StartsWith("multipart/", StringComparison.Ordinal))
        {
            mapped.SubParts = new List<EmailBodyPart>();
            if (entity.SubParts != null)
            {
                int index = 0;
                while (index < entity.SubParts.Count)
                {
                    string childNumber = (index + 1).ToString(CultureInfo.InvariantCulture);
                    string childPath = isRoot ? childNumber : path + "." + childNumber;
                    mapped.SubParts.Add(MapEntity(entity.SubParts[index], childPath, false, result,
                        maximumBodyValueBytes));
                    index = index + 1;
                }
            }
        }
        else
        {
            mapped.PartId = path;
            mapped.BlobId = BlobId(entity.DecodedBody);
            if (entity.Type.StartsWith("text/", StringComparison.Ordinal) && entity.DecodedText != null)
            {
                result.BodyValues.Add(path, MapBodyValue(entity, maximumBodyValueBytes));
                result.PreviewValues.Add(path, NormalizeLineEndings(entity.DecodedText));
            }
        }

        return mapped;
    }

    internal EmailBodyValue MapBodyValue(MimeEntity entity, long maximumBodyValueBytes)
    {
        string normalized = NormalizeLineEndings(entity.DecodedText == null ? "" : entity.DecodedText);
        BodyTextTruncationResult truncated = TruncateUtf8(normalized, maximumBodyValueBytes,
            entity.Type == "text/html");
        EmailBodyValue result = new EmailBodyValue();
        result.Value = truncated.Value;
        result.IsEncodingProblem = entity.IsEncodingProblem;
        result.IsTruncated = entity.IsTruncated || truncated.IsTruncated;
        return result;
    }

    internal void ParseStructure(List<EmailBodyPart> parts, string multipartType, bool inAlternative,
        List<EmailBodyPart>? htmlBody, List<EmailBodyPart>? textBody, List<EmailBodyPart> attachments)
    {
        int textLength = textBody == null ? -1 : textBody.Count;
        int htmlLength = htmlBody == null ? -1 : htmlBody.Count;
        int index = 0;
        while (index < parts.Count)
        {
            EmailBodyPart part = parts[index];
            string type = part.Type == null ? "application/octet-stream" : part.Type;
            bool multipart = type.StartsWith("multipart/", StringComparison.Ordinal);
            bool inline = !string.Equals(part.Disposition, "attachment", StringComparison.OrdinalIgnoreCase) &&
                (type == "text/plain" || type == "text/html" || IsInlineMediaType(type)) &&
                (index == 0 || multipartType != "related" && (IsInlineMediaType(type) || part.Name == null));

            if (multipart)
            {
                string subtype = MultipartSubtype(type);
                if (part.SubParts != null)
                {
                    ParseStructure(part.SubParts, subtype, inAlternative || subtype == "alternative", htmlBody,
                        textBody, attachments);
                }
            }
            else if (inline)
            {
                if (multipartType == "alternative")
                {
                    if (type == "text/plain" && textBody != null)
                    {
                        textBody.Add(part);
                    }
                    else if (type == "text/html" && htmlBody != null)
                    {
                        htmlBody.Add(part);
                    }
                    else
                    {
                        attachments.Add(part);
                    }

                    index = index + 1;
                    continue;
                }

                if (inAlternative)
                {
                    if (type == "text/plain")
                    {
                        htmlBody = null;
                    }

                    if (type == "text/html")
                    {
                        textBody = null;
                    }
                }

                if (textBody != null)
                {
                    textBody.Add(part);
                }

                if (htmlBody != null)
                {
                    htmlBody.Add(part);
                }

                if ((textBody == null || htmlBody == null) && IsInlineMediaType(type))
                {
                    attachments.Add(part);
                }
            }
            else
            {
                attachments.Add(part);
            }

            index = index + 1;
        }

        if (multipartType == "alternative" && textBody != null && htmlBody != null)
        {
            if (textLength == textBody.Count && htmlLength != htmlBody.Count)
            {
                int copyIndex = htmlLength;
                while (copyIndex < htmlBody.Count)
                {
                    textBody.Add(htmlBody[copyIndex]);
                    copyIndex = copyIndex + 1;
                }
            }

            if (htmlLength == htmlBody.Count && textLength != textBody.Count)
            {
                int copyIndex = textLength;
                while (copyIndex < textBody.Count)
                {
                    htmlBody.Add(textBody[copyIndex]);
                    copyIndex = copyIndex + 1;
                }
            }
        }
    }

    internal string BuildPreview(BodyPartMappingResult mapping)
    {
        EmailBodyPart? selected = FindPreviewPart(mapping.TextBody, "text/plain");
        if (selected == null)
        {
            selected = FindAnyTextPart(mapping.TextBody);
        }

        if (selected == null)
        {
            selected = FindAnyTextPart(mapping.HtmlBody);
        }

        if (selected == null || selected.PartId == null || !mapping.PreviewValues.ContainsKey(selected.PartId))
        {
            return "";
        }

        string value = mapping.PreviewValues[selected.PartId];
        if (selected.Type == "text/html")
        {
            value = HtmlToPlainText(value);
        }

        return LimitPreviewCharacters(CollapseWhitespace(value), JmapProtocolLimits.MaximumEmailPreviewLength);
    }

    internal EmailBodyPart? FindPreviewPart(List<EmailBodyPart> parts, string type)
    {
        foreach (EmailBodyPart part in parts)
        {
            if (part.Type == type)
            {
                return part;
            }
        }

        return null;
    }

    internal EmailBodyPart? FindAnyTextPart(List<EmailBodyPart> parts)
    {
        foreach (EmailBodyPart part in parts)
        {
            if (part.Type != null && part.Type.StartsWith("text/", StringComparison.Ordinal))
            {
                return part;
            }
        }

        return null;
    }

    internal BodyTextTruncationResult TruncateUtf8(string value, long maximumBytes, bool avoidHtmlTag)
    {
        BodyTextTruncationResult result = new BodyTextTruncationResult();
        if (maximumBytes == 0)
        {
            result.Value = value;
            return result;
        }

        StringBuilder builder = new StringBuilder();
        long bytes = 0;
        int index = 0;
        int tagStart = -1;
        bool inTag = false;
        while (index < value.Length)
        {
            int length = CharacterLength(value, index);
            int encodedBytes = Utf8Length(value, index, length);
            if (bytes + encodedBytes > maximumBytes)
            {
                result.IsTruncated = true;
                if (avoidHtmlTag && inTag && tagStart >= 0)
                {
                    builder.Length = tagStart;
                }

                break;
            }

            char character = value[index];
            if (avoidHtmlTag && character == '<' && !inTag)
            {
                inTag = true;
                tagStart = builder.Length;
            }

            builder.Append(value, index, length);
            bytes = bytes + encodedBytes;
            if (avoidHtmlTag && character == '>' && inTag)
            {
                inTag = false;
                tagStart = -1;
            }

            index = index + length;
        }

        result.Value = builder.ToString();
        return result;
    }

    internal string NormalizeLineEndings(string value)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '\r')
            {
                result.Append('\n');
                if (index + 1 < value.Length && value[index + 1] == '\n')
                {
                    index = index + 2;
                }
                else
                {
                    index = index + 1;
                }
            }
            else
            {
                result.Append(value[index]);
                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal string HtmlToPlainText(string value)
    {
        StringBuilder result = new StringBuilder();
        bool inTag = false;
        int index = 0;
        while (index < value.Length)
        {
            if (!inTag && value[index] == '<')
            {
                inTag = true;
                result.Append(' ');
                index = index + 1;
            }
            else if (inTag && value[index] == '>')
            {
                inTag = false;
                result.Append(' ');
                index = index + 1;
            }
            else if (!inTag && value[index] == '&')
            {
                HtmlEntityDecodeResult entity = DecodeHtmlEntity(value, index);
                if (entity.Value == null)
                {
                    result.Append(value[index]);
                    index = index + 1;
                }
                else
                {
                    result.Append(entity.Value);
                    index = index + entity.Length;
                }
            }
            else
            {
                if (!inTag)
                {
                    result.Append(value[index]);
                }

                index = index + 1;
            }
        }

        return result.ToString();
    }

    internal HtmlEntityDecodeResult DecodeHtmlEntity(string value, int start)
    {
        HtmlEntityDecodeResult result = new HtmlEntityDecodeResult();
        string[] names = { "&amp;", "&lt;", "&gt;", "&quot;", "&apos;", "&nbsp;" };
        string[] replacements = { "&", "<", ">", "\"", "'", " " };
        int index = 0;
        while (index < names.Length)
        {
            if (start + names[index].Length <= value.Length &&
                string.Equals(value.Substring(start, names[index].Length), names[index], StringComparison.OrdinalIgnoreCase))
            {
                result.Value = replacements[index];
                result.Length = names[index].Length;
                return result;
            }

            index = index + 1;
        }

        return result;
    }

    internal string CollapseWhitespace(string value)
    {
        StringBuilder result = new StringBuilder();
        bool whitespace = true;
        foreach (char character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                whitespace = true;
            }
            else
            {
                if (whitespace && result.Length != 0)
                {
                    result.Append(' ');
                }

                result.Append(character);
                whitespace = false;
            }
        }

        return result.ToString();
    }

    internal string LimitPreviewCharacters(string value, int maximumCharacters)
    {
        int index = 0;
        int count = 0;
        while (index < value.Length && count < maximumCharacters)
        {
            index = index + CharacterLength(value, index);
            count = count + 1;
        }

        return index == value.Length ? value : value.Substring(0, index);
    }

    internal int CharacterLength(string value, int index)
    {
        if (char.IsHighSurrogate(value[index]) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
        {
            return 2;
        }

        return 1;
    }

    internal int Utf8Length(string value, int index, int length)
    {
        if (length == 2)
        {
            return 4;
        }

        int character = value[index];
        if (character <= 127)
        {
            return 1;
        }

        return character <= 2047 ? 2 : 3;
    }

    internal bool IsInlineMediaType(string type)
    {
        return type.StartsWith("image/", StringComparison.Ordinal) ||
            type.StartsWith("audio/", StringComparison.Ordinal) ||
            type.StartsWith("video/", StringComparison.Ordinal);
    }

    internal string MultipartSubtype(string type)
    {
        int slash = type.IndexOf('/');
        return slash < 0 || slash + 1 >= type.Length ? "" : type.Substring(slash + 1);
    }

    internal string? CharsetForPart(MimeEntity entity)
    {
        if (!entity.Type.StartsWith("text/", StringComparison.Ordinal))
        {
            return null;
        }

        return entity.Charset == null || entity.Charset.Length == 0 ? "us-ascii" : entity.Charset;
    }

    internal string BlobId(byte[] value)
    {
        byte[] digest = SHA256.HashData(value);
        return "m" + Convert.ToHexString(digest).ToLowerInvariant();
    }

    internal List<EmailHeader> CopyHeaders(List<EmailHeader> headers)
    {
        List<EmailHeader> result = new List<EmailHeader>();
        foreach (EmailHeader header in headers)
        {
            EmailHeader copied = new EmailHeader();
            copied.Name = header.Name;
            copied.Value = header.Value;
            result.Add(copied);
        }

        return result;
    }

    internal List<string>? CopyStrings(List<string>? values)
    {
        if (values == null)
        {
            return null;
        }

        List<string> result = new List<string>();
        foreach (string value in values)
        {
            result.Add(value);
        }

        return result;
    }
}
