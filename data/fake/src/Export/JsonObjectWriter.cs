using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace fake;

internal class JsonOutputBuffer
{
    StringBuilder Builder;
    int MaximumOutputSize;

    internal JsonOutputBuffer(int maximumOutputSize)
    {
        Builder = new StringBuilder();
        MaximumOutputSize = maximumOutputSize;
    }

    internal void Append(char value)
    {
        Guard.OutputSize(
            (long)Builder.Length + 1L,
            MaximumOutputSize,
            "json");
        Builder.Append(value);
    }

    internal void AppendFromValue(string value)
    {
        Guard.OutputSize(
            (long)Builder.Length + value.Length,
            MaximumOutputSize,
            "json");
        Builder.Append(value);
    }

    internal string Result()
    {
        return Builder.ToString();
    }
}

internal class JsonObjectWriter
{
    const int MaximumDepth = 128;
    GenerationContext Context;
    JsonOutputBuffer Output;
    HashSet<object> Active;

    internal JsonObjectWriter(GenerationContext context)
    {
        Context = context;
        Output = new JsonOutputBuffer(
            context.CapturedOptions.MaximumOutputSize);
        Active = new HashSet<object>(ReferenceEqualityComparer.Instance);
    }

    internal string Write(object? value)
    {
        WriteValue(value, 0);
        return Output.Result();
    }

    void WriteValue(object? value, int depth)
    {
        if (depth > MaximumDepth)
            throw FakeException.Create(
                "JSON export exceeds the maximum depth of " +
                MaximumDepth + ".");
        if (value == null)
        {
            Output.AppendFromValue("null");
            return;
        }
        Type type = value.GetType();
        if (type.IsEnum)
        {
            WriteString(value.ToString() ?? string.Empty);
            return;
        }
        TypeCode code = Type.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.Boolean:
                Output.AppendFromValue((bool)value ? "true" : "false");
                return;
            case TypeCode.Char:
                WriteString(((char)value).ToString());
                return;
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Decimal:
                Output.AppendFromValue(Convert.ToString(
                    value,
                    CultureInfo.InvariantCulture) ?? string.Empty);
                return;
            case TypeCode.Single:
                WriteSingle((float)value);
                return;
            case TypeCode.Double:
                WriteDouble((double)value);
                return;
            case TypeCode.DateTime:
                WriteString(((DateTime)value).ToString(
                    "O",
                    CultureInfo.InvariantCulture));
                return;
            case TypeCode.String:
                WriteString((string)value);
                return;
        }
        if (type.FullName == "System.DateTimeOffset")
        {
            WriteString(((DateTimeOffset)value).ToString(
                "O",
                CultureInfo.InvariantCulture));
            return;
        }
        if (type.FullName == "System.DateOnly")
        {
            WriteString(((DateOnly)value).ToString(
                "O",
                CultureInfo.InvariantCulture));
            return;
        }
        if (type.FullName == "System.TimeOnly")
        {
            WriteString(((TimeOnly)value).ToString(
                "O",
                CultureInfo.InvariantCulture));
            return;
        }
        if (type.FullName == "System.TimeSpan")
        {
            WriteString(((TimeSpan)value).ToString(
                "c",
                CultureInfo.InvariantCulture));
            return;
        }
        if (type.FullName == "System.Guid")
        {
            WriteString(((Guid)value).ToString("D"));
            return;
        }
        if (type.FullName == "System.Uri")
        {
            WriteString(((Uri)value).OriginalString);
            return;
        }
        bool tracked = false;
        if (!type.IsValueType)
        {
            if (!Active.Add(value))
                throw FakeException.Create(
                    "JSON export encountered an object cycle.");
            tracked = true;
        }
        if (type.IsArray)
        {
            WriteArray((Array)value, depth);
        }
        else if (type.GetInterface("System.Collections.IDictionary") != null)
        {
            WriteDictionary((IDictionary)value, depth);
        }
        else if (type.GetInterface("System.Collections.IList") != null)
        {
            WriteList((IList)value, depth);
        }
        else
        {
            WriteFields(value, type, depth);
        }
        if (tracked)
            Active.Remove(value);
    }

    void WriteSingle(float value)
    {
        if (Single.IsNaN(value) || Single.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "JSON numbers must be finite.");
        Output.AppendFromValue(value.ToString("R", CultureInfo.InvariantCulture));
    }

    void WriteDouble(double value)
    {
        if (Double.IsNaN(value) || Double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "JSON numbers must be finite.");
        Output.AppendFromValue(value.ToString("R", CultureInfo.InvariantCulture));
    }

    void WriteString(string value)
    {
        Guard.OutputSize(
            (long)value.Length + 2L,
            Context.CapturedOptions.MaximumOutputSize,
            "json");
        UnicodeText.Validate(value, "value");
        Output.AppendFromValue(JsonSerializer.Serialize(value));
    }

    void WriteArray(Array values, int depth)
    {
        if (values.Rank != 1)
            throw FakeException.Create(
                "JSON export supports only one-dimensional arrays.");
        Guard.Count(
            values.Length,
            Context.CapturedOptions.MaximumCount,
            "value");
        Output.Append('[');
        int lowerBound = values.GetLowerBound(0);
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
                Output.Append(',');
            WriteValue(values.GetValue(lowerBound + index), depth + 1);
            index = index + 1;
        }
        Output.Append(']');
    }

    void WriteList(IList values, int depth)
    {
        Guard.Count(
            values.Count,
            Context.CapturedOptions.MaximumCount,
            "value");
        Output.Append('[');
        int index = 0;
        while (index < values.Count)
        {
            if (index > 0)
                Output.Append(',');
            WriteValue(values[index], depth + 1);
            index = index + 1;
        }
        Output.Append(']');
    }

    void WriteDictionary(IDictionary values, int depth)
    {
        Guard.Count(
            values.Count,
            Context.CapturedOptions.MaximumCount,
            "value");
        Output.Append('{');
        IDictionaryEnumerator enumerator = values.GetEnumerator();
        int index = 0;
        while (enumerator.MoveNext())
        {
            object key = enumerator.Key;
            if (key == null || Type.GetTypeCode(key.GetType()) != TypeCode.String)
                throw FakeException.Create(
                    "JSON object dictionary keys must be strings.");
            if (index > 0)
                Output.Append(',');
            WriteString((string)key);
            Output.Append(':');
            WriteValue(enumerator.Value, depth + 1);
            index = index + 1;
        }
        Output.Append('}');
    }

    void WriteFields(object value, Type type, int depth)
    {
        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance | BindingFlags.Public);
        Guard.Count(
            fields.Length,
            Context.CapturedOptions.MaximumCount,
            "value");
        SortFields(fields);
        Output.Append('{');
        int index = 0;
        while (index < fields.Length)
        {
            if (index > 0)
                Output.Append(',');
            WriteString(fields[index].Name);
            Output.Append(':');
            WriteValue(fields[index].GetValue(value), depth + 1);
            index = index + 1;
        }
        Output.Append('}');
    }

    static void SortFields(FieldInfo[] fields)
    {
        int index = 1;
        while (index < fields.Length)
        {
            FieldInfo selected = fields[index];
            int position = index;
            while (position > 0 && String.CompareOrdinal(
                fields[position - 1].Name,
                selected.Name) > 0)
            {
                fields[position] = fields[position - 1];
                position = position - 1;
            }
            fields[position] = selected;
            index = index + 1;
        }
    }
}
