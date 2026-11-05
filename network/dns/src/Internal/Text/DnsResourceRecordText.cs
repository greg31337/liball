using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace dns;

internal static class DnsResourceRecordText
{
    internal static DnsTextResult ParseResourceRecord(
        string text,
        DnsTextOptions options)
    {
        if (text == null || options == null)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "resourceRecord",
                "Resource-record text and options are required.",
                0);
        }

        List<string> tokens = DnsTextOperations.ReadWhitespaceTokens(text);
        if (tokens.Count < 5)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.InvalidText,
                "resourceRecord",
                "A resource record requires owner, TTL, class, type, and RDATA fields.",
                0);
        }

        DnsTextResult owner = DnsPresentationNameText.ParseNameToken(
            tokens[0], options);
        if (!owner.Success || owner.Name == null)
        {
            return owner;
        }

        DnsTextNumberResult firstClass = DnsMnemonicText.ParseClass(
            tokens[1], options.UseMnemonicCodes);
        DnsTextNumberResult firstTtl = DnsTimeToLiveText.ParseTimeToLive(tokens[1]);
        DnsTextNumberResult secondClass = DnsMnemonicText.ParseClass(
            tokens[2], options.UseMnemonicCodes);
        DnsTextNumberResult secondTtl = DnsTimeToLiveText.ParseTimeToLive(tokens[2]);
        DnsTextNumberResult recordClass;
        DnsTextNumberResult timeToLive;
        if (firstClass.Success && secondTtl.Success)
        {
            recordClass = firstClass;
            timeToLive = secondTtl;
        }
        else if (firstTtl.Success && secondClass.Success)
        {
            recordClass = secondClass;
            timeToLive = firstTtl;
        }
        else
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.InvalidText,
                "resourceRecord",
                "The record requires one TTL and one class field.",
                0);
        }

        DnsTextNumberResult recordType = DnsMnemonicText.ParseRecordType(
            tokens[3], options.UseMnemonicCodes);
        if (!recordType.Success)
        {
            return DnsTextOperations.FromNumberFailure(recordType);
        }

        DnsRecordData data;
        byte[] bytes = [];
        bool useRawData = false;
        if (tokens[4] == "\\#")
        {
            if (tokens.Count < 6)
            {
                return DnsTextOperations.Failure(
                    DnsDiagnosticCode.InvalidText,
                    "recordData",
                    "RFC 3597 RDATA requires a length field.",
                    0);
            }

            DnsTextNumberResult length = DnsTextNumber.ParseDecimal(
                tokens[5], 65535, "recordDataLength");
            if (!length.Success)
            {
                return DnsTextOperations.FromNumberFailure(length);
            }

            DnsTextResult hexResult = ParseHexWords(
                tokens, 6, (int)length.Value);
            if (!hexResult.Success)
            {
                return hexResult;
            }

            bytes = ReadHexBytes(tokens, 6, (int)length.Value);
            DnsOpaqueData opaque = new DnsOpaqueData();
            opaque.Bytes = CopyBytes(bytes);
            data = new DnsRecordData();
            data.Opaque = opaque;
            useRawData = true;
        }
        else
        {
            DnsRecordDataTextResult typed =
                DnsTypedRecordDataParser.ParseRecordData(
                    (int)recordType.Value, tokens, 4, options);
            if (!typed.Success || typed.Data == null)
            {
                return FromTypedFailure(typed);
            }

            data = typed.Data;
        }

        DnsResourceRecord record = new DnsResourceRecord();
        record.Name = owner.Name;
        record.TimeToLive = (uint)timeToLive.Value;
        record.Class = (DnsClass)recordClass.Value;
        record.ClassValue = (int)recordClass.Value;
        record.RecordType = (DnsRecordType)recordType.Value;
        record.RecordTypeValue = (int)recordType.Value;
        record.Data = data;
        record.RawData = bytes;
        record.UseRawData = useRawData;
        DnsTextResult result = DnsTextOperations.SuccessText(text);
        result.Record = record;
        return result;
    }

    internal static DnsTextResult FormatResourceRecord(
        DnsResourceRecord record,
        DnsTextOptions options)
    {
        if (record == null || options == null)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "resourceRecord",
                "A resource record and text options are required.",
                0);
        }

        int classValue = DnsHeaderCodec.ResolveCode(
            record.ClassValue, (int)record.Class);
        int typeValue = DnsHeaderCodec.ResolveCode(
            record.RecordTypeValue, (int)record.RecordType);
        if (classValue < 0 || classValue > 65535 ||
            typeValue < 0 || typeValue > 65535)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.ValueOutOfRange,
                "resourceRecord",
                "Record class and type values must fit in sixteen bits.",
                0);
        }

        byte[]? opaqueData = ReadOpaqueBytes(record, typeValue);
        string recordDataText;
        if (opaqueData != null)
        {
            if (opaqueData.Length > 65535)
            {
                return DnsTextOperations.Failure(
                    DnsDiagnosticCode.InvalidLength,
                    "recordDataLength",
                    "RFC 3597 RDATA cannot exceed 65535 octets.",
                    0);
            }

            recordDataText = "\\# " +
                opaqueData.Length.ToString(CultureInfo.InvariantCulture);
            if (opaqueData.Length > 0)
            {
                recordDataText = recordDataText + " " +
                    FormatHex(opaqueData);
            }
        }
        else if (record.Data != null)
        {
            DnsTextResult typed = DnsTypedRecordDataText.FormatRecordData(
                typeValue, record.Data, options);
            if (!typed.Success)
            {
                return typed;
            }

            recordDataText = typed.Text;
        }
        else
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.InvalidRecordData,
                "recordData",
                "Typed or opaque record data is required.",
                0);
        }

        DnsTextResult owner = DnsPresentationNameText.FormatName(
            record.Name, options);
        if (!owner.Success)
        {
            return owner;
        }

        string text = owner.Text + " " +
            DnsTimeToLiveText.FormatTimeToLive(record.TimeToLive) + " " +
            DnsMnemonicText.FormatClass(
                classValue, options.UseMnemonicCodes) + " " +
            DnsMnemonicText.FormatRecordType(
                typeValue, options.UseMnemonicCodes) + " " + recordDataText;

        DnsTextResult result = DnsTextOperations.SuccessText(text);
        result.Record = record;
        return result;
    }

    static DnsTextResult ParseHexWords(
        List<string> tokens,
        int start,
        int expectedLength)
    {
        int digitCount = 0;
        int tokenIndex = start;
        while (tokenIndex < tokens.Count)
        {
            string token = tokens[tokenIndex];
            if ((token.Length & 1) != 0)
            {
                return DnsTextOperations.Failure(
                    DnsDiagnosticCode.InvalidText,
                    "recordData",
                    "Each RFC 3597 hexadecimal word requires an even number of digits.",
                    tokenIndex);
            }

            int digitIndex = 0;
            while (digitIndex < token.Length)
            {
                if (ReadHexNibble(token[digitIndex]) < 0)
                {
                    return DnsTextOperations.Failure(
                        DnsDiagnosticCode.InvalidText,
                        "recordData",
                        "RFC 3597 RDATA contains a non-hexadecimal digit.",
                        tokenIndex);
                }

                digitCount = digitCount + 1;
                digitIndex = digitIndex + 1;
            }

            tokenIndex = tokenIndex + 1;
        }

        if (digitCount != expectedLength * 2)
        {
            return DnsTextOperations.Failure(
                DnsDiagnosticCode.InvalidLength,
                "recordDataLength",
                "The RFC 3597 length does not match the hexadecimal data.",
                start);
        }

        return DnsTextOperations.SuccessText(string.Empty);
    }

    static byte[] ReadHexBytes(
        List<string> tokens,
        int start,
        int length)
    {
        byte[] bytes = new byte[length];
        int byteIndex = 0;
        int highNibble = -1;
        int tokenIndex = start;
        while (tokenIndex < tokens.Count)
        {
            string token = tokens[tokenIndex];
            int digitIndex = 0;
            while (digitIndex < token.Length)
            {
                int nibble = ReadHexNibble(token[digitIndex]);
                if (highNibble < 0)
                {
                    highNibble = nibble;
                }
                else
                {
                    bytes[byteIndex] = (byte)((highNibble << 4) | nibble);
                    byteIndex = byteIndex + 1;
                    highNibble = -1;
                }

                digitIndex = digitIndex + 1;
            }

            tokenIndex = tokenIndex + 1;
        }

        return bytes;
    }

    static int ReadHexNibble(char value)
    {
        if (value >= '0' && value <= '9') return value - '0';
        if (value >= 'a' && value <= 'f') return value - 'a' + 10;
        if (value >= 'A' && value <= 'F') return value - 'A' + 10;
        return -1;
    }

    static string FormatHex(byte[] data)
    {
        const string Digits = "0123456789ABCDEF";
        StringBuilder builder = new StringBuilder(data.Length * 2);
        int index = 0;
        while (index < data.Length)
        {
            byte value = data[index];
            builder.Append(Digits[value >> 4]);
            builder.Append(Digits[value & 15]);
            index = index + 1;
        }

        return builder.ToString();
    }

    static byte[]? ReadOpaqueBytes(
        DnsResourceRecord record,
        int recordTypeValue)
    {
        if (record.UseRawData)
        {
            return record.RawData;
        }

        if (record.Data != null && record.Data.Opaque != null)
        {
            return record.Data.Opaque.Bytes;
        }

        if (RecordDataModelCatalog.ReadRecordDataModel(recordTypeValue) ==
            RecordDataModel.Unassigned)
        {
            return record.RawData;
        }

        return null;
    }

    static byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        source.CopyTo(copy, 0);
        return copy;
    }

    static DnsTextResult FromTypedFailure(DnsRecordDataTextResult typed)
    {
        DnsTextResult result = new DnsTextResult();
        int index = 0;
        while (index < typed.Diagnostics.Count)
        {
            result.Diagnostics.Add(typed.Diagnostics[index]);
            index = index + 1;
        }

        return result;
    }
}
