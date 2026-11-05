using System;

namespace raw;

internal static class IptcCodec
{
    internal const byte Marker = 28;
    internal const ushort RichTiffIptcTag = 33723;
    internal const ushort PhotoshopResourcesTag = 34377;
    internal const ushort PhotoshopIptcResource = 1028;

    internal static RawIptcRecord Parse(byte[] rawBytes, RawLimits? limits, long offset, string ifdPath)
    {
        ValidateInput(rawBytes, limits, offset);
        System.Collections.Generic.List<RawIptcDataset> datasets = new System.Collections.Generic.List<RawIptcDataset>();
        bool utf8 = false;
        int position = 0;
        int paddingStart = rawBytes.Length;
        while (position < rawBytes.Length)
        {
            if (rawBytes[position] != Marker)
            {
                if (AllZero(rawBytes, position))
                {
                    paddingStart = position;
                    break;
                }
                throw Invalid("An IPTC IIM DataSet does not begin with the 0x1C marker.", At(offset, position));
            }
            if (rawBytes.Length - position < 5)
            {
                throw Truncated("An IPTC IIM DataSet tag is truncated.", At(offset, position));
            }
            if (datasets.Count >= (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxTagCount)
            {
                throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The IPTC IIM DataSet count exceeds the configured tag-count limit.", At(offset, position));
            }
            byte recordNumber = rawBytes[position + 1];
            byte datasetNumber = rawBytes[position + 2];
            RequireRecord(recordNumber, At(offset, position + 1));
            int lengthWord = rawBytes[position + 3] * 256 + rawBytes[position + 4];
            position += 5;
            int dataLength;
            if ((lengthWord & 32768) == 0)
            {
                dataLength = lengthWord;
            }
            else
            {
                int descriptorLength = lengthWord & 32767;
                if (descriptorLength <= 0 || descriptorLength > 4)
                {
                    throw Invalid("An extended IPTC IIM length descriptor must contain one through four octets.", At(offset, position - 2));
                }
                if (rawBytes.Length - position < descriptorLength)
                {
                    throw Truncated("An extended IPTC IIM length descriptor is truncated.", At(offset, position));
                }
                uint extendedLength = 0;
                int lengthIndex = 0;
                while (lengthIndex < descriptorLength)
                {
                    extendedLength = extendedLength * 256 + rawBytes[position + lengthIndex];
                    lengthIndex++;
                }
                if (extendedLength <= 32767 || extendedLength > int.MaxValue)
                {
                    throw Invalid("An extended IPTC IIM length must be greater than 32767 and fit in a managed array.", At(offset, position));
                }
                dataLength = (int)extendedLength;
                position += descriptorLength;
            }
            if (dataLength > limits.MaxTagValueBytes)
            {
                throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "An IPTC IIM DataSet exceeds the configured tag-value limit.", At(offset, position));
            }
            if (dataLength > rawBytes.Length - position)
            {
                throw Truncated("An IPTC IIM DataSet value is truncated.", At(offset, position));
            }
            byte[] valueBytes = rawBytes.AsSpan(position, dataLength).ToArray();
            if (recordNumber == 1 && datasetNumber == 90)
            {
                utf8 = IsUtf8Declaration(valueBytes);
            }
            datasets.Add(DecodeDataset(recordNumber, datasetNumber, valueBytes, utf8));
            position += dataLength;
        }
        byte[] padding = rawBytes.AsSpan(paddingStart).ToArray();
        return new RawIptcRecord(RawTextValue.Copy(rawBytes), padding, datasets.ToArray(), utf8, offset, ifdPath == null ? string.Empty : ifdPath);
    }

    internal static byte[] Encode(RawIptcDataset[]? datasets, RawLimits limits)
    {
        if (datasets == null)
        {
            throw Invalid("IPTC IIM DataSets are required.", -1);
        }
        ValidateLimits(limits);
        if (datasets.Length > limits.MaxTagCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The IPTC IIM DataSet count exceeds the configured tag-count limit.", -1);
        }
        long total = 0;
        bool utf8 = false;
        int index = 0;
        while (index < datasets.Length)
        {
            RawIptcDataset dataset = datasets[index];
            if (dataset == null || dataset.RawBytes == null)
            {
                throw Invalid("Every IPTC IIM DataSet and its bytes are required.", -1);
            }
            RequireRecord(dataset.RecordNumber, -1);
            int length = dataset.RawBytes.Length;
            if (length > limits.MaxTagValueBytes)
            {
                throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "An IPTC IIM DataSet exceeds the configured tag-value limit.", -1);
            }
            int headerLength = 5;
            if (length > 32767)
            {
                headerLength += LengthOctets(length);
            }
            total = CheckedMath.Add(total, headerLength);
            total = CheckedMath.Add(total, length);
            if (dataset.RecordNumber == 1 && dataset.DatasetNumber == 90 && IsUtf8Declaration(dataset.RawBytes))
            {
                utf8 = true;
            }
            index++;
        }
        if (total > limits.MaxMetadataBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The encoded IPTC IIM payload exceeds the configured metadata limit.", -1);
        }
        CheckedMath.AllocationSize(total, 1, limits);
        index = 0;
        while (index < datasets.Length)
        {
            RawIptcDataset dataset = datasets[index];
            if (dataset.ValueKind == RawIptcValueKind.Text && dataset.Encoding == RawTextEncoding.Utf8 && !utf8)
            {
                throw Invalid("UTF-8 IPTC IIM text requires a 1:90 UTF-8 coded-character-set declaration.", -1);
            }
            index++;
        }
        byte[] encoded = new byte[CheckedMath.ToInt32(total)];
        int position = 0;
        index = 0;
        while (index < datasets.Length)
        {
            RawIptcDataset dataset = datasets[index];
            int length = dataset.RawBytes.Length;
            encoded[position] = Marker;
            encoded[position + 1] = dataset.RecordNumber;
            encoded[position + 2] = dataset.DatasetNumber;
            if (length <= 32767)
            {
                encoded[position + 3] = (byte)(length >> 8);
                encoded[position + 4] = BitUtility.WrapUInt8(length);
                position += 5;
            }
            else
            {
                int octets = LengthOctets(length);
                encoded[position + 3] = (byte)(128 | octets >> 8);
                encoded[position + 4] = (byte)octets;
                position += 5;
                int shiftIndex = octets - 1;
                while (shiftIndex >= 0)
                {
                    encoded[position] = BitUtility.WrapUInt8(length >> shiftIndex * 8);
                    position++;
                    shiftIndex--;
                }
            }
            dataset.RawBytes.AsSpan().CopyTo(encoded.AsSpan(position));
            position += length;
            index++;
        }
        return encoded;
    }

    internal static RawIptcDataset CreateBinary(byte recordNumber, byte datasetNumber, byte[] bytes)
    {
        RequireRecord(recordNumber, -1);
        if (bytes == null)
        {
            throw Invalid("IPTC IIM binary value bytes are required.", -1);
        }
        RawIptcValueKind expected = GetKind(recordNumber, datasetNumber);
        if (expected != RawIptcValueKind.Binary)
        {
            throw Invalid("The known IPTC IIM DataSet does not have a binary value form.", -1);
        }
        return new RawIptcDataset(recordNumber, datasetNumber, GetName(recordNumber, datasetNumber), RawIptcValueKind.Binary, bytes, string.Empty, RawTextEncoding.Unknown, 0, 0, 0, 0, 0, 0, 0, false, 0, true);
    }

    internal static RawIptcDataset CreateText(byte recordNumber, byte datasetNumber, string text, bool utf8)
    {
        RequireRecord(recordNumber, -1);
        if (text == null)
        {
            throw Invalid("IPTC IIM text is required.", -1);
        }
        RawIptcValueKind kind = GetKind(recordNumber, datasetNumber);
        if (kind != RawIptcValueKind.Text)
        {
            throw Invalid("The known IPTC IIM DataSet does not have a text value form.", -1);
        }
        if (recordNumber == 1 && utf8)
        {
            throw Invalid("IIM record 1 character DataSets must use the default character set.", -1);
        }
        if (!utf8 && !IsAscii(text))
        {
            throw Invalid("Seven-bit IPTC IIM text contains a non-ASCII character.", -1);
        }
        System.Text.Encoding encoding = utf8 ? new System.Text.UTF8Encoding(false, true) : System.Text.Encoding.ASCII;
        byte[] bytes = encoding.GetBytes(text);
        return new RawIptcDataset(recordNumber, datasetNumber, GetName(recordNumber, datasetNumber), kind, bytes, text, utf8 ? RawTextEncoding.Utf8 : RawTextEncoding.Ascii, 0, 0, 0, 0, 0, 0, 0, false, 0, true);
    }

    internal static RawIptcDataset CreateUnsignedInteger(byte recordNumber, byte datasetNumber, ushort value)
    {
        RequireKind(recordNumber, datasetNumber, RawIptcValueKind.UnsignedInteger);
        byte[] bytes = new byte[2];
        bytes[0] = (byte)(value >> 8);
        bytes[1] = BitUtility.WrapUInt8(value);
        return DecodeDataset(recordNumber, datasetNumber, bytes, false);
    }

    internal static RawIptcDataset CreateDate(byte recordNumber, byte datasetNumber, System.DateTime value)
    {
        RequireKind(recordNumber, datasetNumber, RawIptcValueKind.Date);
        string text = value.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        return DecodeDataset(recordNumber, datasetNumber, System.Text.Encoding.ASCII.GetBytes(text), false);
    }

    internal static RawIptcDataset CreateTime(byte recordNumber, byte datasetNumber, int hour, int minute, int second, bool hasUtcOffset, int utcOffsetMinutes)
    {
        RequireKind(recordNumber, datasetNumber, RawIptcValueKind.Time);
        if (hour < 0 || hour > 24 || minute < 0 || minute > 59 || second < 0 || second > 59 || hour == 24 && (minute != 0 || second != 0))
        {
            throw Invalid("An IPTC IIM time is outside its valid range.", -1);
        }
        if (hasUtcOffset && (utcOffsetMinutes < -1439 || utcOffsetMinutes > 1439))
        {
            throw Invalid("An IPTC IIM UTC offset is outside its four-digit representation.", -1);
        }
        string text = hour.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + minute.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + second.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        if (hasUtcOffset)
        {
            string sign = utcOffsetMinutes < 0 ? "-" : "+";
            int absolute = utcOffsetMinutes < 0 ? -utcOffsetMinutes : utcOffsetMinutes;
            text += sign + (absolute / 60).ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + (absolute % 60).ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        }
        return DecodeDataset(recordNumber, datasetNumber, System.Text.Encoding.ASCII.GetBytes(text), false);
    }

    internal static RawIptcRecord[] Discover(TiffIfdGraph graph, RawLimits? limits)
    {
        if (graph == null)
        {
            return System.Array.Empty<RawIptcRecord>();
        }
        ValidateLimits(limits);
        System.Collections.Generic.List<RawIptcRecord> records = new System.Collections.Generic.List<RawIptcRecord>();
        long total = 0;
        int ifdIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                if (entry.Tag == RichTiffIptcTag && IsIptcFieldType(entry.FieldType) && entry.RawBytes.Length > 0)
                {
                    total = CheckedMath.Add(total, entry.RawBytes.Length);
                    RequireCombinedLimit(total, limits, entry.EntryOffset);
                    records.Add(Parse(entry.RawBytes, limits, XmpCodec.GetValueOffset(graph, entry), ifd.Path));
                }
                if (entry.Tag == PhotoshopResourcesTag && IsByteFieldType(entry.FieldType) && entry.RawBytes.Length > 0)
                {
                    total = DiscoverPhotoshop(records, entry.RawBytes, XmpCodec.GetValueOffset(graph, entry), ifd.Path, limits, total);
                }
                entryIndex++;
            }
            ifdIndex++;
        }
        return records.ToArray();
    }

    internal static long DiscoverPhotoshop(System.Collections.Generic.List<RawIptcRecord> records, byte[] bytes, long baseOffset, string ifdPath, RawLimits? limits, long total)
    {
        int position = 0;
        while (position < bytes.Length)
        {
            if (bytes.Length - position < 7)
            {
                throw Truncated("A Photoshop image resource header is truncated.", At(baseOffset, position));
            }
            if (bytes[position] != 56 || bytes[position + 1] != 66 || bytes[position + 2] != 73 || bytes[position + 3] != 77)
            {
                throw Invalid("A Photoshop image resource does not begin with the 8BIM signature.", At(baseOffset, position));
            }
            ushort resource = (ushort)(bytes[position + 4] * 256 + bytes[position + 5]);
            int nameLength = bytes[position + 6];
            int nameFieldLength = 1 + nameLength;
            if ((nameFieldLength & 1) != 0)
            {
                nameFieldLength++;
            }
            int sizeOffset = position + 6 + nameFieldLength;
            if (sizeOffset < position || sizeOffset > bytes.Length || bytes.Length - sizeOffset < 4)
            {
                throw Truncated("A Photoshop image resource name or size is truncated.", At(baseOffset, position));
            }
            uint unsignedSize = EndianReader.ReadUInt32(bytes.AsSpan(sizeOffset, 4), RawByteOrder.BigEndian);
            if (unsignedSize > int.MaxValue)
            {
                throw Invalid("A Photoshop image resource is too large for a managed array.", At(baseOffset, sizeOffset));
            }
            int size = (int)unsignedSize;
            int dataOffset = sizeOffset + 4;
            if (dataOffset > bytes.Length || size > bytes.Length - dataOffset)
            {
                throw Truncated("A Photoshop image resource value is truncated.", At(baseOffset, dataOffset));
            }
            if (resource == PhotoshopIptcResource && size > 0)
            {
                total = CheckedMath.Add(total, size);
                RequireCombinedLimit(total, limits, At(baseOffset, dataOffset));
                byte[] payload = bytes.AsSpan(dataOffset, size).ToArray();
                records.Add(Parse(payload, limits, At(baseOffset, dataOffset), ifdPath));
            }
            int paddedSize = size;
            if ((paddedSize & 1) != 0)
            {
                paddedSize++;
            }
            if (paddedSize > bytes.Length - dataOffset)
            {
                throw Truncated("Photoshop image resource padding is truncated.", At(baseOffset, dataOffset + size));
            }
            position = dataOffset + paddedSize;
        }
        return total;
    }

    internal static RawIptcDataset DecodeDataset(byte recordNumber, byte datasetNumber, byte[] bytes, bool utf8)
    {
        RawIptcValueKind kind = GetKind(recordNumber, datasetNumber);
        string name = GetName(recordNumber, datasetNumber);
        string text = string.Empty;
        RawTextEncoding encoding = RawTextEncoding.Unknown;
        ulong unsignedValue = 0;
        int year = 0;
        int month = 0;
        int day = 0;
        int hour = 0;
        int minute = 0;
        int second = 0;
        bool hasOffset = false;
        int offsetMinutes = 0;
        bool valid = true;
        if (kind == RawIptcValueKind.Text)
        {
            bool useUtf8 = recordNumber != 1 && utf8;
            encoding = useUtf8 ? RawTextEncoding.Utf8 : RawTextEncoding.Ascii;
            if (useUtf8)
            {
                text = new System.Text.UTF8Encoding(false, false).GetString(bytes);
                valid = IsValidUtf8(bytes);
            }
            else
            {
                text = System.Text.Encoding.ASCII.GetString(bytes);
                valid = IsAscii(bytes);
            }
        }
        if (kind == RawIptcValueKind.UnsignedInteger)
        {
            valid = bytes.Length == 2;
            if (valid)
            {
                unsignedValue = (ulong)(bytes[0] * 256 + bytes[1]);
            }
        }
        if (kind == RawIptcValueKind.Date)
        {
            encoding = RawTextEncoding.Ascii;
            text = System.Text.Encoding.ASCII.GetString(bytes);
            valid = ParseDate(text);
            if (valid)
            {
                year = ParseDigits(text, 0, 4);
                month = ParseDigits(text, 4, 2);
                day = ParseDigits(text, 6, 2);
            }
        }
        if (kind == RawIptcValueKind.Time)
        {
            encoding = RawTextEncoding.Ascii;
            text = System.Text.Encoding.ASCII.GetString(bytes);
            valid = ParseTime(text);
            if (valid)
            {
                hour = ParseDigits(text, 0, 2);
                minute = ParseDigits(text, 2, 2);
                second = ParseDigits(text, 4, 2);
                hasOffset = text.Length == 11;
                if (hasOffset)
                {
                    offsetMinutes = ParseDigits(text, 7, 2) * 60 + ParseDigits(text, 9, 2);
                    if (text[6] == '-')
                    {
                        offsetMinutes = -offsetMinutes;
                    }
                }
            }
        }
        return new RawIptcDataset(recordNumber, datasetNumber, name, kind, bytes, text, encoding, unsignedValue, year, month, day, hour, minute, second, hasOffset, offsetMinutes, valid);
    }

    internal static RawIptcValueKind GetKind(byte recordNumber, byte datasetNumber)
    {
        if (recordNumber == 1 && (datasetNumber == 0 || datasetNumber == 20 || datasetNumber == 22 || datasetNumber == 120 || datasetNumber == 122) || recordNumber == 2 && (datasetNumber == 0 || datasetNumber == 200 || datasetNumber == 201))
        {
            return RawIptcValueKind.UnsignedInteger;
        }
        if (recordNumber == 1 && datasetNumber == 70 || recordNumber == 2 && (datasetNumber == 30 || datasetNumber == 37 || datasetNumber == 47 || datasetNumber == 55 || datasetNumber == 62))
        {
            return RawIptcValueKind.Date;
        }
        if (recordNumber == 1 && datasetNumber == 80 || recordNumber == 2 && (datasetNumber == 35 || datasetNumber == 38 || datasetNumber == 60 || datasetNumber == 63))
        {
            return RawIptcValueKind.Time;
        }
        if (recordNumber == 1 && datasetNumber == 90 || recordNumber == 2 && (datasetNumber == 125 || datasetNumber == 202))
        {
            return RawIptcValueKind.Binary;
        }
        if (recordNumber == 1 && IsKnownRecordOne(datasetNumber) || recordNumber == 2 && IsKnownRecordTwo(datasetNumber))
        {
            return RawIptcValueKind.Text;
        }
        return RawIptcValueKind.Binary;
    }

    internal static string GetName(byte recordNumber, byte datasetNumber)
    {
        if (recordNumber == 1)
        {
            switch (datasetNumber)
            {
                case 0: return "ModelVersion";
                case 5: return "Destination";
                case 20: return "FileFormat";
                case 22: return "FileFormatVersion";
                case 30: return "ServiceIdentifier";
                case 40: return "EnvelopeNumber";
                case 50: return "ProductID";
                case 60: return "EnvelopePriority";
                case 70: return "DateSent";
                case 80: return "TimeSent";
                case 90: return "CodedCharacterSet";
                case 100: return "UNO";
                case 120: return "ARMIdentifier";
                case 122: return "ARMVersion";
                default: return NumericName(recordNumber, datasetNumber);
            }
        }
        if (recordNumber == 2)
        {
            switch (datasetNumber)
            {
                case 0: return "RecordVersion";
                case 3: return "ObjectTypeReference";
                case 4: return "ObjectAttributeReference";
                case 5: return "ObjectName";
                case 7: return "EditStatus";
                case 8: return "EditorialUpdate";
                case 10: return "Urgency";
                case 12: return "SubjectReference";
                case 15: return "Category";
                case 20: return "SupplementalCategory";
                case 22: return "FixtureIdentifier";
                case 25: return "Keywords";
                case 26: return "ContentLocationCode";
                case 27: return "ContentLocationName";
                case 30: return "ReleaseDate";
                case 35: return "ReleaseTime";
                case 37: return "ExpirationDate";
                case 38: return "ExpirationTime";
                case 40: return "SpecialInstructions";
                case 42: return "ActionAdvised";
                case 45: return "ReferenceService";
                case 47: return "ReferenceDate";
                case 50: return "ReferenceNumber";
                case 55: return "DateCreated";
                case 60: return "TimeCreated";
                case 62: return "DigitalCreationDate";
                case 63: return "DigitalCreationTime";
                case 65: return "OriginatingProgram";
                case 70: return "ProgramVersion";
                case 75: return "ObjectCycle";
                case 80: return "Byline";
                case 85: return "BylineTitle";
                case 90: return "City";
                case 92: return "SubLocation";
                case 95: return "ProvinceState";
                case 100: return "CountryCode";
                case 101: return "CountryName";
                case 103: return "OriginalTransmissionReference";
                case 105: return "Headline";
                case 110: return "Credit";
                case 115: return "Source";
                case 116: return "CopyrightNotice";
                case 118: return "Contact";
                case 120: return "CaptionAbstract";
                case 122: return "WriterEditor";
                case 125: return "RasterizedCaption";
                case 130: return "ImageType";
                case 131: return "ImageOrientation";
                case 135: return "LanguageIdentifier";
                case 150: return "AudioType";
                case 151: return "AudioSamplingRate";
                case 152: return "AudioSamplingResolution";
                case 153: return "AudioDuration";
                case 154: return "AudioOutcue";
                case 184: return "JobID";
                case 185: return "MasterDocumentID";
                case 186: return "ShortDocumentID";
                case 187: return "UniqueDocumentID";
                case 188: return "OwnerID";
                case 200: return "ObjectPreviewFileFormat";
                case 201: return "ObjectPreviewFileFormatVersion";
                case 202: return "ObjectPreviewData";
                default: return NumericName(recordNumber, datasetNumber);
            }
        }
        return NumericName(recordNumber, datasetNumber);
    }

    internal static bool IsKnownRecordOne(byte datasetNumber)
    {
        return datasetNumber == 5 || datasetNumber == 30 || datasetNumber == 40 || datasetNumber == 50 || datasetNumber == 60 || datasetNumber == 100;
    }

    internal static bool IsKnownRecordTwo(byte datasetNumber)
    {
        return datasetNumber == 3 || datasetNumber == 4 || datasetNumber == 5 || datasetNumber == 7 || datasetNumber == 8 || datasetNumber == 10 || datasetNumber == 12 || datasetNumber == 15 || datasetNumber == 20 || datasetNumber == 22 || datasetNumber == 25 || datasetNumber == 26 || datasetNumber == 27 || datasetNumber == 40 || datasetNumber == 42 || datasetNumber == 45 || datasetNumber == 47 || datasetNumber == 50 || datasetNumber == 65 || datasetNumber == 70 || datasetNumber == 75 || datasetNumber == 80 || datasetNumber == 85 || datasetNumber == 90 || datasetNumber == 92 || datasetNumber == 95 || datasetNumber == 100 || datasetNumber == 101 || datasetNumber == 103 || datasetNumber == 105 || datasetNumber == 110 || datasetNumber == 115 || datasetNumber == 116 || datasetNumber == 118 || datasetNumber == 120 || datasetNumber == 122 || datasetNumber == 130 || datasetNumber == 131 || datasetNumber == 135 || datasetNumber == 150 || datasetNumber == 151 || datasetNumber == 152 || datasetNumber == 153 || datasetNumber == 154 || datasetNumber == 184 || datasetNumber == 185 || datasetNumber == 186 || datasetNumber == 187 || datasetNumber == 188 || datasetNumber == 200 || datasetNumber == 201;
    }

    internal static bool IsUtf8Declaration(byte[] bytes)
    {
        return bytes.Length == 3 && bytes[0] == 27 && bytes[1] == 37 && bytes[2] == 71;
    }

    internal static bool IsValidUtf8(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            byte first = bytes[index];
            if (first <= 127)
            {
                index++;
                continue;
            }
            int continuationCount;
            uint value;
            uint minimum;
            if (first >= 194 && first <= 223)
            {
                continuationCount = 1;
                value = (uint)(first & 31);
                minimum = 128;
            }
            else if (first >= 224 && first <= 239)
            {
                continuationCount = 2;
                value = (uint)(first & 15);
                minimum = 2048;
            }
            else if (first >= 240 && first <= 244)
            {
                continuationCount = 3;
                value = (uint)(first & 7);
                minimum = 65536;
            }
            else
            {
                return false;
            }
            if (continuationCount > bytes.Length - index - 1)
            {
                return false;
            }
            int continuationIndex = 1;
            while (continuationIndex <= continuationCount)
            {
                byte continuation = bytes[index + continuationIndex];
                if (continuation < 128 || continuation > 191)
                {
                    return false;
                }
                value = value * 64 + (uint)(continuation & 63);
                continuationIndex++;
            }
            if (value < minimum || value > 1114111 || value >= 55296 && value <= 57343)
            {
                return false;
            }
            index += continuationCount + 1;
        }
        return true;
    }

    internal static bool IsAscii(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] > 127)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool IsAscii(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] > 127)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool ParseDate(string text)
    {
        if (text.Length != 8)
        {
            return false;
        }
        int year = ParseDigits(text, 0, 4);
        int month = ParseDigits(text, 4, 2);
        int day = ParseDigits(text, 6, 2);
        if (year < 0 || year > 9999 || month < 0 || month > 12 || day < 0)
        {
            return false;
        }
        if (month == 0)
        {
            return day == 0;
        }
        if (day == 0)
        {
            return true;
        }
        if (year > 0)
        {
            return day <= System.DateTime.DaysInMonth(year, month);
        }
        int maximum = month == 2 ? 29 : month == 4 || month == 6 || month == 9 || month == 11 ? 30 : 31;
        return day <= maximum;
    }

    internal static bool ParseTime(string text)
    {
        if (text.Length != 6 && text.Length != 11)
        {
            return false;
        }
        int hour = ParseDigits(text, 0, 2);
        int minute = ParseDigits(text, 2, 2);
        int second = ParseDigits(text, 4, 2);
        if (hour < 0 || hour > 24 || minute < 0 || minute > 59 || second < 0 || second > 59 || hour == 24 && (minute != 0 || second != 0))
        {
            return false;
        }
        if (text.Length == 11)
        {
            if (text[6] != '+' && text[6] != '-')
            {
                return false;
            }
            int offsetHour = ParseDigits(text, 7, 2);
            int offsetMinute = ParseDigits(text, 9, 2);
            if (offsetHour < 0 || offsetHour > 23 || offsetMinute < 0 || offsetMinute > 59)
            {
                return false;
            }
        }
        return true;
    }

    internal static int ParseDigits(string text, int offset, int count)
    {
        int value = 0;
        int index = 0;
        while (index < count)
        {
            char character = text[offset + index];
            if (character < '0' || character > '9')
            {
                return -1;
            }
            value = value * 10 + character - '0';
            index++;
        }
        return value;
    }

    internal static bool AllZero(byte[] bytes, int offset)
    {
        int index = offset;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static int LengthOctets(int length)
    {
        if (length <= 65535)
        {
            return 2;
        }
        if (length <= 16777215)
        {
            return 3;
        }
        return 4;
    }

    internal static string NumericName(byte recordNumber, byte datasetNumber)
    {
        return recordNumber.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + datasetNumber.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void RequireKind(byte recordNumber, byte datasetNumber, RawIptcValueKind kind)
    {
        RequireRecord(recordNumber, -1);
        if (GetKind(recordNumber, datasetNumber) != kind)
        {
            throw Invalid("The IPTC IIM DataSet does not have the requested value form.", -1);
        }
    }

    internal static void RequireRecord(byte recordNumber, long offset)
    {
        if (recordNumber < 1 || recordNumber > 9)
        {
            throw Invalid("An IPTC IIM record number must be from one through nine.", offset);
        }
    }

    internal static bool IsIptcFieldType(TiffFieldType type)
    {
        return type == TiffFieldType.Byte || type == TiffFieldType.Undefined || type == TiffFieldType.Long;
    }

    internal static bool IsByteFieldType(TiffFieldType type)
    {
        return type == TiffFieldType.Byte || type == TiffFieldType.Undefined;
    }

    internal static void ValidateInput(byte[] rawBytes, RawLimits? limits, long offset)
    {
        ValidateLimits(limits);
        if (rawBytes == null || rawBytes.Length == 0)
        {
            throw Invalid("IPTC IIM payload bytes are required.", offset);
        }
        if (rawBytes.LongLength > (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxMetadataBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The IPTC IIM payload exceeds the configured metadata limit.", offset);
        }
        CheckedMath.AllocationSize(rawBytes.LongLength, 1, limits);
    }

    internal static void ValidateLimits(RawLimits? limits)
    {
        CheckedMath.RequireLimits(limits);
        CheckedMath.RequirePositiveLimit((limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxMetadataBytes, "The configured metadata limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagValueBytes, "The configured tag-value limit must be greater than zero.");
        CheckedMath.RequirePositiveLimit(limits.MaxTagCount, "The configured tag-count limit must be greater than zero.");
    }

    internal static void RequireCombinedLimit(long total, RawLimits? limits, long offset)
    {
        if (total > (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxMetadataBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Combined IPTC IIM payloads exceed the configured metadata limit.", offset);
        }
    }

    internal static long At(long offset, int relative)
    {
        if (offset < 0)
        {
            return offset;
        }
        return CheckedMath.Add(offset, relative);
    }

    internal static RawFormatException Invalid(string message, long offset)
    {
        return new RawFormatException(RawDiagnosticCode.InvalidMetadata, message, offset);
    }

    internal static RawFormatException Truncated(string message, long offset)
    {
        return new RawFormatException(RawDiagnosticCode.TruncatedInput, message, offset);
    }
}
