using System;
using System.Collections.Generic;
using System.Text;

namespace email;

internal class EmailTnefPropertyBudget
{
    int count;

    internal EmailTnefPropertyBudget()
    {
        count = 0;
    }

    internal int GetCount() { return count; }
    internal void Add(int value) { count += value; }
}
internal class EmailTnefPropertyListResult
{
    List<EmailTnefProperty> properties;
    int consumedBytes;
    bool complete;

    internal EmailTnefPropertyListResult(List<EmailTnefProperty> properties, int consumedBytes, bool complete)
    {
        this.properties = properties;
        this.consumedBytes = consumedBytes;
        this.complete = complete;
    }

    internal List<EmailTnefProperty> GetProperties() { return properties; }
    internal int GetConsumedBytes() { return consumedBytes; }
    internal bool IsComplete() { return complete; }
}
internal class EmailTnefPropertyParser
{
    internal static EmailTnefPropertyListResult ParseList(byte[] data, int offset, long sourceBaseOffset, int codePage, EmailTnefReaderOptions options, EmailIssueList issues, EmailTnefPropertyBudget budget, EmailIssueCode issueCode)
    {
        EmailTnefPropertyCursor cursor = new EmailTnefPropertyCursor(data, offset, sourceBaseOffset, issues, issueCode);
        List<EmailTnefProperty> properties = new List<EmailTnefProperty>();
        if (!cursor.CanRead(4))
        {
            cursor.Fail("A TNEF MAPI property list is missing its property count.");
            return new EmailTnefPropertyListResult(properties, cursor.GetPosition() - offset, false);
        }

        long declaredCount = cursor.ReadUInt32();
        if (declaredCount > options.GetMaximumPropertyCount()) throw new EmailLimitException("MaxTnefPropertyCount", options.GetMaximumPropertyCount(), declaredCount);
        long observed = budget.GetCount() + declaredCount;
        if (observed > options.GetMaximumPropertyCount()) throw new EmailLimitException("MaxTnefPropertyCount", options.GetMaximumPropertyCount(), observed);
        budget.Add((int)declaredCount);
        int index = 0;
        while (index < declaredCount && !cursor.HasFailed())
        {
            EmailTnefProperty? property = ParseProperty(cursor, codePage, options);
            if (property != null) properties.Add(property);
            index++;
        }

        bool complete = !cursor.HasFailed() && index == declaredCount;
        return new EmailTnefPropertyListResult(properties, cursor.GetPosition() - offset, complete);
    }

    static EmailTnefProperty? ParseProperty(EmailTnefPropertyCursor cursor, int codePage, EmailTnefReaderOptions options)
    {
        int start = cursor.GetPosition();
        if (!cursor.CanRead(4))
        {
            cursor.Fail("A TNEF MAPI property tag is truncated.");
            return null;
        }

        int typeCode = cursor.ReadUInt16();
        int propertyId = cursor.ReadUInt16();
        EmailTnefNamedPropertyKind namedKind = EmailTnefNamedPropertyKind.None;
        string? namedPropertySet = null;
        long namedNumericId = 0;
        string? namedStringId = null;
        if (propertyId >= 32768)
        {
            if (!cursor.CanRead(20))
            {
                cursor.Fail("A named TNEF MAPI property specification is truncated.");
                return Partial(cursor, start, typeCode, propertyId, namedKind, namedPropertySet, namedNumericId, namedStringId, new List<byte[]>(), new List<string?>(), new List<long>(), new List<bool>());
            }

            byte[] guidBytes = cursor.ReadBytes(16);
            namedPropertySet = new Guid(guidBytes).ToString("D");
            long kind = cursor.ReadUInt32();
            if (kind == 0)
            {
                namedKind = EmailTnefNamedPropertyKind.Numeric;
                if (!cursor.CanRead(4)) cursor.Fail("A numeric named-property identifier is truncated.");
                else namedNumericId = cursor.ReadUInt32();
            }
            else if (kind == 1)
            {
                namedKind = EmailTnefNamedPropertyKind.String;
                if (!cursor.CanRead(4)) cursor.Fail("A string named-property length is truncated.");
                else
                {
                    long byteLength = cursor.ReadUInt32();
                    long characters = byteLength / 2;
                    if (characters > options.GetMaximumNamedPropertyCharacters()) throw new EmailLimitException("MaxTnefNamedPropertyCharacters", options.GetMaximumNamedPropertyCharacters(), characters);
                    if (byteLength > int.MaxValue || !cursor.CanRead((int)byteLength)) cursor.Fail("A string named-property identifier is truncated.");
                    else
                    {
                        byte[] nameBytes = cursor.ReadBytes((int)byteLength);
                        namedStringId = DecodeUnicode(nameBytes, cursor);
                        cursor.SkipPadding((int)byteLength);
                    }
                }
            }
            else
            {
                cursor.Fail("A named TNEF MAPI property uses an unknown identifier kind " + kind + ".");
            }
        }

        List<byte[]> values = new List<byte[]>();
        List<string?> texts = new List<string?>();
        List<long> integers = new List<long>();
        List<bool> hasIntegers = new List<bool>();
        if (!cursor.HasFailed()) ReadValues(cursor, typeCode, codePage, options, values, texts, integers, hasIntegers);
        int end = cursor.GetPosition();
        byte[] raw = cursor.CopyRange(start, end - start);
        return new EmailTnefProperty(typeCode, propertyId, namedKind, namedPropertySet, namedNumericId, namedStringId, values, texts, integers, hasIntegers, raw, new EmailRange(cursor.GetSourceBaseOffset() + start, end - start), !cursor.HasFailed());
    }

    static EmailTnefProperty Partial(EmailTnefPropertyCursor cursor, int start, int typeCode, int propertyId, EmailTnefNamedPropertyKind namedKind, string? namedPropertySet, long namedNumericId, string? namedStringId, List<byte[]> values, List<string?> texts, List<long> integers, List<bool> hasIntegers)
    {
        int end = cursor.GetPosition();
        return new EmailTnefProperty(typeCode, propertyId, namedKind, namedPropertySet, namedNumericId, namedStringId, values, texts, integers, hasIntegers, cursor.CopyRange(start, end - start), new EmailRange(cursor.GetSourceBaseOffset() + start, end - start), false);
    }

    static void ReadValues(EmailTnefPropertyCursor cursor, int typeCode, int codePage, EmailTnefReaderOptions options, List<byte[]> values, List<string?> texts, List<long> integers, List<bool> hasIntegers)
    {
        int baseType = typeCode & 4095;
        bool multi = (typeCode & 4096) != 0;
        if (IsVariable(baseType))
        {
            if (!cursor.CanRead(4))
            {
                cursor.Fail("A variable-length TNEF MAPI property is missing its value count.");
                return;
            }

            long count = cursor.ReadUInt32();
            if (count > options.GetMaximumPropertyValueCount()) throw new EmailLimitException("MaxTnefPropertyValueCount", options.GetMaximumPropertyValueCount(), count);
            if (!multi && count != 1) cursor.AddIssue("A scalar variable-length TNEF MAPI property declares " + count + " values instead of one.");
            int index = 0;
            while (index < count && !cursor.HasFailed())
            {
                if (!cursor.CanRead(4))
                {
                    cursor.Fail("A variable-length TNEF MAPI property value is missing its size.");
                    break;
                }

                long length = cursor.ReadUInt32();
                if (length > options.GetLimits().GetMaxEntryBytes()) throw new EmailLimitException("MaxTnefPropertyValueBytes", options.GetLimits().GetMaxEntryBytes(), length);
                if (length > int.MaxValue || !cursor.CanRead((int)length))
                {
                    cursor.Fail("A variable-length TNEF MAPI property value is truncated.");
                    break;
                }

                byte[] value = cursor.ReadBytes((int)length);
                AddProjection(value, baseType, codePage, cursor, values, texts, integers, hasIntegers);
                cursor.SkipPadding((int)length);
                index++;
            }

            return;
        }

        int size = FixedSize(baseType);
        if (size < 0)
        {
            cursor.AddIssue("A TNEF MAPI property uses unsupported type 0x" + typeCode.ToString("X4") + "; the remaining property-list bytes cannot be framed safely.");
            byte[] remaining = cursor.ReadRemaining();
            AddProjection(remaining, baseType, codePage, cursor, values, texts, integers, hasIntegers);
            cursor.MarkFailed();
            return;
        }

        long fixedCount = 1;
        if (multi)
        {
            if (!cursor.CanRead(4))
            {
                cursor.Fail("A multivalue TNEF MAPI property is missing its value count.");
                return;
            }

            fixedCount = cursor.ReadUInt32();
            if (fixedCount > options.GetMaximumPropertyValueCount()) throw new EmailLimitException("MaxTnefPropertyValueCount", options.GetMaximumPropertyValueCount(), fixedCount);
        }

        int fixedIndex = 0;
        while (fixedIndex < fixedCount && !cursor.HasFailed())
        {
            if (!cursor.CanRead(size))
            {
                cursor.Fail("A fixed-length TNEF MAPI property value is truncated.");
                break;
            }

            byte[] value = cursor.ReadBytes(size);
            AddProjection(value, baseType, codePage, cursor, values, texts, integers, hasIntegers);
            cursor.SkipPadding(size);
            fixedIndex++;
        }
    }

    static void AddProjection(byte[] value, int baseType, int codePage, EmailTnefPropertyCursor cursor, List<byte[]> values, List<string?> texts, List<long> integers, List<bool> hasIntegers)
    {
        values.Add(value);
        string? text = null;
        long integer = 0;
        bool hasInteger = false;
        if (baseType == 30) text = DecodeString8(value, codePage, cursor);
        else if (baseType == 31) text = DecodeUnicode(value, cursor);
        else if (baseType == 2 || baseType == 11)
        {
            integer = ReadSigned16(value);
            hasInteger = true;
        }
        else if (baseType == 3 || baseType == 10)
        {
            integer = ReadSigned32(value);
            hasInteger = true;
        }
        else if (baseType == 20)
        {
            integer = ReadSigned64(value);
            hasInteger = true;
        }

        texts.Add(text);
        integers.Add(integer);
        hasIntegers.Add(hasInteger);
    }

    static bool IsVariable(int typeCode)
    {
        return typeCode == 13 || typeCode == 30 || typeCode == 31 || typeCode == 258;
    }

    static int FixedSize(int typeCode)
    {
        if (typeCode == 0 || typeCode == 1) return 0;
        if (typeCode == 2 || typeCode == 11) return 2;
        if (typeCode == 3 || typeCode == 4 || typeCode == 10) return 4;
        if (typeCode == 5 || typeCode == 6 || typeCode == 7 || typeCode == 20 || typeCode == 64) return 8;
        if (typeCode == 72) return 16;
        return -1;
    }

    static string DecodeString8(byte[] value, int codePage, EmailTnefPropertyCursor cursor)
    {
        int length = TrimSingleNull(value);
        EmailCharsetRegistry registry = new EmailCharsetRegistry();
        string name = codePage <= 0 ? "cp437" : "cp" + codePage;
        EmailCharsetLookupResult lookup = registry.Lookup(name);
        if (!lookup.IsFound())
        {
            cursor.AddIssueWithCode(EmailIssueCode.TnefCharsetUnsupported, "TNEF code page " + codePage + " is unavailable; the value was decoded as ISO-8859-1.");
            return Encoding.Latin1.GetString(value, 0, length);
        }

        return (lookup.GetEncoding() ?? throw new InvalidOperationException()).GetString(value, 0, length);
    }

    static string DecodeUnicode(byte[] value, EmailTnefPropertyCursor cursor)
    {
        int length = value.Length;
        if ((length & 1) != 0)
        {
            cursor.AddIssue("A UTF-16LE TNEF property value has an odd byte length.");
            length--;
        }

        if (length >= 2 && value[length - 2] == 0 && value[length - 1] == 0) length -= 2;
        return Encoding.Unicode.GetString(value, 0, length);
    }

    static int TrimSingleNull(byte[] value)
    {
        return value.Length > 0 && value[value.Length - 1] == 0 ? value.Length - 1 : value.Length;
    }

    static long ReadSigned16(byte[] value)
    {
        if (value.Length < 2) return 0;
        int raw = value[0] | value[1] << 8;
        return raw >= 32768 ? raw - 65536 : raw;
    }

    static long ReadSigned32(byte[] value)
    {
        if (value.Length < 4) return 0;
        return value[0] | value[1] << 8 | value[2] << 16 | value[3] << 24;
    }

    static long ReadSigned64(byte[] value)
    {
        if (value.Length < 8) return 0;
        long raw = 0;
        int index = 0;
        while (index < 8)
        {
            raw |= (long)value[index] << index * 8;
            index++;
        }

        return raw;
    }
}
internal class EmailTnefPropertyCursor
{
    byte[] data;
    int position;
    long sourceBaseOffset;
    EmailIssueList issues;
    EmailIssueCode issueCode;
    bool failed;

    internal EmailTnefPropertyCursor(byte[] data, int position, long sourceBaseOffset, EmailIssueList issues, EmailIssueCode issueCode)
    {
        this.data = data;
        this.position = position;
        this.sourceBaseOffset = sourceBaseOffset;
        this.issues = issues;
        this.issueCode = issueCode;
        failed = false;
    }

    internal int GetPosition() { return position; }
    internal long GetSourceBaseOffset() { return sourceBaseOffset; }
    internal bool HasFailed() { return failed; }
    internal bool CanRead(int count) { return count >= 0 && position <= data.Length && count <= data.Length - position; }
    internal int ReadUInt16() { int value = data[position] | data[position + 1] << 8; position += 2; return value; }
    internal long ReadUInt32() { uint value = (uint)data[position] | (uint)data[position + 1] << 8 | (uint)data[position + 2] << 16 | (uint)data[position + 3] << 24; position += 4; return value; }

    internal byte[] ReadBytes(int count)
    {
        byte[] value = CopyRange(position, count);
        position += count;
        return value;
    }

    internal byte[] ReadRemaining()
    {
        byte[] value = CopyRange(position, data.Length - position);
        position = data.Length;
        return value;
    }

    internal void SkipPadding(int valueLength)
    {
        int padding = (4 - valueLength % 4) % 4;
        if (!CanRead(padding)) Fail("TNEF MAPI property padding is truncated.");
        else position += padding;
    }

    internal byte[] CopyRange(int offset, int count)
    {
        byte[] value = new byte[count];
        if (count > 0) Array.Copy(data, offset, value, 0, count);
        return value;
    }

    internal void Fail(string description)
    {
        AddIssue(description);
        position = data.Length;
        failed = true;
    }

    internal void MarkFailed() { failed = true; }
    internal void AddIssue(string description) { AddIssueWithCode(issueCode, description); }

    internal void AddIssueWithCode(EmailIssueCode code, string description)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, code, description, new EmailSourcePosition(sourceBaseOffset + position, 1, sourceBaseOffset + position + 1), null, null, "The property bytes parsed before the error were retained."));
    }
}
