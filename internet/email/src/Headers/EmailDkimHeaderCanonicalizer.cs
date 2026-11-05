using System;
using System.Collections.Generic;

namespace email;

internal class EmailDkimBlankedSignature
{
    byte[] bytes;
    bool blanked;

    internal EmailDkimBlankedSignature(byte[] bytes, bool blanked)
    {
        this.bytes = bytes;
        this.blanked = blanked;
    }

    internal byte[] GetBytes() { return bytes; }
    internal bool WasBlanked() { return blanked; }
}
internal static class EmailDkimHeaderCanonicalizer
{
    internal static byte[] CanonicalizeArcField(EmailHeader header, bool blankSignatureValue, bool appendLineEnding)
    {
        if (header == null) throw new ArgumentNullException("header");
        byte[] raw = header.GetRawFieldBytes();
        int fieldEnd = GetFieldEnd(raw, header.GetLineEnding());
        if (blankSignatureValue)
        {
            EmailDkimBlankedSignature blanked = BlankSignatureValue(raw, fieldEnd);
            return CanonicalizeRelaxed(blanked.GetBytes(), blanked.GetBytes().Length, appendLineEnding);
        }

        return CanonicalizeRelaxed(raw, fieldEnd, appendLineEnding);
    }

    internal static EmailDkimHeaderCanonicalizationResult Canonicalize(EmailHeaders headers, List<string> signedHeaderNames, EmailHeader signatureHeader, EmailDkimHeaderCanonicalizationOptions options)
    {
        if (headers == null) throw new ArgumentNullException("headers");
        if (signedHeaderNames == null) throw new ArgumentNullException("signedHeaderNames");
        if (signatureHeader == null) throw new ArgumentNullException("signatureHeader");
        if (options == null) throw new ArgumentNullException("options");
        EmailDkimHeaderCanonicalizationOptions snapshot = options.Clone();
        if (signedHeaderNames.Count > snapshot.GetMaximumSelectedHeaderCount()) throw new EmailLimitException("MaxDkimSelectedHeaderCount", snapshot.GetMaximumSelectedHeaderCount(), signedHeaderNames.Count);

        bool[] consumed = new bool[headers.GetCount()];
        List<EmailDkimHeaderSelection> selections = new List<EmailDkimHeaderSelection>();
        List<byte> output = new List<byte>();
        long sourceByteCount = 0;
        int missingCount = 0;
        int requestedIndex = 0;
        while (requestedIndex < signedHeaderNames.Count)
        {
            string requestedName = signedHeaderNames[requestedIndex];
            ValidateHeaderName(requestedName);
            int sourceIndex = FindFromBottom(headers, requestedName, signatureHeader, consumed);
            if (sourceIndex < 0)
            {
                selections.Add(new EmailDkimHeaderSelection(requestedName, -1, null));
                missingCount++;
            }
            else
            {
                consumed[sourceIndex] = true;
                EmailHeader selected = headers.GetAt(sourceIndex);
                selections.Add(new EmailDkimHeaderSelection(requestedName, sourceIndex, selected));
                byte[] raw = selected.GetRawFieldBytes();
                sourceByteCount = AddSourceBytes(sourceByteCount, raw.Length, snapshot);
                byte[] canonical = snapshot.GetKind() == EmailDkimCanonicalizationKind.Simple ? raw : CanonicalizeRelaxed(raw, GetFieldEnd(raw, selected.GetLineEnding()), true);
                AddOutput(output, canonical, snapshot);
            }

            requestedIndex++;
        }

        byte[] signatureRaw = signatureHeader.GetRawFieldBytes();
        sourceByteCount = AddSourceBytes(sourceByteCount, signatureRaw.Length, snapshot);
        int signatureEnd = GetFieldEnd(signatureRaw, signatureHeader.GetLineEnding());
        EmailDkimBlankedSignature blanked = BlankSignatureValue(signatureRaw, signatureEnd);
        byte[] signatureCanonical = snapshot.GetKind() == EmailDkimCanonicalizationKind.Simple ? blanked.GetBytes() : CanonicalizeRelaxed(blanked.GetBytes(), blanked.GetBytes().Length, false);
        AddOutput(output, signatureCanonical, snapshot);
        return new EmailDkimHeaderCanonicalizationResult(snapshot.GetKind(), output.ToArray(), selections, sourceByteCount, missingCount, blanked.WasBlanked());
    }

    static int FindFromBottom(EmailHeaders headers, string requestedName, EmailHeader signatureHeader, bool[] consumed)
    {
        int index = headers.GetCount() - 1;
        while (index >= 0)
        {
            EmailHeader candidate = headers.GetAt(index);
            if (!consumed[index] && !object.ReferenceEquals(candidate, signatureHeader) && NameEquals(candidate.GetNameBytes(), requestedName)) return index;
            index--;
        }

        return -1;
    }

    static bool NameEquals(byte[] fieldName, string requestedName)
    {
        int length = fieldName.Length;
        while (length > 0 && (fieldName[length - 1] == 32 || fieldName[length - 1] == 9)) length--;
        if (length != requestedName.Length) return false;
        int index = 0;
        while (index < length)
        {
            int left = fieldName[index];
            int right = requestedName[index];
            if (left >= 65 && left <= 90) left += 32;
            if (right >= 65 && right <= 90) right += 32;
            if (left != right) return false;
            index++;
        }

        return true;
    }

    static byte[] CanonicalizeRelaxed(byte[] raw, int fieldEnd, bool appendLineEnding)
    {
        int colon = FindColon(raw, fieldEnd);
        if (colon < 0) throw new ArgumentException("A selected DKIM header must contain a colon separator.", "raw");
        List<byte> output = new List<byte>();
        int nameEnd = colon;
        while (nameEnd > 0 && (raw[nameEnd - 1] == 32 || raw[nameEnd - 1] == 9)) nameEnd--;
        int index = 0;
        while (index < nameEnd)
        {
            byte value = raw[index];
            output.Add(value >= 65 && value <= 90 ? (byte)(value + 32) : value);
            index++;
        }

        output.Add(58);
        index = colon + 1;
        bool pendingWhitespace = false;
        bool hasValue = false;
        while (index < fieldEnd)
        {
            if (raw[index] == 13 && index + 2 < fieldEnd && raw[index + 1] == 10 && IsWhitespace(raw[index + 2]))
            {
                index += 2;
                continue;
            }

            byte value = raw[index];
            if (IsWhitespace(value))
            {
                pendingWhitespace = true;
            }
            else
            {
                if (pendingWhitespace && hasValue) output.Add(32);
                output.Add(value);
                pendingWhitespace = false;
                hasValue = true;
            }

            index++;
        }

        if (appendLineEnding)
        {
            output.Add(13);
            output.Add(10);
        }

        return output.ToArray();
    }

    static EmailDkimBlankedSignature BlankSignatureValue(byte[] raw, int fieldEnd)
    {
        int colon = FindColon(raw, fieldEnd);
        if (colon < 0) throw new ArgumentException("A DKIM signature header must contain a colon separator.", "raw");
        List<byte> output = new List<byte>();
        int copiedThrough = 0;
        int segmentStart = colon + 1;
        bool blanked = false;
        while (segmentStart <= fieldEnd)
        {
            int segmentEnd = segmentStart;
            while (segmentEnd < fieldEnd && raw[segmentEnd] != 59) segmentEnd++;
            int cursor = segmentStart;
            while (cursor < segmentEnd && IsFoldingWhitespace(raw[cursor])) cursor++;
            int nameStart = cursor;
            while (cursor < segmentEnd && IsTagNameByte(raw[cursor])) cursor++;
            int nameEnd = cursor;
            while (cursor < segmentEnd && IsFoldingWhitespace(raw[cursor])) cursor++;
            if (nameEnd - nameStart == 1 && raw[nameStart] == 98 && cursor < segmentEnd && raw[cursor] == 61)
            {
                int blankStart = cursor + 1;
                AddRange(output, raw, copiedThrough, blankStart);
                copiedThrough = segmentEnd;
                blanked = true;
            }

            if (segmentEnd >= fieldEnd) break;
            segmentStart = segmentEnd + 1;
        }

        AddRange(output, raw, copiedThrough, fieldEnd);
        return new EmailDkimBlankedSignature(output.ToArray(), blanked);
    }

    static int GetFieldEnd(byte[] raw, EmailLineEnding ending)
    {
        int endingLength = ending == EmailLineEnding.CrLf ? 2 : ending == EmailLineEnding.None ? 0 : 1;
        if (endingLength > raw.Length) return raw.Length;
        if (ending == EmailLineEnding.CrLf && (raw[raw.Length - 2] != 13 || raw[raw.Length - 1] != 10)) return raw.Length;
        if (ending == EmailLineEnding.Cr && raw[raw.Length - 1] != 13) return raw.Length;
        if (ending == EmailLineEnding.Lf && raw[raw.Length - 1] != 10) return raw.Length;
        return raw.Length - endingLength;
    }

    static int FindColon(byte[] raw, int end)
    {
        int index = 0;
        while (index < end)
        {
            if (raw[index] == 58) return index;
            index++;
        }

        return -1;
    }

    static void AddOutput(List<byte> output, byte[] value, EmailDkimHeaderCanonicalizationOptions options)
    {
        if (value.LongLength > options.GetMaximumCanonicalizedBytes() - output.Count) throw new EmailLimitException("MaxDkimCanonicalizedHeaderBytes", options.GetMaximumCanonicalizedBytes(), (long)output.Count + value.LongLength);
        output.AddRange(value);
    }

    static long AddSourceBytes(long count, int value, EmailDkimHeaderCanonicalizationOptions options)
    {
        if (value > options.GetMaximumInputBytes() - count) throw new EmailLimitException("MaxDkimHeaderInputBytes", options.GetMaximumInputBytes(), count + value);
        return count + value;
    }

    static void AddRange(List<byte> output, byte[] source, int start, int end)
    {
        int index = start;
        while (index < end)
        {
            output.Add(source[index]);
            index++;
        }
    }

    static bool IsWhitespace(byte value) { return value == 32 || value == 9; }
    static bool IsFoldingWhitespace(byte value) { return value == 32 || value == 9 || value == 13 || value == 10; }
    static bool IsTagNameByte(byte value)
    {
        return value >= 65 && value <= 90 || value >= 97 && value <= 122 || value >= 48 && value <= 57 || value == 95;
    }

    static void ValidateHeaderName(string value)
    {
        if (value == null) throw new ArgumentException("A signed header name cannot be null.", "signedHeaderNames");
        if (value.Length == 0) throw new ArgumentException("A signed header name cannot be empty.", "signedHeaderNames");
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current < 33 || current > 126 || current == 58) throw new ArgumentException("A signed header name must contain printable ASCII field-name characters.", "signedHeaderNames");
            index++;
        }
    }
}
