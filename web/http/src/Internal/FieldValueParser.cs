using System;
using System.Collections.Generic;
using System.Text;

namespace http;

internal static class FieldValueParser
{
    internal static HttpPriority ParsePriority(byte[] input)
    {
        return PriorityCodec.Parse(input);
    }

    internal static CacheControlParseResult ParseCacheControlValue(
        byte[] input,
        int maximumSize,
        int maximumDirectives,
        int maximumFieldNames)
    {
        return CacheControlCodec.Parse(
            input,
            maximumSize,
            maximumDirectives,
            maximumFieldNames);
    }

    internal static FieldValueParseResult ParseAllow(
        byte[] input,
        int maximumSize,
        int maximumMethods)
    {
        return CommonFieldValueParser.ParseTokenList(input, maximumSize, maximumMethods);
    }

    internal static FieldValueParseResult ParseConnection(
        byte[] input,
        int maximumSize,
        int maximumOptions)
    {
        return CommonFieldValueParser.ParseTokenList(input, maximumSize, maximumOptions);
    }

    internal static FieldValueParseResult ParseUpgrade(
        byte[] input,
        int maximumSize,
        int maximumProtocols)
    {
        return CommonFieldValueParser.ParseUpgrade(input, maximumSize, maximumProtocols);
    }

    internal static MediaTypeParseResult ParseTe(
        byte[] input,
        int maximumSize,
        int maximumCodings,
        int maximumParameters)
    {
        return CommonFieldValueParser.ParseTe(
            input,
            maximumSize,
            maximumCodings,
            maximumParameters);
    }

    internal static FieldValueParseResult ParseTrailer(
        byte[] input,
        int maximumSize,
        int maximumNames)
    {
        return CommonFieldValueParser.ParseTokenList(input, maximumSize, maximumNames);
    }

    internal static CommonFieldParseResult ParseExpect(
        byte[] input,
        int maximumSize,
        int maximumExpectations,
        int maximumParameters)
    {
        return CommonFieldValueParser.ParseExpect(
            input,
            maximumSize,
            maximumExpectations,
            maximumParameters);
    }

    internal static CommonFieldParseResult ParseMaxForwards(byte[] input, int maximumSize)
    {
        return CommonFieldValueParser.ParseNumber(input, maximumSize);
    }

    internal static FieldValueParseResult ParseLocation(byte[] input, int maximumSize)
    {
        return CommonFieldValueParser.ParseLocation(input, maximumSize, true);
    }

    internal static FieldValueParseResult ParseContentLocation(byte[] input, int maximumSize)
    {
        return CommonFieldValueParser.ParseLocation(input, maximumSize, false);
    }

    internal static CommonFieldParseResult ParseRetryAfter(byte[] input, int maximumSize)
    {
        return CommonFieldValueParser.ParseRetryAfter(input, maximumSize);
    }

    internal static FieldValueParseResult ParseVary(
        byte[] input,
        int maximumSize,
        int maximumNames)
    {
        return CommonFieldValueParser.ParseTokenList(input, maximumSize, maximumNames);
    }

    internal static CommonFieldParseResult ParseViaEntries(
        byte[] input,
        int maximumSize,
        int maximumEntries,
        int maximumCommentDepth)
    {
        return CommonFieldValueParser.ParseVia(
            input,
            maximumSize,
            maximumEntries,
            maximumCommentDepth);
    }

    internal static AuthenticationParseResult ParseAuthenticationCredentials(
        byte[] input,
        int maximumSize,
        int maximumParameters)
    {
        if (input == null)
        {
            return CreateMalformedAuthentication(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize || maximumParameters < 0)
        {
            return CreateLimitAuthentication();
        }

        AuthenticationParseResult result = ReadAuthenticationValueAt(
            input,
            0,
            input.Length,
            maximumParameters,
            false);
        if (result.Status == HttpOperationStatus.Done && result.NextPosition != input.Length)
        {
            return CreateMalformedAuthentication(result.NextPosition);
        }

        if (result.Status == HttpOperationStatus.Done)
        {
            result.Consumed = input.Length;
        }

        return result;
    }

    internal static AuthenticationParseResult ParseAuthenticationValues(
        byte[] input,
        int maximumSize,
        int maximumChallenges,
        int maximumParameters)
    {
        if (input == null)
        {
            return CreateMalformedAuthentication(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize ||
            maximumChallenges < 0 || maximumParameters < 0)
        {
            return CreateLimitAuthentication();
        }

        AuthenticationParseResult result = CreateDoneAuthentication(input.Length);
        int position = 0;
        bool afterChallengeComma = false;
        while (position < input.Length)
        {
            position = SkipOptionalWhitespace(input, position, input.Length);
            if (position >= input.Length)
            {
                if (afterChallengeComma)
                {
                    if (result.EmptyElements >= maximumChallenges)
                    {
                        return CreateLimitAuthentication();
                    }

                    result.EmptyElements = result.EmptyElements + 1;
                }

                return result;
            }

            if (input[position] == 0x2c)
            {
                if (result.EmptyElements >= maximumChallenges)
                {
                    return CreateLimitAuthentication();
                }

                result.EmptyElements = result.EmptyElements + 1;
                position = position + 1;
                afterChallengeComma = true;
                continue;
            }

            if (result.Values.Count >= maximumChallenges)
            {
                return CreateLimitAuthentication();
            }

            afterChallengeComma = false;
            AuthenticationParseResult value = ReadAuthenticationValueAt(
                input,
                position,
                input.Length,
                maximumParameters,
                true);
            if (value.Status != HttpOperationStatus.Done)
            {
                return value;
            }

            result.Values.Add(value.Value);
            if (result.Values.Count == 1)
            {
                result.Value = value.Value;
            }

            position = value.NextPosition;
            if (position < input.Length)
            {
                if (input[position] != 0x2c)
                {
                    return CreateMalformedAuthentication(position);
                }

                position = position + 1;
                afterChallengeComma = true;
            }
        }

        if (afterChallengeComma)
        {
            if (result.EmptyElements >= maximumChallenges)
            {
                return CreateLimitAuthentication();
            }

            result.EmptyElements = result.EmptyElements + 1;
        }

        return result;
    }

    internal static FieldValueParseResult ParseRangeUnit(byte[] input, int maximumSize)
    {
        return ParseToken(input, maximumSize);
    }

    internal static RangeValueParseResult ParseRangeValue(
        byte[] input,
        int maximumSize,
        int maximumRanges)
    {
        if (input == null)
        {
            return CreateMalformedRange(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize || maximumRanges < 0)
        {
            return CreateLimitRange();
        }

        int equals = FindByte(input, 0x3d);
        if (equals <= 0)
        {
            return CreateMalformedRange(equals < 0 ? 0 : equals);
        }

        byte[] unitBytes = CopyRange(input, 0, equals);
        FieldValueParseResult unit = ParseRangeUnit(unitBytes, unitBytes.Length);
        if (unit.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedRange(unit.ErrorPosition);
        }

        FieldValueParseResult members = ParseRawRangeMembers(input, equals + 1, maximumRanges);
        if (members.IsLimitExceeded)
        {
            return CreateLimitRange();
        }

        if (members.Status != HttpOperationStatus.Done || members.Values.Count == 0)
        {
            return CreateMalformedRange(members.ErrorPosition);
        }

        RangeValueParseResult result = CreateDoneRange(input.Length);
        result.Range.Unit = Encoding.ASCII.GetString(unit.Value);
        result.EmptyElements = members.EmptyElements;
        bool bytesUnit = Ascii.EqualsStringsIgnoreCase(result.Range.Unit, HttpRangeUnits.Bytes);
        result.IsExtensionUnit = !bytesUnit;
        int index = 0;
        while (index < members.Values.Count)
        {
            byte[] member = members.Values[index];
            if (bytesUnit)
            {
                RangeValueParseResult parsedRange = ParseByteRangeMember(member);
                if (parsedRange.Status != HttpOperationStatus.Done)
                {
                    return parsedRange;
                }

                result.Range.Ranges.Add(parsedRange.ByteRange);
            }
            else
            {
                if (!IsOtherRange(member))
                {
                    return CreateMalformedRange(index);
                }

                result.Range.OtherRanges.Add(member);
            }

            index = index + 1;
        }

        return result;
    }

    internal static RangeValueParseResult ParseContentRange(byte[] input, int maximumSize)
    {
        if (input == null)
        {
            return CreateMalformedRange(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize)
        {
            return CreateLimitRange();
        }

        int space = FindByte(input, 0x20);
        if (space <= 0 || space + 1 >= input.Length)
        {
            return CreateMalformedRange(space < 0 ? 0 : space);
        }

        byte[] unitBytes = CopyRange(input, 0, space);
        FieldValueParseResult unit = ParseRangeUnit(unitBytes, unitBytes.Length);
        if (unit.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedRange(unit.ErrorPosition);
        }

        if (input[space + 1] == 0x20 || input[space + 1] == 0x09)
        {
            return CreateMalformedRange(space + 1);
        }

        RangeValueParseResult result = CreateDoneRange(input.Length);
        HttpContentRange contentRange = result.ContentRange;
        contentRange.Unit = Encoding.ASCII.GetString(unit.Value);
        int valueStart = space + 1;
        if (input[valueStart] == 0x2a)
        {
            if (valueStart + 2 >= input.Length || input[valueStart + 1] != 0x2f)
            {
                return CreateMalformedRange(valueStart);
            }

            IntegerParseResult complete = IntegerParser.ParseDecimal(
                input.AsSpan(valueStart + 2, input.Length - valueStart - 2));
            if (!complete.IsValid)
            {
                return CreateMalformedRange(valueStart + 2 + complete.Consumed);
            }

            contentRange.IsUnsatisfied = true;
            contentRange.HasCompleteLength = true;
            contentRange.CompleteLength = complete.Value;
            result.IsExtensionUnit = !Ascii.EqualsStringsIgnoreCase(
                contentRange.Unit,
                HttpRangeUnits.Bytes);
            return result;
        }

        int hyphen = FindByteFrom(input, 0x2d, valueStart);
        int slash = FindByteFrom(input, 0x2f, valueStart);
        if (hyphen <= valueStart || slash <= hyphen + 1 || slash + 1 >= input.Length)
        {
            return CreateMalformedRange(valueStart);
        }

        IntegerParseResult first = IntegerParser.ParseDecimal(
            input.AsSpan(valueStart, hyphen - valueStart));
        IntegerParseResult last = IntegerParser.ParseDecimal(
            input.AsSpan(hyphen + 1, slash - hyphen - 1));
        if (!first.IsValid || !last.IsValid)
        {
            return CreateMalformedRange(valueStart);
        }

        if (last.Value < first.Value)
        {
            return CreateMalformedRange(hyphen + 1);
        }

        contentRange.HasStart = true;
        contentRange.Start = first.Value;
        contentRange.HasEnd = true;
        contentRange.End = last.Value;
        if (input[slash + 1] == 0x2a)
        {
            if (slash + 2 != input.Length)
            {
                return CreateMalformedRange(slash + 2);
            }
        }
        else
        {
            IntegerParseResult complete = IntegerParser.ParseDecimal(
                input.AsSpan(slash + 1, input.Length - slash - 1));
            if (!complete.IsValid)
            {
                return CreateMalformedRange(slash + 1 + complete.Consumed);
            }

            if (complete.Value <= last.Value)
            {
                return CreateMalformedRange(slash + 1);
            }

            contentRange.HasCompleteLength = true;
            contentRange.CompleteLength = complete.Value;
        }

        result.IsExtensionUnit = !Ascii.EqualsStringsIgnoreCase(
            contentRange.Unit,
            HttpRangeUnits.Bytes);
        return result;
    }

    internal static RangeValueParseResult ParseAcceptRanges(
        byte[] input,
        int maximumSize,
        int maximumUnits)
    {
        if (input == null)
        {
            return CreateMalformedRange(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize || maximumUnits < 0)
        {
            return CreateLimitRange();
        }

        FieldValueParseResult members = ParseRawRangeMembers(input, 0, maximumUnits);
        if (members.IsLimitExceeded)
        {
            return CreateLimitRange();
        }

        if (members.Status != HttpOperationStatus.Done || members.Values.Count == 0)
        {
            return CreateMalformedRange(members.ErrorPosition);
        }

        RangeValueParseResult result = CreateDoneRange(input.Length);
        result.EmptyElements = members.EmptyElements;
        int index = 0;
        while (index < members.Values.Count)
        {
            FieldValueParseResult unit = ParseRangeUnit(
                members.Values[index],
                members.Values[index].Length);
            if (unit.Status != HttpOperationStatus.Done)
            {
                return CreateMalformedRange(index);
            }

            result.Units.Add(Encoding.ASCII.GetString(unit.Value));
            index = index + 1;
        }

        return result;
    }

    internal static EntityTagParseResult ParseEntityTag(byte[] input, int maximumSize)
    {
        if (input == null)
        {
            return CreateMalformedEntityTag(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize)
        {
            return CreateLimitEntityTag();
        }

        EntityTagParseResult result = ReadEntityTagAt(input, 0, input.Length);
        if (result.Status == HttpOperationStatus.Done && result.NextPosition != input.Length)
        {
            return CreateMalformedEntityTag(result.NextPosition);
        }

        if (result.Status == HttpOperationStatus.Done)
        {
            result.Consumed = input.Length;
        }

        return result;
    }

    internal static EntityTagParseResult ParseEntityTags(
        byte[] input,
        int maximumSize,
        int maximumMembers)
    {
        if (input == null)
        {
            return CreateMalformedEntityTag(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize || maximumMembers < 0)
        {
            return CreateLimitEntityTag();
        }

        EntityTagParseResult result = CreateDoneEntityTag(input.Length);
        int position = SkipOptionalWhitespace(input, 0, input.Length);
        if (position >= input.Length)
        {
            return result;
        }

        if (input[position] == 0x2a)
        {
            int wildcardEnd = SkipOptionalWhitespace(input, position + 1, input.Length);
            if (wildcardEnd != input.Length)
            {
                return CreateMalformedEntityTag(wildcardEnd);
            }

            if (maximumMembers < 1)
            {
                return CreateLimitEntityTag();
            }

            HttpEntityTag wildcard = new HttpEntityTag();
            wildcard.IsWildcard = true;
            result.IsWildcard = true;
            result.Value = wildcard;
            result.Values.Add(wildcard);
            return result;
        }

        bool afterComma = false;
        while (position < input.Length)
        {
            position = SkipOptionalWhitespace(input, position, input.Length);
            if (position >= input.Length)
            {
                if (afterComma)
                {
                    result.EmptyElements = result.EmptyElements + 1;
                    if (result.EmptyElements > maximumMembers)
                    {
                        return CreateLimitEntityTag();
                    }
                }

                return result;
            }

            if (input[position] == 0x2c)
            {
                result.EmptyElements = result.EmptyElements + 1;
                if (result.EmptyElements > maximumMembers)
                {
                    return CreateLimitEntityTag();
                }

                position = position + 1;
                afterComma = true;
                continue;
            }

            if (result.Values.Count >= maximumMembers)
            {
                return CreateLimitEntityTag();
            }

            EntityTagParseResult member = ReadEntityTagAt(input, position, input.Length);
            if (member.Status != HttpOperationStatus.Done)
            {
                return member;
            }

            result.Values.Add(member.Value);
            if (result.Values.Count == 1)
            {
                result.Value = member.Value;
            }

            position = SkipOptionalWhitespace(input, member.NextPosition, input.Length);
            afterComma = false;
            if (position >= input.Length)
            {
                return result;
            }

            if (input[position] != 0x2c)
            {
                return CreateMalformedEntityTag(position);
            }

            position = position + 1;
            afterComma = true;
        }

        if (afterComma)
        {
            result.EmptyElements = result.EmptyElements + 1;
            if (result.EmptyElements > maximumMembers)
            {
                return CreateLimitEntityTag();
            }
        }

        return result;
    }

    internal static QualityValueParseResult ParseQualityValue(byte[] input, int maximumSize)
    {
        if (input == null)
        {
            return CreateMalformedQuality(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize)
        {
            return CreateLimitQuality();
        }

        if (input.Length == 0)
        {
            return CreateMalformedQuality(0);
        }

        byte whole = input[0];
        if (whole != 0x30 && whole != 0x31)
        {
            return CreateMalformedQuality(0);
        }

        if (input.Length == 1)
        {
            return CreateDoneQuality(whole == 0x31 ? 1000 : 0, 1);
        }

        if (input[1] != 0x2e)
        {
            return CreateMalformedQuality(1);
        }

        int fractionalDigits = input.Length - 2;
        if (fractionalDigits > 3)
        {
            return CreateMalformedQuality(5);
        }

        int quality = 0;
        int position = 2;
        while (position < input.Length)
        {
            byte digit = input[position];
            if (digit < 0x30 || digit > 0x39)
            {
                return CreateMalformedQuality(position);
            }

            if (whole == 0x31 && digit != 0x30)
            {
                return CreateMalformedQuality(position);
            }

            if (whole == 0x30)
            {
                if (position == 2)
                {
                    quality = quality + (digit - 0x30) * 100;
                }
                else if (position == 3)
                {
                    quality = quality + (digit - 0x30) * 10;
                }
                else
                {
                    quality = quality + digit - 0x30;
                }
            }

            position = position + 1;
        }

        if (whole == 0x31)
        {
            quality = 1000;
        }

        return CreateDoneQuality(quality, input.Length);
    }

    internal static MediaTypeParseResult ParseMediaType(
        byte[] input,
        int maximumSize,
        int maximumParameters)
    {
        return ParseMediaValue(input, maximumSize, maximumParameters, false);
    }

    internal static MediaTypeParseResult ParseMediaRange(
        byte[] input,
        int maximumSize,
        int maximumParameters)
    {
        return ParseMediaValue(input, maximumSize, maximumParameters, true);
    }

    internal static MediaTypeParseResult ParseWeightedMediaRanges(
        byte[] input,
        int maximumSize,
        int maximumMembers,
        int maximumParameters)
    {
        return ParseWeightedList(input, maximumSize, maximumMembers, maximumParameters, true);
    }

    internal static MediaTypeParseResult ParseWeightedValues(
        byte[] input,
        int maximumSize,
        int maximumMembers,
        int maximumParameters)
    {
        return ParseWeightedList(input, maximumSize, maximumMembers, maximumParameters, false);
    }

    internal static LanguageRangeParseResult ParseLanguageRanges(
        byte[] input,
        int maximumSize,
        int maximumRanges,
        bool allowExtended)
    {
        MediaTypeParseResult parsed = ParseWeightedValues(
            input,
            maximumSize,
            maximumRanges,
            int.MaxValue);
        LanguageRangeParseResult result = new LanguageRangeParseResult();
        result.Status = parsed.Status;
        result.Consumed = parsed.Consumed;
        result.ErrorPosition = parsed.ErrorPosition;
        result.EmptyElements = parsed.EmptyElements;
        result.IsMalformed = parsed.IsMalformed;
        result.IsLimitExceeded = parsed.IsLimitExceeded;
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return result;
        }

        int index = 0;
        while (index < parsed.Values.Count)
        {
            HttpWeightedValue weighted = parsed.Values[index];
            if (weighted.Parameters.Count != 0 ||
                !IsValidLanguageRange(weighted.Value, allowExtended))
            {
                result.Status = HttpOperationStatus.Rejected;
                result.ErrorPosition = index;
                result.IsMalformed = true;
                return result;
            }

            HttpLanguageRange range = new HttpLanguageRange();
            range.Range = weighted.Value;
            range.IsWildcard = weighted.Value == "*";
            range.Quality = weighted.Quality;
            range.SourceOrder = weighted.SourceOrder;
            range.Specificity = CalculateLanguageRangeSpecificity(weighted.Value);
            result.Values.Add(range);
            index = index + 1;
        }

        return result;
    }

    internal static FieldValueParseResult ParseToken(byte[] input, int maximumSize)
    {
        FieldValueParseResult limitResult = ValidateInputSize(input, maximumSize);
        if (limitResult.Status != HttpOperationStatus.Done)
        {
            return limitResult;
        }

        FieldValueParseResult result = ReadTokenAt(input, 0, input.Length);
        if (result.Status == HttpOperationStatus.Done && result.NextPosition != input.Length)
        {
            return CreateMalformed(result.NextPosition);
        }

        if (result.Status == HttpOperationStatus.Done)
        {
            result.Consumed = input.Length;
        }

        return result;
    }

    internal static FieldValueParseResult ParseQuotedString(byte[] input, int maximumSize)
    {
        FieldValueParseResult limitResult = ValidateInputSize(input, maximumSize);
        if (limitResult.Status != HttpOperationStatus.Done)
        {
            return limitResult;
        }

        FieldValueParseResult result = ReadQuotedStringAt(input, 0, input.Length);
        if (result.Status == HttpOperationStatus.Done && result.NextPosition != input.Length)
        {
            return CreateMalformed(result.NextPosition);
        }

        if (result.Status == HttpOperationStatus.Done)
        {
            result.Consumed = input.Length;
        }

        return result;
    }

    internal static FieldValueParseResult ParseQuotedPair(byte[] input, int maximumSize)
    {
        FieldValueParseResult limitResult = ValidateInputSize(input, maximumSize);
        if (limitResult.Status != HttpOperationStatus.Done)
        {
            return limitResult;
        }

        if (input.Length != 2 || input[0] != 0x5c)
        {
            return CreateMalformed(0);
        }

        if (!HttpGrammar.IsQuotedPairCharacter(input[1]))
        {
            return CreateMalformed(1);
        }

        FieldValueParseResult result = CreateDone(input.Length);
        result.Value = new byte[] { input[1] };
        return result;
    }

    internal static FieldValueParseResult ParseOptionalWhitespace(byte[] input, int maximumSize)
    {
        FieldValueParseResult limitResult = ValidateInputSize(input, maximumSize);
        if (limitResult.Status != HttpOperationStatus.Done)
        {
            return limitResult;
        }

        int index = 0;
        while (index < input.Length)
        {
            if (input[index] != 0x20 && input[index] != 0x09)
            {
                return CreateMalformed(index);
            }

            index = index + 1;
        }

        FieldValueParseResult result = CreateDone(input.Length);
        result.Value = CopyRange(input, 0, input.Length);
        return result;
    }

    internal static FieldValueParseResult ParseCommaList(
        byte[] input,
        int maximumSize,
        int maximumMembers,
        int maximumDepth)
    {
        return ParseList(input, maximumSize, maximumMembers, maximumDepth, false);
    }

    internal static FieldValueParseResult ParseHashList(
        byte[] input,
        int maximumSize,
        int maximumMembers,
        int maximumDepth)
    {
        return ParseList(input, maximumSize, maximumMembers, maximumDepth, true);
    }

    internal static FieldValueParseResult ParseComment(byte[] input, int maximumSize, int maximumDepth)
    {
        FieldValueParseResult limitResult = ValidateInputSize(input, maximumSize);
        if (limitResult.Status != HttpOperationStatus.Done)
        {
            return limitResult;
        }

        if (maximumDepth < 0)
        {
            return CreateLimit(0);
        }

        FieldValueParseResult result = ReadCommentAt(input, 0, input.Length, maximumDepth);
        if (result.Status == HttpOperationStatus.Done && result.NextPosition != input.Length)
        {
            return CreateMalformed(result.NextPosition);
        }

        if (result.Status == HttpOperationStatus.Done)
        {
            result.Consumed = input.Length;
        }

        return result;
    }

    internal static FieldValueParseResult ParseParameters(
        byte[] input,
        int maximumSize,
        int maximumParameters)
    {
        FieldValueParseResult limitResult = ValidateInputSize(input, maximumSize);
        if (limitResult.Status != HttpOperationStatus.Done)
        {
            return limitResult;
        }

        if (maximumParameters < 0)
        {
            return CreateLimit(0);
        }

        FieldValueParseResult result = CreateDone(input.Length);
        int position = SkipOptionalWhitespace(input, 0, input.Length);
        int separators = 0;
        while (position < input.Length)
        {
            if (input[position] != 0x3b)
            {
                return CreateMalformed(position);
            }

            separators = separators + 1;
            if (separators > maximumParameters)
            {
                return CreateLimit(position);
            }

            position = SkipOptionalWhitespace(input, position + 1, input.Length);
            if (position >= input.Length || input[position] == 0x3b)
            {
                result.EmptyElements = result.EmptyElements + 1;
                continue;
            }

            FieldValueParseResult nameResult = ReadTokenAt(input, position, input.Length);
            if (nameResult.Status != HttpOperationStatus.Done)
            {
                return nameResult;
            }

            position = nameResult.NextPosition;
            if (position >= input.Length || input[position] != 0x3d)
            {
                return CreateMalformed(position);
            }

            position = position + 1;
            FieldValueParseResult valueResult;
            bool quoted = false;
            if (position < input.Length && input[position] == 0x22)
            {
                quoted = true;
                valueResult = ReadQuotedStringAt(input, position, input.Length);
            }
            else
            {
                valueResult = ReadTokenAt(input, position, input.Length);
            }

            if (valueResult.Status != HttpOperationStatus.Done)
            {
                return valueResult;
            }

            if (result.Parameters.Count >= maximumParameters)
            {
                return CreateLimit(position);
            }

            HttpParameter parameter = new HttpParameter();
            parameter.Name = Encoding.ASCII.GetString(nameResult.Value);
            parameter.HasValue = true;
            parameter.Value = valueResult.Value;
            parameter.IsQuoted = quoted;
            result.Parameters.Add(parameter);

            position = SkipOptionalWhitespace(input, valueResult.NextPosition, input.Length);
            if (position < input.Length && input[position] != 0x3b)
            {
                return CreateMalformed(position);
            }
        }

        result.Consumed = input.Length;
        result.NextPosition = input.Length;
        return result;
    }

    static AuthenticationParseResult ReadAuthenticationValueAt(
        byte[] input,
        int start,
        int end,
        int maximumParameters,
        bool allowChallengeBoundary)
    {
        FieldValueParseResult scheme = ReadTokenAt(input, start, end);
        if (scheme.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedAuthentication(start);
        }

        AuthenticationParseResult result = CreateDoneAuthentication(scheme.NextPosition - start);
        result.Value.Scheme = Encoding.ASCII.GetString(scheme.Value);
        int position = scheme.NextPosition;
        if (position >= end)
        {
            result.NextPosition = end;
            return result;
        }

        if (input[position] == 0x2c && allowChallengeBoundary)
        {
            result.NextPosition = position;
            return result;
        }

        if (input[position] != 0x20)
        {
            int whitespaceEnd = SkipOptionalWhitespace(input, position, end);
            if (whitespaceEnd == end)
            {
                result.NextPosition = end;
                return result;
            }

            if (allowChallengeBoundary && input[whitespaceEnd] == 0x2c)
            {
                result.NextPosition = whitespaceEnd;
                return result;
            }

            return CreateMalformedAuthentication(position);
        }

        while (position < end && input[position] == 0x20)
        {
            position = position + 1;
        }

        position = SkipOptionalWhitespace(input, position, end);
        if (position >= end)
        {
            result.NextPosition = end;
            return result;
        }

        if (input[position] == 0x2c && allowChallengeBoundary)
        {
            result.NextPosition = position;
            return result;
        }

        AuthenticationParseResult firstParameter = ReadAuthenticationParameterAt(input, position, end);
        if (firstParameter.Status == HttpOperationStatus.Done)
        {
            if (maximumParameters < 1)
            {
                return CreateLimitAuthentication();
            }

            result.Value.Parameters.Add(firstParameter.Parameter);
            position = firstParameter.NextPosition;
            while (position < end)
            {
                position = SkipOptionalWhitespace(input, position, end);
                if (position >= end)
                {
                    result.NextPosition = end;
                    return result;
                }

                if (input[position] != 0x2c)
                {
                    return CreateMalformedAuthentication(position);
                }

                int boundary = position;
                int candidate = position + 1;
                int emptyElements = 0;
                while (candidate < end)
                {
                    candidate = SkipOptionalWhitespace(input, candidate, end);
                    if (candidate < end && input[candidate] == 0x2c)
                    {
                        emptyElements = emptyElements + 1;
                        if (emptyElements > maximumParameters)
                        {
                            return CreateLimitAuthentication();
                        }

                        candidate = candidate + 1;
                        continue;
                    }

                    break;
                }

                if (candidate >= end)
                {
                    if (allowChallengeBoundary)
                    {
                        result.NextPosition = boundary;
                    }
                    else
                    {
                        if (emptyElements >= maximumParameters - result.EmptyElements)
                        {
                            return CreateLimitAuthentication();
                        }

                        result.NextPosition = end;
                        result.EmptyElements = result.EmptyElements + emptyElements + 1;
                    }

                    return result;
                }

                AuthenticationParseResult nextParameter = ReadAuthenticationParameterAt(
                    input,
                    candidate,
                    end);
                if (nextParameter.Status != HttpOperationStatus.Done)
                {
                    if (allowChallengeBoundary)
                    {
                        result.NextPosition = boundary;
                        return result;
                    }

                    return CreateMalformedAuthentication(candidate);
                }

                if (HasAuthenticationParameter(result.Value.Parameters, nextParameter.Parameter.Name))
                {
                    AuthenticationParseResult duplicate = CreateMalformedAuthentication(candidate);
                    duplicate.IsDuplicateParameter = true;
                    return duplicate;
                }

                if (result.Value.Parameters.Count >= maximumParameters)
                {
                    return CreateLimitAuthentication();
                }

                if (emptyElements > maximumParameters - result.EmptyElements)
                {
                    return CreateLimitAuthentication();
                }

                result.EmptyElements = result.EmptyElements + emptyElements;
                result.Value.Parameters.Add(nextParameter.Parameter);
                position = nextParameter.NextPosition;
            }

            result.NextPosition = end;
            return result;
        }

        FieldValueParseResult token68 = ReadToken68At(input, position, end);
        if (token68.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedAuthentication(token68.ErrorPosition);
        }

        result.Value.Token68 = token68.Value;
        position = SkipOptionalWhitespace(input, token68.NextPosition, end);
        if (position < end && (!allowChallengeBoundary || input[position] != 0x2c))
        {
            return CreateMalformedAuthentication(position);
        }

        result.NextPosition = position;
        return result;
    }

    static AuthenticationParseResult ReadAuthenticationParameterAt(
        byte[] input,
        int start,
        int end)
    {
        FieldValueParseResult name = ReadTokenAt(input, start, end);
        if (name.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedAuthentication(start);
        }

        int position = SkipOptionalWhitespace(input, name.NextPosition, end);
        if (position >= end || input[position] != 0x3d)
        {
            return CreateMalformedAuthentication(position);
        }

        position = SkipOptionalWhitespace(input, position + 1, end);
        FieldValueParseResult value;
        bool quoted = false;
        if (position < end && input[position] == 0x22)
        {
            quoted = true;
            value = ReadQuotedStringAt(input, position, end);
        }
        else
        {
            value = ReadTokenAt(input, position, end);
        }

        if (value.Status != HttpOperationStatus.Done)
        {
            return CreateMalformedAuthentication(position);
        }

        AuthenticationParseResult result = CreateDoneAuthentication(value.NextPosition - start);
        result.NextPosition = value.NextPosition;
        result.Parameter.Name = Encoding.ASCII.GetString(name.Value);
        result.Parameter.HasValue = true;
        result.Parameter.Value = value.Value;
        result.Parameter.IsQuoted = quoted;
        return result;
    }

    static FieldValueParseResult ReadToken68At(byte[] input, int start, int end)
    {
        int position = start;
        int coreCharacters = 0;
        bool padding = false;
        while (position < end)
        {
            byte character = input[position];
            if (character == 0x20 || character == 0x09 || character == 0x2c)
            {
                break;
            }

            if (character == 0x3d)
            {
                if (coreCharacters == 0)
                {
                    return CreateMalformed(position);
                }

                padding = true;
            }
            else
            {
                if (padding || !IsToken68Character(character))
                {
                    return CreateMalformed(position);
                }

                coreCharacters = coreCharacters + 1;
            }

            position = position + 1;
        }

        if (coreCharacters == 0)
        {
            return CreateMalformed(start);
        }

        FieldValueParseResult result = CreateDone(position - start);
        result.NextPosition = position;
        result.Value = CopyRange(input, start, position);
        return result;
    }

    static bool IsToken68Character(byte value)
    {
        if (value >= 0x41 && value <= 0x5a || value >= 0x61 && value <= 0x7a ||
            value >= 0x30 && value <= 0x39)
        {
            return true;
        }

        return value == 0x2d || value == 0x2e || value == 0x5f || value == 0x7e ||
            value == 0x2b || value == 0x2f;
    }

    static bool HasAuthenticationParameter(List<HttpParameter> parameters, string name)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            if (Ascii.EqualsStringsIgnoreCase(parameters[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static AuthenticationParseResult CreateDoneAuthentication(int consumed)
    {
        AuthenticationParseResult result = new AuthenticationParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        return result;
    }

    static AuthenticationParseResult CreateMalformedAuthentication(int errorPosition)
    {
        AuthenticationParseResult result = new AuthenticationParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static AuthenticationParseResult CreateLimitAuthentication()
    {
        AuthenticationParseResult result = new AuthenticationParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }

    static EntityTagParseResult ReadEntityTagAt(byte[] input, int start, int end)
    {
        int position = start;
        bool weak = false;
        if (position + 1 < end && input[position] == 0x57 && input[position + 1] == 0x2f)
        {
            weak = true;
            position = position + 2;
        }

        if (position >= end || input[position] != 0x22)
        {
            return CreateMalformedEntityTag(position);
        }

        int opaqueStart = position + 1;
        position = opaqueStart;
        while (position < end)
        {
            byte character = input[position];
            if (character == 0x22)
            {
                EntityTagParseResult result = CreateDoneEntityTag(position + 1 - start);
                result.NextPosition = position + 1;
                result.Value.IsWeak = weak;
                result.Value.OpaqueTag = CopyRange(input, opaqueStart, position);
                return result;
            }

            if (!IsEntityTagCharacter(character))
            {
                return CreateMalformedEntityTag(position);
            }

            position = position + 1;
        }

        return CreateMalformedEntityTag(end);
    }

    static bool IsEntityTagCharacter(byte value)
    {
        if (value == 0x21)
        {
            return true;
        }

        if (value >= 0x23 && value <= 0x7e)
        {
            return true;
        }

        return value >= 0x80;
    }

    static EntityTagParseResult CreateDoneEntityTag(int consumed)
    {
        EntityTagParseResult result = new EntityTagParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        return result;
    }

    static EntityTagParseResult CreateMalformedEntityTag(int errorPosition)
    {
        EntityTagParseResult result = new EntityTagParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static EntityTagParseResult CreateLimitEntityTag()
    {
        EntityTagParseResult result = new EntityTagParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }

    static FieldValueParseResult ParseRawRangeMembers(
        byte[] input,
        int start,
        int maximumMembers)
    {
        FieldValueParseResult result = CreateDone(input.Length);
        int memberStart = start;
        int position = start;
        while (position < input.Length)
        {
            if (input[position] == 0x2c)
            {
                FieldValueParseResult added = AddRawRangeMember(
                    result,
                    input,
                    memberStart,
                    position,
                    maximumMembers);
                if (added.Status != HttpOperationStatus.Done)
                {
                    return added;
                }

                memberStart = position + 1;
            }

            position = position + 1;
        }

        FieldValueParseResult last = AddRawRangeMember(
            result,
            input,
            memberStart,
            input.Length,
            maximumMembers);
        if (last.Status != HttpOperationStatus.Done)
        {
            return last;
        }

        return result;
    }

    static FieldValueParseResult AddRawRangeMember(
        FieldValueParseResult result,
        byte[] input,
        int start,
        int end,
        int maximumMembers)
    {
        int contentStart = SkipOptionalWhitespace(input, start, end);
        int contentEnd = end;
        while (contentEnd > contentStart &&
            (input[contentEnd - 1] == 0x20 || input[contentEnd - 1] == 0x09))
        {
            contentEnd = contentEnd - 1;
        }

        if (contentStart == contentEnd)
        {
            if (result.EmptyElements >= maximumMembers)
            {
                return CreateLimit(contentStart);
            }

            result.EmptyElements = result.EmptyElements + 1;
            return CreateDone(contentEnd);
        }

        if (result.Values.Count >= maximumMembers)
        {
            return CreateLimit(contentStart);
        }

        result.Values.Add(CopyRange(input, contentStart, contentEnd));
        return CreateDone(contentEnd);
    }

    static RangeValueParseResult ParseByteRangeMember(byte[] input)
    {
        int hyphen = FindByte(input, 0x2d);
        if (hyphen < 0)
        {
            return CreateMalformedRange(0);
        }

        RangeValueParseResult result = CreateDoneRange(input.Length);
        if (hyphen == 0)
        {
            IntegerParseResult suffix = IntegerParser.ParseDecimal(
                input.AsSpan(1, input.Length - 1));
            if (!suffix.IsValid)
            {
                return CreateMalformedRange(1 + suffix.Consumed);
            }

            result.ByteRange.IsSuffix = true;
            result.ByteRange.SuffixLength = suffix.Value;
            return result;
        }

        IntegerParseResult first = IntegerParser.ParseDecimal(input.AsSpan(0, hyphen));
        if (!first.IsValid)
        {
            return CreateMalformedRange(first.Consumed);
        }

        result.ByteRange.HasStart = true;
        result.ByteRange.Start = first.Value;
        if (hyphen + 1 < input.Length)
        {
            IntegerParseResult last = IntegerParser.ParseDecimal(
                input.AsSpan(hyphen + 1, input.Length - hyphen - 1));
            if (!last.IsValid)
            {
                return CreateMalformedRange(hyphen + 1 + last.Consumed);
            }

            if (last.Value < first.Value)
            {
                return CreateMalformedRange(hyphen + 1);
            }

            result.ByteRange.HasEnd = true;
            result.ByteRange.End = last.Value;
        }

        return result;
    }

    static bool IsOtherRange(byte[] input)
    {
        if (input.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < input.Length)
        {
            byte character = input[index];
            if (character < 0x21 || character > 0x7e || character == 0x2c)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int FindByteFrom(byte[] input, byte value, int start)
    {
        int index = start;
        while (index < input.Length)
        {
            if (input[index] == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static RangeValueParseResult CreateDoneRange(int consumed)
    {
        RangeValueParseResult result = new RangeValueParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        return result;
    }

    static RangeValueParseResult CreateMalformedRange(int errorPosition)
    {
        RangeValueParseResult result = new RangeValueParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static RangeValueParseResult CreateLimitRange()
    {
        RangeValueParseResult result = new RangeValueParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }

    static MediaTypeParseResult ParseMediaValue(
        byte[] input,
        int maximumSize,
        int maximumParameters,
        bool allowWildcards)
    {
        if (input == null)
        {
            return CreateMalformedMedia(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize || maximumParameters < 0)
        {
            return CreateLimitMedia();
        }

        int semicolon = FindByte(input, 0x3b);
        int mediaEnd = input.Length;
        if (semicolon >= 0)
        {
            mediaEnd = semicolon;
            while (mediaEnd > 0 && (input[mediaEnd - 1] == 0x20 || input[mediaEnd - 1] == 0x09))
            {
                mediaEnd = mediaEnd - 1;
            }
        }

        int slash = -1;
        int position = 0;
        while (position < mediaEnd)
        {
            if (input[position] == 0x2f)
            {
                if (slash >= 0)
                {
                    return CreateMalformedMedia(position);
                }

                slash = position;
            }

            position = position + 1;
        }

        if (slash <= 0 || slash + 1 >= mediaEnd)
        {
            return CreateMalformedMedia(slash < 0 ? 0 : slash);
        }

        if (!IsTokenRange(input, 0, slash))
        {
            return CreateMalformedMedia(FindInvalidTokenByte(input, 0, slash));
        }

        if (!IsTokenRange(input, slash + 1, mediaEnd))
        {
            return CreateMalformedMedia(FindInvalidTokenByte(input, slash + 1, mediaEnd));
        }

        bool typeWildcard = slash == 1 && input[0] == 0x2a;
        bool subtypeWildcard = mediaEnd == slash + 2 && input[slash + 1] == 0x2a;
        if (!allowWildcards && (typeWildcard || subtypeWildcard))
        {
            return CreateMalformedMedia(typeWildcard ? 0 : slash + 1);
        }

        if (typeWildcard && !subtypeWildcard)
        {
            return CreateMalformedMedia(slash + 1);
        }

        MediaTypeParseResult result = CreateDoneMedia(input.Length);
        result.Value.IsTypeWildcard = typeWildcard;
        result.Value.IsSubtypeWildcard = subtypeWildcard;
        if (!typeWildcard)
        {
            result.Value.Type = Encoding.ASCII.GetString(input, 0, slash);
        }

        if (!subtypeWildcard)
        {
            result.Value.Subtype = Encoding.ASCII.GetString(input, slash + 1, mediaEnd - slash - 1);
        }

        if (semicolon >= 0)
        {
            byte[] parameterBytes = CopyRange(input, mediaEnd, input.Length);
            FieldValueParseResult parameters = ParseParameters(
                parameterBytes,
                parameterBytes.Length,
                maximumParameters);
            if (parameters.IsLimitExceeded)
            {
                return CreateLimitMedia();
            }

            if (parameters.Status != HttpOperationStatus.Done)
            {
                return CreateMalformedMedia(mediaEnd + parameters.ErrorPosition);
            }

            result.Value.Parameters = parameters.Parameters;
        }

        return result;
    }

    static MediaTypeParseResult ParseWeightedList(
        byte[] input,
        int maximumSize,
        int maximumMembers,
        int maximumParameters,
        bool mediaRanges)
    {
        if (maximumParameters < 0)
        {
            return CreateLimitMedia();
        }

        if (input == null)
        {
            return CreateMalformedMedia(0);
        }

        FieldValueParseResult list = ParseHashList(
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
            MediaTypeParseResult member = ParseWeightedMember(
                list.Values[index],
                maximumParameters,
                mediaRanges);
            if (member.Status != HttpOperationStatus.Done)
            {
                return member;
            }

            HttpWeightedValue value = member.Values[0];
            value.SourceOrder = index;
            result.Values.Add(value);
            index = index + 1;
        }

        return result;
    }

    static MediaTypeParseResult ParseWeightedMember(
        byte[] input,
        int maximumParameters,
        bool mediaRange)
    {
        List<HttpParameter> parameters;
        HttpMediaType parsedMedia = new HttpMediaType();
        string valueText;
        if (mediaRange)
        {
            MediaTypeParseResult media = ParseMediaRange(input, input.Length, maximumParameters);
            if (media.Status != HttpOperationStatus.Done)
            {
                return media;
            }

            parsedMedia = media.Value;
            parameters = parsedMedia.Parameters;
            valueText = GetMediaRangeText(parsedMedia);
        }
        else
        {
            int semicolon = FindByte(input, 0x3b);
            int valueEnd = input.Length;
            if (semicolon >= 0)
            {
                valueEnd = semicolon;
                while (valueEnd > 0 && (input[valueEnd - 1] == 0x20 || input[valueEnd - 1] == 0x09))
                {
                    valueEnd = valueEnd - 1;
                }
            }

            byte[] tokenBytes = CopyRange(input, 0, valueEnd);
            FieldValueParseResult token = ParseToken(tokenBytes, tokenBytes.Length);
            if (token.Status != HttpOperationStatus.Done)
            {
                return CreateMalformedMedia(token.ErrorPosition);
            }

            valueText = Encoding.ASCII.GetString(token.Value);
            parameters = new List<HttpParameter>();
            if (semicolon >= 0)
            {
                byte[] parameterBytes = CopyRange(input, valueEnd, input.Length);
                FieldValueParseResult parsedParameters = ParseParameters(
                    parameterBytes,
                    parameterBytes.Length,
                    maximumParameters);
                if (parsedParameters.IsLimitExceeded)
                {
                    return CreateLimitMedia();
                }

                if (parsedParameters.Status != HttpOperationStatus.Done)
                {
                    return CreateMalformedMedia(valueEnd + parsedParameters.ErrorPosition);
                }

                parameters = parsedParameters.Parameters;
            }
        }

        HttpWeightedValue weighted = new HttpWeightedValue();
        weighted.Value = valueText;
        int quality = 1000;
        bool hasQuality = false;
        int parameterIndex = 0;
        while (parameterIndex < parameters.Count)
        {
            HttpParameter parameter = parameters[parameterIndex];
            if (Ascii.EqualsStringsIgnoreCase(parameter.Name, "q"))
            {
                if (!parameter.HasValue || parameter.IsQuoted)
                {
                    return CreateMalformedMedia(parameterIndex);
                }

                QualityValueParseResult parsedQuality = ParseQualityValue(
                    parameter.Value,
                    parameter.Value.Length);
                if (parsedQuality.Status != HttpOperationStatus.Done)
                {
                    return CreateMalformedMedia(parameterIndex);
                }

                if (hasQuality)
                {
                    MediaTypeParseResult duplicate = CreateMalformedMedia(parameterIndex);
                    duplicate.IsDuplicateQuality = true;
                    duplicate.IsConflictingQuality = quality != parsedQuality.Quality;
                    return duplicate;
                }

                hasQuality = true;
                quality = parsedQuality.Quality;
            }
            else
            {
                weighted.Parameters.Add(parameter);
            }

            parameterIndex = parameterIndex + 1;
        }

        weighted.Quality = quality;
        if (mediaRange)
        {
            weighted.Specificity = CalculateMediaSpecificity(parsedMedia, weighted.Parameters.Count);
        }
        else
        {
            weighted.Specificity = weighted.Parameters.Count;
            if (weighted.Value != "*")
            {
                weighted.Specificity = weighted.Specificity + 1000;
            }
        }

        MediaTypeParseResult result = CreateDoneMedia(input.Length);
        result.Values.Add(weighted);
        return result;
    }

    static int CalculateMediaSpecificity(HttpMediaType value, int parameterCount)
    {
        int specificity = parameterCount;
        if (!value.IsTypeWildcard)
        {
            specificity = specificity + 1000;
        }

        if (!value.IsSubtypeWildcard)
        {
            specificity = specificity + 1000;
        }

        return specificity;
    }

    static bool IsValidLanguageRange(string value, bool allowExtended)
    {
        if (value == null || value.Length == 0)
        {
            return false;
        }

        if (value == "*")
        {
            return true;
        }

        int start = 0;
        bool first = true;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '-')
            {
                int length = index - start;
                if (length == 0 || length > 8)
                {
                    return false;
                }

                if (length == 1 && value[start] == '*')
                {
                    if (!allowExtended)
                    {
                        return false;
                    }
                }
                else if (!IsValidLanguageRangeSubtag(value, start, index, first))
                {
                    return false;
                }

                first = false;
                start = index + 1;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsValidLanguageRangeSubtag(
        string value,
        int start,
        int end,
        bool lettersOnly)
    {
        int index = start;
        while (index < end)
        {
            char character = value[index];
            bool letter = character >= 'A' && character <= 'Z' ||
                character >= 'a' && character <= 'z';
            bool digit = character >= '0' && character <= '9';
            if (!letter && (lettersOnly || !digit))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static int CalculateLanguageRangeSpecificity(string value)
    {
        if (value == "*")
        {
            return 0;
        }

        int specificity = 0;
        int start = 0;
        int index = 0;
        while (index <= value.Length)
        {
            if (index == value.Length || value[index] == '-')
            {
                if (!(index - start == 1 && value[start] == '*'))
                {
                    specificity = specificity + 1;
                }

                start = index + 1;
            }

            index = index + 1;
        }

        return specificity;
    }

    static string GetMediaRangeText(HttpMediaType value)
    {
        if (value.IsTypeWildcard)
        {
            return "*/*";
        }

        if (value.IsSubtypeWildcard)
        {
            return $"{value.Type}/*";
        }

        return $"{value.Type}/{value.Subtype}";
    }

    static int FindByte(byte[] input, byte value)
    {
        int index = 0;
        while (index < input.Length)
        {
            if (input[index] == value)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
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

    static int FindInvalidTokenByte(byte[] input, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            if (!HttpGrammar.IsTokenCharacter(input[index]))
            {
                return index;
            }

            index = index + 1;
        }

        return start;
    }

    static QualityValueParseResult CreateDoneQuality(int quality, int consumed)
    {
        QualityValueParseResult result = new QualityValueParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Quality = quality;
        result.Consumed = consumed;
        return result;
    }

    static QualityValueParseResult CreateMalformedQuality(int errorPosition)
    {
        QualityValueParseResult result = new QualityValueParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static QualityValueParseResult CreateLimitQuality()
    {
        QualityValueParseResult result = new QualityValueParseResult();
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

    static FieldValueParseResult ParseList(
        byte[] input,
        int maximumSize,
        int maximumMembers,
        int maximumDepth,
        bool allowEmpty)
    {
        FieldValueParseResult limitResult = ValidateInputSize(input, maximumSize);
        if (limitResult.Status != HttpOperationStatus.Done)
        {
            return limitResult;
        }

        if (maximumMembers < 0 || maximumDepth < 0)
        {
            return CreateLimit(0);
        }

        FieldValueParseResult result = CreateDone(input.Length);
        if (input.Length == 0)
        {
            if (allowEmpty)
            {
                return result;
            }

            return CreateMalformed(0);
        }

        int elementStart = 0;
        int position = 0;
        int commentDepth = 0;
        bool quoted = false;
        while (position < input.Length)
        {
            byte character = input[position];
            if (quoted)
            {
                if (character == 0x5c)
                {
                    if (position + 1 >= input.Length ||
                        !HttpGrammar.IsQuotedPairCharacter(input[position + 1]))
                    {
                        return CreateMalformed(position);
                    }

                    position = position + 2;
                    continue;
                }

                if (character == 0x22)
                {
                    quoted = false;
                }
                else if (!HttpGrammar.IsQuotedTextCharacter(character))
                {
                    return CreateMalformed(position);
                }
            }
            else if (commentDepth > 0)
            {
                if (character == 0x5c)
                {
                    if (position + 1 >= input.Length ||
                        !HttpGrammar.IsQuotedPairCharacter(input[position + 1]))
                    {
                        return CreateMalformed(position);
                    }

                    position = position + 2;
                    continue;
                }

                if (character == 0x28)
                {
                    commentDepth = commentDepth + 1;
                    if (commentDepth > maximumDepth)
                    {
                        return CreateLimit(position);
                    }
                }
                else if (character == 0x29)
                {
                    commentDepth = commentDepth - 1;
                }
                else if (!IsCommentTextCharacter(character))
                {
                    return CreateMalformed(position);
                }
            }
            else if (character == 0x22)
            {
                quoted = true;
            }
            else if (character == 0x28)
            {
                commentDepth = 1;
                if (commentDepth > maximumDepth)
                {
                    return CreateLimit(position);
                }
            }
            else if (character == 0x29)
            {
                return CreateMalformed(position);
            }
            else if (character == 0x2c)
            {
                FieldValueParseResult elementResult = AddListElement(
                    result,
                    input,
                    elementStart,
                    position,
                    maximumMembers,
                    allowEmpty);
                if (elementResult.Status != HttpOperationStatus.Done)
                {
                    return elementResult;
                }

                elementStart = position + 1;
            }
            else if (character != 0x09 && character < 0x20 || character == 0x7f)
            {
                return CreateMalformed(position);
            }

            position = position + 1;
        }

        if (quoted || commentDepth != 0)
        {
            return CreateMalformed(input.Length);
        }

        FieldValueParseResult lastResult = AddListElement(
            result,
            input,
            elementStart,
            input.Length,
            maximumMembers,
            allowEmpty);
        if (lastResult.Status != HttpOperationStatus.Done)
        {
            return lastResult;
        }

        result.Consumed = input.Length;
        result.NextPosition = input.Length;
        return result;
    }

    static FieldValueParseResult AddListElement(
        FieldValueParseResult result,
        byte[] input,
        int start,
        int end,
        int maximumMembers,
        bool allowEmpty)
    {
        int contentStart = SkipOptionalWhitespace(input, start, end);
        int contentEnd = end;
        while (contentEnd > contentStart &&
            (input[contentEnd - 1] == 0x20 || input[contentEnd - 1] == 0x09))
        {
            contentEnd = contentEnd - 1;
        }

        if (contentStart == contentEnd)
        {
            if (!allowEmpty)
            {
                return CreateMalformed(contentStart);
            }

            if (result.EmptyElements >= maximumMembers)
            {
                return CreateLimit(contentStart);
            }

            result.EmptyElements = result.EmptyElements + 1;
            return result;
        }

        if (result.Values.Count >= maximumMembers)
        {
            return CreateLimit(contentStart);
        }

        result.Values.Add(CopyRange(input, contentStart, contentEnd));
        return result;
    }

    static FieldValueParseResult ReadTokenAt(byte[] input, int start, int end)
    {
        int position = start;
        while (position < end && HttpGrammar.IsTokenCharacter(input[position]))
        {
            position = position + 1;
        }

        if (position == start)
        {
            return CreateMalformed(start);
        }

        FieldValueParseResult result = CreateDone(position - start);
        result.NextPosition = position;
        result.Value = CopyRange(input, start, position);
        return result;
    }

    static FieldValueParseResult ReadQuotedStringAt(byte[] input, int start, int end)
    {
        if (start >= end || input[start] != 0x22)
        {
            return CreateMalformed(start);
        }

        BoundedByteBuffer decoded = new BoundedByteBuffer(end - start);
        int position = start + 1;
        while (position < end)
        {
            byte character = input[position];
            if (character == 0x22)
            {
                FieldValueParseResult result = CreateDone(position + 1 - start);
                result.NextPosition = position + 1;
                result.Value = decoded.ToArray();
                return result;
            }

            if (character == 0x5c)
            {
                if (position + 1 >= end || !HttpGrammar.IsQuotedPairCharacter(input[position + 1]))
                {
                    return CreateMalformed(position);
                }

                decoded.AppendByte(input[position + 1]);
                position = position + 2;
                continue;
            }

            if (!HttpGrammar.IsQuotedTextCharacter(character))
            {
                return CreateMalformed(position);
            }

            decoded.AppendByte(character);
            position = position + 1;
        }

        return CreateMalformed(end);
    }

    static FieldValueParseResult ReadCommentAt(byte[] input, int start, int end, int maximumDepth)
    {
        if (start >= end || input[start] != 0x28)
        {
            return CreateMalformed(start);
        }

        if (maximumDepth < 1)
        {
            return CreateLimit(start);
        }

        BoundedByteBuffer decoded = new BoundedByteBuffer(end - start);
        int depth = 1;
        int position = start + 1;
        while (position < end)
        {
            byte character = input[position];
            if (character == 0x5c)
            {
                if (position + 1 >= end || !HttpGrammar.IsQuotedPairCharacter(input[position + 1]))
                {
                    return CreateMalformed(position);
                }

                decoded.AppendByte(input[position + 1]);
                position = position + 2;
                continue;
            }

            if (character == 0x28)
            {
                depth = depth + 1;
                if (depth > maximumDepth)
                {
                    return CreateLimit(position);
                }

                decoded.AppendByte(character);
            }
            else if (character == 0x29)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    FieldValueParseResult result = CreateDone(position + 1 - start);
                    result.NextPosition = position + 1;
                    result.Value = decoded.ToArray();
                    return result;
                }

                decoded.AppendByte(character);
            }
            else
            {
                if (!IsCommentTextCharacter(character))
                {
                    return CreateMalformed(position);
                }

                decoded.AppendByte(character);
            }

            position = position + 1;
        }

        return CreateMalformed(end);
    }

    static int SkipOptionalWhitespace(byte[] input, int start, int end)
    {
        int position = start;
        while (position < end && (input[position] == 0x20 || input[position] == 0x09))
        {
            position = position + 1;
        }

        return position;
    }

    static bool IsCommentTextCharacter(byte value)
    {
        if (value == 0x09 || value == 0x20)
        {
            return true;
        }

        if (value >= 0x21 && value <= 0x27)
        {
            return true;
        }

        if (value >= 0x2a && value <= 0x5b)
        {
            return true;
        }

        if (value >= 0x5d && value <= 0x7e)
        {
            return true;
        }

        return value >= 0x80;
    }

    static byte[] CopyRange(byte[] input, int start, int end)
    {
        byte[] result = new byte[end - start];
        int index = 0;
        while (start + index < end)
        {
            result[index] = input[start + index];
            index = index + 1;
        }

        return result;
    }

    static FieldValueParseResult ValidateInputSize(byte[] input, int maximumSize)
    {
        if (input == null)
        {
            return CreateMalformed(0);
        }

        if (maximumSize < 0 || input.Length > maximumSize)
        {
            int position = 0;
            if (maximumSize >= 0)
            {
                position = maximumSize;
            }

            return CreateLimit(position);
        }

        return CreateDone(0);
    }

    static FieldValueParseResult CreateDone(int consumed)
    {
        FieldValueParseResult result = new FieldValueParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        result.NextPosition = consumed;
        return result;
    }

    static FieldValueParseResult CreateMalformed(int errorPosition)
    {
        FieldValueParseResult result = new FieldValueParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = errorPosition;
        result.NextPosition = errorPosition;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static FieldValueParseResult CreateLimit(int errorPosition)
    {
        FieldValueParseResult result = new FieldValueParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.Consumed = errorPosition;
        result.NextPosition = errorPosition;
        result.ErrorPosition = errorPosition;
        result.IsLimitExceeded = true;
        return result;
    }
}
