using System;
using System.Collections.Generic;
using System.Text;

namespace http;

internal static class FieldValueWriter
{
    internal static byte[] SerializePriority(HttpPriority value)
    {
        return PriorityCodec.Serialize(value);
    }

    internal static FieldValueWriteResult SerializeCacheControlValue(
        HttpCacheControl value,
        int maximumSize,
        int maximumDirectives,
        int maximumFieldNames)
    {
        return CacheControlCodec.Serialize(
            value,
            maximumSize,
            maximumDirectives,
            maximumFieldNames);
    }

    internal static FieldValueWriteResult SerializeAllow(
        List<string> values,
        int maximumSize,
        int maximumMethods)
    {
        return CommonFieldValueWriter.SerializeTokenList(values, maximumSize, maximumMethods);
    }

    internal static FieldValueWriteResult SerializeConnection(
        List<string> values,
        int maximumSize,
        int maximumOptions)
    {
        return CommonFieldValueWriter.SerializeTokenList(values, maximumSize, maximumOptions);
    }

    internal static FieldValueWriteResult SerializeUpgrade(
        List<string> values,
        int maximumSize,
        int maximumProtocols)
    {
        return CommonFieldValueWriter.SerializeUpgrade(values, maximumSize, maximumProtocols);
    }

    internal static FieldValueWriteResult SerializeTe(
        List<HttpWeightedValue> values,
        int maximumSize,
        int maximumCodings,
        int maximumParameters)
    {
        return CommonFieldValueWriter.SerializeTe(
            values,
            maximumSize,
            maximumCodings,
            maximumParameters);
    }

    internal static FieldValueWriteResult SerializeTrailer(
        List<string> values,
        int maximumSize,
        int maximumNames)
    {
        return CommonFieldValueWriter.SerializeTokenList(values, maximumSize, maximumNames);
    }

    internal static FieldValueWriteResult SerializeExpect(
        List<HttpExpectationValue> values,
        int maximumSize,
        int maximumExpectations,
        int maximumParameters)
    {
        return CommonFieldValueWriter.SerializeExpect(
            values,
            maximumSize,
            maximumExpectations,
            maximumParameters);
    }

    internal static FieldValueWriteResult SerializeMaxForwards(long value, int maximumSize)
    {
        return CommonFieldValueWriter.SerializeNumber(value, maximumSize);
    }

    internal static FieldValueWriteResult SerializeLocation(string value, int maximumSize)
    {
        return CommonFieldValueWriter.SerializeLocation(value, maximumSize, true);
    }

    internal static FieldValueWriteResult SerializeContentLocation(string value, int maximumSize)
    {
        return CommonFieldValueWriter.SerializeLocation(value, maximumSize, false);
    }

    internal static FieldValueWriteResult SerializeRetryAfterDelay(long value, int maximumSize)
    {
        return CommonFieldValueWriter.SerializeNumber(value, maximumSize);
    }

    internal static FieldValueWriteResult SerializeRetryAfterDate(
        DateTimeOffset value,
        int maximumSize)
    {
        return CommonFieldValueWriter.SerializeRetryAfterDate(value, maximumSize);
    }

    internal static FieldValueWriteResult SerializeVary(
        List<string> values,
        int maximumSize,
        int maximumNames)
    {
        return CommonFieldValueWriter.SerializeTokenList(values, maximumSize, maximumNames);
    }

    internal static FieldValueWriteResult SerializeViaEntries(
        List<HttpViaEntry> values,
        int maximumSize,
        int maximumEntries,
        int maximumCommentDepth)
    {
        return CommonFieldValueWriter.SerializeVia(
            values,
            maximumSize,
            maximumEntries,
            maximumCommentDepth);
    }

    internal static FieldValueWriteResult SerializeAuthenticationCredentials(
        HttpAuthenticationValue value,
        int maximumSize,
        int maximumParameters)
    {
        return SerializeAuthenticationValue(value, maximumSize, maximumParameters);
    }

    internal static FieldValueWriteResult SerializeAuthenticationValues(
        List<HttpAuthenticationValue> values,
        int maximumSize,
        int maximumChallenges,
        int maximumParameters)
    {
        if (values == null)
        {
            return CreateMalformed(0);
        }

        if (maximumChallenges < 0 || values.Count > maximumChallenges)
        {
            return CreateLimit(0);
        }

        if (maximumParameters < 0)
        {
            return CreateLimit(0);
        }

        List<byte[]> serialized = new List<byte[]>();
        int index = 0;
        while (index < values.Count)
        {
            FieldValueWriteResult value = SerializeAuthenticationValue(
                values[index],
                int.MaxValue,
                maximumParameters);
            if (value.Status != HttpOperationStatus.Done)
            {
                value.ErrorPosition = index;
                return value;
            }

            serialized.Add(value.Bytes);
            index = index + 1;
        }

        return SerializeRawRangeValues(serialized, maximumSize);
    }

    internal static FieldValueWriteResult SerializeRangeUnit(string value, int maximumSize)
    {
        if (!HeaderStore.IsValidName(value))
        {
            return CreateMalformed(0);
        }

        return SerializeToken(Encoding.ASCII.GetBytes(value), maximumSize);
    }

    internal static FieldValueWriteResult SerializeRangeValue(
        HttpRangeRequest value,
        int maximumSize,
        int maximumRanges)
    {
        if (value == null || value.Unit == null || value.Ranges == null || value.OtherRanges == null)
        {
            return CreateMalformed(0);
        }

        FieldValueWriteResult unit = SerializeRangeUnit(value.Unit, int.MaxValue);
        if (unit.Status != HttpOperationStatus.Done)
        {
            return unit;
        }

        if (maximumRanges < 0)
        {
            return CreateLimit(0);
        }

        List<byte[]> members = new List<byte[]>();
        bool bytesUnit = Ascii.EqualsStringsIgnoreCase(value.Unit, HttpRangeUnits.Bytes);
        if (bytesUnit)
        {
            if (value.OtherRanges.Count != 0 || value.Ranges.Count == 0)
            {
                return CreateMalformed(0);
            }

            if (value.Ranges.Count > maximumRanges)
            {
                return CreateLimit(0);
            }

            int index = 0;
            while (index < value.Ranges.Count)
            {
                FieldValueWriteResult member = SerializeByteRangeMember(value.Ranges[index]);
                if (member.Status != HttpOperationStatus.Done)
                {
                    member.ErrorPosition = index;
                    return member;
                }

                members.Add(member.Bytes);
                index = index + 1;
            }
        }
        else
        {
            if (value.Ranges.Count != 0 || value.OtherRanges.Count == 0)
            {
                return CreateMalformed(0);
            }

            if (value.OtherRanges.Count > maximumRanges)
            {
                return CreateLimit(0);
            }

            int index = 0;
            while (index < value.OtherRanges.Count)
            {
                byte[] member = value.OtherRanges[index];
                if (member == null || !IsOtherRangeValue(member))
                {
                    return CreateMalformed(index);
                }

                members.Add(CopyBytes(member));
                index = index + 1;
            }
        }

        FieldValueWriteResult rangeSet = SerializeRawRangeValues(members, int.MaxValue);
        long requiredSize = (long)unit.Bytes.Length + 1 + rangeSet.Bytes.Length;
        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = CopyBytesAt(unit.Bytes, output, 0);
        output[outputIndex] = 0x3d;
        outputIndex = outputIndex + 1;
        CopyBytesAt(rangeSet.Bytes, output, outputIndex);
        return CreateDone(output);
    }

    internal static FieldValueWriteResult SerializeContentRange(
        HttpContentRange value,
        int maximumSize)
    {
        if (value == null || value.Unit == null)
        {
            return CreateMalformed(0);
        }

        FieldValueWriteResult unit = SerializeRangeUnit(value.Unit, int.MaxValue);
        if (unit.Status != HttpOperationStatus.Done)
        {
            return unit;
        }

        byte[] rangeBytes;
        if (value.IsUnsatisfied)
        {
            if (value.HasStart || value.HasEnd || !value.HasCompleteLength || value.CompleteLength < 0)
            {
                return CreateMalformed(0);
            }

            byte[] complete = GetDecimalBytes(value.CompleteLength);
            rangeBytes = new byte[2 + complete.Length];
            rangeBytes[0] = 0x2a;
            rangeBytes[1] = 0x2f;
            CopyBytesAt(complete, rangeBytes, 2);
        }
        else
        {
            if (!value.HasStart || !value.HasEnd || value.Start < 0 || value.End < value.Start)
            {
                return CreateMalformed(0);
            }

            if (value.HasCompleteLength &&
                (value.CompleteLength < 0 || value.CompleteLength <= value.End))
            {
                return CreateMalformed(0);
            }

            byte[] first = GetDecimalBytes(value.Start);
            byte[] last = GetDecimalBytes(value.End);
            byte[] complete = value.HasCompleteLength ?
                GetDecimalBytes(value.CompleteLength) : new byte[] { 0x2a };
            rangeBytes = new byte[first.Length + 1 + last.Length + 1 + complete.Length];
            int rangeIndex = CopyBytesAt(first, rangeBytes, 0);
            rangeBytes[rangeIndex] = 0x2d;
            rangeIndex = rangeIndex + 1;
            rangeIndex = CopyBytesAt(last, rangeBytes, rangeIndex);
            rangeBytes[rangeIndex] = 0x2f;
            rangeIndex = rangeIndex + 1;
            CopyBytesAt(complete, rangeBytes, rangeIndex);
        }

        long requiredSize = (long)unit.Bytes.Length + 1 + rangeBytes.Length;
        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = CopyBytesAt(unit.Bytes, output, 0);
        output[outputIndex] = 0x20;
        CopyBytesAt(rangeBytes, output, outputIndex + 1);
        return CreateDone(output);
    }

    internal static FieldValueWriteResult SerializeAcceptRanges(
        List<string> values,
        int maximumSize,
        int maximumUnits)
    {
        if (values == null)
        {
            return CreateMalformed(0);
        }

        if (maximumUnits < 0 || values.Count > maximumUnits)
        {
            return CreateLimit(0);
        }

        if (values.Count == 0)
        {
            return CreateMalformed(0);
        }

        List<byte[]> units = new List<byte[]>();
        int index = 0;
        while (index < values.Count)
        {
            FieldValueWriteResult unit = SerializeRangeUnit(values[index], int.MaxValue);
            if (unit.Status != HttpOperationStatus.Done)
            {
                unit.ErrorPosition = index;
                return unit;
            }

            units.Add(unit.Bytes);
            index = index + 1;
        }

        return SerializeRawRangeValues(units, maximumSize);
    }

    internal static FieldValueWriteResult SerializeEntityTag(HttpEntityTag value, int maximumSize)
    {
        if (value == null || value.IsWildcard || value.OpaqueTag == null)
        {
            return CreateMalformed(0);
        }

        int index = 0;
        while (index < value.OpaqueTag.Length)
        {
            if (!IsEntityTagCharacter(value.OpaqueTag[index]))
            {
                return CreateMalformed(index);
            }

            index = index + 1;
        }

        long requiredSize = value.OpaqueTag.Length + 2L;
        if (value.IsWeak)
        {
            requiredSize = requiredSize + 2;
        }

        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = 0;
        if (value.IsWeak)
        {
            output[0] = 0x57;
            output[1] = 0x2f;
            outputIndex = 2;
        }

        output[outputIndex] = 0x22;
        outputIndex = outputIndex + 1;
        outputIndex = CopyBytesAt(value.OpaqueTag, output, outputIndex);
        output[outputIndex] = 0x22;
        return CreateDone(output);
    }

    internal static FieldValueWriteResult SerializeEntityTags(
        List<HttpEntityTag> values,
        int maximumSize,
        int maximumMembers)
    {
        if (values == null)
        {
            return CreateMalformed(0);
        }

        if (maximumMembers < 0 || values.Count > maximumMembers)
        {
            return CreateLimit(0);
        }

        if (values.Count == 1 && values[0] != null && values[0].IsWildcard)
        {
            HttpEntityTag wildcard = values[0];
            if (wildcard.IsWeak || wildcard.OpaqueTag == null || wildcard.OpaqueTag.Length != 0)
            {
                return CreateMalformed(0);
            }

            if (maximumSize < 1)
            {
                return CreateLimit(1);
            }

            return CreateDone(new byte[] { 0x2a });
        }

        List<byte[]> serialized = new List<byte[]>();
        long requiredSize = 0;
        int index = 0;
        while (index < values.Count)
        {
            HttpEntityTag value = values[index];
            if (value == null || value.IsWildcard)
            {
                return CreateMalformed(index);
            }

            FieldValueWriteResult member = SerializeEntityTag(value, int.MaxValue);
            if (member.Status != HttpOperationStatus.Done)
            {
                member.ErrorPosition = index;
                return member;
            }

            if (index > 0)
            {
                requiredSize = requiredSize + 2;
            }

            requiredSize = requiredSize + member.Bytes.Length;
            serialized.Add(member.Bytes);
            index = index + 1;
        }

        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = 0;
        index = 0;
        while (index < serialized.Count)
        {
            if (index > 0)
            {
                output[outputIndex] = 0x2c;
                output[outputIndex + 1] = 0x20;
                outputIndex = outputIndex + 2;
            }

            outputIndex = CopyBytesAt(serialized[index], output, outputIndex);
            index = index + 1;
        }

        return CreateDone(output);
    }

    internal static FieldValueWriteResult SerializeQualityValue(int quality, int maximumSize)
    {
        if (quality < 0 || quality > 1000)
        {
            return CreateMalformed(0);
        }

        byte[] output;
        if (quality == 1000)
        {
            output = new byte[] { 0x31 };
        }
        else if (quality == 0)
        {
            output = new byte[] { 0x30 };
        }
        else if (quality % 100 == 0)
        {
            output = new byte[]
            {
                0x30,
                0x2e,
                (byte)(0x30 + quality / 100)
            };
        }
        else if (quality % 10 == 0)
        {
            output = new byte[]
            {
                0x30,
                0x2e,
                (byte)(0x30 + quality / 100),
                (byte)(0x30 + quality / 10 % 10)
            };
        }
        else
        {
            output = new byte[]
            {
                0x30,
                0x2e,
                (byte)(0x30 + quality / 100),
                (byte)(0x30 + quality / 10 % 10),
                (byte)(0x30 + quality % 10)
            };
        }

        if (maximumSize < 0 || output.Length > maximumSize)
        {
            return CreateLimit(output.Length);
        }

        return CreateDone(output);
    }

    internal static FieldValueWriteResult SerializeMediaType(
        HttpMediaType value,
        int maximumSize,
        int maximumParameters)
    {
        return SerializeMediaValue(value, maximumSize, maximumParameters, false);
    }

    internal static FieldValueWriteResult SerializeMediaRange(
        HttpMediaType value,
        int maximumSize,
        int maximumParameters)
    {
        return SerializeMediaValue(value, maximumSize, maximumParameters, true);
    }

    internal static FieldValueWriteResult SerializeWeightedMediaRanges(
        List<HttpWeightedValue> values,
        int maximumSize,
        int maximumMembers,
        int maximumParameters)
    {
        return SerializeWeightedList(
            values,
            maximumSize,
            maximumMembers,
            maximumParameters,
            true);
    }

    internal static FieldValueWriteResult SerializeWeightedValues(
        List<HttpWeightedValue> values,
        int maximumSize,
        int maximumMembers,
        int maximumParameters)
    {
        return SerializeWeightedList(
            values,
            maximumSize,
            maximumMembers,
            maximumParameters,
            false);
    }

    internal static FieldValueWriteResult SerializeFieldValue(byte[] value, int maximumSize)
    {
        if (value == null)
        {
            return CreateMalformed(0);
        }

        if (!HeaderStore.IsValidValue(value))
        {
            return CreateMalformed(FindInvalidFieldValuePosition(value));
        }

        if (maximumSize < 0 || value.Length > maximumSize)
        {
            return CreateLimit(value.Length);
        }

        return CreateDone(CopyBytes(value));
    }

    internal static FieldValueWriteResult WriteFieldValue(
        byte[] value,
        Span<byte> output,
        int offset,
        int maximumSize)
    {
        FieldValueWriteResult serialized = SerializeFieldValue(value, maximumSize);
        if (serialized.Status != HttpOperationStatus.Done)
        {
            return serialized;
        }

        if (offset < 0)
        {
            return CreateMalformed(0);
        }

        int requiredSize = int.MaxValue;
        if (serialized.Bytes.Length <= int.MaxValue - offset)
        {
            requiredSize = offset + serialized.Bytes.Length;
        }

        if (offset > output.Length || serialized.Bytes.Length > output.Length - offset)
        {
            FieldValueWriteResult result = new FieldValueWriteResult();
            result.Status = HttpOperationStatus.NeedOutput;
            result.RequiredSize = requiredSize;
            return result;
        }

        int index = 0;
        while (index < serialized.Bytes.Length)
        {
            output[offset + index] = serialized.Bytes[index];
            index = index + 1;
        }

        FieldValueWriteResult success = new FieldValueWriteResult();
        success.Status = HttpOperationStatus.Done;
        success.Written = serialized.Bytes.Length;
        success.RequiredSize = requiredSize;
        return success;
    }

    internal static FieldValueWriteResult SerializeToken(byte[] value, int maximumSize)
    {
        if (value == null || value.Length == 0)
        {
            return CreateMalformed(0);
        }

        int index = 0;
        while (index < value.Length)
        {
            if (!HttpGrammar.IsTokenCharacter(value[index]))
            {
                return CreateMalformed(index);
            }

            index = index + 1;
        }

        if (maximumSize < 0 || value.Length > maximumSize)
        {
            return CreateLimit(value.Length);
        }

        return CreateDone(CopyBytes(value));
    }

    internal static FieldValueWriteResult SerializeQuotedString(byte[] value, int maximumSize)
    {
        if (value == null)
        {
            return CreateMalformed(0);
        }

        long requiredSize = 2;
        int index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character == 0x22 || character == 0x5c)
            {
                requiredSize = requiredSize + 2;
            }
            else
            {
                if (!HttpGrammar.IsQuotedTextCharacter(character))
                {
                    return CreateMalformed(index);
                }

                requiredSize = requiredSize + 1;
            }

            index = index + 1;
        }

        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        output[0] = 0x22;
        int outputIndex = 1;
        index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character == 0x22 || character == 0x5c)
            {
                output[outputIndex] = 0x5c;
                outputIndex = outputIndex + 1;
            }

            output[outputIndex] = character;
            outputIndex = outputIndex + 1;
            index = index + 1;
        }

        output[outputIndex] = 0x22;
        return CreateDone(output);
    }

    internal static FieldValueWriteResult SerializeQuotedPair(byte value, int maximumSize)
    {
        if (!HttpGrammar.IsQuotedPairCharacter(value))
        {
            return CreateMalformed(0);
        }

        if (maximumSize < 2)
        {
            return CreateLimit(2);
        }

        return CreateDone(new byte[] { 0x5c, value });
    }

    internal static FieldValueWriteResult SerializeComment(byte[] value, int maximumSize)
    {
        if (value == null)
        {
            return CreateMalformed(0);
        }

        long requiredSize = 2;
        int index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character == 0x28 || character == 0x29 || character == 0x5c)
            {
                requiredSize = requiredSize + 2;
            }
            else
            {
                if (!IsCommentTextCharacter(character))
                {
                    return CreateMalformed(index);
                }

                requiredSize = requiredSize + 1;
            }

            index = index + 1;
        }

        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        output[0] = 0x28;
        int outputIndex = 1;
        index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character == 0x28 || character == 0x29 || character == 0x5c)
            {
                output[outputIndex] = 0x5c;
                outputIndex = outputIndex + 1;
            }

            output[outputIndex] = character;
            outputIndex = outputIndex + 1;
            index = index + 1;
        }

        output[outputIndex] = 0x29;
        return CreateDone(output);
    }

    internal static FieldValueWriteResult SerializeCommaList(
        List<byte[]> values,
        int maximumSize,
        int maximumMembers,
        int maximumDepth)
    {
        return SerializeListValues(values, maximumSize, maximumMembers, maximumDepth);
    }

    internal static FieldValueWriteResult SerializeHashList(
        List<byte[]> values,
        int maximumSize,
        int maximumMembers,
        int maximumDepth)
    {
        return SerializeListValues(values, maximumSize, maximumMembers, maximumDepth);
    }

    internal static FieldValueWriteResult SerializeParameters(
        List<HttpParameter> parameters,
        int maximumSize,
        int maximumParameters)
    {
        if (parameters == null)
        {
            return CreateMalformed(0);
        }

        if (maximumParameters < 0 || parameters.Count > maximumParameters)
        {
            return CreateLimit(0);
        }

        long requiredSize = 0;
        int index = 0;
        while (index < parameters.Count)
        {
            HttpParameter parameter = parameters[index];
            FieldValueWriteResult validation = ValidateParameter(parameter);
            if (validation.Status != HttpOperationStatus.Done)
            {
                validation.ErrorPosition = index;
                return validation;
            }

            byte[] name = Encoding.ASCII.GetBytes(parameter.Name);
            requiredSize = requiredSize + 2 + name.Length;
            if (parameter.HasValue)
            {
                requiredSize = requiredSize + 1 + validation.Bytes.Length;
            }

            index = index + 1;
        }

        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = 0;
        index = 0;
        while (index < parameters.Count)
        {
            HttpParameter parameter = parameters[index];
            FieldValueWriteResult valueResult = ValidateParameter(parameter);
            byte[] name = Encoding.ASCII.GetBytes(parameter.Name);
            output[outputIndex] = 0x3b;
            output[outputIndex + 1] = 0x20;
            outputIndex = outputIndex + 2;

            int nameIndex = 0;
            while (nameIndex < name.Length)
            {
                output[outputIndex] = name[nameIndex];
                outputIndex = outputIndex + 1;
                nameIndex = nameIndex + 1;
            }

            if (parameter.HasValue)
            {
                output[outputIndex] = 0x3d;
                outputIndex = outputIndex + 1;
                int valueIndex = 0;
                while (valueIndex < valueResult.Bytes.Length)
                {
                    output[outputIndex] = valueResult.Bytes[valueIndex];
                    outputIndex = outputIndex + 1;
                    valueIndex = valueIndex + 1;
                }
            }

            index = index + 1;
        }

        return CreateDone(output);
    }

    static FieldValueWriteResult SerializeMediaValue(
        HttpMediaType value,
        int maximumSize,
        int maximumParameters,
        bool allowWildcards)
    {
        if (value == null || value.Parameters == null)
        {
            return CreateMalformed(0);
        }

        if (value.Type == null || value.Subtype == null)
        {
            return CreateMalformed(0);
        }

        if (maximumParameters < 0 || value.Parameters.Count > maximumParameters)
        {
            return CreateLimit(0);
        }

        if (!allowWildcards && (value.IsTypeWildcard || value.IsSubtypeWildcard))
        {
            return CreateMalformed(0);
        }

        if (value.IsTypeWildcard && !value.IsSubtypeWildcard)
        {
            return CreateMalformed(0);
        }

        if (value.IsTypeWildcard)
        {
            if (value.Type.Length != 0)
            {
                return CreateMalformed(0);
            }
        }
        else if (!HeaderStore.IsValidName(value.Type) || value.Type == "*")
        {
            return CreateMalformed(0);
        }

        if (value.IsSubtypeWildcard)
        {
            if (value.Subtype.Length != 0)
            {
                return CreateMalformed(0);
            }
        }
        else if (!HeaderStore.IsValidName(value.Subtype) || value.Subtype == "*")
        {
            return CreateMalformed(0);
        }

        int parameterIndex = 0;
        while (parameterIndex < value.Parameters.Count)
        {
            HttpParameter parameter = value.Parameters[parameterIndex];
            if (parameter == null || !parameter.HasValue)
            {
                return CreateMalformed(parameterIndex);
            }

            parameterIndex = parameterIndex + 1;
        }

        FieldValueWriteResult parameters = SerializeParameters(
            value.Parameters,
            int.MaxValue,
            maximumParameters);
        if (parameters.Status != HttpOperationStatus.Done)
        {
            return parameters;
        }

        byte[] typeBytes = value.IsTypeWildcard ? new byte[] { 0x2a } : Encoding.ASCII.GetBytes(value.Type);
        byte[] subtypeBytes = value.IsSubtypeWildcard ? new byte[] { 0x2a } : Encoding.ASCII.GetBytes(value.Subtype);
        long requiredSize = (long)typeBytes.Length + 1 + subtypeBytes.Length + parameters.Bytes.Length;
        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = CopyBytesAt(typeBytes, output, 0);
        output[outputIndex] = 0x2f;
        outputIndex = outputIndex + 1;
        outputIndex = CopyBytesAt(subtypeBytes, output, outputIndex);
        CopyBytesAt(parameters.Bytes, output, outputIndex);
        return CreateDone(output);
    }

    static FieldValueWriteResult SerializeWeightedList(
        List<HttpWeightedValue> values,
        int maximumSize,
        int maximumMembers,
        int maximumParameters,
        bool mediaRanges)
    {
        if (values == null)
        {
            return CreateMalformed(0);
        }

        if (maximumMembers < 0 || maximumParameters < 0 || values.Count > maximumMembers)
        {
            return CreateLimit(0);
        }

        List<byte[]> members = new List<byte[]>();
        int index = 0;
        while (index < values.Count)
        {
            FieldValueWriteResult member = SerializeWeightedMember(
                values[index],
                maximumParameters,
                mediaRanges);
            if (member.Status != HttpOperationStatus.Done)
            {
                member.ErrorPosition = index;
                return member;
            }

            members.Add(member.Bytes);
            index = index + 1;
        }

        return SerializeHashList(members, maximumSize, maximumMembers, 0);
    }

    static FieldValueWriteResult SerializeWeightedMember(
        HttpWeightedValue value,
        int maximumParameters,
        bool mediaRange)
    {
        if (value == null || value.Value == null || value.Parameters == null ||
            value.Quality < 0 || value.Quality > 1000)
        {
            return CreateMalformed(0);
        }

        int qualityParameterCount = value.Quality == 1000 ? 0 : 1;
        if (value.Parameters.Count > maximumParameters - qualityParameterCount)
        {
            return CreateLimit(0);
        }

        int parameterIndex = 0;
        while (parameterIndex < value.Parameters.Count)
        {
            HttpParameter parameter = value.Parameters[parameterIndex];
            if (parameter == null || parameter.Name == null || !parameter.HasValue ||
                Ascii.EqualsStringsIgnoreCase(parameter.Name, "q"))
            {
                return CreateMalformed(parameterIndex);
            }

            parameterIndex = parameterIndex + 1;
        }

        if (!IsAsciiString(value.Value))
        {
            return CreateMalformed(0);
        }

        byte[] valueBytes = Encoding.ASCII.GetBytes(value.Value);
        FieldValueWriteResult baseValue;
        if (mediaRange)
        {
            MediaTypeParseResult parsed = FieldValueParser.ParseMediaRange(
                valueBytes,
                valueBytes.Length,
                0);
            if (parsed.Status != HttpOperationStatus.Done || parsed.Value.Parameters.Count != 0)
            {
                return CreateMalformed(parsed.ErrorPosition);
            }

            parsed.Value.Parameters = value.Parameters;
            baseValue = SerializeMediaRange(parsed.Value, int.MaxValue, maximumParameters);
        }
        else
        {
            FieldValueWriteResult token = SerializeToken(valueBytes, int.MaxValue);
            if (token.Status != HttpOperationStatus.Done)
            {
                return token;
            }

            FieldValueWriteResult parameters = SerializeParameters(
                value.Parameters,
                int.MaxValue,
                maximumParameters);
            if (parameters.Status != HttpOperationStatus.Done)
            {
                return parameters;
            }

            byte[] combined = new byte[token.Bytes.Length + parameters.Bytes.Length];
            int combinedIndex = CopyBytesAt(token.Bytes, combined, 0);
            CopyBytesAt(parameters.Bytes, combined, combinedIndex);
            baseValue = CreateDone(combined);
        }

        if (baseValue.Status != HttpOperationStatus.Done || value.Quality == 1000)
        {
            return baseValue;
        }

        FieldValueWriteResult quality = SerializeQualityValue(value.Quality, 5);
        byte[] output = new byte[baseValue.Bytes.Length + 4 + quality.Bytes.Length];
        int outputIndex = CopyBytesAt(baseValue.Bytes, output, 0);
        output[outputIndex] = 0x3b;
        output[outputIndex + 1] = 0x20;
        output[outputIndex + 2] = 0x71;
        output[outputIndex + 3] = 0x3d;
        CopyBytesAt(quality.Bytes, output, outputIndex + 4);
        return CreateDone(output);
    }

    static FieldValueWriteResult SerializeAuthenticationValue(
        HttpAuthenticationValue value,
        int maximumSize,
        int maximumParameters)
    {
        if (value == null || value.Scheme == null || value.Parameters == null)
        {
            return CreateMalformed(0);
        }

        if (maximumParameters < 0)
        {
            return CreateLimit(0);
        }

        FieldValueWriteResult scheme = SerializeToken(
            Encoding.ASCII.GetBytes(value.Scheme),
            int.MaxValue);
        if (!HeaderStore.IsValidName(value.Scheme) || scheme.Status != HttpOperationStatus.Done)
        {
            return CreateMalformed(0);
        }

        byte[] data = Array.Empty<byte>();
        if (value.Token68 != null)
        {
            if (value.Parameters.Count != 0 || !IsToken68Value(value.Token68))
            {
                return CreateMalformed(0);
            }

            data = CopyBytes(value.Token68);
        }
        else
        {
            if (maximumParameters < 0 || value.Parameters.Count > maximumParameters)
            {
                return CreateLimit(0);
            }

            List<byte[]> parameters = new List<byte[]>();
            int index = 0;
            while (index < value.Parameters.Count)
            {
                HttpParameter parameter = value.Parameters[index];
                FieldValueWriteResult serialized = SerializeAuthenticationParameter(parameter);
                if (serialized.Status != HttpOperationStatus.Done)
                {
                    serialized.ErrorPosition = index;
                    return serialized;
                }

                if (HasDuplicateAuthenticationParameter(value.Parameters, index))
                {
                    return CreateMalformed(index);
                }

                parameters.Add(serialized.Bytes);
                index = index + 1;
            }

            FieldValueWriteResult parameterList = SerializeRawRangeValues(parameters, int.MaxValue);
            data = parameterList.Bytes;
        }

        long requiredSize = scheme.Bytes.Length;
        if (data.Length > 0)
        {
            requiredSize = requiredSize + 1 + data.Length;
        }

        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = CopyBytesAt(scheme.Bytes, output, 0);
        if (data.Length > 0)
        {
            output[outputIndex] = 0x20;
            CopyBytesAt(data, output, outputIndex + 1);
        }

        return CreateDone(output);
    }

    static FieldValueWriteResult SerializeAuthenticationParameter(HttpParameter parameter)
    {
        if (parameter == null || parameter.Name == null || !parameter.HasValue ||
            parameter.Value == null || !HeaderStore.IsValidName(parameter.Name))
        {
            return CreateMalformed(0);
        }

        FieldValueWriteResult value;
        if (parameter.IsQuoted)
        {
            value = SerializeQuotedString(parameter.Value, int.MaxValue);
        }
        else
        {
            value = SerializeToken(parameter.Value, int.MaxValue);
        }

        if (value.Status != HttpOperationStatus.Done)
        {
            return value;
        }

        byte[] name = Encoding.ASCII.GetBytes(parameter.Name);
        byte[] output = new byte[name.Length + 1 + value.Bytes.Length];
        int outputIndex = CopyBytesAt(name, output, 0);
        output[outputIndex] = 0x3d;
        CopyBytesAt(value.Bytes, output, outputIndex + 1);
        return CreateDone(output);
    }

    static bool HasDuplicateAuthenticationParameter(List<HttpParameter> parameters, int current)
    {
        HttpParameter value = parameters[current];
        int index = 0;
        while (index < current)
        {
            HttpParameter previous = parameters[index];
            if (previous != null && previous.Name != null &&
                Ascii.EqualsStringsIgnoreCase(previous.Name, value.Name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static bool IsToken68Value(byte[] value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int coreCharacters = 0;
        bool padding = false;
        int index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character == 0x3d)
            {
                if (coreCharacters == 0)
                {
                    return false;
                }

                padding = true;
            }
            else
            {
                if (padding || !IsToken68CharacterValue(character))
                {
                    return false;
                }

                coreCharacters = coreCharacters + 1;
            }

            index = index + 1;
        }

        return coreCharacters > 0;
    }

    static bool IsToken68CharacterValue(byte value)
    {
        if (value >= 0x41 && value <= 0x5a || value >= 0x61 && value <= 0x7a ||
            value >= 0x30 && value <= 0x39)
        {
            return true;
        }

        return value == 0x2d || value == 0x2e || value == 0x5f || value == 0x7e ||
            value == 0x2b || value == 0x2f;
    }

    static FieldValueWriteResult SerializeByteRangeMember(HttpByteRange value)
    {
        if (value == null)
        {
            return CreateMalformed(0);
        }

        if (value.IsSuffix)
        {
            if (value.HasStart || value.HasEnd || value.SuffixLength < 0)
            {
                return CreateMalformed(0);
            }

            byte[] suffix = GetDecimalBytes(value.SuffixLength);
            byte[] output = new byte[suffix.Length + 1];
            output[0] = 0x2d;
            CopyBytesAt(suffix, output, 1);
            return CreateDone(output);
        }

        if (!value.HasStart || value.Start < 0)
        {
            return CreateMalformed(0);
        }

        if (value.HasEnd && (value.End < 0 || value.End < value.Start))
        {
            return CreateMalformed(0);
        }

        byte[] first = GetDecimalBytes(value.Start);
        byte[] last = Array.Empty<byte>();
        if (value.HasEnd)
        {
            last = GetDecimalBytes(value.End);
        }

        byte[] range = new byte[first.Length + 1 + last.Length];
        int rangeIndex = CopyBytesAt(first, range, 0);
        range[rangeIndex] = 0x2d;
        CopyBytesAt(last, range, rangeIndex + 1);
        return CreateDone(range);
    }

    static FieldValueWriteResult SerializeRawRangeValues(List<byte[]> values, int maximumSize)
    {
        long requiredSize = 0;
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                requiredSize = requiredSize + 2;
            }

            requiredSize = requiredSize + values[index].Length;
            index = index + 1;
        }

        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = 0;
        index = 0;
        while (index < values.Count)
        {
            if (index > 0)
            {
                output[outputIndex] = 0x2c;
                output[outputIndex + 1] = 0x20;
                outputIndex = outputIndex + 2;
            }

            outputIndex = CopyBytesAt(values[index], output, outputIndex);
            index = index + 1;
        }

        return CreateDone(output);
    }

    static byte[] GetDecimalBytes(long value)
    {
        if (value == 0)
        {
            return new byte[] { 0x30 };
        }

        long remaining = value;
        int digits = 0;
        while (remaining > 0)
        {
            digits = digits + 1;
            remaining = remaining / 10;
        }

        byte[] output = new byte[digits];
        remaining = value;
        int index = digits - 1;
        while (index >= 0)
        {
            output[index] = (byte)(0x30 + remaining % 10);
            remaining = remaining / 10;
            index = index - 1;
        }

        return output;
    }

    static bool IsOtherRangeValue(byte[] value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character < 0x21 || character > 0x7e || character == 0x2c)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool IsAsciiString(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
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

    static int CopyBytesAt(byte[] source, byte[] destination, int offset)
    {
        int index = 0;
        while (index < source.Length)
        {
            destination[offset + index] = source[index];
            index = index + 1;
        }

        return offset + source.Length;
    }

    static FieldValueWriteResult SerializeListValues(
        List<byte[]> values,
        int maximumSize,
        int maximumMembers,
        int maximumDepth)
    {
        if (values == null)
        {
            return CreateMalformed(0);
        }

        if (maximumMembers < 0 || maximumDepth < 0 || values.Count > maximumMembers)
        {
            return CreateLimit(0);
        }

        List<byte[]> canonicalValues = new List<byte[]>();
        long requiredSize = 0;
        int index = 0;
        while (index < values.Count)
        {
            FieldValueWriteResult memberResult = CanonicalizeListMember(values[index], maximumDepth);
            if (memberResult.Status != HttpOperationStatus.Done)
            {
                memberResult.ErrorPosition = index;
                return memberResult;
            }

            canonicalValues.Add(memberResult.Bytes);
            if (index > 0)
            {
                requiredSize = requiredSize + 2;
            }

            requiredSize = requiredSize + memberResult.Bytes.Length;
            index = index + 1;
        }

        if (maximumSize < 0 || requiredSize > maximumSize || requiredSize > int.MaxValue)
        {
            return CreateLimit(SaturateRequiredSize(requiredSize));
        }

        byte[] output = new byte[(int)requiredSize];
        int outputIndex = 0;
        index = 0;
        while (index < canonicalValues.Count)
        {
            if (index > 0)
            {
                output[outputIndex] = 0x2c;
                output[outputIndex + 1] = 0x20;
                outputIndex = outputIndex + 2;
            }

            byte[] value = canonicalValues[index];
            int valueIndex = 0;
            while (valueIndex < value.Length)
            {
                output[outputIndex] = value[valueIndex];
                outputIndex = outputIndex + 1;
                valueIndex = valueIndex + 1;
            }

            index = index + 1;
        }

        return CreateDone(output);
    }

    static FieldValueWriteResult CanonicalizeListMember(byte[] value, int maximumDepth)
    {
        if (value == null)
        {
            return CreateMalformed(0);
        }

        FieldValueParseResult parsed = FieldValueParser.ParseCommaList(
            value,
            value.Length,
            int.MaxValue,
            maximumDepth);
        if (parsed.IsLimitExceeded)
        {
            return CreateLimit(value.Length);
        }

        if (parsed.Status != HttpOperationStatus.Done || parsed.Values.Count != 1)
        {
            return CreateMalformed(parsed.ErrorPosition);
        }

        return CreateDone(parsed.Values[0]);
    }

    static FieldValueWriteResult ValidateParameter(HttpParameter parameter)
    {
        if (parameter == null || !HeaderStore.IsValidName(parameter.Name))
        {
            return CreateMalformed(0);
        }

        if (!parameter.HasValue)
        {
            if (parameter.IsQuoted)
            {
                return CreateMalformed(0);
            }

            return CreateDone(Array.Empty<byte>());
        }

        if (parameter.IsQuoted)
        {
            return SerializeQuotedString(parameter.Value, int.MaxValue);
        }

        return SerializeToken(parameter.Value, int.MaxValue);
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

    static int FindInvalidFieldValuePosition(byte[] value)
    {
        int index = 0;
        while (index < value.Length)
        {
            byte character = value[index];
            if (character != 0x09 && character < 0x20 || character == 0x7f)
            {
                return index;
            }

            index = index + 1;
        }

        return value.Length;
    }

    static byte[] CopyBytes(byte[] value)
    {
        byte[] result = new byte[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = value[index];
            index = index + 1;
        }

        return result;
    }

    static int SaturateRequiredSize(long requiredSize)
    {
        if (requiredSize > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (requiredSize < 0)
        {
            return 0;
        }

        return (int)requiredSize;
    }

    static FieldValueWriteResult CreateDone(byte[] bytes)
    {
        FieldValueWriteResult result = new FieldValueWriteResult();
        result.Status = HttpOperationStatus.Done;
        result.Bytes = bytes;
        result.Written = bytes.Length;
        result.RequiredSize = bytes.Length;
        return result;
    }

    static FieldValueWriteResult CreateMalformed(int errorPosition)
    {
        FieldValueWriteResult result = new FieldValueWriteResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static FieldValueWriteResult CreateLimit(int requiredSize)
    {
        FieldValueWriteResult result = new FieldValueWriteResult();
        result.Status = HttpOperationStatus.Rejected;
        result.RequiredSize = requiredSize;
        result.IsLimitExceeded = true;
        return result;
    }
}
