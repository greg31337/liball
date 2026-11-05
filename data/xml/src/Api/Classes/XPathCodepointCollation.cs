namespace xml;

/// <summary>Provides the built-in Unicode codepoint collation.</summary>
public class XPathCodepointCollation : IXPathCollation
{
    /// <summary>Creates a Unicode codepoint collation.</summary>
    public XPathCodepointCollation()
    {
    }

    /// <summary>Compares strings by ordinal codepoint order.</summary>
    public int CompareXPathCollation(string left, string right)
    {
        if (left == null)
        {
            throw new System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new System.ArgumentNullException("right");
        }

        int leftIndex = 0;
        int rightIndex = 0;
        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            int leftScalar = ReadXPathCollationScalar(left, leftIndex);
            int rightScalar = ReadXPathCollationScalar(right, rightIndex);
            if (leftScalar != rightScalar)
            {
                return leftScalar < rightScalar ? -1 : 1;
            }

            leftIndex = leftIndex + (leftScalar > 65535 ? 2 : 1);
            rightIndex = rightIndex + (rightScalar > 65535 ? 2 : 1);
        }

        if (leftIndex == left.Length && rightIndex == right.Length)
        {
            return 0;
        }

        return leftIndex == left.Length ? -1 : 1;
    }

    /// <summary>Creates a stable UTF-8 codepoint collation key.</summary>
    public byte[] CreateXPathCollationKey(string value)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException("value");
        }

        return System.Text.Encoding.UTF8.GetBytes(value);
    }

    internal int ReadXPathCollationScalar(string value, int index)
    {
        int first = value[index];
        if (first >= 55296 && first <= 56319 && index + 1 < value.Length)
        {
            int second = value[index + 1];
            if (second >= 56320 && second <= 57343)
            {
                return 65536 + (first - 55296) * 1024 + second - 56320;
            }
        }

        return first;
    }
}
