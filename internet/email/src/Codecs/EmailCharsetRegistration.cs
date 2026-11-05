using System.Text;

namespace email;

internal class EmailCharsetRegistration
{
    string canonicalName;
    Encoding encoding;

    internal EmailCharsetRegistration(string canonicalName, Encoding encoding)
    {
        this.canonicalName = canonicalName;
        this.encoding = (Encoding)encoding.Clone();
    }

    internal string GetCanonicalName()
    {
        return canonicalName;
    }

    internal Encoding GetEncoding()
    {
        return (Encoding)encoding.Clone();
    }
}
