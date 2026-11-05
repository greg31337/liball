using System;
using System.Collections.Generic;
using System.Text;

namespace websocket;

internal class WebSocketExtensionResponseParseResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int ConsumedCount;
    internal WebSocketExtensionOffer[] Extensions = Array.Empty<WebSocketExtensionOffer>();
}

internal class WebSocketExtensionParserState
{
    internal int MaximumFieldCount;
    internal int MaximumExtensionCount;
    internal int MaximumParameterCount;
    internal int MaximumTokenLength;
    internal int MaximumTotalLength;
    internal int FieldCount;
    internal int TotalLength;
    internal bool NeedsFieldSeparator;
    internal bool Terminal;
    internal WebSocketParseStatus TerminalStatus = WebSocketParseStatus.NeedInput;
    internal WebSocketExtensionOffer[] ParsedExtensions =
        Array.Empty<WebSocketExtensionOffer>();
    internal StringBuilder BufferedValue = new StringBuilder();

    internal WebSocketExtensionParserState(
        int maximumFieldCount,
        int maximumExtensionCount,
        int maximumParameterCount,
        int maximumTokenLength,
        int maximumTotalLength)
    {
        MaximumFieldCount = maximumFieldCount;
        MaximumExtensionCount = maximumExtensionCount;
        MaximumParameterCount = maximumParameterCount;
        MaximumTokenLength = maximumTokenLength;
        MaximumTotalLength = maximumTotalLength;
        if (maximumFieldCount < 0 ||
            maximumExtensionCount < 0 ||
            maximumParameterCount < 0 ||
            maximumTokenLength < 0 ||
            maximumTotalLength < 0)
        {
            Terminal = true;
            TerminalStatus = WebSocketParseStatus.LimitRejected;
        }
    }

    internal WebSocketExtensionResponseParseResult ParseChunk(
        string source,
        bool endOfField,
        bool endOfInput)
    {
        WebSocketExtensionResponseParseResult result =
            new WebSocketExtensionResponseParseResult();
        if (Terminal)
        {
            result.Status = TerminalStatus;
            result.Extensions = ParsedExtensions;
            return result;
        }

        if (source == null || (endOfInput && !endOfField))
        {
            Terminal = true;
            TerminalStatus = WebSocketParseStatus.ProtocolFailure;
            return result;
        }

        int additionalLength = source.Length;
        if (NeedsFieldSeparator)
        {
            additionalLength = additionalLength + 1;
        }

        if (additionalLength < 0 ||
            TotalLength > MaximumTotalLength - additionalLength ||
            (endOfField && FieldCount == MaximumFieldCount))
        {
            Terminal = true;
            TerminalStatus = WebSocketParseStatus.LimitRejected;
            result.Status = TerminalStatus;
            return result;
        }

        if (NeedsFieldSeparator)
        {
            BufferedValue.Append(',');
            NeedsFieldSeparator = false;
        }

        BufferedValue.Append(source);
        TotalLength = TotalLength + additionalLength;
        result.ConsumedCount = source.Length;
        if (endOfField)
        {
            FieldCount = FieldCount + 1;
            NeedsFieldSeparator = !endOfInput;
        }

        if (!endOfInput)
        {
            result.Status = WebSocketParseStatus.NeedInput;
            return result;
        }

        WebSocketExtensionResponseParseResult parsed =
            WebSocketExtensionResponseParser.ParseBufferedValue(
                BufferedValue.ToString(),
                MaximumExtensionCount,
                MaximumParameterCount,
                MaximumTokenLength);
        Terminal = true;
        TerminalStatus = parsed.Status;
        ParsedExtensions = parsed.Extensions;
        parsed.ConsumedCount = source.Length;
        return parsed;
    }
}

internal static class WebSocketExtensionResponseParser
{
    internal static WebSocketExtensionResponseParseResult ParseResponse(
        string value,
        int maximumExtensionCount)
    {
        if (value == null)
        {
            return new WebSocketExtensionResponseParseResult();
        }

        WebSocketExtensionParserState parser = new WebSocketExtensionParserState(
            1,
            maximumExtensionCount,
            value.Length,
            value.Length,
            value.Length);
        return parser.ParseChunk(value, true, true);
    }

    internal static WebSocketExtensionResponseParseResult ParseFields(
        WebSocketHeaderField[] fields,
        int maximumFieldCount,
        int maximumExtensionCount,
        int maximumParameterCount,
        int maximumTokenLength,
        int maximumTotalLength)
    {
        WebSocketExtensionResponseParseResult result =
            new WebSocketExtensionResponseParseResult();
        if (fields == null)
        {
            return result;
        }

        if (maximumFieldCount < 0 ||
            maximumExtensionCount < 0 ||
            maximumParameterCount < 0 ||
            maximumTokenLength < 0 ||
            maximumTotalLength < 0)
        {
            result.Status = WebSocketParseStatus.LimitRejected;
            return result;
        }

        if (fields.Length == 0)
        {
            result.Status = WebSocketParseStatus.Complete;
            return result;
        }

        WebSocketExtensionParserState parser = new WebSocketExtensionParserState(
            maximumFieldCount,
            maximumExtensionCount,
            maximumParameterCount,
            maximumTokenLength,
            maximumTotalLength);
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index] == null || fields[index].Value == null)
            {
                return result;
            }

            result = parser.ParseChunk(
                fields[index].Value,
                true,
                index == fields.Length - 1);
            if (result.Status != WebSocketParseStatus.NeedInput &&
                index != fields.Length - 1)
            {
                return result;
            }

            index = index + 1;
        }

        return result;
    }

    internal static WebSocketExtensionResponseParseResult ParseBufferedValue(
        string value,
        int maximumExtensionCount,
        int maximumParameterCount,
        int maximumTokenLength)
    {
        WebSocketExtensionResponseParseResult result =
            new WebSocketExtensionResponseParseResult();
        List<WebSocketExtensionOffer> extensions =
            new List<WebSocketExtensionOffer>();
        int parameterCount = 0;
        int offset = 0;
        while (offset < value.Length)
        {
            offset = HttpFieldValue.SkipOptionalWhitespace(value, offset);
            if (offset == value.Length)
            {
                break;
            }

            if (value[offset] == ',')
            {
                offset = offset + 1;
                continue;
            }

            int nameLength = AsciiToken.ReadLength(value, offset);
            if (nameLength == 0)
            {
                return result;
            }

            if (nameLength > maximumTokenLength ||
                extensions.Count == maximumExtensionCount)
            {
                result.Status = WebSocketParseStatus.LimitRejected;
                return result;
            }

            WebSocketExtensionOffer extension = new WebSocketExtensionOffer();
            extension.Name = new string(value.AsSpan(offset, nameLength));
            offset = offset + nameLength;
            List<WebSocketExtensionParameter> parameters =
                new List<WebSocketExtensionParameter>();
            bool extensionComplete = false;
            while (!extensionComplete)
            {
                offset = HttpFieldValue.SkipOptionalWhitespace(value, offset);
                if (offset == value.Length || value[offset] == ',')
                {
                    extensionComplete = true;
                }
                else if (value[offset] == ';')
                {
                    offset = offset + 1;
                    offset = HttpFieldValue.SkipOptionalWhitespace(value, offset);
                    int parameterNameLength = AsciiToken.ReadLength(value, offset);
                    if (parameterNameLength == 0)
                    {
                        return result;
                    }

                    if (parameterNameLength > maximumTokenLength ||
                        parameterCount == maximumParameterCount)
                    {
                        result.Status = WebSocketParseStatus.LimitRejected;
                        return result;
                    }

                    WebSocketExtensionParameter parameter =
                        new WebSocketExtensionParameter();
                    parameter.Name = new string(
                        value.AsSpan(offset, parameterNameLength));
                    offset = offset + parameterNameLength;
                    offset = HttpFieldValue.SkipOptionalWhitespace(value, offset);
                    if (offset < value.Length && value[offset] == '=')
                    {
                        offset = offset + 1;
                        offset = HttpFieldValue.SkipOptionalWhitespace(value, offset);
                        if (offset == value.Length)
                        {
                            return result;
                        }

                        parameter.HasValue = true;
                        if (value[offset] == '"')
                        {
                            QuotedStringReadResult quoted =
                                QuotedStringCodec.Read(value, offset);
                            if (quoted.Status != WebSocketParseStatus.Complete ||
                                !AsciiToken.IsValid(quoted.Value))
                            {
                                return result;
                            }

                            if (quoted.Value.Length > maximumTokenLength)
                            {
                                result.Status = WebSocketParseStatus.LimitRejected;
                                return result;
                            }

                            parameter.Value = quoted.Value;
                            offset = offset + quoted.ConsumedCount;
                        }
                        else
                        {
                            int parameterValueLength =
                                AsciiToken.ReadLength(value, offset);
                            if (parameterValueLength == 0)
                            {
                                return result;
                            }

                            if (parameterValueLength > maximumTokenLength)
                            {
                                result.Status = WebSocketParseStatus.LimitRejected;
                                return result;
                            }

                            parameter.Value = new string(
                                value.AsSpan(offset, parameterValueLength));
                            offset = offset + parameterValueLength;
                        }
                    }

                    parameters.Add(parameter);
                    parameterCount = parameterCount + 1;
                }
                else
                {
                    return result;
                }
            }

            extension.Parameters = parameters.ToArray();
            extensions.Add(extension);
            if (offset < value.Length)
            {
                offset = offset + 1;
            }
        }

        if (extensions.Count == 0)
        {
            return result;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Extensions = extensions.ToArray();
        return result;
    }
}
