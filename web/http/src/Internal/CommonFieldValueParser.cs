using System;
using System.Text;

namespace http;

internal static class CommonFieldValueParser
{
    internal static FieldValueParseResult ParseTokenList(
        byte[] input,
        int maximumSize,
        int maximumMembers)
    {
        if (input == null)
        {
            return CreateMalformedField(0);
        }

        FieldValueParseResult list = FieldValueParser.ParseHashList(
            input,
            maximumSize,
            maximumMembers,
            input.Length);
        if (list.Status != HttpOperationStatus.Done)
        {
            return list;
        }

        int index = 0;
        while (index < list.Values.Count)
        {
            FieldValueParseResult token = FieldValueParser.ParseToken(
                list.Values[index],
                list.Values[index].Length);
            if (token.Status != HttpOperationStatus.Done)
            {
                return CreateMalformedField(index);
            }

            index = index + 1;
        }

        return list;
    }

    internal static FieldValueParseResult ParseUpgrade(
        byte[] input,
        int maximumSize,
        int maximumMembers)
    {
        if (input == null)
        {
            return CreateMalformedField(0);
        }

        FieldValueParseResult list = FieldValueParser.ParseHashList(
            input,
            maximumSize,
            maximumMembers,
            input.Length);
        if (list.Status != HttpOperationStatus.Done)
        {
            return list;
        }

        int index = 0;
        while (index < list.Values.Count)
        {
            if (!IsProtocol(list.Values[index]))
            {
                return CreateMalformedField(index);
            }

            index = index + 1;
        }

        return list;
    }

    internal static MediaTypeParseResult ParseTe(
        byte[] input,
        int maximumSize,
        int maximumMembers,
        int maximumParameters)
    {
        if (input == null)
        {
            return CreateMalformedMedia(0);
        }

        if (maximumParameters < 0)
        {
            return CreateLimitMedia();
        }

        FieldValueParseResult list = FieldValueParser.ParseHashList(
            input,
            maximumSize,
            maximumMembers,
            input.Length);
        if (list.IsLimitExceeded)
        {
            return CreateLimitMedia();
        }

        if (list.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedMedia(list.ErrorPosition);
        }

        MediaTypeParseResult result = CreateDoneMedia(input.Length);
        result.EmptyElements = list.EmptyElements;
        int index = 0;
        while (index < list.Values.Count)
        {
            MediaTypeParseResult member = ParseTeMember(list.Values[index], maximumParameters);
            if (member.Status != HttpOperationStatus.Done)
            {
                member.ErrorPosition = index;
                return member;
            }

            member.Values[0].SourceOrder = index;
            result.Values.Add(member.Values[0]);
            index = index + 1;
        }

        return result;
    }

    internal static CommonFieldParseResult ParseExpect(
        byte[] input,
        int maximumSize,
        int maximumMembers,
        int maximumParameters)
    {
        if (input == null)
        {
            return CreateMalformed(0);
        }

        if (maximumParameters < 0)
        {
            return CreateLimit();
        }

        FieldValueParseResult list = FieldValueParser.ParseHashList(
            input,
            maximumSize,
            maximumMembers,
            input.Length);
        if (list.IsLimitExceeded)
        {
            return CreateLimit();
        }

        if (list.Status != HttpOperationStatus.Done)
        {
            return CreateMalformed(list.ErrorPosition);
        }

        CommonFieldParseResult result = CreateDone(input.Length);
        result.EmptyElements = list.EmptyElements;
        int index = 0;
        while (index < list.Values.Count)
        {
            CommonFieldParseResult member = ParseExpectation(list.Values[index], maximumParameters);
            if (member.Status != HttpOperationStatus.Done)
            {
                member.ErrorPosition = index;
                return member;
            }

            result.Expectations.Add(member.Expectations[0]);
            index = index + 1;
        }

        return result;
    }

    internal static CommonFieldParseResult ParseNumber(byte[] input, int maximumSize)
    {
        if (input == null)
        {
            return CreateMalformed(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize)
        {
            return CreateLimit();
        }

        IntegerParseResult parsed = IntegerParser.ParseDecimal(input);
        if (!parsed.IsValid || parsed.Consumed != input.Length)
        {
            return CreateMalformed(parsed.Consumed);
        }

        CommonFieldParseResult result = CreateDone(input.Length);
        result.Number = parsed.Value;
        return result;
    }

    internal static FieldValueParseResult ParseLocation(
        byte[] input,
        int maximumSize,
        bool allowFragment)
    {
        if (input == null)
        {
            return CreateMalformedField(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize)
        {
            return CreateLimitField();
        }

        int index = 0;
        while (index < input.Length)
        {
            if (input[index] > 0x7f)
            {
                return CreateMalformedField(index);
            }

            index = index + 1;
        }

        string text = Encoding.ASCII.GetString(input);
        UriValidationResult validation = UriCodec.ValidateUriReference(text, maximumSize);
        if (validation.IsLimitExceeded)
        {
            return CreateLimitField();
        }

        if (validation.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedField(validation.ErrorPosition);
        }

        if (!allowFragment && validation.HasFragment)
        {
            return CreateMalformedField(FindByte(input, 0x23));
        }

        FieldValueParseResult result = CreateDoneField(input.Length);
        result.Value = CopyBytes(input);
        return result;
    }

    internal static CommonFieldParseResult ParseRetryAfter(byte[] input, int maximumSize)
    {
        CommonFieldParseResult number = ParseNumber(input, maximumSize);
        if (number.Status == HttpOperationStatus.Done || number.IsLimitExceeded)
        {
            return number;
        }

        HttpDateParseResult date = HttpDateCodec.ParseHttpDate(input, maximumSize);
        if (date.IsLimitExceeded)
        {
            return CreateLimit();
        }

        if (date.Status != HttpOperationStatus.Done)
        {
            return CreateMalformed(date.ErrorPosition);
        }

        CommonFieldParseResult result = CreateDone(input.Length);
        result.IsDate = true;
        result.Date = date.Value;
        return result;
    }

    internal static CommonFieldParseResult ParseVia(
        byte[] input,
        int maximumSize,
        int maximumEntries,
        int maximumCommentDepth)
    {
        FieldValueParseResult list = FieldValueParser.ParseHashList(
            input,
            maximumSize,
            maximumEntries,
            maximumCommentDepth);
        if (list.IsLimitExceeded)
        {
            return CreateLimit();
        }

        if (list.Status != HttpOperationStatus.Done)
        {
            return CreateMalformed(list.ErrorPosition);
        }

        CommonFieldParseResult result = CreateDone(input.Length);
        result.EmptyElements = list.EmptyElements;
        int index = 0;
        while (index < list.Values.Count)
        {
            CommonFieldParseResult member = ParseViaMember(
                list.Values[index],
                maximumCommentDepth);
            if (member.Status != HttpOperationStatus.Done)
            {
                member.ErrorPosition = index;
                return member;
            }

            result.ViaEntries.Add(member.ViaEntries[0]);
            index = index + 1;
        }

        return result;
    }

    static MediaTypeParseResult ParseTeMember(byte[] input, int maximumParameters)
    {
        int tokenEnd = ReadTokenEnd(input, 0);
        if (tokenEnd == 0)
        {
            return CreateMalformedMedia(0);
        }

        HttpWeightedValue value = new HttpWeightedValue();
        value.Value = Encoding.ASCII.GetString(input, 0, tokenEnd);
        int position = SkipWhitespace(input, tokenEnd);
        if (Ascii.EqualsStringsIgnoreCase(value.Value, "trailers"))
        {
            if (position != input.Length)
            {
                return CreateMalformedMedia(position);
            }

            MediaTypeParseResult trailers = CreateDoneMedia(input.Length);
            trailers.Values.Add(value);
            return trailers;
        }

        bool hasQuality = false;
        while (position < input.Length)
        {
            if (input[position] != 0x3b)
            {
                return CreateMalformedMedia(position);
            }

            if (value.Parameters.Count + (hasQuality ? 1 : 0) >= maximumParameters)
            {
                return CreateLimitMedia();
            }

            position = SkipWhitespace(input, position + 1);
            int nameStart = position;
            int nameEnd = ReadTokenEnd(input, nameStart);
            if (nameEnd == nameStart)
            {
                return CreateMalformedMedia(position);
            }

            position = SkipWhitespace(input, nameEnd);
            if (position >= input.Length || input[position] != 0x3d)
            {
                return CreateMalformedMedia(position);
            }

            position = SkipWhitespace(input, position + 1);
            int valueStart = position;
            bool quoted = position < input.Length && input[position] == 0x22;
            int valueEnd = quoted ? ReadQuotedEnd(input, position) : ReadTokenEnd(input, position);
            if (valueEnd <= valueStart)
            {
                return CreateMalformedMedia(valueStart);
            }

            byte[] encodedValue = CopyRange(input, valueStart, valueEnd);
            FieldValueParseResult parsedValue = quoted ?
                FieldValueParser.ParseQuotedString(encodedValue, encodedValue.Length) :
                FieldValueParser.ParseToken(encodedValue, encodedValue.Length);
            if (parsedValue.Status != HttpOperationStatus.Done)
            {
                return CreateMalformedMedia(valueStart + parsedValue.ErrorPosition);
            }

            string name = Encoding.ASCII.GetString(input, nameStart, nameEnd - nameStart);
            position = SkipWhitespace(input, valueEnd);
            if (Ascii.EqualsStringsIgnoreCase(name, "q"))
            {
                if (hasQuality || quoted)
                {
                    MediaTypeParseResult duplicate = CreateMalformedMedia(nameStart);
                    duplicate.IsDuplicateQuality = hasQuality;
                    return duplicate;
                }

                QualityValueParseResult quality = FieldValueParser.ParseQualityValue(
                    parsedValue.Value,
                    parsedValue.Value.Length);
                if (quality.Status != HttpOperationStatus.Done)
                {
                    return CreateMalformedMedia(valueStart);
                }

                hasQuality = true;
                value.Quality = quality.Quality;
                if (position != input.Length)
                {
                    return CreateMalformedMedia(position);
                }
            }
            else
            {
                HttpParameter parameter = new HttpParameter();
                parameter.Name = name;
                parameter.HasValue = true;
                parameter.Value = parsedValue.Value;
                parameter.IsQuoted = quoted;
                value.Parameters.Add(parameter);
            }
        }

        value.Specificity = value.Parameters.Count + 1000;
        MediaTypeParseResult result = CreateDoneMedia(input.Length);
        result.Values.Add(value);
        return result;
    }

    static CommonFieldParseResult ParseExpectation(byte[] input, int maximumParameters)
    {
        int nameEnd = ReadTokenEnd(input, 0);
        if (nameEnd == 0)
        {
            return CreateMalformed(0);
        }

        HttpExpectationValue expectation = new HttpExpectationValue();
        expectation.Name = Encoding.ASCII.GetString(input, 0, nameEnd);
        int position = nameEnd;
        if (position < input.Length && input[position] == 0x3d)
        {
            expectation.HasValue = true;
            position = position + 1;
            int valueStart = position;
            expectation.IsQuoted = position < input.Length && input[position] == 0x22;
            int valueEnd = expectation.IsQuoted ?
                ReadQuotedEnd(input, position) : ReadTokenEnd(input, position);
            if (valueEnd <= valueStart)
            {
                return CreateMalformed(valueStart);
            }

            byte[] encodedValue = CopyRange(input, valueStart, valueEnd);
            FieldValueParseResult parsedValue = expectation.IsQuoted ?
                FieldValueParser.ParseQuotedString(encodedValue, encodedValue.Length) :
                FieldValueParser.ParseToken(encodedValue, encodedValue.Length);
            if (parsedValue.Status != HttpOperationStatus.Done)
            {
                return CreateMalformed(valueStart + parsedValue.ErrorPosition);
            }

            expectation.Value = parsedValue.Value;
            position = valueEnd;
            if (position < input.Length)
            {
                byte[] parameterBytes = CopyRange(input, position, input.Length);
                FieldValueParseResult parameters = FieldValueParser.ParseParameters(
                    parameterBytes,
                    parameterBytes.Length,
                    maximumParameters);
                if (parameters.IsLimitExceeded)
                {
                    return CreateLimit();
                }

                if (parameters.Status != HttpOperationStatus.Done)
                {
                    return CreateMalformed(position + parameters.ErrorPosition);
                }

                expectation.Parameters = parameters.Parameters;
            }
        }
        else if (position != input.Length)
        {
            return CreateMalformed(position);
        }

        CommonFieldParseResult result = CreateDone(input.Length);
        result.Expectations.Add(expectation);
        return result;
    }

    static CommonFieldParseResult ParseViaMember(byte[] input, int maximumCommentDepth)
    {
        int protocolEnd = 0;
        while (protocolEnd < input.Length && input[protocolEnd] != 0x20 &&
            input[protocolEnd] != 0x09)
        {
            protocolEnd = protocolEnd + 1;
        }

        byte[] protocolBytes = CopyRange(input, 0, protocolEnd);
        if (!IsProtocol(protocolBytes) || protocolEnd == input.Length)
        {
            return CreateMalformed(protocolEnd);
        }

        int receivedByStart = SkipWhitespace(input, protocolEnd);
        if (receivedByStart == protocolEnd)
        {
            return CreateMalformed(protocolEnd);
        }

        int receivedByEnd = receivedByStart;
        while (receivedByEnd < input.Length && input[receivedByEnd] != 0x20 &&
            input[receivedByEnd] != 0x09)
        {
            receivedByEnd = receivedByEnd + 1;
        }

        int colon = FindByteRange(input, receivedByStart, receivedByEnd, 0x3a);
        int pseudonymEnd = colon < 0 ? receivedByEnd : colon;
        if (!IsTokenRange(input, receivedByStart, pseudonymEnd))
        {
            return CreateMalformed(receivedByStart);
        }

        HttpViaEntry entry = new HttpViaEntry();
        entry.ReceivedProtocol = Encoding.ASCII.GetString(protocolBytes);
        entry.ReceivedBy = Encoding.ASCII.GetString(
            input,
            receivedByStart,
            pseudonymEnd - receivedByStart);
        if (colon >= 0)
        {
            IntegerParseResult port = IntegerParser.ParseDecimal(
                input.AsSpan(colon + 1, receivedByEnd - colon - 1));
            if (!port.IsValid || port.Consumed != receivedByEnd - colon - 1 || port.Value > 65535)
            {
                return CreateMalformed(colon + 1);
            }

            entry.HasPort = true;
            entry.Port = (int)port.Value;
        }

        int position = SkipWhitespace(input, receivedByEnd);
        if (position < input.Length)
        {
            if (position == receivedByEnd || input[position] != 0x28)
            {
                return CreateMalformed(position);
            }

            byte[] commentBytes = CopyRange(input, position, input.Length);
            FieldValueParseResult comment = FieldValueParser.ParseComment(
                commentBytes,
                commentBytes.Length,
                maximumCommentDepth);
            if (comment.IsLimitExceeded)
            {
                return CreateLimit();
            }

            if (comment.Status != HttpOperationStatus.Done)
            {
                return CreateMalformed(position + comment.ErrorPosition);
            }

            entry.Comment = comment.Value;
        }

        CommonFieldParseResult result = CreateDone(input.Length);
        result.ViaEntries.Add(entry);
        return result;
    }

    static bool IsProtocol(byte[] input)
    {
        int slash = FindByte(input, 0x2f);
        if (slash < 0)
        {
            return IsTokenRange(input, 0, input.Length);
        }

        if (!IsTokenRange(input, 0, slash) || !IsTokenRange(input, slash + 1, input.Length))
        {
            return false;
        }

        return FindByteRange(input, slash + 1, input.Length, 0x2f) < 0;
    }

    static bool IsTokenRange(byte[] input, int start, int end)
    {
        if (start >= end)
        {
            return false;
        }

        int index = start;
        while (index < end)
        {
            if (!HttpGrammar.IsTokenCharacter(input[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int ReadTokenEnd(byte[] input, int start)
    {
        int position = start;
        while (position < input.Length && HttpGrammar.IsTokenCharacter(input[position]))
        {
            position = position + 1;
        }

        return position;
    }

    static int ReadQuotedEnd(byte[] input, int start)
    {
        if (start >= input.Length || input[start] != 0x22)
        {
            return start;
        }

        int position = start + 1;
        while (position < input.Length)
        {
            if (input[position] == 0x5c)
            {
                position = position + 2;
                continue;
            }

            if (input[position] == 0x22)
            {
                return position + 1;
            }

            position = position + 1;
        }

        return start;
    }

    static int SkipWhitespace(byte[] input, int start)
    {
        int position = start;
        while (position < input.Length && (input[position] == 0x20 || input[position] == 0x09))
        {
            position = position + 1;
        }

        return position;
    }

    static int FindByte(byte[] input, byte value)
    {
        return FindByteRange(input, 0, input.Length, value);
    }

    static int FindByteRange(byte[] input, int start, int end, byte value)
    {
        int index = start;
        while (index < end)
        {
            if (input[index] == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static byte[] CopyRange(byte[] input, int start, int end)
    {
        byte[] output = new byte[end - start];
        int index = 0;
        while (index < output.Length)
        {
            output[index] = input[start + index];
            index = index + 1;
        }

        return output;
    }

    static byte[] CopyBytes(byte[] input)
    {
        return CopyRange(input, 0, input.Length);
    }

    static CommonFieldParseResult CreateDone(int consumed)
    {
        CommonFieldParseResult result = new CommonFieldParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        return result;
    }

    static CommonFieldParseResult CreateMalformed(int errorPosition)
    {
        CommonFieldParseResult result = new CommonFieldParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static CommonFieldParseResult CreateLimit()
    {
        CommonFieldParseResult result = new CommonFieldParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }

    static FieldValueParseResult CreateDoneField(int consumed)
    {
        FieldValueParseResult result = new FieldValueParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        result.NextPosition = consumed;
        return result;
    }

    static FieldValueParseResult CreateMalformedField(int errorPosition)
    {
        FieldValueParseResult result = new FieldValueParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static FieldValueParseResult CreateLimitField()
    {
        FieldValueParseResult result = new FieldValueParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }

    static MediaTypeParseResult CreateDoneMedia(int consumed)
    {
        MediaTypeParseResult result = new MediaTypeParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        return result;
    }

    static MediaTypeParseResult CreateMalformedMedia(int errorPosition)
    {
        MediaTypeParseResult result = new MediaTypeParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static MediaTypeParseResult CreateLimitMedia()
    {
        MediaTypeParseResult result = new MediaTypeParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }
}
