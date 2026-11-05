using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml;

namespace fake;

internal class XmlOutputBuffer
{
    StringBuilder Builder;
    int MaximumOutputSize;

    internal XmlOutputBuffer(int maximumOutputSize)
    {
        Builder = new StringBuilder();
        MaximumOutputSize = maximumOutputSize;
    }

    internal void Append(char value)
    {
        Guard.OutputSize(
            (long)Builder.Length + 1L,
            MaximumOutputSize,
            "xml");
        Builder.Append(value);
    }

    internal void AppendFromValue(string value)
    {
        Guard.OutputSize(
            (long)Builder.Length + value.Length,
            MaximumOutputSize,
            "xml");
        Builder.Append(value);
    }

    internal string Result()
    {
        return Builder.ToString();
    }
}

internal class XmlObjectWriter
{
    const int MaximumDepth = 128;
    GenerationContext Context;
    XmlOutputBuffer Output;
    HashSet<object> Active;

    internal XmlObjectWriter(GenerationContext context)
    {
        Context = context;
        Output = new XmlOutputBuffer(
            context.CapturedOptions.MaximumOutputSize);
        Active = new HashSet<object>(ReferenceEqualityComparer.Instance);
    }

    internal string Write(object? value, string? rootName)
    {
        string name = rootName ?? DefaultRootName(value);
        if (rootName != null)
            XmlConvert.VerifyNCName(name);
        Output.AppendFromValue("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        WriteElement(name, value, 0);
        return Output.Result();
    }

    string DefaultRootName(object? value)
    {
        if (value == null)
            return "Value";
        Type type = value.GetType();
        if (CsvValueFormatter.IsScalar(type))
            return "Value";
        if (type.IsArray ||
            type.GetInterface("System.Collections.IList") != null)
            return "Array";
        if (type.GetInterface("System.Collections.IDictionary") != null)
            return "Object";
        return XmlConvert.EncodeLocalName(type.Name);
    }

    void WriteElement(string name, object? value, int depth)
    {
        if (depth > MaximumDepth)
            throw FakeException.Create(
                "XML export exceeds the maximum depth of " +
                MaximumDepth + ".");
        Output.Append('<');
        Output.AppendFromValue(name);
        Output.Append('>');
        if (value != null)
        {
            Type type = value.GetType();
            if (CsvValueFormatter.IsScalar(type))
            {
                WriteText(XmlScalar(value));
            }
            else
            {
                WriteComplex(value, type, depth);
            }
        }
        Output.AppendFromValue("</");
        Output.AppendFromValue(name);
        Output.Append('>');
    }

    void WriteComplex(object value, Type type, int depth)
    {
        if (!type.IsValueType && !Active.Add(value))
            throw FakeException.Create(
                "XML export encountered an object cycle.");
        bool tracked = !type.IsValueType;
        if (type.IsArray)
            WriteArray((Array)value, depth);
        else if (type.GetInterface("System.Collections.IDictionary") != null)
            WriteDictionary((IDictionary)value, depth);
        else if (type.GetInterface("System.Collections.IList") != null)
            WriteList((IList)value, depth);
        else
            WriteFields(value, type, depth);
        if (tracked)
            Active.Remove(value);
    }

    void WriteArray(Array values, int depth)
    {
        if (values.Rank != 1)
            throw FakeException.Create(
                "XML export supports only one-dimensional arrays.");
        Guard.Count(
            values.Length,
            Context.CapturedOptions.MaximumCount,
            "value");
        int lowerBound = values.GetLowerBound(0);
        int index = 0;
        while (index < values.Length)
        {
            WriteElement(
                "Item",
                values.GetValue(lowerBound + index),
                depth + 1);
            index = index + 1;
        }
    }

    void WriteList(IList values, int depth)
    {
        Guard.Count(
            values.Count,
            Context.CapturedOptions.MaximumCount,
            "value");
        int index = 0;
        while (index < values.Count)
        {
            WriteElement("Item", values[index], depth + 1);
            index = index + 1;
        }
    }

    void WriteDictionary(IDictionary values, int depth)
    {
        Guard.Count(
            values.Count,
            Context.CapturedOptions.MaximumCount,
            "value");
        IDictionaryEnumerator enumerator = values.GetEnumerator();
        while (enumerator.MoveNext())
        {
            object key = enumerator.Key;
            if (key == null || Type.GetTypeCode(key.GetType()) != TypeCode.String)
                throw FakeException.Create(
                    "XML object dictionary keys must be strings.");
            string name = EncodeName((string)key);
            WriteElement(name, enumerator.Value, depth + 1);
        }
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
        int index = 0;
        while (index < fields.Length)
        {
            WriteElement(
                XmlConvert.EncodeLocalName(fields[index].Name),
                fields[index].GetValue(value),
                depth + 1);
            index = index + 1;
        }
    }

    void WriteText(string value)
    {
        UnicodeText.Validate(value, "value");
        XmlConvert.VerifyXmlChars(value);
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '&')
                Output.AppendFromValue("&amp;");
            else if (character == '<')
                Output.AppendFromValue("&lt;");
            else if (character == '>')
                Output.AppendFromValue("&gt;");
            else if (character == '"')
                Output.AppendFromValue("&quot;");
            else if (character == '\'')
                Output.AppendFromValue("&apos;");
            else if (character == '\r')
                Output.AppendFromValue("&#xD;");
            else
                Output.Append(character);
            index = index + 1;
        }
    }

    static string XmlScalar(object value)
    {
        Type type = value.GetType();
        if (type.IsEnum)
            return value.ToString() ?? string.Empty;
        TypeCode code = Type.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.String:
                return (string)value;
            case TypeCode.Char:
                return ((char)value).ToString();
            case TypeCode.Boolean:
                return (bool)value ? "true" : "false";
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Decimal:
                return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            case TypeCode.Single:
                return SingleValue((float)value);
            case TypeCode.Double:
                return DoubleValue((double)value);
            case TypeCode.DateTime:
                return ((DateTime)value).ToString(
                    "O",
                    CultureInfo.InvariantCulture);
        }
        string? name = type.FullName;
        if (name == "System.DateTimeOffset")
            return ((DateTimeOffset)value).ToString(
                "O",
                CultureInfo.InvariantCulture);
        if (name == "System.DateOnly")
            return ((DateOnly)value).ToString(
                "O",
                CultureInfo.InvariantCulture);
        if (name == "System.TimeOnly")
            return ((TimeOnly)value).ToString(
                "O",
                CultureInfo.InvariantCulture);
        if (name == "System.TimeSpan")
            return ((TimeSpan)value).ToString(
                "c",
                CultureInfo.InvariantCulture);
        if (name == "System.Guid")
            return ((Guid)value).ToString("D");
        return ((Uri)value).OriginalString;
    }

    static string SingleValue(float value)
    {
        if (Single.IsNaN(value) || Single.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "XML numbers must be finite.");
        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    static string DoubleValue(double value)
    {
        if (Double.IsNaN(value) || Double.IsInfinity(value))
            throw new ArgumentOutOfRangeException(
                "value",
                value,
                "XML numbers must be finite.");
        return value.ToString("R", CultureInfo.InvariantCulture);
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

    static string EncodeName(string value)
    {
        if (value.Length == 0)
            return "_x0000_";
        return XmlConvert.EncodeLocalName(value);
    }
}
