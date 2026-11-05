using System;
using System.Collections.Generic;
using System.Formats.Asn1;

namespace email;
internal class EmailCmsDerEncoder
{
    EmailCryptographicLimits limits;
    EmailIssueList issues;
    int elementCount;

    internal EmailCmsDerEncoder(EmailCryptographicLimits limits, EmailIssueList issues)
    {
        this.limits = limits;
        this.issues = issues;
        elementCount = 3;
    }

    internal byte[]? Encode(EmailCmsAsn1Node node)
    {
        return EncodeNode(node, 3);
    }

    byte[]? EncodeNode(EmailCmsAsn1Node node, int depth)
    {
        elementCount++;
        if (elementCount > limits.GetMaxAsn1ElementCount()) throw new EmailLimitException("MaxAsn1ElementCount", limits.GetMaxAsn1ElementCount(), elementCount);
        if (depth > limits.GetMaxAsn1Depth()) throw new EmailLimitException("MaxAsn1Depth", limits.GetMaxAsn1Depth(), depth);

        if (node.IsConstructed() && node.HasTag(EmailCmsAsn1TagClass.Universal, 4))
        {
            EmailCmsDerOctetCollector collector = new EmailCmsDerOctetCollector(limits.GetMaxInputBytes());
            collector.Add(node);
            if (!collector.IsValid())
            {
                AddFailure("A constructed OCTET STRING contains a non-OCTET STRING child and cannot be normalized to DER.");
                return null;
            }

            return EncodeValue(EmailCmsAsn1TagClass.Universal, 4, false, collector.GetBytes());
        }

        byte[]? content;
        if (node.IsConstructed())
        {
            List<byte[]> children = new List<byte[]>();
            int index = 0;
            while (index < node.GetChildCount())
            {
                byte[]? child = EncodeNode(node.GetChildAt(index), depth + 1);
                if (child == null) return null;
                children.Add(child);
                index++;
            }

            if (node.HasTag(EmailCmsAsn1TagClass.Universal, 17)) SortEncoded(children);
            content = Join(children);
        }
        else
        {
            content = CanonicalPrimitiveContent(node);
            if (content == null) return null;
        }

        byte[] encoded = EncodeValue(node.GetTagClass(), node.GetTagNumber(), node.IsConstructed(), content);
        if (encoded.LongLength > limits.GetMaxInputBytes()) throw new EmailLimitException("MaxCmsOutputBytes", limits.GetMaxInputBytes(), encoded.LongLength);
        return encoded;
    }

    byte[]? CanonicalPrimitiveContent(EmailCmsAsn1Node node)
    {
        byte[] content = node.GetContentBytes();
        if (node.GetTagClass() != EmailCmsAsn1TagClass.Universal) return content;
        int tag = node.GetTagNumber();
        if (tag == 1)
        {
            if (content.Length != 1)
            {
                AddFailure("A BOOLEAN value does not contain exactly one content octet.");
                return null;
            }

            return new byte[] { content[0] == 0 ? (byte)0 : (byte)255 };
        }

        if (tag == 2)
        {
            if (content.Length == 0)
            {
                AddFailure("An INTEGER value has empty content.");
                return null;
            }

            int start = 0;
            while (start + 1 < content.Length && content[start] == 0 && (content[start + 1] & 128) == 0) start++;
            while (start + 1 < content.Length && content[start] == 255 && (content[start + 1] & 128) != 0) start++;
            return Copy(content, start, content.Length - start);
        }

        if (tag == 5)
        {
            if (content.Length != 0)
            {
                AddFailure("A NULL value has non-empty content.");
                return null;
            }

            return content;
        }

        if (tag == 6 && !IsObjectIdentifierContent(content))
        {
            AddFailure("An OBJECT IDENTIFIER value has malformed or non-minimal content.");
            return null;
        }

        return content;
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

    byte[] EncodeValue(EmailCmsAsn1TagClass tagClass, int tagNumber, bool constructed, byte[] content)
    {
        List<byte> output = new List<byte>();
        int classBits = (int)tagClass << 6;
        int constructedBit = constructed ? 32 : 0;
        if (tagNumber < 31)
        {
            output.Add((byte)(classBits | constructedBit | tagNumber));
        }
        else
        {
            output.Add((byte)(classBits | constructedBit | 31));
            int[] groups = new int[6];
            int count = 0;
            int remaining = tagNumber;
            do
            {
                groups[count] = remaining & 127;
                count++;
                remaining >>= 7;
            }
            while (remaining > 0);

            int index = count - 1;
            while (index >= 0)
            {
                int value = groups[index];
                if (index > 0) value |= 128;
                output.Add((byte)value);
                index--;
            }
        }

        AddLength(output, content.Length);
        output.AddRange(content);
        return output.ToArray();
    }

    static void AddLength(List<byte> output, int length)
    {
        if (length < 128)
        {
            output.Add((byte)length);
            return;
        }

        byte[] bytes = new byte[4];
        int count = 0;
        int remaining = length;
        while (remaining > 0)
        {
            bytes[bytes.Length - 1 - count] = (byte)(remaining & 255);
            remaining >>= 8;
            count++;
        }

        output.Add((byte)(128 | count));
        int index = bytes.Length - count;
        while (index < bytes.Length)
        {
            output.Add(bytes[index]);
            index++;
        }
    }

    static byte[] Join(List<byte[]> values)
    {
        long length = 0;
        int index = 0;
        while (index < values.Count)
        {
            length += values[index].Length;
            if (length > int.MaxValue) throw new EmailLimitException("MaxCmsOutputBytes", int.MaxValue, length);
            index++;
        }

        byte[] output = new byte[(int)length];
        int offset = 0;
        index = 0;
        while (index < values.Count)
        {
            values[index].CopyTo(output, offset);
            offset += values[index].Length;
            index++;
        }

        return output;
    }

    static void SortEncoded(List<byte[]> values)
    {
        int outer = 0;
        while (outer < values.Count)
        {
            int inner = outer + 1;
            while (inner < values.Count)
            {
                if (Compare(values[outer], values[inner]) > 0)
                {
                    byte[] temporary = values[outer];
                    values[outer] = values[inner];
                    values[inner] = temporary;
                }

                inner++;
            }

            outer++;
        }
    }

    static int Compare(byte[] left, byte[] right)
    {
        int length = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < length)
        {
            if (left[index] < right[index]) return -1;
            if (left[index] > right[index]) return 1;
            index++;
        }

        if (left.Length < right.Length) return -1;
        return left.Length == right.Length ? 0 : 1;
    }

    static byte[] Copy(byte[] value, int offset, int length)
    {
        byte[] copy = new byte[length];
        new ReadOnlySpan<byte>(value, offset, length).CopyTo(copy);
        return copy;
    }

    void AddFailure(string description)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Error, EmailIssueCode.CmsDerEncodingFailed, description, null, null, null, "The CMS output was withheld rather than emitting invalid DER."));
    }
}
internal class EmailCmsDerOctetCollector
{
    long maximumBytes;
    List<byte> bytes;
    bool valid;

    internal EmailCmsDerOctetCollector(long maximumBytes)
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
