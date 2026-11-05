using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace http;

internal static class CacheControlCodec
{
    internal static CacheControlParseResult Parse(
        byte[] input,
        int maximumSize,
        int maximumDirectives,
        int maximumFieldNames)
    {
        if (input == null)
        {
            return CreateMalformed(0);
        }

        if (maximumDirectives < 0 || maximumFieldNames < 0)
        {
            return CreateLimit();
        }

        FieldValueParseResult list = FieldValueParser.ParseHashList(
            input,
            maximumSize,
            maximumDirectives,
            input.Length);
        if (list.IsLimitExceeded)
        {
            return CreateLimit();
        }

        if (list.Status != HttpOperationStatus.Done)
        {
            return CreateMalformed(list.ErrorPosition);
        }

        CacheControlParseResult result = CreateDone(input.Length);
        int index = 0;
        while (index < list.Values.Count)
        {
            CacheControlParseResult directiveResult = ParseDirective(list.Values[index]);
            if (directiveResult.Status != HttpOperationStatus.Done)
            {
                directiveResult.ErrorPosition = index;
                return directiveResult;
            }

            HttpParameter directive = directiveResult.Value.Extensions[0];
            CacheControlParseResult applied = ApplyDirective(
                result.Value,
                directive,
                maximumFieldNames);
            if (applied.Status != HttpOperationStatus.Done)
            {
                applied.ErrorPosition = index;
                return applied;
            }

            index = index + 1;
        }

        return result;
    }

    internal static FieldValueWriteResult Serialize(
        HttpCacheControl value,
        int maximumSize,
        int maximumDirectives,
        int maximumFieldNames)
    {
        if (value == null || value.NoCacheFields == null || value.PrivateFields == null ||
            value.Extensions == null)
        {
            return CreateMalformedWrite(0);
        }

        if (maximumDirectives < 0 || maximumFieldNames < 0)
        {
            return CreateLimitWrite(0);
        }

        if ((!value.NoCache && value.NoCacheFields.Count != 0) ||
            (!value.Private && value.PrivateFields.Count != 0) ||
            (!value.HasMaxAge && value.MaxAge != 0) ||
            (!value.HasSharedMaxAge && value.SharedMaxAge != 0) ||
            (!value.HasMaxStale && (value.HasMaxStaleValue || value.MaxStale != 0)) ||
            (!value.HasMaxStaleValue && value.MaxStale != 0) ||
            (!value.HasMinFresh && value.MinFresh != 0) ||
            (!value.HasStaleIfError && value.StaleIfError != 0) ||
            (!value.HasStaleWhileRevalidate && value.StaleWhileRevalidate != 0))
        {
            return CreateMalformedWrite(0);
        }

        List<byte[]> members = new List<byte[]>();
        FieldValueWriteResult addition = AddQualifiedMember(
            members,
            value.NoCache,
            HttpHeaderValues.NoCache,
            value.NoCacheFields,
            maximumFieldNames);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        AddFlagMember(members, value.NoStore, HttpHeaderValues.NoStore);
        AddFlagMember(members, value.NoTransform, HttpHeaderValues.NoTransform);
        AddFlagMember(members, value.OnlyIfCached, HttpHeaderValues.OnlyIfCached);
        AddFlagMember(members, value.MustRevalidate, HttpHeaderValues.MustRevalidate);
        AddFlagMember(members, value.ProxyRevalidate, HttpHeaderValues.ProxyRevalidate);
        AddFlagMember(members, value.MustUnderstand, HttpHeaderValues.MustUnderstand);
        addition = AddQualifiedMember(
            members,
            value.Private,
            HttpHeaderValues.Private,
            value.PrivateFields,
            maximumFieldNames);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        AddFlagMember(members, value.Public, HttpHeaderValues.Public);
        AddFlagMember(members, value.Immutable, HttpHeaderValues.Immutable);
        addition = AddNumberMember(members, value.HasMaxAge, HttpHeaderValues.MaxAge, value.MaxAge);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        addition = AddNumberMember(
            members,
            value.HasSharedMaxAge,
            HttpHeaderValues.SMaxAge,
            value.SharedMaxAge);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        addition = AddMaxStaleMember(members, value);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        addition = AddNumberMember(
            members,
            value.HasMinFresh,
            HttpHeaderValues.MinFresh,
            value.MinFresh);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        addition = AddNumberMember(
            members,
            value.HasStaleIfError,
            HttpHeaderValues.StaleIfError,
            value.StaleIfError);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        addition = AddNumberMember(
            members,
            value.HasStaleWhileRevalidate,
            HttpHeaderValues.StaleWhileRevalidate,
            value.StaleWhileRevalidate);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        addition = AddExtensionMembers(members, value.Extensions);
        if (addition.Status != HttpOperationStatus.Done)
        {
            return addition;
        }

        return FieldValueWriter.SerializeHashList(
            members,
            maximumSize,
            maximumDirectives,
            0);
    }

    static CacheControlParseResult ParseDirective(byte[] input)
    {
        int nameEnd = 0;
        while (nameEnd < input.Length && HttpGrammar.IsTokenCharacter(input[nameEnd]))
        {
            nameEnd = nameEnd + 1;
        }

        if (nameEnd == 0)
        {
            return CreateMalformed(0);
        }

        HttpParameter directive = new HttpParameter();
        directive.Name = Encoding.ASCII.GetString(input, 0, nameEnd);
        if (nameEnd < input.Length)
        {
            if (input[nameEnd] != 0x3d || nameEnd + 1 >= input.Length)
            {
                return CreateMalformed(nameEnd);
            }

            directive.HasValue = true;
            byte[] valueBytes = CopyRange(input, nameEnd + 1, input.Length);
            FieldValueParseResult parsedValue;
            if (valueBytes[0] == 0x22)
            {
                directive.IsQuoted = true;
                parsedValue = FieldValueParser.ParseQuotedString(valueBytes, valueBytes.Length);
            }
            else
            {
                parsedValue = FieldValueParser.ParseToken(valueBytes, valueBytes.Length);
            }

            if (parsedValue.Status != HttpOperationStatus.Done)
            {
                return CreateMalformed(nameEnd + 1 + parsedValue.ErrorPosition);
            }

            directive.Value = parsedValue.Value;
        }

        CacheControlParseResult result = CreateDone(input.Length);
        result.Value.Extensions.Add(directive);
        return result;
    }

    static CacheControlParseResult ApplyDirective(
        HttpCacheControl control,
        HttpParameter directive,
        int maximumFieldNames)
    {
        string name = directive.Name;
        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.NoCache))
        {
            return ApplyQualifiedDirective(control, directive, maximumFieldNames, true);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.Private))
        {
            return ApplyQualifiedDirective(control, directive, maximumFieldNames, false);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MaxAge))
        {
            return ApplyNumericDirective(control, directive, 0);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.SMaxAge))
        {
            return ApplyNumericDirective(control, directive, 1);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MaxStale))
        {
            return ApplyMaxStale(control, directive);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MinFresh))
        {
            return ApplyNumericDirective(control, directive, 2);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.StaleIfError))
        {
            return ApplyNumericDirective(control, directive, 3);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.StaleWhileRevalidate))
        {
            return ApplyNumericDirective(control, directive, 4);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.NoStore))
        {
            if (directive.HasValue)
            {
                return CreateMalformed(0);
            }

            control.NoStore = true;
            return CreateDone(0);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.NoTransform))
        {
            if (directive.HasValue)
            {
                return CreateMalformed(0);
            }

            control.NoTransform = true;
            return CreateDone(0);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.OnlyIfCached))
        {
            if (directive.HasValue)
            {
                return CreateMalformed(0);
            }

            control.OnlyIfCached = true;
            return CreateDone(0);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MustRevalidate))
        {
            if (directive.HasValue)
            {
                return CreateMalformed(0);
            }

            control.MustRevalidate = true;
            return CreateDone(0);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.ProxyRevalidate))
        {
            if (directive.HasValue)
            {
                return CreateMalformed(0);
            }

            control.ProxyRevalidate = true;
            return CreateDone(0);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MustUnderstand))
        {
            if (directive.HasValue)
            {
                return CreateMalformed(0);
            }

            control.MustUnderstand = true;
            return CreateDone(0);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.Public))
        {
            if (directive.HasValue)
            {
                return CreateMalformed(0);
            }

            control.Public = true;
            return CreateDone(0);
        }

        if (Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.Immutable))
        {
            if (directive.HasValue)
            {
                return CreateMalformed(0);
            }

            control.Immutable = true;
            return CreateDone(0);
        }

        control.Extensions.Add(directive);
        return CreateDone(0);
    }

    static CacheControlParseResult ApplyNumericDirective(
        HttpCacheControl control,
        HttpParameter directive,
        int kind)
    {
        if (!directive.HasValue)
        {
            return CreateMalformed(0);
        }

        CacheControlParseResult parsed = ParseDeltaSeconds(directive.Value);
        if (parsed.Status != HttpOperationStatus.Done)
        {
            return parsed;
        }

        long value = parsed.Value.MaxAge;
        switch (kind)
        {
            case 0:
                if (control.HasMaxAge && control.MaxAge != value)
                {
                    control.HasConflicts = true;
                }
                else if (!control.HasMaxAge)
                {
                    control.HasMaxAge = true;
                    control.MaxAge = value;
                }

                break;
            case 1:
                if (control.HasSharedMaxAge && control.SharedMaxAge != value)
                {
                    control.HasConflicts = true;
                }
                else if (!control.HasSharedMaxAge)
                {
                    control.HasSharedMaxAge = true;
                    control.SharedMaxAge = value;
                }

                break;
            case 2:
                if (control.HasMinFresh && control.MinFresh != value)
                {
                    control.HasConflicts = true;
                }
                else if (!control.HasMinFresh)
                {
                    control.HasMinFresh = true;
                    control.MinFresh = value;
                }

                break;
            case 3:
                if (control.HasStaleIfError && control.StaleIfError != value)
                {
                    control.HasConflicts = true;
                }
                else if (!control.HasStaleIfError)
                {
                    control.HasStaleIfError = true;
                    control.StaleIfError = value;
                }

                break;
            default:
                if (control.HasStaleWhileRevalidate && control.StaleWhileRevalidate != value)
                {
                    control.HasConflicts = true;
                }
                else if (!control.HasStaleWhileRevalidate)
                {
                    control.HasStaleWhileRevalidate = true;
                    control.StaleWhileRevalidate = value;
                }

                break;
        }

        return CreateDone(0);
    }

    static CacheControlParseResult ApplyMaxStale(
        HttpCacheControl control,
        HttpParameter directive)
    {
        long value = 0;
        if (directive.HasValue)
        {
            CacheControlParseResult parsed = ParseDeltaSeconds(directive.Value);
            if (parsed.Status != HttpOperationStatus.Done)
            {
                return parsed;
            }

            value = parsed.Value.MaxAge;
        }

        if (control.HasMaxStale &&
            (control.HasMaxStaleValue != directive.HasValue ||
            directive.HasValue && control.MaxStale != value))
        {
            control.HasConflicts = true;
        }
        else if (!control.HasMaxStale)
        {
            control.HasMaxStale = true;
            control.HasMaxStaleValue = directive.HasValue;
            control.MaxStale = value;
        }

        return CreateDone(0);
    }

    static CacheControlParseResult ApplyQualifiedDirective(
        HttpCacheControl control,
        HttpParameter directive,
        int maximumFieldNames,
        bool noCache)
    {
        List<string> fields = new List<string>();
        if (directive.HasValue)
        {
            CacheControlParseResult parsed = ParseFieldNames(directive.Value, maximumFieldNames);
            if (parsed.Status != HttpOperationStatus.Done)
            {
                return parsed;
            }

            fields = parsed.Value.NoCacheFields;
        }

        bool alreadyPresent = noCache ? control.NoCache : control.Private;
        List<string> existing = noCache ? control.NoCacheFields : control.PrivateFields;
        if (alreadyPresent)
        {
            if (directive.HasValue != (existing.Count != 0) ||
                directive.HasValue && !AreFieldListsEqual(existing, fields))
            {
                control.HasConflicts = true;
            }

            return CreateDone(0);
        }

        if (noCache)
        {
            control.NoCache = true;
            control.NoCacheFields = fields;
        }
        else
        {
            control.Private = true;
            control.PrivateFields = fields;
        }

        return CreateDone(0);
    }

    static CacheControlParseResult ParseDeltaSeconds(byte[] input)
    {
        if (input == null || input.Length == 0)
        {
            return CreateMalformed(0);
        }

        long value = 0;
        bool saturated = false;
        int index = 0;
        while (index < input.Length)
        {
            byte character = input[index];
            if (character < 0x30 || character > 0x39)
            {
                return CreateMalformed(index);
            }

            int digit = character - 0x30;
            if (!saturated)
            {
                if (value > (long.MaxValue - digit) / 10)
                {
                    value = long.MaxValue;
                    saturated = true;
                }
                else
                {
                    value = value * 10 + digit;
                }
            }

            index = index + 1;
        }

        CacheControlParseResult result = CreateDone(input.Length);
        result.Value.HasMaxAge = true;
        result.Value.MaxAge = value;
        return result;
    }

    static CacheControlParseResult ParseFieldNames(byte[] input, int maximumFieldNames)
    {
        if (input.Length == 0)
        {
            return CreateMalformed(0);
        }

        FieldValueParseResult parsed = FieldValueParser.ParseHashList(
            input,
            input.Length,
            maximumFieldNames,
            input.Length);
        if (parsed.IsLimitExceeded)
        {
            return CreateLimit();
        }

        if (parsed.Status != HttpOperationStatus.Done || parsed.Values.Count == 0)
        {
            return CreateMalformed(parsed.ErrorPosition);
        }

        CacheControlParseResult result = CreateDone(input.Length);
        int index = 0;
        while (index < parsed.Values.Count)
        {
            FieldValueParseResult token = FieldValueParser.ParseToken(
                parsed.Values[index],
                parsed.Values[index].Length);
            if (token.Status != HttpOperationStatus.Done)
            {
                return CreateMalformed(index);
            }

            result.Value.NoCacheFields.Add(Encoding.ASCII.GetString(token.Value));
            index = index + 1;
        }

        return result;
    }

    static bool AreFieldListsEqual(List<string> left, List<string> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        int index = 0;
        while (index < left.Count)
        {
            if (!Ascii.EqualsStringsIgnoreCase(left[index], right[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static FieldValueWriteResult AddQualifiedMember(
        List<byte[]> members,
        bool present,
        string name,
        List<string> fields,
        int maximumFieldNames)
    {
        if (!present)
        {
            return CreateDoneWrite(Array.Empty<byte>());
        }

        if (fields.Count == 0)
        {
            members.Add(Encoding.ASCII.GetBytes(name));
            return CreateDoneWrite(Array.Empty<byte>());
        }

        if (fields.Count > maximumFieldNames)
        {
            return CreateLimitWrite(0);
        }

        StringBuilder joined = new StringBuilder();
        int index = 0;
        while (index < fields.Count)
        {
            if (!HeaderStore.IsValidName(fields[index]))
            {
                return CreateMalformedWrite(index);
            }

            if (index > 0)
            {
                joined.Append(", ");
            }

            joined.Append(fields[index]);
            index = index + 1;
        }

        byte[] decoded = Encoding.ASCII.GetBytes(joined.ToString());
        FieldValueWriteResult quoted = FieldValueWriter.SerializeQuotedString(decoded, int.MaxValue);
        byte[] member = new byte[name.Length + 1 + quoted.Bytes.Length];
        int position = CopyBytesAt(Encoding.ASCII.GetBytes(name), member, 0);
        member[position] = 0x3d;
        CopyBytesAt(quoted.Bytes, member, position + 1);
        members.Add(member);
        return CreateDoneWrite(Array.Empty<byte>());
    }

    static void AddFlagMember(List<byte[]> members, bool present, string name)
    {
        if (present)
        {
            members.Add(Encoding.ASCII.GetBytes(name));
        }
    }

    static FieldValueWriteResult AddNumberMember(
        List<byte[]> members,
        bool present,
        string name,
        long value)
    {
        if (!present)
        {
            return CreateDoneWrite(Array.Empty<byte>());
        }

        if (value < 0)
        {
            return CreateMalformedWrite(0);
        }

        string text = name + "=" + value.ToString(CultureInfo.InvariantCulture);
        members.Add(Encoding.ASCII.GetBytes(text));
        return CreateDoneWrite(Array.Empty<byte>());
    }

    static FieldValueWriteResult AddMaxStaleMember(
        List<byte[]> members,
        HttpCacheControl value)
    {
        if (!value.HasMaxStale)
        {
            return CreateDoneWrite(Array.Empty<byte>());
        }

        if (!value.HasMaxStaleValue)
        {
            members.Add(Encoding.ASCII.GetBytes(HttpHeaderValues.MaxStale));
            return CreateDoneWrite(Array.Empty<byte>());
        }

        return AddNumberMember(members, true, HttpHeaderValues.MaxStale, value.MaxStale);
    }

    static FieldValueWriteResult AddExtensionMembers(
        List<byte[]> members,
        List<HttpParameter> extensions)
    {
        int index = 0;
        while (index < extensions.Count)
        {
            HttpParameter extension = extensions[index];
            if (extension == null || extension.Name == null || extension.Value == null ||
                IsKnownDirective(extension.Name) ||
                !HeaderStore.IsValidName(extension.Name) ||
                !extension.HasValue && (extension.Value.Length != 0 || extension.IsQuoted))
            {
                return CreateMalformedWrite(index);
            }

            byte[] name = Encoding.ASCII.GetBytes(extension.Name);
            FieldValueWriteResult validName = FieldValueWriter.SerializeToken(name, int.MaxValue);
            if (validName.Status != HttpOperationStatus.Done)
            {
                return CreateMalformedWrite(index);
            }

            if (!extension.HasValue)
            {
                members.Add(name);
                index = index + 1;
                continue;
            }

            FieldValueWriteResult value = extension.IsQuoted ?
                FieldValueWriter.SerializeQuotedString(extension.Value, int.MaxValue) :
                FieldValueWriter.SerializeToken(extension.Value, int.MaxValue);
            if (value.Status != HttpOperationStatus.Done)
            {
                return CreateMalformedWrite(index);
            }

            byte[] member = new byte[name.Length + 1 + value.Bytes.Length];
            int position = CopyBytesAt(name, member, 0);
            member[position] = 0x3d;
            CopyBytesAt(value.Bytes, member, position + 1);
            members.Add(member);
            index = index + 1;
        }

        return CreateDoneWrite(Array.Empty<byte>());
    }

    static bool IsKnownDirective(string name)
    {
        return Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.NoCache) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.NoStore) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.NoTransform) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.OnlyIfCached) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MustRevalidate) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.ProxyRevalidate) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MustUnderstand) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.Private) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.Public) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.Immutable) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MaxAge) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.SMaxAge) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MaxStale) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.MinFresh) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.StaleIfError) ||
            Ascii.EqualsStringsIgnoreCase(name, HttpHeaderValues.StaleWhileRevalidate);
    }

    static byte[] CopyRange(byte[] input, int start, int end)
    {
        byte[] result = new byte[end - start];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = input[start + index];
            index = index + 1;
        }

        return result;
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

    static CacheControlParseResult CreateDone(int consumed)
    {
        CacheControlParseResult result = new CacheControlParseResult();
        result.Status = HttpOperationStatus.Done;
        result.Consumed = consumed;
        return result;
    }

    static CacheControlParseResult CreateMalformed(int errorPosition)
    {
        CacheControlParseResult result = new CacheControlParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static CacheControlParseResult CreateLimit()
    {
        CacheControlParseResult result = new CacheControlParseResult();
        result.Status = HttpOperationStatus.Rejected;
        result.IsLimitExceeded = true;
        return result;
    }

    static FieldValueWriteResult CreateDoneWrite(byte[] bytes)
    {
        FieldValueWriteResult result = new FieldValueWriteResult();
        result.Status = HttpOperationStatus.Done;
        result.Bytes = bytes;
        result.Written = bytes.Length;
        result.RequiredSize = bytes.Length;
        return result;
    }

    static FieldValueWriteResult CreateMalformedWrite(int errorPosition)
    {
        FieldValueWriteResult result = new FieldValueWriteResult();
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorPosition = errorPosition;
        result.IsMalformed = true;
        return result;
    }

    static FieldValueWriteResult CreateLimitWrite(int requiredSize)
    {
        FieldValueWriteResult result = new FieldValueWriteResult();
        result.Status = HttpOperationStatus.Rejected;
        result.RequiredSize = requiredSize;
        result.IsLimitExceeded = true;
        return result;
    }
}
