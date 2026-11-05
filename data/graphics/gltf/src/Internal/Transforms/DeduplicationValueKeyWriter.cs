using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace gltf;

internal class DeduplicationKeyEntry
{
    internal string Key = "";
    internal object? Value;
}

internal class DeduplicationValueKeyWriter
{
    internal DeterministicJsonWriter JsonWriter = new DeterministicJsonWriter();
    internal GltfResourceSet? Resources;
    internal HashSet<object> Active = new HashSet<object>(
        ReferenceEqualityComparer.Instance);

    internal string CreateKey(object? value)
    {
        Active.Clear();
        StringBuilder result = new StringBuilder();
        AppendValue(result, value, false);
        return result.ToString();
    }

    internal string CreateExactKey(object? value)
    {
        Active.Clear();
        StringBuilder result = new StringBuilder();
        AppendValue(result, value, true);
        return result.ToString();
    }

    internal void AppendValue(StringBuilder result, object? value, bool exactBytes)
    {
        if (value == null)
        {
            result.Append('N');
            return;
        }

        string? text = value as string;
        if (text != null)
        {
            AppendText(result, "S", text);
            return;
        }

        JsonNode? json = value as JsonNode;
        if (json != null)
        {
            AppendText(result, "J", JsonWriter.WriteCanonicalValue(json));
            return;
        }

        byte[]? bytes = value as byte[];
        if (bytes != null)
        {
            AppendBytes(result, bytes, exactBytes);
            return;
        }

        Type type = value.GetType();
        if (type.IsEnum)
        {
            AppendText(
                result,
                "E" + GetTypeName(type),
                Convert.ToInt64(value, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture));
            return;
        }

        TypeCode code = Type.GetTypeCode(type);
        if (code != TypeCode.Object)
        {
            AppendPrimitive(result, value, code);
            return;
        }

        if (Active.Contains(value))
        {
            AppendText(result, "C", GetTypeName(type));
            return;
        }

        Active.Add(value);
        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            AppendDictionary(result, dictionary, exactBytes);
            Active.Remove(value);
            return;
        }

        IList? list = value as IList;
        if (list != null)
        {
            AppendList(result, list, exactBytes);
            Active.Remove(value);
            return;
        }

        AppendObject(result, value, type, exactBytes);
        Active.Remove(value);
    }

    internal void AppendBytes(StringBuilder result, byte[] value, bool exactBytes)
    {
        string content;
        if (exactBytes)
        {
            content = Convert.ToBase64String(value);
        }
        else
        {
            content = Convert.ToHexString(SHA256.HashData(value));
        }

        AppendText(result, exactBytes ? "BX" : "BH", value.Length + ":" + content);
    }

    internal void AppendPrimitive(StringBuilder result, object value, TypeCode code)
    {
        string content;
        if (code == TypeCode.Double)
        {
            double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            content = number == 0.0
                ? "0"
                : number.ToString("R", CultureInfo.InvariantCulture);
        }
        else if (code == TypeCode.Single)
        {
            float number = Convert.ToSingle(value, CultureInfo.InvariantCulture);
            content = number == 0.0f
                ? "0"
                : number.ToString("R", CultureInfo.InvariantCulture);
        }
        else if (code == TypeCode.Decimal)
        {
            decimal number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            content = number == 0m
                ? "0"
                : number.ToString("G29", CultureInfo.InvariantCulture);
        }
        else
        {
            content = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        }

        AppendText(result, "P" + ((int)code).ToString(CultureInfo.InvariantCulture), content);
    }

    internal void AppendDictionary(
        StringBuilder result,
        IDictionary value,
        bool exactBytes)
    {
        List<DeduplicationKeyEntry> entries = new List<DeduplicationKeyEntry>();
        foreach (DictionaryEntry item in value)
        {
            DeduplicationKeyEntry entry = new DeduplicationKeyEntry();
            StringBuilder key = new StringBuilder();
            AppendValue(key, item.Key, exactBytes);
            entry.Key = key.ToString();
            entry.Value = item.Value;
            InsertEntry(entries, entry);
        }

        result.Append('D');
        result.Append(entries.Count.ToString(CultureInfo.InvariantCulture));
        result.Append('{');
        int index = 0;
        while (index < entries.Count)
        {
            AppendText(result, "K", entries[index].Key);
            AppendValue(result, entries[index].Value, exactBytes);
            index++;
        }

        result.Append('}');
    }

    internal void InsertEntry(
        List<DeduplicationKeyEntry> entries,
        DeduplicationKeyEntry entry)
    {
        int index = entries.Count;
        entries.Add(entry);
        while (index > 0
            && string.CompareOrdinal(entries[index - 1].Key, entry.Key) > 0)
        {
            entries[index] = entries[index - 1];
            index--;
        }

        entries[index] = entry;
    }

    internal void AppendList(StringBuilder result, IList value, bool exactBytes)
    {
        result.Append('L');
        result.Append(value.Count.ToString(CultureInfo.InvariantCulture));
        result.Append('[');
        int index = 0;
        while (index < value.Count)
        {
            AppendValue(result, value[index], exactBytes);
            index++;
        }

        result.Append(']');
    }

    internal void AppendObject(
        StringBuilder result,
        object value,
        Type type,
        bool exactBytes)
    {
        List<FieldInfo> fields = GetSortedFields(type);
        AppendText(result, "O", GetTypeName(type));
        result.Append(fields.Count.ToString(CultureInfo.InvariantCulture));
        result.Append('{');
        int index = 0;
        while (index < fields.Count)
        {
            FieldInfo field = fields[index];
            AppendText(result, "F", field.Name);
            if (IsBoundResourceField(value, field))
            {
                AppendBoundResource(result, value, exactBytes);
            }
            else
            {
                AppendValue(result, field.GetValue(value), exactBytes);
            }

            index++;
        }

        result.Append('}');
    }

    internal bool IsBoundResourceField(object value, FieldInfo field)
    {
        if (field.Name != "Resource")
        {
            return false;
        }

        GltfBuffer? buffer = value as GltfBuffer;
        if (buffer != null)
        {
            return buffer.HasResource;
        }

        GltfImage? image = value as GltfImage;
        return image != null && image.HasResource;
    }

    internal void AppendBoundResource(
        StringBuilder result,
        object value,
        bool exactBytes)
    {
        int resourceIndex;
        GltfBuffer? buffer = value as GltfBuffer;
        if (buffer != null)
        {
            resourceIndex = buffer.Resource;
        }
        else
        {
            GltfImage? image = value as GltfImage;
            resourceIndex = image == null
                ? GltfConstants.AbsentIndex
                : image.Resource;
        }

        if (Resources != null
            && resourceIndex >= 0
            && resourceIndex < Resources.Resources.Count)
        {
            AppendText(result, "BR", "value");
            AppendValue(result, Resources.Resources[resourceIndex], exactBytes);
        }
        else
        {
            AppendText(
                result,
                "BR",
                resourceIndex.ToString(CultureInfo.InvariantCulture));
        }
    }

    internal List<FieldInfo> GetSortedFields(Type type)
    {
        FieldInfo[] source = type.GetFields(
            BindingFlags.Instance | BindingFlags.Public);
        List<FieldInfo> result = new List<FieldInfo>();
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            FieldInfo field = source[sourceIndex];
            int index = result.Count;
            result.Add(field);
            while (index > 0
                && string.CompareOrdinal(result[index - 1].Name, field.Name) > 0)
            {
                result[index] = result[index - 1];
                index--;
            }

            result[index] = field;
            sourceIndex++;
        }

        return result;
    }

    internal string GetTypeName(Type type)
    {
        return type.FullName ?? type.Name;
    }

    internal void AppendText(StringBuilder result, string prefix, string value)
    {
        result.Append(prefix);
        result.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        result.Append(':');
        result.Append(value);
    }
}
