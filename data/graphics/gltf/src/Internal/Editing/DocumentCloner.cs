using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;

namespace gltf;

internal class DocumentCloner
{
    internal Dictionary<object, object> Clones =
        new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
    internal bool CloneJson;
    internal bool CloneResourceBytes;

    internal GltfDocument CloneDocumentData(
        GltfDocument document,
        GltfEditOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Clones.Clear();
        CloneJson = options.CloneJson;
        CloneResourceBytes = options.CloneResourceBytes;
        object? cloned = CloneValue(document);
        GltfDocument? result = cloned as GltfDocument;
        if (result == null)
        {
            throw new GltfUnsupportedFeatureException(
                "The document object graph could not be cloned.");
        }

        return result;
    }

    internal object? CloneValue(object? source)
    {
        if (source == null)
        {
            return null;
        }

        string? text = source as string;
        if (text != null)
        {
            return text;
        }

        Type type = source.GetType();
        if (type.IsValueType)
        {
            return source;
        }

        JsonNode? json = source as JsonNode;
        if (json != null)
        {
            if (!CloneJson)
            {
                return json;
            }

            JsonNode copiedJson = json.DeepClone();
            Clones[source] = copiedJson;
            return copiedJson;
        }

        if (Clones.ContainsKey(source))
        {
            return Clones[source];
        }

        Array? sourceArray = source as Array;
        if (sourceArray != null)
        {
            return CloneArray(sourceArray, type);
        }

        IDictionary? sourceDictionary = source as IDictionary;
        if (sourceDictionary != null)
        {
            return CloneDictionary(sourceDictionary, type);
        }

        IList? sourceList = source as IList;
        if (sourceList != null)
        {
            return CloneList(sourceList, type);
        }

        object? created = Activator.CreateInstance(type);
        if (created == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"Type '{type.FullName}' could not be created while cloning a document.");
        }

        Clones[source] = created;
        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            FieldInfo field = fields[fieldIndex];
            if (!field.IsStatic)
            {
                field.SetValue(created, CloneValue(field.GetValue(source)));
            }

            fieldIndex++;
        }

        return created;
    }

    internal object CloneArray(Array source, Type type)
    {
        Type? elementType = type.GetElementType();
        if (elementType == null || source.Rank != 1)
        {
            throw new GltfUnsupportedFeatureException(
                $"Array type '{type.FullName}' cannot be cloned.");
        }

        if (!CloneResourceBytes && elementType.FullName == "System.Byte")
        {
            return source;
        }

        Array result = Array.CreateInstance(elementType, source.Length);
        Clones[source] = result;
        int index = 0;
        while (index < source.Length)
        {
            result.SetValue(CloneValue(source.GetValue(index)), index);
            index++;
        }

        return result;
    }

    internal object CloneDictionary(IDictionary source, Type type)
    {
        object? created = Activator.CreateInstance(type);
        IDictionary? result = created as IDictionary;
        if (result == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"Dictionary type '{type.FullName}' could not be created while cloning a document.");
        }

        Clones[source] = result;
        foreach (DictionaryEntry entry in source)
        {
            object? key = CloneValue(entry.Key);
            if (key == null)
            {
                throw new GltfUnsupportedFeatureException(
                    "A document dictionary contains a null key.");
            }

            result.Add(key, CloneValue(entry.Value));
        }

        return result;
    }

    internal object CloneList(IList source, Type type)
    {
        object? created = Activator.CreateInstance(type);
        IList? result = created as IList;
        if (result == null)
        {
            throw new GltfUnsupportedFeatureException(
                $"List type '{type.FullName}' could not be created while cloning a document.");
        }

        Clones[source] = result;
        int index = 0;
        while (index < source.Count)
        {
            result.Add(CloneValue(source[index]));
            index++;
        }

        return result;
    }
}
