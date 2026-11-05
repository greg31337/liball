using System.Globalization;

namespace docx;

internal class DocxXmlAttributes
{
    internal static string ReadString(DocxXmlReader reader, string localName, string namespaceUri, string defaultValue)
    {
        ValidateReaderAndDefault(reader, defaultValue);

        string value = reader.GetAttribute(localName, namespaceUri);

        if (value == "")
        {
            return defaultValue;
        }

        return value;
    }

    internal static int ReadInt(DocxXmlReader reader, string localName, string namespaceUri, int defaultValue)
    {
        string value = ReadString(reader, localName, namespaceUri, "");

        if (value == "")
        {
            return defaultValue;
        }

        int result = 0;

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result))
        {
            throw new FormatException("Attribute value is not a valid integer.");
        }

        return result;
    }

    internal static bool ReadBool(DocxXmlReader reader, string localName, string namespaceUri, bool defaultValue)
    {
        string value = ReadString(reader, localName, namespaceUri, "");

        if (value == "")
        {
            return defaultValue;
        }

        return ParseBool(value);
    }

    internal static string ReadColor(DocxXmlReader reader, string localName, string namespaceUri, string defaultValue)
    {
        ValidateReaderAndDefault(reader, defaultValue);

        string value = reader.GetAttribute(localName, namespaceUri);

        if (value == "")
        {
            if (defaultValue == "")
            {
                return "";
            }

            return NormalizeColor(defaultValue);
        }

        return NormalizeColor(value);
    }

    internal static int ReadMeasurement(DocxXmlReader reader, string localName, string namespaceUri, int defaultValue)
    {
        return ReadInt(reader, localName, namespaceUri, defaultValue);
    }

    internal static void WriteString(DocxXmlWriter writer, string prefix, string localName, string namespaceUri, string value)
    {
        ValidateWriterAndValue(writer, value);
        writer.WriteAttribute(prefix, localName, namespaceUri, value);
    }

    internal static void WriteInt(DocxXmlWriter writer, string prefix, string localName, string namespaceUri, int value)
    {
        ValidateWriter(writer);
        writer.WriteAttribute(prefix, localName, namespaceUri, value.ToString(CultureInfo.InvariantCulture));
    }

    internal static void WriteBool(DocxXmlWriter writer, string prefix, string localName, string namespaceUri, bool value)
    {
        ValidateWriter(writer);

        if (value)
        {
            writer.WriteAttribute(prefix, localName, namespaceUri, "1");
        }
        else
        {
            writer.WriteAttribute(prefix, localName, namespaceUri, "0");
        }
    }

    internal static void WriteColor(DocxXmlWriter writer, string prefix, string localName, string namespaceUri, string value)
    {
        ValidateWriterAndValue(writer, value);
        writer.WriteAttribute(prefix, localName, namespaceUri, NormalizeColor(value));
    }

    internal static void WriteMeasurement(DocxXmlWriter writer, string prefix, string localName, string namespaceUri, int value)
    {
        WriteInt(writer, prefix, localName, namespaceUri, value);
    }

    internal static bool ParseBool(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || value == "1"
            || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
            || value == "0"
            || string.Equals(value, "off", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        throw new FormatException("Attribute value is not a valid boolean.");
    }

    internal static string NormalizeColor(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (string.Equals(value, "auto", StringComparison.OrdinalIgnoreCase))
        {
            return "auto";
        }

        string color = value;

        if (color.StartsWith("#"))
        {
            color = color.Substring(1);
        }

        if (color.Length != 6)
        {
            throw new ArgumentException("Color must be 'auto' or a six digit RGB hex value.", "value");
        }

        char[] normalized = new char[6];

        for (int index = 0; index < color.Length; index = index + 1)
        {
            char digit = color[index];

            if (!IsHexDigit(digit))
            {
                throw new ArgumentException("Color must contain only hexadecimal digits.", "value");
            }

            normalized[index] = NormalizeHexDigit(digit);
        }

        return new string(normalized);
    }

    internal static bool IsHexDigit(char value)
    {
        return (value >= '0' && value <= '9')
            || (value >= 'a' && value <= 'f')
            || (value >= 'A' && value <= 'F');
    }

    internal static char NormalizeHexDigit(char value)
    {
        if (value >= 'a' && value <= 'f')
        {
            return (char)(value - 32);
        }

        return value;
    }

    internal static void ValidateReaderAndDefault(DocxXmlReader reader, string defaultValue)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (defaultValue == null)
        {
            throw new ArgumentNullException("defaultValue");
        }
    }

    internal static void ValidateWriter(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }
    }

    internal static void ValidateWriterAndValue(DocxXmlWriter writer, string value)
    {
        ValidateWriter(writer);

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }
    }
}
