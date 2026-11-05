using System;
using System.Collections.Generic;
using System.Text;

namespace dns;

internal static class SvcbRecordDataCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success)
        {
            return validation;
        }

        if (length < 3)
        {
            return DecodeFailure(
                "svcb",
                "SVCB data requires a priority and target name.",
                offset + length);
        }

        DnsDecodeResult target = SharedRecordDataCodec.DecodeUncompressedNamePrefix(
            message, offset + 2, length - 2, "svcb.targetName");
        if (!target.Success || target.Name == null)
        {
            return target;
        }

        DnsSvcbData svcb = new DnsSvcbData();
        svcb.Priority = DnsHeaderCodec.ReadUnsigned16(message, offset);
        svcb.TargetName = target.Name;
        int cursor = offset + 2 + target.BytesConsumed;
        int end = offset + length;
        int previousKey = -1;
        while (cursor < end)
        {
            if (end - cursor < 4)
            {
                return DecodeFailure(
                    "svcb.parameters",
                    "A Service Binding parameter header is truncated.",
                    cursor);
            }

            int key = DnsHeaderCodec.ReadUnsigned16(message, cursor);
            int valueLength = DnsHeaderCodec.ReadUnsigned16(message, cursor + 2);
            if (key == 65535)
            {
                return DecodeFailure(
                    "svcb.parameterKey",
                    "Service Binding parameter key 65535 is reserved as invalid.",
                    cursor);
            }

            if (key <= previousKey)
            {
                return DecodeFailure(
                    "svcb.parameterKey",
                    "Service Binding parameter keys must be strictly increasing.",
                    cursor);
            }

            if (valueLength > end - cursor - 4)
            {
                return DecodeFailure(
                    "svcb.parameterValue",
                    "A Service Binding parameter value is truncated.",
                    cursor + 4);
            }

            byte[] rawValue = SharedRecordDataCodec.CopyRegion(
                message, cursor + 4, valueLength);
            DnsSvcbParameter parameter = new DnsSvcbParameter();
            parameter.Key = (DnsSvcbParameterKey)key;
            parameter.KeyValue = key;
            parameter.RawValue = rawValue;
            DnsDecodeResult parameterResult = DecodeParameterValue(
                parameter, key, rawValue, cursor + 4);
            if (!parameterResult.Success)
            {
                return parameterResult;
            }

            svcb.Parameters.Add(parameter);
            previousKey = key;
            cursor = cursor + 4 + valueLength;
        }

        DnsDecodeResult consistency = ValidateDecodedParameters(svcb, offset);
        if (!consistency.Success)
        {
            return consistency;
        }

        DnsRecordData data = new DnsRecordData();
        data.Svcb = svcb;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeRecordData(
        WireWriter? writer,
        CompressionTable table,
        DnsSvcbData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.TargetName == null || data.Parameters == null)
        {
            return WriteFailure(
                writer, "svcb", "Complete SVCB data and encoding state are required.");
        }

        if (data.Priority < 0 || data.Priority > 65535)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidSvcbParameters,
                "svcb.priority",
                "A Service Binding priority must fit in sixteen bits.",
                writer.WriteOffset());
        }

        List<DnsSvcbParameter> parameters = CopyAndSortParameters(data.Parameters);
        WireWriteResult consistency = ValidateEncodedParameters(
            writer, parameters, data.Priority);
        if (!consistency.Success)
        {
            return consistency;
        }

        int start = writer.WriteOffset();
        WireWriteResult priority = writer.WriteUnsigned16((ulong)data.Priority);
        if (!priority.Success)
        {
            return priority;
        }

        WireWriteResult target = table.WriteName(
            writer, data.TargetName, options, false);
        if (!target.Success)
        {
            return target;
        }

        int index = 0;
        while (index < parameters.Count)
        {
            DnsSvcbParameter parameter = parameters[index];
            int key = ResolveParameterKey(parameter);
            WireWriter counter = new WireWriter(null, 0, 65535);
            WireWriteResult counted = EncodeParameterValue(counter, parameter, key);
            if (!counted.Success)
            {
                return counted;
            }

            WireWriteResult keyResult = writer.WriteUnsigned16((ulong)key);
            if (!keyResult.Success)
            {
                return keyResult;
            }

            WireWriteResult lengthResult = writer.WriteUnsigned16(
                (ulong)counter.WrittenLength());
            if (!lengthResult.Success)
            {
                return lengthResult;
            }

            WireWriteResult valueResult = EncodeParameterValue(
                writer, parameter, key);
            if (!valueResult.Success)
            {
                return valueResult;
            }

            index = index + 1;
        }

        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    static DnsDecodeResult DecodeParameterValue(
        DnsSvcbParameter parameter,
        int key,
        byte[] value,
        int offset)
    {
        if (key == 0)
        {
            if (value.Length == 0 || value.Length % 2 != 0)
            {
                return DecodeFailure(
                    "svcb.mandatory",
                    "The mandatory value must contain one or more complete keys.",
                    offset);
            }

            int cursor = 0;
            int previous = -1;
            while (cursor < value.Length)
            {
                int mandatoryKey = DnsHeaderCodec.ReadUnsigned16(value, cursor);
                if (mandatoryKey == 0 || mandatoryKey == 65535 ||
                    mandatoryKey <= previous)
                {
                    return DecodeFailure(
                        "svcb.mandatory",
                        "Mandatory keys must be valid, unique, and strictly increasing.",
                        offset + cursor);
                }

                parameter.MandatoryKeyValues.Add(mandatoryKey);
                previous = mandatoryKey;
                cursor = cursor + 2;
            }

            return DecodeValueSuccess();
        }

        if (key == 1)
        {
            int cursor = 0;
            while (cursor < value.Length)
            {
                int identifierLength = value[cursor];
                cursor = cursor + 1;
                if (identifierLength == 0 || identifierLength > value.Length - cursor)
                {
                    return DecodeFailure(
                        "svcb.alpn",
                        "ALPN identifiers must be non-empty and exactly fill the value.",
                        offset + cursor - 1);
                }

                parameter.AlpnIdentifiers.Add(
                    SharedRecordDataCodec.CopyRegion(
                        value, cursor, identifierLength));
                cursor = cursor + identifierLength;
            }

            if (parameter.AlpnIdentifiers.Count == 0)
            {
                return DecodeFailure(
                    "svcb.alpn",
                    "The ALPN value must contain at least one identifier.",
                    offset);
            }

            return DecodeValueSuccess();
        }

        if (key == 2)
        {
            if (value.Length != 0)
            {
                return DecodeFailure(
                    "svcb.noDefaultAlpn",
                    "The no-default-alpn value must be empty.",
                    offset);
            }

            parameter.NoDefaultAlpn = true;
            return DecodeValueSuccess();
        }

        if (key == 3)
        {
            if (value.Length != 2)
            {
                return DecodeFailure(
                    "svcb.port",
                    "The port value must contain exactly two octets.",
                    offset);
            }

            parameter.HasPort = true;
            parameter.Port = DnsHeaderCodec.ReadUnsigned16(value, 0);
            return DecodeValueSuccess();
        }

        if (key == 4)
        {
            return DecodeAddressHints(parameter.Ipv4Hints, value, 4, offset);
        }

        if (key == 5)
        {
            if (!EchConfigListIsValid(value))
            {
                return DecodeFailure(
                    "svcb.ech",
                    "The ECH value must be a complete non-empty ECHConfigList.",
                    offset);
            }

            parameter.Ech = value;
            return DecodeValueSuccess();
        }

        if (key == 6)
        {
            return DecodeAddressHints(parameter.Ipv6Hints, value, 16, offset);
        }

        if (key == 7)
        {
            if (!Utf8BytesAreValid(value))
            {
                return DecodeFailure(
                    "svcb.dohPath",
                    "The DoH path template must contain valid UTF-8.",
                    offset);
            }

            string path = Encoding.UTF8.GetString(value);
            if (!DohPathIsValid(path))
            {
                return DecodeFailure(
                    "svcb.dohPath",
                    "The DoH path must be a relative URI template containing dns.",
                    offset);
            }

            parameter.DohPath = path;
            return DecodeValueSuccess();
        }

        if (key == 8)
        {
            if (value.Length != 0)
            {
                return DecodeFailure(
                    "svcb.obliviousHttp",
                    "The ohttp value must be empty.",
                    offset);
            }

            parameter.ObliviousHttp = value;
            return DecodeValueSuccess();
        }

        return DecodeValueSuccess();
    }

    static DnsDecodeResult DecodeAddressHints(
        List<byte[]> destination,
        byte[] value,
        int width,
        int offset)
    {
        if (value.Length == 0 || value.Length % width != 0)
        {
            return DecodeFailure(
                width == 4 ? "svcb.ipv4Hint" : "svcb.ipv6Hint",
                "Address hints must contain one or more complete addresses.",
                offset);
        }

        int cursor = 0;
        while (cursor < value.Length)
        {
            destination.Add(SharedRecordDataCodec.CopyRegion(
                value, cursor, width));
            cursor = cursor + width;
        }

        return DecodeValueSuccess();
    }

    static DnsDecodeResult ValidateDecodedParameters(
        DnsSvcbData data,
        int offset)
    {
        if (data.Priority == 0)
        {
            return DecodeValueSuccess();
        }

        bool hasAlpn = ParameterKeyIsPresent(data.Parameters, 1);
        if (ParameterKeyIsPresent(data.Parameters, 2) && !hasAlpn)
        {
            return DecodeFailure(
                "svcb.noDefaultAlpn",
                "no-default-alpn requires an alpn parameter.",
                offset);
        }

        DnsSvcbParameter? mandatory = FindParameter(data.Parameters, 0);
        if (mandatory != null)
        {
            int index = 0;
            while (index < mandatory.MandatoryKeyValues.Count)
            {
                if (!ParameterKeyIsPresent(
                    data.Parameters, mandatory.MandatoryKeyValues[index]))
                {
                    return DecodeFailure(
                        "svcb.mandatory",
                        "Every mandatory key must also be present in the record.",
                        offset);
                }

                index = index + 1;
            }
        }

        return DecodeValueSuccess();
    }

    static WireWriteResult ValidateEncodedParameters(
        WireWriter writer,
        List<DnsSvcbParameter> parameters,
        int priority)
    {
        int previousKey = -1;
        int index = 0;
        while (index < parameters.Count)
        {
            DnsSvcbParameter parameter = parameters[index];
            if (parameter == null)
            {
                return writer.FailAt(
                    DnsDiagnosticCode.InvalidSvcbParameters,
                    "svcb.parameters",
                    "Service Binding parameters cannot contain null entries.",
                    writer.WriteOffset());
            }

            int key = ResolveParameterKey(parameter);
            if (key < 0 || key >= 65535)
            {
                return writer.FailAt(
                    DnsDiagnosticCode.InvalidSvcbParameters,
                    "svcb.parameterKey",
                    "A Service Binding parameter key must be between zero and 65534.",
                    writer.WriteOffset());
            }

            if (key == previousKey)
            {
                return writer.FailAt(
                    DnsDiagnosticCode.InvalidSvcbParameters,
                    "svcb.parameterKey",
                    "Service Binding parameter keys must be unique.",
                    writer.WriteOffset());
            }

            previousKey = key;
            index = index + 1;
        }

        if (priority != 0 && ParameterKeyIsPresent(parameters, 2) &&
            !ParameterKeyIsPresent(parameters, 1))
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidSvcbParameters,
                "svcb.noDefaultAlpn",
                "no-default-alpn requires an alpn parameter.",
                writer.WriteOffset());
        }

        DnsSvcbParameter? mandatory = FindParameter(parameters, 0);
        if (mandatory != null)
        {
            List<int> values;
            if (mandatory.UseRawValue)
            {
                if (mandatory.RawValue == null)
                {
                    return writer.FailAt(
                        DnsDiagnosticCode.InvalidSvcbParameters,
                        "svcb.mandatory",
                        "A raw mandatory value is required.",
                        writer.WriteOffset());
                }

                DnsSvcbParameter decoded = new DnsSvcbParameter();
                DnsDecodeResult rawValidation = DecodeParameterValue(
                    decoded, 0, mandatory.RawValue, writer.WriteOffset());
                if (!rawValidation.Success)
                {
                    return writer.FailAt(
                        DnsDiagnosticCode.InvalidSvcbParameters,
                        rawValidation.Diagnostics[0].Field ?? "svcb.mandatory",
                        rawValidation.Diagnostics[0].Reason,
                        writer.WriteOffset());
                }

                values = decoded.MandatoryKeyValues;
            }
            else
            {
                values = CopyAndSortKeys(mandatory.MandatoryKeyValues);
            }

            int valueIndex = 0;
            while (valueIndex < values.Count)
            {
                int key = values[valueIndex];
                if (key <= 0 || key >= 65535 ||
                    (valueIndex > 0 && key == values[valueIndex - 1]) ||
                    (priority != 0 && !ParameterKeyIsPresent(parameters, key)))
                {
                    return writer.FailAt(
                        DnsDiagnosticCode.InvalidSvcbParameters,
                        "svcb.mandatory",
                        "Mandatory keys must be valid, unique, and present in the record.",
                        writer.WriteOffset());
                }

                valueIndex = valueIndex + 1;
            }

            if (values.Count == 0)
            {
                return writer.FailAt(
                    DnsDiagnosticCode.InvalidSvcbParameters,
                    "svcb.mandatory",
                    "The mandatory value must contain at least one key.",
                    writer.WriteOffset());
            }
        }

        return writer.Succeed(writer.WriteOffset(), 0, -1);
    }

    static WireWriteResult EncodeParameterValue(
        WireWriter writer,
        DnsSvcbParameter parameter,
        int key)
    {
        if (parameter.UseRawValue || key > 8)
        {
            if (parameter.RawValue == null)
            {
                return WriteFailure(
                    writer, "svcb.parameterValue", "A raw parameter value is required.");
            }

            if (key <= 8)
            {
                DnsSvcbParameter decoded = new DnsSvcbParameter();
                DnsDecodeResult validation = DecodeParameterValue(
                    decoded, key, parameter.RawValue, writer.WriteOffset());
                if (!validation.Success)
                {
                    return writer.FailAt(
                        DnsDiagnosticCode.InvalidSvcbParameters,
                        validation.Diagnostics[0].Field ?? "svcb.parameterValue",
                        validation.Diagnostics[0].Reason,
                        writer.WriteOffset());
                }
            }

            return writer.WriteBytes(parameter.RawValue);
        }

        if (key == 0)
        {
            List<int> values = CopyAndSortKeys(parameter.MandatoryKeyValues);
            int index = 0;
            while (index < values.Count)
            {
                WireWriteResult keyResult = writer.WriteUnsigned16(
                    (ulong)values[index]);
                if (!keyResult.Success)
                {
                    return keyResult;
                }

                index = index + 1;
            }

            return writer.Succeed(
                writer.WriteOffset() - values.Count * 2, values.Count * 2, -1);
        }

        if (key == 1)
        {
            if (parameter.AlpnIdentifiers == null ||
                parameter.AlpnIdentifiers.Count == 0)
            {
                return WriteFailure(
                    writer, "svcb.alpn", "At least one ALPN identifier is required.");
            }

            int start = writer.WriteOffset();
            int index = 0;
            while (index < parameter.AlpnIdentifiers.Count)
            {
                byte[] identifier = parameter.AlpnIdentifiers[index];
                if (identifier == null || identifier.Length == 0 ||
                    identifier.Length > 255)
                {
                    return WriteFailure(
                        writer,
                        "svcb.alpn",
                        "Each ALPN identifier must contain between one and 255 octets.");
                }

                WireWriteResult identifierResult =
                    writer.WriteCharacterString(identifier);
                if (!identifierResult.Success)
                {
                    return identifierResult;
                }

                index = index + 1;
            }

            return writer.Succeed(start, writer.WriteOffset() - start, -1);
        }

        if (key == 2)
        {
            if (!parameter.NoDefaultAlpn)
            {
                return WriteFailure(
                    writer,
                    "svcb.noDefaultAlpn",
                    "The typed no-default-alpn indicator must be set.");
            }

            return writer.Succeed(writer.WriteOffset(), 0, -1);
        }

        if (key == 3)
        {
            if (!parameter.HasPort || parameter.Port < 0 ||
                parameter.Port > 65535)
            {
                return WriteFailure(
                    writer,
                    "svcb.port",
                    "A typed port must be present and fit in sixteen bits.");
            }

            return writer.WriteUnsigned16((ulong)parameter.Port);
        }

        if (key == 4)
        {
            return EncodeAddressHints(writer, parameter.Ipv4Hints, 4);
        }

        if (key == 5)
        {
            if (parameter.Ech == null || !EchConfigListIsValid(parameter.Ech))
            {
                return WriteFailure(
                    writer,
                    "svcb.ech",
                    "The ECH value must be a complete non-empty ECHConfigList.");
            }

            return writer.WriteBytes(parameter.Ech);
        }

        if (key == 6)
        {
            return EncodeAddressHints(writer, parameter.Ipv6Hints, 16);
        }

        if (key == 7)
        {
            if (parameter.DohPath == null ||
                !Utf16StringIsValid(parameter.DohPath) ||
                !DohPathIsValid(parameter.DohPath))
            {
                return WriteFailure(
                    writer,
                    "svcb.dohPath",
                    "The DoH path must be a valid relative URI template containing dns.");
            }

            return writer.WriteBytes(Encoding.UTF8.GetBytes(parameter.DohPath));
        }

        if (parameter.ObliviousHttp == null ||
            parameter.ObliviousHttp.Length != 0)
        {
            return WriteFailure(
                writer, "svcb.obliviousHttp", "The ohttp value must be empty.");
        }

        return writer.Succeed(writer.WriteOffset(), 0, -1);
    }

    static WireWriteResult EncodeAddressHints(
        WireWriter writer,
        List<byte[]> addresses,
        int width)
    {
        if (addresses == null || addresses.Count == 0)
        {
            return WriteFailure(
                writer,
                width == 4 ? "svcb.ipv4Hint" : "svcb.ipv6Hint",
                "At least one address hint is required.");
        }

        int start = writer.WriteOffset();
        int index = 0;
        while (index < addresses.Count)
        {
            byte[] address = addresses[index];
            if (address == null || address.Length != width)
            {
                return WriteFailure(
                    writer,
                    width == 4 ? "svcb.ipv4Hint" : "svcb.ipv6Hint",
                    "Each address hint must have the expected address width.");
            }

            WireWriteResult addressResult = writer.WriteBytes(address);
            if (!addressResult.Success)
            {
                return addressResult;
            }

            index = index + 1;
        }

        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    static List<DnsSvcbParameter> CopyAndSortParameters(
        List<DnsSvcbParameter> source)
    {
        List<DnsSvcbParameter> result = new List<DnsSvcbParameter>();
        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }

        result.Sort((left, right) =>
            ResolveParameterKey(left).CompareTo(ResolveParameterKey(right)));
        return result;
    }

    static List<int> CopyAndSortKeys(List<int> source)
    {
        List<int> result = new List<int>();
        if (source == null)
        {
            return result;
        }

        int index = 0;
        while (index < source.Count)
        {
            result.Add(source[index]);
            index = index + 1;
        }

        result.Sort();
        return result;
    }

    static int ResolveParameterKey(DnsSvcbParameter parameter)
    {
        if (parameter == null)
        {
            return -1;
        }

        return DnsHeaderCodec.ResolveCode(
            parameter.KeyValue, (int)parameter.Key);
    }

    static bool ParameterKeyIsPresent(
        List<DnsSvcbParameter> parameters,
        int key)
    {
        return FindParameter(parameters, key) != null;
    }

    static DnsSvcbParameter? FindParameter(
        List<DnsSvcbParameter> parameters,
        int key)
    {
        int index = 0;
        while (index < parameters.Count)
        {
            DnsSvcbParameter parameter = parameters[index];
            if (parameter != null && ResolveParameterKey(parameter) == key)
            {
                return parameter;
            }

            index = index + 1;
        }

        return null;
    }

    static bool EchConfigListIsValid(byte[] value)
    {
        if (value == null || value.Length < 6)
        {
            return false;
        }

        int encodedLength = DnsHeaderCodec.ReadUnsigned16(value, 0);
        if (encodedLength == 0 || encodedLength != value.Length - 2)
        {
            return false;
        }

        int cursor = 2;
        while (cursor < value.Length)
        {
            if (value.Length - cursor < 4)
            {
                return false;
            }

            int configLength = DnsHeaderCodec.ReadUnsigned16(value, cursor + 2);
            if (configLength > value.Length - cursor - 4)
            {
                return false;
            }

            cursor = cursor + 4 + configLength;
        }

        return cursor == value.Length;
    }

    static bool Utf8BytesAreValid(byte[] value)
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

            if (first >= 194 && first <= 223)
            {
                if (!ContinuationByteIsValid(value, index + 1)) return false;
                index = index + 2;
                continue;
            }

            if (first >= 224 && first <= 239)
            {
                if (index + 2 >= value.Length) return false;
                int second = value[index + 1];
                if ((first == 224 && (second < 160 || second > 191)) ||
                    (first == 237 && (second < 128 || second > 159)) ||
                    (first != 224 && first != 237 &&
                        (second < 128 || second > 191)) ||
                    !ContinuationByteIsValid(value, index + 2))
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (first >= 240 && first <= 244)
            {
                if (index + 3 >= value.Length) return false;
                int second = value[index + 1];
                if ((first == 240 && (second < 144 || second > 191)) ||
                    (first == 244 && (second < 128 || second > 143)) ||
                    (first != 240 && first != 244 &&
                        (second < 128 || second > 191)) ||
                    !ContinuationByteIsValid(value, index + 2) ||
                    !ContinuationByteIsValid(value, index + 3))
                {
                    return false;
                }

                index = index + 4;
                continue;
            }

            return false;
        }

        return true;
    }

    static bool ContinuationByteIsValid(byte[] value, int index)
    {
        return index < value.Length && value[index] >= 128 && value[index] <= 191;
    }

    static bool Utf16StringIsValid(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character >= 55296 && character <= 56319)
            {
                if (index + 1 >= value.Length || value[index + 1] < 56320 ||
                    value[index + 1] > 57343)
                {
                    return false;
                }

                index = index + 2;
                continue;
            }

            if (character >= 56320 && character <= 57343)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static bool DohPathIsValid(string value)
    {
        if (value.Length == 0 || value[0] != '/')
        {
            return false;
        }

        bool hasDns = false;
        int index = 0;
        while (index < value.Length)
        {
            int character = value[index];
            if (character <= 32 || character == 127 || character == '#')
            {
                return false;
            }

            if (character == '{')
            {
                int close = value.IndexOf('}', index + 1);
                if (close < 0 || close == index + 1 ||
                    !TemplateExpressionIsValid(value, index + 1, close))
                {
                    return false;
                }

                if (TemplateExpressionContainsDns(value, index + 1, close))
                {
                    hasDns = true;
                }

                index = close + 1;
                continue;
            }

            if (character == '}')
            {
                return false;
            }

            if (character == '%')
            {
                if (index + 2 >= value.Length ||
                    !HexCharacterIsValid(value[index + 1]) ||
                    !HexCharacterIsValid(value[index + 2]))
                {
                    return false;
                }

                index = index + 3;
                continue;
            }

            if (character < 128 &&
                !TemplateLiteralCharacterIsValid((char)character))
            {
                return false;
            }

            index = index + 1;
        }

        return hasDns;
    }

    static bool TemplateExpressionIsValid(
        string value,
        int start,
        int end)
    {
        int cursor = start;
        if (cursor < end && (value[cursor] == '+' || value[cursor] == '#' ||
            value[cursor] == '.' || value[cursor] == '/' ||
            value[cursor] == ';' || value[cursor] == '?' ||
            value[cursor] == '&'))
        {
            if (value[cursor] == '#')
            {
                return false;
            }

            cursor = cursor + 1;
        }

        while (cursor < end)
        {
            int itemEnd = cursor;
            while (itemEnd < end && value[itemEnd] != ',')
            {
                itemEnd = itemEnd + 1;
            }

            if (!TemplateVariableIsValid(value, cursor, itemEnd))
            {
                return false;
            }

            cursor = itemEnd + 1;
        }

        return cursor == end + 1;
    }

    static bool TemplateVariableIsValid(
        string value,
        int start,
        int end)
    {
        int modifier = start;
        while (modifier < end && value[modifier] != ':' &&
            value[modifier] != '*')
        {
            modifier = modifier + 1;
        }

        if (modifier == start || value[start] == '.' ||
            value[modifier - 1] == '.')
        {
            return false;
        }

        int cursor = start;
        bool previousDot = false;
        while (cursor < modifier)
        {
            int character = value[cursor];
            if (character == '.')
            {
                if (previousDot)
                {
                    return false;
                }

                previousDot = true;
                cursor = cursor + 1;
                continue;
            }

            previousDot = false;
            if (character == '%')
            {
                if (cursor + 2 >= modifier ||
                    !HexCharacterIsValid(value[cursor + 1]) ||
                    !HexCharacterIsValid(value[cursor + 2]))
                {
                    return false;
                }

                cursor = cursor + 3;
                continue;
            }

            if (!AsciiAlphaNumericIsValid((char)character) && character != '_')
            {
                return false;
            }

            cursor = cursor + 1;
        }

        if (modifier == end)
        {
            return true;
        }

        if (value[modifier] == '*')
        {
            return modifier + 1 == end;
        }

        cursor = modifier + 1;
        int digitCount = 0;
        while (cursor < end)
        {
            if (value[cursor] < '0' || value[cursor] > '9' || digitCount == 4)
            {
                return false;
            }

            digitCount = digitCount + 1;
            cursor = cursor + 1;
        }

        return digitCount > 0 && value[modifier + 1] != '0';
    }

    static bool TemplateLiteralCharacterIsValid(char value)
    {
        if (AsciiAlphaNumericIsValid(value) || value == '-' || value == '.' ||
            value == '_' || value == '~')
        {
            return true;
        }

        return value == ':' || value == '/' || value == '?' || value == '@' ||
            value == '!' || value == '$' || value == '&' || value == '\'' ||
            value == '(' || value == ')' || value == '*' || value == '+' ||
            value == ',' || value == ';' || value == '=';
    }

    static bool AsciiAlphaNumericIsValid(char value)
    {
        return (value >= 'a' && value <= 'z') ||
            (value >= 'A' && value <= 'Z') ||
            (value >= '0' && value <= '9');
    }

    static bool HexCharacterIsValid(char value)
    {
        return (value >= 'a' && value <= 'f') ||
            (value >= 'A' && value <= 'F') ||
            (value >= '0' && value <= '9');
    }

    static bool TemplateExpressionContainsDns(
        string value,
        int start,
        int end)
    {
        int cursor = start;
        if (cursor < end && (value[cursor] == '+' || value[cursor] == '#' ||
            value[cursor] == '.' || value[cursor] == '/' ||
            value[cursor] == ';' || value[cursor] == '?' ||
            value[cursor] == '&'))
        {
            cursor = cursor + 1;
        }

        while (cursor < end)
        {
            int itemEnd = cursor;
            while (itemEnd < end && value[itemEnd] != ',')
            {
                itemEnd = itemEnd + 1;
            }

            int modifier = cursor;
            while (modifier < itemEnd && value[modifier] != ':' &&
                value[modifier] != '*')
            {
                modifier = modifier + 1;
            }

            if (modifier - cursor == 3 && value[cursor] == 'd' &&
                value[cursor + 1] == 'n' && value[cursor + 2] == 's')
            {
                return true;
            }

            cursor = itemEnd + 1;
        }

        return false;
    }

    static DnsDecodeResult DecodeValueSuccess()
    {
        DnsDecodeResult result = new DnsDecodeResult();
        result.Success = true;
        return result;
    }

    static DnsDecodeResult DecodeFailure(
        string field,
        string reason,
        int offset)
    {
        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidSvcbParameters,
            field,
            reason,
            offset);
    }

    static WireWriteResult WriteFailure(
        WireWriter? writer,
        string field,
        string reason)
    {
        if (writer == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.InvalidSvcbParameters,
                field,
                reason,
                0);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidSvcbParameters,
            field,
            reason,
            writer.WriteOffset());
    }
}
