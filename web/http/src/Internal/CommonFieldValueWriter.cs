using System;
using System.Collections.Generic;
using System.Text;

namespace http;

internal static class CommonFieldValueWriter
{
    internal static FieldValueWriteResult SerializeTokenList(
        List<string> values,
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

        List<byte[]> members = new List<byte[]>();
        int index = 0;
        while (index < values.Count)
        {
            byte[] bytes = GetAsciiBytes(values[index]);
            if (bytes.Length == 0)
            {
                return CreateMalformed(index);
            }

            FieldValueParseResult parsed = FieldValueParser.ParseToken(bytes, bytes.Length);
            if (parsed.Status != HttpOperationStatus.Done)
            {
                return CreateMalformed(index);
            }

            members.Add(bytes);
            index = index + 1;
        }

        return FieldValueWriter.SerializeHashList(members, maximumSize, maximumMembers, 0);
    }

    internal static FieldValueWriteResult SerializeUpgrade(
        List<string> values,
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

        List<byte[]> members = new List<byte[]>();
        int index = 0;
        while (index < values.Count)
        {
            byte[] bytes = GetAsciiBytes(values[index]);
            FieldValueParseResult parsed = CommonFieldValueParser.ParseUpgrade(
                bytes,
                bytes.Length,
                1);
            if (parsed.Status != HttpOperationStatus.Done || parsed.Values.Count != 1)
            {
                return CreateMalformed(index);
            }

            members.Add(bytes);
            index = index + 1;
        }

        return FieldValueWriter.SerializeHashList(members, maximumSize, maximumMembers, 0);
    }

    internal static FieldValueWriteResult SerializeTe(
        List<HttpWeightedValue> values,
        int maximumSize,
        int maximumMembers,
        int maximumParameters)
    {
        if (values == null)
        {
            return CreateMalformed(0);
        }

        int index = 0;
        while (index < values.Count)
        {
            HttpWeightedValue value = values[index];
            if (value == null || value.Value == null ||
                Ascii.EqualsStringsIgnoreCase(value.Value, "trailers") &&
                (value.Quality != 1000 || value.Parameters == null || value.Parameters.Count != 0))
            {
                return CreateMalformed(index);
            }

            index = index + 1;
        }

        return FieldValueWriter.SerializeWeightedValues(
            values,
            maximumSize,
            maximumMembers,
            maximumParameters);
    }

    internal static FieldValueWriteResult SerializeExpect(
        List<HttpExpectationValue> values,
        int maximumSize,
        int maximumMembers,
        int maximumParameters)
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
            FieldValueWriteResult member = SerializeExpectation(values[index], maximumParameters);
            if (member.Status != HttpOperationStatus.Done)
            {
                member.ErrorPosition = index;
                return member;
            }

            members.Add(member.Bytes);
            index = index + 1;
        }

        return FieldValueWriter.SerializeHashList(members, maximumSize, maximumMembers, 0);
    }

    internal static FieldValueWriteResult SerializeNumber(long value, int maximumSize)
    {
        if (value < 0)
        {
            return CreateMalformed(0);
        }

        byte[] bytes = Encoding.ASCII.GetBytes(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        if (maximumSize < 0 || bytes.Length > maximumSize)
        {
            return CreateLimit(bytes.Length);
        }

        return CreateDone(bytes);
    }

    internal static FieldValueWriteResult SerializeLocation(
        string value,
        int maximumSize,
        bool allowFragment)
    {
        byte[] bytes = GetAsciiBytes(value);
        if (value == null || value.Length != bytes.Length)
        {
            return CreateMalformed(0);
        }

        FieldValueParseResult parsed = CommonFieldValueParser.ParseLocation(
            bytes,
            maximumSize,
            allowFragment);
        if (parsed.IsLimitExceeded)
        {
            return CreateLimit(bytes.Length);
        }

        if (parsed.Status != HttpOperationStatus.Done)
        {
            return CreateMalformed(parsed.ErrorPosition);
        }

        return CreateDone(bytes);
    }

    internal static FieldValueWriteResult SerializeRetryAfterDate(
        DateTimeOffset value,
        int maximumSize)
    {
        return HttpDateCodec.SerializeHttpDate(value, maximumSize);
    }

    internal static FieldValueWriteResult SerializeVia(
        List<HttpViaEntry> values,
        int maximumSize,
        int maximumEntries,
        int maximumCommentDepth)
    {
        if (values == null)
        {
            return CreateMalformed(0);
        }

        if (maximumEntries < 0 || maximumCommentDepth < 0 || values.Count > maximumEntries)
        {
            return CreateLimit(0);
        }

        List<byte[]> members = new List<byte[]>();
        int index = 0;
        while (index < values.Count)
        {
            FieldValueWriteResult member = SerializeViaMember(values[index], maximumCommentDepth);
            if (member.Status != HttpOperationStatus.Done)
            {
                member.ErrorPosition = index;
                return member;
            }

            members.Add(member.Bytes);
            index = index + 1;
        }

        return FieldValueWriter.SerializeHashList(
            members,
            maximumSize,
            maximumEntries,
            maximumCommentDepth);
    }

    static FieldValueWriteResult SerializeExpectation(
        HttpExpectationValue value,
        int maximumParameters)
    {
        if (value == null || value.Name == null || value.Value == null || value.Parameters == null)
        {
            return CreateMalformed(0);
        }

        byte[] name = GetAsciiBytes(value.Name);
        FieldValueParseResult parsedName = FieldValueParser.ParseToken(name, name.Length);
        if (parsedName.Status != HttpOperationStatus.Done)
        {
            return CreateMalformed(0);
        }

        if (!value.HasValue)
        {
            if (value.Value.Length != 0 || value.IsQuoted || value.Parameters.Count != 0)
            {
                return CreateMalformed(0);
            }

            return CreateDone(name);
        }

        int parameterIndex = 0;
        while (parameterIndex < value.Parameters.Count)
        {
            if (value.Parameters[parameterIndex] == null ||
                !value.Parameters[parameterIndex].HasValue)
            {
                return CreateMalformed(parameterIndex);
            }

            parameterIndex = parameterIndex + 1;
        }

        FieldValueWriteResult encodedValue = value.IsQuoted ?
            FieldValueWriter.SerializeQuotedString(value.Value, int.MaxValue) :
            FieldValueWriter.SerializeToken(value.Value, int.MaxValue);
        if (encodedValue.Status != HttpOperationStatus.Done)
        {
            return encodedValue;
        }

        FieldValueWriteResult parameters = FieldValueWriter.SerializeParameters(
            value.Parameters,
            int.MaxValue,
            maximumParameters);
        if (parameters.Status != HttpOperationStatus.Done)
        {
            return parameters;
        }

        byte[] output = new byte[name.Length + 1 + encodedValue.Bytes.Length + parameters.Bytes.Length];
        int position = CopyBytesAt(name, output, 0);
        output[position] = 0x3d;
        position = CopyBytesAt(encodedValue.Bytes, output, position + 1);
        CopyBytesAt(parameters.Bytes, output, position);
        return CreateDone(output);
    }

    static FieldValueWriteResult SerializeViaMember(
        HttpViaEntry value,
        int maximumCommentDepth)
    {
        if (value == null || value.ReceivedProtocol == null || value.ReceivedBy == null ||
            value.HasPort && (value.Port < 0 || value.Port > 65535))
        {
            return CreateMalformed(0);
        }

        byte[] protocol = GetAsciiBytes(value.ReceivedProtocol);
        FieldValueParseResult parsedProtocol = CommonFieldValueParser.ParseUpgrade(
            protocol,
            protocol.Length,
            1);
        byte[] receivedBy = GetAsciiBytes(value.ReceivedBy);
        FieldValueParseResult parsedReceivedBy = FieldValueParser.ParseToken(
            receivedBy,
            receivedBy.Length);
        if (parsedProtocol.Status != HttpOperationStatus.Done || parsedProtocol.Values.Count != 1 ||
            parsedReceivedBy.Status != HttpOperationStatus.Done)
        {
            return CreateMalformed(0);
        }

        byte[] port = value.HasPort ?
            Encoding.ASCII.GetBytes(value.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)) :
            Array.Empty<byte>();
        byte[] comment = Array.Empty<byte>();
        if (value.Comment != null)
        {
            FieldValueWriteResult encodedComment = FieldValueWriter.SerializeComment(
                value.Comment,
                int.MaxValue);
            if (encodedComment.Status != HttpOperationStatus.Done)
            {
                return encodedComment;
            }

            comment = encodedComment.Bytes;
        }

        int required = protocol.Length + 1 + receivedBy.Length;
        if (value.HasPort)
        {
            required = required + 1 + port.Length;
        }

        if (value.Comment != null)
        {
            required = required + 1 + comment.Length;
        }

        byte[] output = new byte[required];
        int position = CopyBytesAt(protocol, output, 0);
        output[position] = 0x20;
        position = CopyBytesAt(receivedBy, output, position + 1);
        if (value.HasPort)
        {
            output[position] = 0x3a;
            position = CopyBytesAt(port, output, position + 1);
        }

        if (value.Comment != null)
        {
            output[position] = 0x20;
            CopyBytesAt(comment, output, position + 1);
        }

        CommonFieldParseResult validation = CommonFieldValueParser.ParseVia(
            output,
            output.Length,
            1,
            maximumCommentDepth);
        if (validation.Status != HttpOperationStatus.Done)
        {
            return validation.IsLimitExceeded ? CreateLimit(required) : CreateMalformed(0);
        }

        return CreateDone(output);
    }

    static byte[] GetAsciiBytes(string value)
    {
        if (value == null)
        {
            return Array.Empty<byte>();
        }

        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 0x7f)
            {
                return Array.Empty<byte>();
            }

            index = index + 1;
        }

        return Encoding.ASCII.GetBytes(value);
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
