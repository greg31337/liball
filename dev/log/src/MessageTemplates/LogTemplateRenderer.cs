using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace log;

internal class LogTemplateRenderer
{
    internal string Render(LogTemplate template, object?[]? values)
    {
        LogField[] fields = LogTemplateBinder.BindTemplate(template, values);
        return RenderFields(template, fields);
    }

    internal static string RenderTemplate(LogTemplate template, object?[]? values)
    {
        LogTemplateRenderer renderer = new LogTemplateRenderer();
        return renderer.Render(template, values);
    }

    internal static string RenderParsedTemplate(string template, object?[]? values)
    {
        LogTemplate parsedTemplate = LogTemplateParser.ParseTemplate(template);
        return LogTemplateRenderer.RenderTemplate(parsedTemplate, values);
    }

    internal string RenderFields(LogTemplate template, LogField[] fields)
    {
        if (template == null)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        int placeholderIndex = 0;

        foreach (LogTemplateToken token in template.Tokens)
        {
            if (token == null)
            {
                continue;
            }

            if (token.IsLiteral)
            {
                builder.Append(token.Text);
                continue;
            }

            if (token.IsPlaceholder)
            {
                if (token.IsValid)
                {
                    LogField? field = LogTemplateRenderer.FindFieldForPlaceholder(fields, placeholderIndex, token.Name);
                    if (field == null)
                    {
                        builder.Append(LogTemplateRenderer.ReconstructPlaceholder(token));
                    }
                    else
                    {
                        builder.Append(LogTemplateRenderer.FormatValue(field.Value));
                    }
                }
                else
                {
                    builder.Append(LogTemplateRenderer.ReconstructPlaceholder(token));
                }

                placeholderIndex = placeholderIndex + 1;
            }
        }

        return builder.ToString();
    }

    internal static LogField? FindFieldForPlaceholder(LogField[]? fields, int position, string name)
    {
        if (fields == null)
        {
            return null;
        }

        if (position >= 0 && position < fields.Length)
        {
            LogField fieldAtPosition = fields[position];
            if (fieldAtPosition != null && fieldAtPosition.IsValid && LogTemplateRenderer.FieldMatchesPlaceholder(fieldAtPosition, name))
            {
                return fieldAtPosition;
            }
        }

        foreach (LogField field in fields)
        {
            if (field != null && field.IsValid && LogTemplateRenderer.FieldMatchesPlaceholder(field, name))
            {
                return field;
            }
        }

        return null;
    }

    internal static bool FieldMatchesPlaceholder(LogField field, string name)
    {
        if (field == null)
        {
            return false;
        }

        if (field.Name == name)
        {
            return true;
        }

        if (LogTemplateRenderer.FieldNameMatchesPrefix(field.Name, name))
        {
            return true;
        }

        string reservedName = LogReservedFields.ReservedFieldName(name);
        if (field.Name == reservedName)
        {
            return true;
        }

        return LogTemplateRenderer.FieldNameMatchesPrefix(field.Name, reservedName);
    }

    internal static bool FieldNameMatchesPrefix(string fieldName, string name)
    {
        string prefix = name + "_";
        if (!fieldName.StartsWith(prefix))
        {
            return false;
        }

        if (fieldName.Length == prefix.Length)
        {
            return false;
        }

        int index = prefix.Length;
        while (index < fieldName.Length)
        {
            if (!char.IsDigit(fieldName[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string FormatValue(LogValue value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value.Kind == LogValueKind.Null)
        {
            return "null";
        }

        if (value.Kind == LogValueKind.String)
        {
            return value.TextValue;
        }

        if (value.Kind == LogValueKind.Bool)
        {
            return value.BoolValue.ToString();
        }

        if (value.Kind == LogValueKind.Integer)
        {
            return value.IntegerValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.Kind == LogValueKind.Decimal)
        {
            return value.DecimalValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.Kind == LogValueKind.FloatingPoint)
        {
            return value.FloatingPointValue.ToString(CultureInfo.InvariantCulture);
        }

        if (value.Kind == LogValueKind.DateTime)
        {
            return value.DateTimeValue.ToString("O", CultureInfo.InvariantCulture);
        }

        if (value.Kind == LogValueKind.Duration)
        {
            return value.DurationValue.ToString();
        }

        if (value.Kind == LogValueKind.Exception)
        {
            return value.TextValue;
        }

        if (value.Kind == LogValueKind.ObjectText)
        {
            return value.TextValue;
        }

        if (value.Kind == LogValueKind.Array)
        {
            return LogTemplateRenderer.FormatArray(value.ArrayValue);
        }

        if (value.Kind == LogValueKind.Dictionary)
        {
            return LogTemplateRenderer.FormatDictionary(value.DictionaryValue);
        }

        if (value.Kind == LogValueKind.Raw)
        {
            return value.RawValue;
        }

        return "";
    }

    internal static string FormatArray(LogValue[] values)
    {
        if (values == null)
        {
            return "[]";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("[");
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(LogTemplateRenderer.FormatValue(values[index]));
            index = index + 1;
        }

        builder.Append("]");
        return builder.ToString();
    }

    internal static string FormatDictionary(Dictionary<string, LogValue> values)
    {
        if (values == null)
        {
            return "{}";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("{");
        int index = 0;
        foreach (KeyValuePair<string, LogValue> entry in values)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append(entry.Key);
            builder.Append(": ");
            builder.Append(LogTemplateRenderer.FormatValue(entry.Value));
            index = index + 1;
        }

        builder.Append("}");
        return builder.ToString();
    }

    internal static string ReconstructPlaceholder(LogTemplateToken token)
    {
        if (token == null)
        {
            return "";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append("{");
        if (token.IsDestructured)
        {
            builder.Append("@");
        }

        if (token.IsStringified)
        {
            builder.Append("$");
        }

        builder.Append(token.Name);
        if (token.HasFormat)
        {
            builder.Append(":");
            builder.Append(token.Format);
        }

        builder.Append("}");
        return builder.ToString();
    }
}
