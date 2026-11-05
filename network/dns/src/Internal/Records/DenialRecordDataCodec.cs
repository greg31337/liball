using System;
using System.Collections.Generic;

namespace dns;

internal static class DenialRecordDataCodec
{
    internal static DnsDecodeResult DecodeRecordData(
        int recordTypeValue,
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        if (recordTypeValue == 30)
        {
            return DecodeNxtData(message, offset, length, options);
        }

        if (recordTypeValue == 47)
        {
            return DecodeNsecData(message, offset, length);
        }

        if (recordTypeValue == 50)
        {
            return DecodeNsec3Data(message, offset, length);
        }

        if (recordTypeValue == 51)
        {
            return DecodeNsec3ParamData(message, offset, length);
        }

        if (recordTypeValue == 62)
        {
            return DecodeCsyncData(message, offset, length);
        }

        return SharedRecordDataCodec.DecodeFailure(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of this denial record codec family.",
            offset);
    }

    internal static WireWriteResult EncodeRecordData(
        WireWriter writer,
        CompressionTable table,
        int recordTypeValue,
        DnsRecordData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null)
        {
            return CompressionTable.FailureAt(
                DnsDiagnosticCode.ValueOutOfRange,
                "recordData",
                "Record data and encoding state are required.",
                0);
        }

        if (recordTypeValue == 30 || recordTypeValue == 47)
        {
            if (data.Nsec == null) return MissingSchema(writer, "nsec");
            if (recordTypeValue == 30)
            {
                return EncodeNxtData(writer, table, data.Nsec, options);
            }

            return EncodeNsecData(writer, table, data.Nsec, options);
        }

        if (recordTypeValue == 50)
        {
            if (data.Nsec3 == null) return MissingSchema(writer, "nsec3");
            return EncodeNsec3Data(writer, data.Nsec3);
        }

        if (recordTypeValue == 51)
        {
            if (data.Nsec3Param == null)
                return MissingSchema(writer, "nsec3Param");
            return EncodeNsec3ParamData(writer, data.Nsec3Param);
        }

        if (recordTypeValue == 62)
        {
            if (data.Csync == null) return MissingSchema(writer, "csync");
            return EncodeCsyncData(writer, data.Csync);
        }

        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            "recordType",
            "The record type is not part of this denial record codec family.",
            writer.WriteOffset());
    }

    internal static DnsDecodeResult DecodeNxtData(
        ReadOnlySpan<byte> message,
        int offset,
        int length,
        DnsDecodeOptions options)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (options == null)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.ValueOutOfRange,
                "options",
                "DNS decode options are required.",
                offset);
        }

        DnsDecodeResult name = CompressedDnsNameDecoder.DecodeName(
            message, offset, options);
        if (!name.Success || name.Name == null) return name;
        if (name.BytesConsumed > length)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.RecordDataLengthMismatch,
                "nxt.nextDomainName",
                "The NXT next name exceeds the record data.",
                offset);
        }

        int bitmapOffset = offset + name.BytesConsumed;
        int bitmapLength = length - name.BytesConsumed;
        if (bitmapLength < 4 || bitmapLength > 16)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidTypeBitmap,
                "nxt.typeBitmap",
                "An NXT bitmap must contain four to sixteen octets.",
                bitmapOffset);
        }

        if ((message[bitmapOffset] & 128) != 0 ||
            message[bitmapOffset + bitmapLength - 1] == 0 ||
            (message[bitmapOffset + 3] & 2) == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidTypeBitmap,
                "nxt.typeBitmap",
                "The NXT bitmap has an extension bit, trailing zero, or missing NXT bit.",
                bitmapOffset);
        }

        DnsNsecData nsec = new DnsNsecData();
        nsec.NextDomainName = name.Name;
        int byteIndex = 0;
        while (byteIndex < bitmapLength)
        {
            int octet = message[bitmapOffset + byteIndex];
            int bitIndex = 0;
            while (bitIndex < 8)
            {
                int mask = 128 >> bitIndex;
                if ((octet & mask) != 0)
                {
                    nsec.RecordTypeValues.Add((byteIndex * 8) + bitIndex);
                }

                bitIndex = bitIndex + 1;
            }

            byteIndex = byteIndex + 1;
        }

        DnsRecordData data = new DnsRecordData();
        data.Nsec = nsec;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeNxtData(
        WireWriter writer,
        CompressionTable table,
        DnsNsecData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.RecordTypeValues == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "nxt", "Complete NXT data and encoding state are required.");
        }

        int maximumType = -1;
        bool hasNxt = false;
        int validationIndex = 0;
        while (validationIndex < data.RecordTypeValues.Count)
        {
            int recordType = data.RecordTypeValues[validationIndex];
            if (recordType < 1 || recordType > 127)
            {
                return writer.FailAt(
                    DnsDiagnosticCode.InvalidTypeBitmap,
                    "nxt.typeBitmap",
                    "An NXT bitmap type must be between one and 127.",
                    writer.WriteOffset());
            }

            if (recordType == 30) hasNxt = true;
            if (recordType > maximumType) maximumType = recordType;
            validationIndex = validationIndex + 1;
        }

        if (!hasNxt)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidTypeBitmap,
                "nxt.typeBitmap",
                "An NXT bitmap must include the NXT record type.",
                writer.WriteOffset());
        }

        byte[] bitmap = new byte[(maximumType / 8) + 1];
        int index = 0;
        while (index < data.RecordTypeValues.Count)
        {
            int recordType = data.RecordTypeValues[index];
            int bitmapIndex = recordType / 8;
            int mask = 128 >> (recordType % 8);
            bitmap[bitmapIndex] = (byte)(bitmap[bitmapIndex] | mask);
            index = index + 1;
        }

        int start = writer.WriteOffset();
        WireWriteResult name = table.WriteName(
            writer, data.NextDomainName, options, true);
        if (!name.Success) return name;
        WireWriteResult bitmapResult = writer.WriteBytes(bitmap);
        if (!bitmapResult.Success) return bitmapResult;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeNsecData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        DnsDecodeResult name = SharedRecordDataCodec.DecodeUncompressedNamePrefix(
            message, offset, length, "nsec.nextDomainName");
        if (!name.Success || name.Name == null) return name;
        int bitmapOffset = offset + name.BytesConsumed;
        int bitmapLength = length - name.BytesConsumed;
        if (bitmapLength == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidTypeBitmap,
                "nsec.typeBitmap",
                "An NSEC record requires at least one type bitmap window.",
                bitmapOffset);
        }

        WireReader reader = new WireReader(
            message.ToArray(), bitmapOffset, bitmapLength);
        WireReadResult bitmap = reader.ReadTypeBitmap(bitmapLength);
        if (!bitmap.Success) return SharedRecordDataCodec.FromReadFailure(bitmap);
        DnsNsecData nsec = new DnsNsecData();
        nsec.NextDomainName = name.Name;
        nsec.RecordTypeValues = bitmap.IntegerValues;
        DnsRecordData data = new DnsRecordData();
        data.Nsec = nsec;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeNsecData(
        WireWriter writer,
        CompressionTable table,
        DnsNsecData data,
        DnsEncodeOptions options)
    {
        if (writer == null || table == null || data == null || options == null ||
            data.RecordTypeValues == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "nsec", "Complete NSEC data and encoding state are required.");
        }

        if (data.RecordTypeValues.Count == 0)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidTypeBitmap,
                "nsec.typeBitmap",
                "An NSEC record requires at least one record type.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult name = table.WriteName(
            writer, data.NextDomainName, options, false);
        if (!name.Success) return name;
        WireWriteResult bitmap = writer.WriteTypeBitmap(data.RecordTypeValues);
        if (!bitmap.Success) return bitmap;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeNsec3Data(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 7)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "nsec3",
                "NSEC3 data is truncated.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult algorithm = reader.ReadUnsigned8();
        WireReadResult flags = reader.ReadUnsigned8();
        WireReadResult iterations = reader.ReadUnsigned16();
        WireReadResult saltLength = reader.ReadUnsigned8();
        if (flags.UnsignedValue > 1 ||
            saltLength.UnsignedValue + 1 > (ulong)reader.RemainingLength())
        {
            return SharedRecordDataCodec.DecodeFailure(
                flags.UnsignedValue > 1 ? DnsDiagnosticCode.InvalidRecordData :
                    DnsDiagnosticCode.UnexpectedEnd,
                "nsec3",
                "NSEC3 flags are reserved or its salt is truncated.",
                reader.ReadOffset());
        }

        WireReadResult salt = reader.ReadBytes((int)saltLength.UnsignedValue);
        WireReadResult hashLength = reader.ReadUnsigned8();
        if (hashLength.UnsignedValue == 0 ||
            hashLength.UnsignedValue > (ulong)reader.RemainingLength())
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "nsec3.nextHashedOwnerName",
                "The NSEC3 next hashed owner name is empty or truncated.",
                reader.ReadOffset());
        }

        WireReadResult nextHash = reader.ReadBytes((int)hashLength.UnsignedValue);
        if (!HashLengthIsValid(
            (int)algorithm.UnsignedValue, nextHash.Bytes.Length))
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidLength,
                "nsec3.nextHashedOwnerName",
                "The next hashed owner name length does not match its algorithm.",
                nextHash.StartOffset);
        }

        WireReadResult bitmap = reader.ReadTypeBitmap(reader.RemainingLength());
        if (!bitmap.Success) return SharedRecordDataCodec.FromReadFailure(bitmap);
        DnsNsec3Data nsec3 = new DnsNsec3Data();
        nsec3.HashAlgorithm = (DnsNsec3HashAlgorithm)algorithm.UnsignedValue;
        nsec3.HashAlgorithmValue = (int)algorithm.UnsignedValue;
        nsec3.Flags = (int)flags.UnsignedValue;
        nsec3.Iterations = (int)iterations.UnsignedValue;
        nsec3.Salt = salt.Bytes;
        nsec3.NextHashedOwnerName = nextHash.Bytes;
        nsec3.RecordTypeValues = bitmap.IntegerValues;
        DnsRecordData data = new DnsRecordData();
        data.Nsec3 = nsec3;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeNsec3Data(
        WireWriter writer,
        DnsNsec3Data data)
    {
        if (writer == null || data == null || data.Salt == null ||
            data.NextHashedOwnerName == null || data.RecordTypeValues == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "nsec3", "Complete NSEC3 data is required.");
        }

        int algorithm = DnsHeaderCodec.ResolveCode(
            data.HashAlgorithmValue, (int)data.HashAlgorithm);
        if (!Unsigned8IsValid(algorithm) || data.Flags < 0 || data.Flags > 1 ||
            !Unsigned16IsValid(data.Iterations) || data.Salt.Length > 255 ||
            data.NextHashedOwnerName.Length < 1 ||
            data.NextHashedOwnerName.Length > 255 ||
            !HashLengthIsValid(algorithm, data.NextHashedOwnerName.Length))
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "nsec3",
                "An NSEC3 field is out of range or has the wrong length.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult algorithmResult = writer.WriteUnsigned8((ulong)algorithm);
        if (!algorithmResult.Success) return algorithmResult;
        WireWriteResult flags = writer.WriteUnsigned8((ulong)data.Flags);
        if (!flags.Success) return flags;
        WireWriteResult iterations = writer.WriteUnsigned16((ulong)data.Iterations);
        if (!iterations.Success) return iterations;
        WireWriteResult saltLength = writer.WriteUnsigned8((ulong)data.Salt.Length);
        if (!saltLength.Success) return saltLength;
        WireWriteResult salt = writer.WriteBytes(data.Salt);
        if (!salt.Success) return salt;
        WireWriteResult hashLength = writer.WriteUnsigned8(
            (ulong)data.NextHashedOwnerName.Length);
        if (!hashLength.Success) return hashLength;
        WireWriteResult nextHash = writer.WriteBytes(data.NextHashedOwnerName);
        if (!nextHash.Success) return nextHash;
        WireWriteResult bitmap = writer.WriteTypeBitmap(data.RecordTypeValues);
        if (!bitmap.Success) return bitmap;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeNsec3ParamData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 5)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "nsec3Param",
                "NSEC3PARAM data is truncated.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult algorithm = reader.ReadUnsigned8();
        WireReadResult flags = reader.ReadUnsigned8();
        WireReadResult iterations = reader.ReadUnsigned16();
        WireReadResult saltLength = reader.ReadUnsigned8();
        if (flags.UnsignedValue != 0 ||
            saltLength.UnsignedValue != (ulong)reader.RemainingLength())
        {
            return SharedRecordDataCodec.DecodeFailure(
                flags.UnsignedValue != 0 ? DnsDiagnosticCode.InvalidRecordData :
                    DnsDiagnosticCode.RecordDataLengthMismatch,
                "nsec3Param",
                "NSEC3PARAM flags are reserved or its salt length is inconsistent.",
                reader.ReadOffset());
        }

        WireReadResult salt = reader.ReadBytes((int)saltLength.UnsignedValue);
        DnsNsec3ParamData parameter = new DnsNsec3ParamData();
        parameter.HashAlgorithm = (DnsNsec3HashAlgorithm)algorithm.UnsignedValue;
        parameter.HashAlgorithmValue = (int)algorithm.UnsignedValue;
        parameter.Flags = (int)flags.UnsignedValue;
        parameter.Iterations = (int)iterations.UnsignedValue;
        parameter.Salt = salt.Bytes;
        DnsRecordData data = new DnsRecordData();
        data.Nsec3Param = parameter;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeNsec3ParamData(
        WireWriter writer,
        DnsNsec3ParamData data)
    {
        if (writer == null || data == null || data.Salt == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "nsec3Param", "Complete NSEC3PARAM data is required.");
        }

        int algorithm = DnsHeaderCodec.ResolveCode(
            data.HashAlgorithmValue, (int)data.HashAlgorithm);
        if (!Unsigned8IsValid(algorithm) || data.Flags != 0 ||
            !Unsigned16IsValid(data.Iterations) || data.Salt.Length > 255)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                "nsec3Param",
                "An NSEC3PARAM field is out of range or reserved.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult algorithmResult = writer.WriteUnsigned8((ulong)algorithm);
        if (!algorithmResult.Success) return algorithmResult;
        WireWriteResult flags = writer.WriteUnsigned8((ulong)data.Flags);
        if (!flags.Success) return flags;
        WireWriteResult iterations = writer.WriteUnsigned16((ulong)data.Iterations);
        if (!iterations.Success) return iterations;
        WireWriteResult saltLength = writer.WriteUnsigned8((ulong)data.Salt.Length);
        if (!saltLength.Success) return saltLength;
        WireWriteResult salt = writer.WriteBytes(data.Salt);
        if (!salt.Success) return salt;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static DnsDecodeResult DecodeCsyncData(
        ReadOnlySpan<byte> message,
        int offset,
        int length)
    {
        DnsDecodeResult validation = SharedRecordDataCodec.ValidateRegion(
            message.Length, offset, length);
        if (!validation.Success) return validation;
        if (length < 6)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.UnexpectedEnd,
                "csync",
                "CSYNC data requires serial and flags fields.",
                offset + length);
        }

        WireReader reader = new WireReader(message.ToArray(), offset, length);
        WireReadResult serial = reader.ReadUnsigned32();
        WireReadResult flags = reader.ReadUnsigned16();
        if (flags.UnsignedValue > 3)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidRecordData,
                "csync.flags",
                "CSYNC reserved flags must be zero.",
                offset + 4);
        }

        if (reader.RemainingLength() == 0)
        {
            return SharedRecordDataCodec.DecodeFailure(
                DnsDiagnosticCode.InvalidTypeBitmap,
                "csync.typeBitmap",
                "A CSYNC record requires at least one type bitmap window.",
                reader.ReadOffset());
        }

        WireReadResult bitmap = reader.ReadTypeBitmap(reader.RemainingLength());
        if (!bitmap.Success) return SharedRecordDataCodec.FromReadFailure(bitmap);
        DnsCsyncData csync = new DnsCsyncData();
        csync.Serial = (uint)serial.UnsignedValue;
        csync.Flags = (int)flags.UnsignedValue;
        csync.RecordTypeValues = bitmap.IntegerValues;
        DnsRecordData data = new DnsRecordData();
        data.Csync = csync;
        return SharedRecordDataCodec.DecodeSuccess(data, length);
    }

    internal static WireWriteResult EncodeCsyncData(
        WireWriter writer,
        DnsCsyncData data)
    {
        if (writer == null || data == null || data.RecordTypeValues == null)
        {
            return SharedRecordDataCodec.WriteFailure(
                writer, "csync", "Complete CSYNC data is required.");
        }

        if (data.Flags < 0 || data.Flags > 3 ||
            data.RecordTypeValues.Count == 0)
        {
            return writer.FailAt(
                DnsDiagnosticCode.InvalidRecordData,
                data.RecordTypeValues.Count == 0 ? "csync.typeBitmap" :
                    "csync.flags",
                "CSYNC flags or record types are invalid.",
                writer.WriteOffset());
        }

        int start = writer.WriteOffset();
        WireWriteResult serial = writer.WriteUnsigned32(data.Serial);
        if (!serial.Success) return serial;
        WireWriteResult flags = writer.WriteUnsigned16((ulong)data.Flags);
        if (!flags.Success) return flags;
        WireWriteResult bitmap = writer.WriteTypeBitmap(data.RecordTypeValues);
        if (!bitmap.Success) return bitmap;
        return writer.Succeed(start, writer.WriteOffset() - start, -1);
    }

    internal static bool HashLengthIsValid(int algorithm, int length)
    {
        if (length < 1 || length > 255) return false;
        if (algorithm == 1) return length == 20;
        return true;
    }

    internal static bool Unsigned8IsValid(int value)
    {
        return value >= 0 && value <= 255;
    }

    internal static bool Unsigned16IsValid(int value)
    {
        return value >= 0 && value <= 65535;
    }

    internal static WireWriteResult MissingSchema(WireWriter writer, string field)
    {
        return writer.FailAt(
            DnsDiagnosticCode.InvalidRecordData,
            field,
            "The record data does not contain its required typed schema.",
            writer.WriteOffset());
    }
}
