using System.Text;

namespace xml;

internal class XPathAsciiCaseInsensitiveCollation : IXPathCollation
{
    internal const string Uri = "http://www.w3.org/2005/xpath-functions/collation/html-ascii-case-insensitive";

    internal XPathAsciiCaseInsensitiveCollation()
    {
    }

    public int CompareXPathCollation(string left, string right)
    {
        int limit = left.Length < right.Length ? left.Length : right.Length;
        int index = 0;
        while (index < limit)
        {
            char leftCharacter = FoldXPathAsciiCharacter(left[index]);
            char rightCharacter = FoldXPathAsciiCharacter(right[index]);
            if (leftCharacter != rightCharacter)
            {
                return leftCharacter < rightCharacter ? -1 : 1;
            }

            index = index + 1;
        }

        if (left.Length == right.Length)
        {
            return 0;
        }

        return left.Length < right.Length ? -1 : 1;
    }

    public byte[] CreateXPathCollationKey(string value)
    {
        StringBuilder folded = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            folded.Append(FoldXPathAsciiCharacter(value[index]));
            index = index + 1;
        }

        return Encoding.UTF8.GetBytes(folded.ToString());
    }

    internal char FoldXPathAsciiCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' ? (char)(value + 32) : value;
    }
}
