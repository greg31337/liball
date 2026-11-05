using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;

namespace gltf;

internal class PropertyJsonRetention
{
    internal HashSet<object> Visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
    internal bool RemovedExtensions;
    internal bool RemovedExtras;

    internal void ApplyRetention(
        GltfDocument document,
        bool keepExtensions,
        bool keepExtras)
    {
        Visited.Clear();
        RemovedExtensions = false;
        RemovedExtras = false;
        ApplyObjectRetention(document, keepExtensions, keepExtras);
        if (!keepExtensions)
        {
            document.ExtensionsUsed.Clear();
            document.ExtensionsRequired.Clear();
        }
    }

    internal void ApplyObjectRetention(
        object? value,
        bool keepExtensions,
        bool keepExtras)
    {
        if (value == null)
        {
            return;
        }

        if (value as string != null
            || value as JsonNode != null
            || value.GetType().IsValueType
            || Visited.Contains(value))
        {
            return;
        }

        Visited.Add(value);
        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                ApplyObjectRetention(entry.Value, keepExtensions, keepExtras);
            }

            return;
        }

        IList? list = value as IList;
        if (list != null)
        {
            int itemIndex = 0;
            while (itemIndex < list.Count)
            {
                ApplyObjectRetention(list[itemIndex], keepExtensions, keepExtras);
                itemIndex++;
            }

            return;
        }

        FieldInfo[] fields = value.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            FieldInfo field = fields[fieldIndex];
            if (!field.IsStatic)
            {
                object? fieldValue = field.GetValue(value);
                GltfPropertyJson? propertyJson = fieldValue as GltfPropertyJson;
                if (propertyJson != null)
                {
                    ApplyPropertyRetention(propertyJson, keepExtensions, keepExtras);
                }

                ApplyObjectRetention(fieldValue, keepExtensions, keepExtras);
            }

            fieldIndex++;
        }
    }

    internal void ApplyPropertyRetention(
        GltfPropertyJson propertyJson,
        bool keepExtensions,
        bool keepExtras)
    {
        if (!keepExtensions)
        {
            if (propertyJson.Extensions.Count > 0
                || propertyJson.TypedExtensions.Count > 0)
            {
                RemovedExtensions = true;
            }

            propertyJson.Extensions.Clear();
            propertyJson.TypedExtensions.Clear();
        }

        if (!keepExtras)
        {
            if (propertyJson.HasExtras || propertyJson.Extras != null)
            {
                RemovedExtras = true;
            }

            propertyJson.HasExtras = false;
            propertyJson.Extras = null;
        }
    }
}
