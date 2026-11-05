using System.Text;

namespace docx;

internal class DocxMarkupCompatibility
{
    internal List<string> IgnorablePrefixes;
    internal List<DocxXmlBuffer> AlternateContents;

    internal DocxMarkupCompatibility()
    {
        this.IgnorablePrefixes = new List<string>();
        this.AlternateContents = new List<DocxXmlBuffer>();
    }

    internal static bool IsAlternateContent(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("AlternateContent", DocxConstants.NamespaceMarkupCompatibility);
    }

    internal static DocxXmlBuffer CaptureAlternateContent(DocxXmlReader reader)
    {
        if (!IsAlternateContent(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on mc:AlternateContent.");
        }

        return DocxXmlBuffer.Capture(reader);
    }

    internal static List<string> ReadIgnorablePrefixes(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("Ignorable", DocxConstants.NamespaceMarkupCompatibility);
        return ParsePrefixList(value);
    }

    internal static List<string> ParsePrefixList(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxMarkupCompatibility compatibility = new DocxMarkupCompatibility();
        int prefixStart = -1;

        for (int index = 0; index <= value.Length; index = index + 1)
        {
            bool atEnd = index == value.Length;
            bool isSeparator = false;

            if (!atEnd)
            {
                char character = value[index];
                isSeparator = character == ' ' || character == '\t' || character == '\r' || character == '\n';
            }

            if (atEnd || isSeparator)
            {
                if (prefixStart >= 0)
                {
                    string prefix = value.Substring(prefixStart, index - prefixStart);
                    compatibility.AddIgnorablePrefix(prefix);
                    prefixStart = -1;
                }
            }
            else if (prefixStart < 0)
            {
                prefixStart = index;
            }
        }

        return compatibility.IgnorablePrefixes;
    }

    internal static void WriteIgnorablePrefixes(DocxXmlWriter writer, List<string> prefixes)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (prefixes == null)
        {
            throw new ArgumentNullException("prefixes");
        }

        if (prefixes.Count == 0)
        {
            return;
        }

        writer.WriteAttribute("mc", "Ignorable", DocxConstants.NamespaceMarkupCompatibility, JoinPrefixes(prefixes));
    }

    internal static string JoinPrefixes(List<string> prefixes)
    {
        if (prefixes == null)
        {
            throw new ArgumentNullException("prefixes");
        }

        StringBuilder builder = new StringBuilder();

        foreach (string prefix in prefixes)
        {
            ValidatePrefix(prefix);

            if (builder.Length > 0)
            {
                builder.Append(" ");
            }

            builder.Append(prefix);
        }

        return builder.ToString();
    }

    internal void AddIgnorablePrefix(string prefix)
    {
        ValidatePrefix(prefix);

        if (!HasIgnorablePrefix(prefix))
        {
            this.IgnorablePrefixes.Add(prefix);
        }
    }

    internal bool HasIgnorablePrefix(string prefix)
    {
        ValidatePrefix(prefix);

        foreach (string existingPrefix in this.IgnorablePrefixes)
        {
            if (existingPrefix == prefix)
            {
                return true;
            }
        }

        return false;
    }

    internal int IgnorablePrefixCount()
    {
        return this.IgnorablePrefixes.Count;
    }

    internal void AddAlternateContent(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        this.AlternateContents.Add(buffer);
    }

    internal int AlternateContentCount()
    {
        return this.AlternateContents.Count;
    }

    internal void WriteAlternateContents(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxXmlBuffer buffer in this.AlternateContents)
        {
            buffer.WriteTo(writer);
        }
    }

    internal static void ValidatePrefix(string prefix)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (prefix == "")
        {
            throw new ArgumentException("Prefix is required.", "prefix");
        }

        for (int index = 0; index < prefix.Length; index = index + 1)
        {
            char character = prefix[index];

            if (character == ' ' || character == '\t' || character == '\r' || character == '\n')
            {
                throw new ArgumentException("Prefix cannot contain whitespace.", "prefix");
            }
        }
    }
}
