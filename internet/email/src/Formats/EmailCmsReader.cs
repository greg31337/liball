using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.IO;

namespace email;
internal class EmailCmsScanResult
{
    bool valid;
    List<EmailCmsAsn1Node> roots;
    int elementCount;
    int maximumDepth;
    int errorOffset;

    internal EmailCmsScanResult(bool valid, List<EmailCmsAsn1Node> roots, int elementCount, int maximumDepth, int errorOffset)
    {
        this.valid = valid;
        this.roots = roots;
        this.elementCount = elementCount;
        this.maximumDepth = maximumDepth;
        this.errorOffset = errorOffset;
    }

    internal bool IsValid() { return valid; }
    internal int GetRootCount() { return roots.Count; }
    internal EmailCmsAsn1Node GetRootAt(int index) { return roots[index]; }
    internal int GetElementCount() { return elementCount; }
    internal int GetMaximumDepth() { return maximumDepth; }
    internal int GetErrorOffset() { return errorOffset; }
}
internal class EmailCmsScanState
{
    EmailCryptographicLimits limits;
    int elementCount;
    int maximumDepth;
    int errorOffset;

    internal EmailCmsScanState(EmailCryptographicLimits limits)
    {
        this.limits = limits;
        elementCount = 0;
        maximumDepth = 0;
        errorOffset = 0;
    }

    internal void AddElement(int depth, int offset)
    {
        int proposed = elementCount + 1;
        if (proposed > limits.GetMaxAsn1ElementCount()) throw new EmailLimitException("MaxAsn1ElementCount", limits.GetMaxAsn1ElementCount(), proposed);
        if (depth > limits.GetMaxAsn1Depth()) throw new EmailLimitException("MaxAsn1Depth", limits.GetMaxAsn1Depth(), depth);
        elementCount = proposed;
        if (depth > maximumDepth) maximumDepth = depth;
        errorOffset = offset;
    }

    internal void SetErrorOffset(int value) { errorOffset = value; }
    internal int GetElementCount() { return elementCount; }
    internal int GetMaximumDepth() { return maximumDepth; }
    internal int GetErrorOffset() { return errorOffset; }
}
internal static class EmailCmsAsn1Scanner
{
    internal static EmailCmsScanResult Scan(byte[] bytes, AsnEncodingRules rules, EmailCryptographicLimits limits)
    {
        EmailCmsScanState state = new EmailCmsScanState(limits);
        List<EmailCmsAsn1Node> roots = new List<EmailCmsAsn1Node>();
        bool valid = ReadElements(bytes, 0, bytes.Length, rules, 1, roots, state);
        return new EmailCmsScanResult(valid, roots, state.GetElementCount(), state.GetMaximumDepth(), state.GetErrorOffset());
    }

    static bool ReadElements(byte[] bytes, int start, int length, AsnEncodingRules rules, int depth, List<EmailCmsAsn1Node> destination, EmailCmsScanState state)
    {
        int offset = start;
        int end = start + length;
        while (offset < end)
        {
            Asn1Tag tag = default(Asn1Tag);
            int contentOffset = 0;
            int contentLength = 0;
            int consumed = 0;
            EmailCapturedOperation captured = EmailOperationCapture.RunWithOperationUsingFunc(() => AsnDecoder.TryReadEncodedValue(new ReadOnlySpan<byte>(bytes, offset, end - offset), rules, out tag, out contentOffset, out contentLength, out consumed));
            if (!captured.IsSuccess())
            {
                state.SetErrorOffset(offset);
                return false;
            }
            bool read = (bool)(captured.GetValue() ?? throw new InvalidOperationException());
            if (!read || consumed < 1 || consumed > end - offset || contentOffset < 1 || contentLength < 0 || contentOffset > consumed || contentLength > consumed - contentOffset)
            {
                state.SetErrorOffset(offset);
                return false;
            }

            state.AddElement(depth, offset);
            List<EmailCmsAsn1Node> children = new List<EmailCmsAsn1Node>();
            if (tag.IsConstructed && !ReadElements(bytes, offset + contentOffset, contentLength, rules, depth + 1, children, state)) return false;
            bool indefinite = consumed > contentOffset + contentLength;
            EmailCmsAsn1Node node = new EmailCmsAsn1NodeOperations().CreateWithTagClassAndTagNumberAndConstructedAndIndefiniteLengthAndBytesAndEncodedOffsetAndEncodedLengthAndContentOffsetAndContentLengthAndChildren(TagClass(tag.TagClass), tag.TagValue, tag.IsConstructed, indefinite, bytes, offset, consumed, offset + contentOffset, contentLength, children);
            if (!ValidateNode(node, rules))
            {
                state.SetErrorOffset(offset);
                return false;
            }

            destination.Add(node);
            offset += consumed;
        }

        if (offset != end)
        {
            state.SetErrorOffset(offset);
            return false;
        }

        return true;
    }

    static bool ValidateNode(EmailCmsAsn1Node node, AsnEncodingRules rules)
    {
        if (node.GetTagClass() != EmailCmsAsn1TagClass.Universal) return true;
        int tag = node.GetTagNumber();
        if (tag == 0) return false;
        if (node.IsConstructed())
        {
            if (rules == AsnEncodingRules.DER && tag == 17) return IsSorted(node);
            return true;
        }

        byte[] content = node.GetContentBytes();
        if (tag == 1)
        {
            if (content.Length != 1) return false;
            return rules != AsnEncodingRules.DER || content[0] == 0 || content[0] == 255;
        }

        if (tag == 2)
        {
            if (content.Length == 0) return false;
            if (rules != AsnEncodingRules.DER || content.Length == 1) return true;
            if (content[0] == 0 && (content[1] & 128) == 0) return false;
            if (content[0] == 255 && (content[1] & 128) != 0) return false;
            return true;
        }

        if (tag == 3)
        {
            if (content.Length == 0 || content[0] > 7) return false;
            if (content.Length == 1) return content[0] == 0;
            int mask = (1 << content[0]) - 1;
            return (content[content.Length - 1] & mask) == 0;
        }

        if (tag == 5) return content.Length == 0;
        if (tag == 6 && rules == AsnEncodingRules.DER) return IsObjectIdentifierContent(content);
        return true;
    }

    static bool IsSorted(EmailCmsAsn1Node node)
    {
        int index = 1;
        while (index < node.GetChildCount())
        {
            if (Compare(node.GetChildAt(index - 1).GetEncodedBytes(), node.GetChildAt(index).GetEncodedBytes()) > 0) return false;
            index++;
        }

        return true;
    }

    static int Compare(byte[] left, byte[] right)
    {
        int length = Math.Min(left.Length, right.Length);
        int index = 0;
        while (index < length)
        {
            int comparison = left[index].CompareTo(right[index]);
            if (comparison != 0) return comparison;
            index++;
        }

        return left.Length.CompareTo(right.Length);
    }

    static bool IsObjectIdentifierContent(byte[] content)
    {
        if (content.Length == 0) return false;
        int index = 0;
        while (index < content.Length)
        {
            if (content[index] == 128) return false;
            while ((content[index] & 128) != 0)
            {
                index++;
                if (index >= content.Length) return false;
            }

            index++;
        }

        return true;
    }

    static EmailCmsAsn1TagClass TagClass(TagClass value)
    {
        if (value == System.Formats.Asn1.TagClass.Application) return EmailCmsAsn1TagClass.Application;
        if (value == System.Formats.Asn1.TagClass.ContextSpecific) return EmailCmsAsn1TagClass.ContextSpecific;
        if (value == System.Formats.Asn1.TagClass.Private) return EmailCmsAsn1TagClass.Private;
        return EmailCmsAsn1TagClass.Universal;
    }

}
internal class EmailCmsOctetCollector
{
    long maximumBytes;
    List<byte> bytes;
    bool valid;

    internal EmailCmsOctetCollector(long maximumBytes)
    {
        this.maximumBytes = maximumBytes;
        bytes = new List<byte>();
        valid = true;
    }

    internal void Add(EmailCmsAsn1Node node)
    {
        if (!valid) return;
        if (!node.HasTag(EmailCmsAsn1TagClass.Universal, 4))
        {
            valid = false;
            return;
        }

        if (!node.IsConstructed())
        {
            byte[] content = node.GetContentBytes();
            long observed = (long)bytes.Count + content.Length;
            if (observed > maximumBytes) throw new EmailLimitException("MaxCmsDataBytes", maximumBytes, observed);
            bytes.AddRange(content);
            return;
        }

        int index = 0;
        while (index < node.GetChildCount())
        {
            Add(node.GetChildAt(index));
            index++;
        }
    }

    internal bool IsValid() { return valid; }
    internal byte[] GetBytes() { return bytes.ToArray(); }
}
