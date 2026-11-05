using System;
using System.Globalization;
using System.Text;

namespace template;

/// <summary>
/// Converts template values to text and applies the selected output escaping rules.
/// </summary>
internal static class TemplateOutputEncoder
{
    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string Encode(string text, TemplateEscapingMode mode)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (mode == TemplateEscapingMode.None)
        {
            return text;
        }

        if (mode == TemplateEscapingMode.Html)
        {
            return EncodeHtml(text);
        }

        if (mode == TemplateEscapingMode.Xml)
        {
            return EncodeXml(text);
        }

        if (mode == TemplateEscapingMode.JsonString)
        {
            return EncodeJsonString(text);
        }

        if (mode == TemplateEscapingMode.Url)
        {
            return EncodeUrl(text);
        }

        if (mode == TemplateEscapingMode.JavaScriptString)
        {
            return EncodeJavaScriptString(text);
        }

        if (mode == TemplateEscapingMode.CsvField)
        {
            return EncodeCsvField(text);
        }

        return text;
    }

    /// <summary>
    /// Escapes the supplied text or value and writes it through the quota-aware template writer.
    /// </summary>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="text">The text to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void WriteEncoded(TemplateWriter writer, string text, TemplateEscapingMode mode)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.Write(Encode(text, mode));
    }

    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    /// <returns>The resulting text.</returns>
    public static string EncodeValue(TemplateValue value, TemplateEscapingMode mode)
    {
        return EncodeValue(value, mode, false);
    }

    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    /// <param name="raw">Whether to bypass automatic escaping.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string EncodeValue(TemplateValue value, TemplateEscapingMode mode, bool raw)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string text = ConvertValueToText(value);
        if (raw || value.IsSafeString())
        {
            return text;
        }

        return Encode(text, mode);
    }

    /// <summary>
    /// Escapes the supplied text or value and writes it through the quota-aware template writer.
    /// </summary>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    public static void WriteEncodedValue(TemplateWriter writer, TemplateValue value, TemplateEscapingMode mode)
    {
        WriteEncodedValue(writer, value, mode, false);
    }

    /// <summary>
    /// Escapes the supplied text or value and writes it through the quota-aware template writer.
    /// </summary>
    /// <param name="writer">The destination that receives rendered text.</param>
    /// <param name="value">The value to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    /// <param name="raw">Whether to bypass automatic escaping.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void WriteEncodedValue(TemplateWriter writer, TemplateValue value, TemplateEscapingMode mode, bool raw)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.Write(EncodeValue(value, mode, raw));
    }

    /// <summary>
    /// Converts a template value to invariant text without applying output escaping.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string ConvertValueToText(TemplateValue value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.IsMissing() || value.IsNull())
        {
            return "";
        }

        if (value.IsStringLike())
        {
            return value.StringValue;
        }

        if (value.Kind == TemplateValueKind.Number)
        {
            return value.NumberValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            if (value.BooleanValue)
            {
                return "true";
            }

            return "false";
        }

        if (value.ObjectValue == TemplateExpressionEvaluator.LiquidEmptyMarker || value.ObjectValue == TemplateExpressionEvaluator.LiquidBlankMarker)
        {
            return "";
        }

        if (value.ObjectValue != null)
        {
            string? text = value.ObjectValue.ToString();
            if (text != null)
            {
                return text;
            }
        }

        return "";
    }

    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string EncodeHtml(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '&')
            {
                builder.Append("&amp;");
            }
            else if (current == '<')
            {
                builder.Append("&lt;");
            }
            else if (current == '>')
            {
                builder.Append("&gt;");
            }
            else if (current == '"')
            {
                builder.Append("&quot;");
            }
            else if (current == '\'')
            {
                builder.Append("&#39;");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string EncodeHandlebarsHtml(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '&')
            {
                builder.Append("&amp;");
            }
            else if (current == '<')
            {
                builder.Append("&lt;");
            }
            else if (current == '>')
            {
                builder.Append("&gt;");
            }
            else if (current == '"')
            {
                builder.Append("&quot;");
            }
            else if (current == '\'')
            {
                builder.Append("&#x27;");
            }
            else if (current == '`')
            {
                builder.Append("&#x60;");
            }
            else if (current == '=')
            {
                builder.Append("&#x3D;");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string EncodeXml(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (char.IsHighSurrogate(current))
            {
                if (index + 1 >= text.Length || !char.IsLowSurrogate(text[index + 1]))
                {
                    throw new InvalidOperationException("XML output contains an invalid Unicode surrogate.");
                }

                builder.Append(current);
                builder.Append(text[index + 1]);
                index = index + 2;
                continue;
            }

            if (char.IsLowSurrogate(current) || !IsValidXmlCharacter(current))
            {
                throw new InvalidOperationException("XML output contains a character that is not permitted by XML 1.0.");
            }

            if (current == '&')
            {
                builder.Append("&amp;");
            }
            else if (current == '<')
            {
                builder.Append("&lt;");
            }
            else if (current == '>')
            {
                builder.Append("&gt;");
            }
            else if (current == '"')
            {
                builder.Append("&quot;");
            }
            else if (current == '\'')
            {
                builder.Append("&apos;");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string EncodeJsonString(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            AppendJsonEscapedCharacter(builder, current);
            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string EncodeJavaScriptString(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '\'')
            {
                builder.Append("\\'");
            }
            else if (current == '<')
            {
                builder.Append("\\u003C");
            }
            else if (current == '>')
            {
                builder.Append("\\u003E");
            }
            else if (current == '&')
            {
                builder.Append("\\u0026");
            }
            else if (current == '=')
            {
                builder.Append("\\u003D");
            }
            else if (current == '-')
            {
                builder.Append("\\u002D");
            }
            else if ((int)current == 8232)
            {
                builder.Append("\\u2028");
            }
            else if ((int)current == 8233)
            {
                builder.Append("\\u2029");
            }
            else
            {
                AppendJsonEscapedCharacter(builder, current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string EncodeUrl(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return Uri.EscapeDataString(text);
    }

    /// <summary>
    /// Escapes the supplied text or value according to the requested output format.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string EncodeCsvField(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (!RequiresCsvQuotes(text))
        {
            return text;
        }

        StringBuilder builder = new StringBuilder();
        builder.Append('"');
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '"')
            {
                builder.Append("\"\"");
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        builder.Append('"');
        return builder.ToString();
    }

    internal static void AppendJsonEscapedCharacter(StringBuilder builder, char current)
    {
        if (current == '"')
        {
            builder.Append("\\\"");
        }
        else if (current == '\\')
        {
            builder.Append("\\\\");
        }
        else if (current == '\b')
        {
            builder.Append("\\b");
        }
        else if (current == '\f')
        {
            builder.Append("\\f");
        }
        else if (current == '\n')
        {
            builder.Append("\\n");
        }
        else if (current == '\r')
        {
            builder.Append("\\r");
        }
        else if (current == '\t')
        {
            builder.Append("\\t");
        }
        else if (current < ' ' || char.IsSurrogate(current))
        {
            builder.Append(ToUnicodeEscape(current));
        }
        else
        {
            builder.Append(current);
        }
    }

    internal static bool RequiresCsvQuotes(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == ',' || current == '"' || current == '\r' || current == '\n')
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool IsValidXmlCharacter(char value)
    {
        int number = (int)value;
        return number == 9
            || number == 10
            || number == 13
            || (number >= 32 && number <= 55295)
            || (number >= 57344 && number <= 65533);
    }

    internal static string ToUnicodeEscape(char value)
    {
        return "\\u" + ((int)value).ToString("X4", CultureInfo.InvariantCulture);
    }
}
