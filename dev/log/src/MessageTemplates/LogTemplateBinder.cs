using System;
using System.Collections.Generic;

namespace log;

internal class LogTemplateBinder
{
    internal LogField[] Bind(LogTemplate? template, object?[]? values)
    {
        if (template == null)
        {
            return new LogField[0];
        }

        LogTemplateToken[] placeholders = template.GetPlaceholders();
        int count = LogTemplateBinder.CountBindablePlaceholders(placeholders);
        LogField[] fields = new LogField[count];
        int fieldIndex = 0;
        int valueIndex = 0;

        foreach (LogTemplateToken placeholder in placeholders)
        {
            if (!placeholder.IsValid)
            {
                continue;
            }

            object? value = null;
            if (values != null && valueIndex < values.Length)
            {
                value = values[valueIndex];
            }

            fields[fieldIndex] = LogField.ValueField(placeholder.Name, LogTemplateBinder.ToLogValue(value));
            fieldIndex = fieldIndex + 1;
            valueIndex = valueIndex + 1;
        }

        return LogReservedFields.Resolve(fields);
    }

    internal static LogField[] BindTemplate(LogTemplate? template, object?[]? values)
    {
        LogTemplateBinder binder = new LogTemplateBinder();
        return binder.Bind(template, values);
    }

    internal static LogField[] BindParsedTemplate(string? template, object?[]? values)
    {
        LogTemplate parsedTemplate = LogTemplateParser.ParseTemplate(template);
        return LogTemplateBinder.BindTemplate(parsedTemplate, values);
    }

    internal static int CountBindablePlaceholders(LogTemplateToken[] placeholders)
    {
        if (placeholders == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogTemplateToken placeholder in placeholders)
        {
            if (placeholder != null && placeholder.IsValid)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static LogValue ToLogValue(object? value)
    {
        return LogTemplateBinder.ToLogValueLimit(value, LogValue.DefaultMaximumStringLength, LogValue.DefaultMaximumCollectionLength, LogValue.DefaultMaximumDepth);
    }

    internal static LogValue ToLogValueLimit(object? value, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        if (value == null)
        {
            return LogValue.Null();
        }

        LogValue? logValue = value as LogValue;
        if (logValue != null)
        {
            return logValue;
        }

        string? stringValue = value as string;
        if (stringValue != null)
        {
            return LogValue.StringLimit(stringValue, maximumStringLength);
        }

        Exception? exceptionValue = value as Exception;
        if (exceptionValue != null)
        {
            return LogValue.ExceptionLimit(exceptionValue, maximumStringLength);
        }

        LogValue[]? logValueArray = value as LogValue[];
        if (logValueArray != null)
        {
            return LogValue.ArrayLimit(logValueArray, maximumCollectionLength);
        }

        object?[]? objectArray = value as object[];
        if (objectArray != null)
        {
            if (maximumDepth <= 0)
            {
                return LogValue.ObjectTextLimit("[depth limit]", maximumStringLength);
            }

            return LogTemplateBinder.ToArrayValueLimit(objectArray, maximumStringLength, maximumCollectionLength, maximumDepth);
        }

        Dictionary<string, LogValue>? logValueDictionary = value as Dictionary<string, LogValue>;
        if (logValueDictionary != null)
        {
            return LogValue.DictionaryLimit(logValueDictionary, maximumCollectionLength);
        }

        Dictionary<string, object>? objectDictionary = value as Dictionary<string, object>;
        if (objectDictionary != null)
        {
            if (maximumDepth <= 0)
            {
                return LogValue.ObjectTextLimit("[depth limit]", maximumStringLength);
            }

            return LogTemplateBinder.ToDictionaryValueLimit(objectDictionary, maximumStringLength, maximumCollectionLength, maximumDepth);
        }

        string typeName = "";
        string? rawTypeName = value.GetType().FullName;
        if (rawTypeName != null)
        {
            typeName = rawTypeName;
        }
        if (typeName == "System.Boolean")
        {
            return LogValue.Bool((bool)value);
        }

        if (typeName == "System.Byte")
        {
            return LogValue.Integer((byte)value);
        }

        if (typeName == "System.Int16")
        {
            return LogValue.Integer((short)value);
        }

        if (typeName == "System.Int32")
        {
            return LogValue.Integer((int)value);
        }

        if (typeName == "System.Int64")
        {
            return LogValue.Integer((long)value);
        }

        if (typeName == "System.Decimal")
        {
            return LogValue.Decimal((decimal)value);
        }

        if (typeName == "System.Single")
        {
            return LogValue.FloatingPoint((float)value);
        }

        if (typeName == "System.Double")
        {
            return LogValue.FloatingPoint((double)value);
        }

        if (typeName == "System.DateTimeOffset")
        {
            return LogValue.DateTime((DateTimeOffset)value);
        }

        if (typeName == "System.DateTime")
        {
            DateTime dateTime = (DateTime)value;
            return LogValue.DateTime(new DateTimeOffset(dateTime.ToUniversalTime()));
        }

        if (typeName == "System.TimeSpan")
        {
            return LogValue.Duration((TimeSpan)value);
        }

        return LogValue.ObjectTextLimit(value, maximumStringLength);
    }

    internal static LogValue ToArrayValue(object?[]? values)
    {
        return LogTemplateBinder.ToArrayValueLimit(values, LogValue.DefaultMaximumStringLength, LogValue.DefaultMaximumCollectionLength, LogValue.DefaultMaximumDepth);
    }

    internal static LogValue ToArrayValueLimit(object?[]? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        if (values == null)
        {
            return LogValue.Null();
        }

        int limit = LogValue.NormalizeMaximumLength(maximumCollectionLength);
        int length = values.Length;
        if (length > limit)
        {
            length = limit;
        }

        LogValue[] converted = new LogValue[length];
        int index = 0;
        while (index < length)
        {
            converted[index] = LogTemplateBinder.ToLogValueLimit(values[index], maximumStringLength, maximumCollectionLength, maximumDepth - 1);
            index = index + 1;
        }

        LogValue result = LogValue.ArrayLimit(converted, maximumCollectionLength);
        result.SetLimitMetadata(length < values.Length, values.Length, length, limit, maximumDepth);
        return result;
    }

    internal static LogValue ToDictionaryValue(Dictionary<string, object>? values)
    {
        return LogTemplateBinder.ToDictionaryValueLimit(values, LogValue.DefaultMaximumStringLength, LogValue.DefaultMaximumCollectionLength, LogValue.DefaultMaximumDepth);
    }

    internal static LogValue ToDictionaryValueLimit(Dictionary<string, object>? values, int maximumStringLength, int maximumCollectionLength, int maximumDepth)
    {
        if (values == null)
        {
            return LogValue.Null();
        }

        int limit = LogValue.NormalizeMaximumLength(maximumCollectionLength);
        Dictionary<string, LogValue> converted = new Dictionary<string, LogValue>();
        int count = 0;
        foreach (KeyValuePair<string, object> entry in values)
        {
            if (count >= limit)
            {
                break;
            }

            if (entry.Key != null)
            {
                converted[entry.Key] = LogTemplateBinder.ToLogValueLimit(entry.Value, maximumStringLength, maximumCollectionLength, maximumDepth - 1);
                count = count + 1;
            }
        }

        LogValue result = LogValue.DictionaryLimit(converted, maximumCollectionLength);
        result.SetLimitMetadata(count < values.Count, values.Count, count, limit, maximumDepth);
        return result;
    }
}
