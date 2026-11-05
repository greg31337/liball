using System;
using System.Collections.Generic;
using System.Reflection;

namespace htmlgui;

internal class ModelNormalizer
{
    internal Dictionary<object, object> Copies;
    internal HashSet<object> CanonicalizedObjects;

    internal ModelNormalizer()
    {
        Copies = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        CanonicalizedObjects = new HashSet<object>(ReferenceEqualityComparer.Instance);
    }

    internal UiApplication NormalizeApplication(UiApplication? application)
    {
        Guard.ArgumentNotNull(application, "application");
        Copies.Clear();
        CanonicalizedObjects.Clear();
        object? copiedValue = CopyValue(application);
        if (copiedValue == null)
        {
            throw new InvalidOperationException("The application copy could not be created.");
        }

        UiApplication result = (UiApplication)copiedValue;
        ApplyHierarchyInheritance(result);
        CanonicalizeObject(result);
        return result;
    }

    internal object? CopyValue(object? value)
    {
        if (value == null)
        {
            return null;
        }

        Type type = value.GetType();
        if (IsImmutableType(type))
        {
            return value;
        }

        if (Copies.ContainsKey(value))
        {
            return Copies[value];
        }

        if (type.IsArray)
        {
            return CopyArray((Array)value, type);
        }

        object? copy = Activator.CreateInstance(type);
        if (copy == null)
        {
            throw new InvalidOperationException("A model value could not be constructed.");
        }

        Copies.Add(value, copy);
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        int index = 0;
        while (index < fields.Length)
        {
            FieldInfo field = fields[index];
            object? fieldValue = field.GetValue(value);
            if (fieldValue != null)
            {
                field.SetValue(copy, CopyValue(fieldValue));
            }

            index++;
        }

        return copy;
    }

    internal Array CopyArray(Array source, Type arrayType)
    {
        Type? elementType = arrayType.GetElementType();
        if (elementType == null)
        {
            throw new InvalidOperationException("The array element type is unavailable.");
        }

        Array copy = Array.CreateInstance(elementType, source.Length);
        Copies.Add(source, copy);
        int index = 0;
        while (index < source.Length)
        {
            copy.SetValue(CopyValue(source.GetValue(index)), index);
            index++;
        }

        return copy;
    }

    internal bool IsImmutableType(Type type)
    {
        if (type.IsEnum)
        {
            return true;
        }

        TypeCode code = Type.GetTypeCode(type);
        switch (code)
        {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.Char:
            case TypeCode.DateTime:
            case TypeCode.Decimal:
            case TypeCode.Double:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.SByte:
            case TypeCode.Single:
            case TypeCode.String:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    internal void ApplyHierarchyInheritance(UiApplication application)
    {
        UiWindow[] windows = application.Windows;
        int windowIndex = 0;
        while (windowIndex < windows.Length)
        {
            UiWindow? window = windows[windowIndex];
            if (window != null)
            {
                UiPage[] pages = window.Pages;
                int pageIndex = 0;
                while (pageIndex < pages.Length)
                {
                    UiPage? page = pages[pageIndex];
                    if (page != null)
                    {
                        ApplyPageInheritance(window.Document, page);
                    }

                    pageIndex++;
                }
            }

            windowIndex++;
        }
    }

    internal void ApplyPageInheritance(UiDocumentMetadata windowMetadata, UiPage page)
    {
        UiDocumentMetadata pageMetadata = page.Metadata;
        string title = FirstNonEmpty(page.Title, pageMetadata.Title, windowMetadata.Title);
        page.Title = title;
        pageMetadata.Title = title;

        string? language = FirstOptionalText(page.Language, pageMetadata.Language, windowMetadata.Language);
        page.Language = language;
        pageMetadata.Language = language;

        string? direction = FirstOptionalText(page.TextDirection, pageMetadata.TextDirection, windowMetadata.TextDirection);
        page.TextDirection = direction;
        pageMetadata.TextDirection = direction;

        pageMetadata.BaseUrl = InheritOptionalText(pageMetadata.BaseUrl, windowMetadata.BaseUrl);
        pageMetadata.Description = InheritOptionalText(pageMetadata.Description, windowMetadata.Description);
        pageMetadata.ApplicationName = InheritOptionalText(pageMetadata.ApplicationName, windowMetadata.ApplicationName);
        pageMetadata.ContentSecurityPolicyNonce = InheritOptionalText(pageMetadata.ContentSecurityPolicyNonce, windowMetadata.ContentSecurityPolicyNonce);
        if (pageMetadata.CharacterSet == "utf-8" && windowMetadata.CharacterSet != "utf-8" && windowMetadata.CharacterSet.Length > 0)
        {
            pageMetadata.CharacterSet = windowMetadata.CharacterSet;
        }

        if (pageMetadata.Viewport == "width=device-width, initial-scale=1" && windowMetadata.Viewport != "width=device-width, initial-scale=1" && windowMetadata.Viewport.Length > 0)
        {
            pageMetadata.Viewport = windowMetadata.Viewport;
        }

        pageMetadata.Values = MergeValueEntries(windowMetadata.Values, pageMetadata.Values);
    }

    internal string FirstNonEmpty(string? first, string? second, string? third)
    {
        if (first != null && first.Length > 0)
        {
            return first;
        }

        if (second != null && second.Length > 0)
        {
            return second;
        }

        if (third != null)
        {
            return third;
        }

        return string.Empty;
    }

    internal string? FirstOptionalText(string? first, string? second, string? third)
    {
        if (first != null && first.Length > 0)
        {
            return first;
        }

        if (second != null && second.Length > 0)
        {
            return second;
        }

        if (third != null && third.Length > 0)
        {
            return third;
        }

        return null;
    }

    internal string? InheritOptionalText(string? local, string? inherited)
    {
        if (local != null && local.Length > 0)
        {
            return local;
        }

        return inherited;
    }

    internal UiValueEntry[] MergeValueEntries(UiValueEntry[] inherited, UiValueEntry[] local)
    {
        List<UiValueEntry> result = new List<UiValueEntry>();
        int inheritedIndex = 0;
        while (inheritedIndex < inherited.Length)
        {
            UiValueEntry? entry = inherited[inheritedIndex];
            if (entry != null)
            {
                result.Add(CopyDetachedValueEntry(entry));
            }

            inheritedIndex++;
        }

        int localIndex = 0;
        while (localIndex < local.Length)
        {
            UiValueEntry? localEntry = local[localIndex];
            if (localEntry != null)
            {
                int existingIndex = FindValueEntry(result, localEntry.Key);
                UiValueEntry copiedEntry = CopyDetachedValueEntry(localEntry);
                if (existingIndex >= 0)
                {
                    result[existingIndex] = copiedEntry;
                }
                else
                {
                    result.Add(copiedEntry);
                }
            }

            localIndex++;
        }

        return result.ToArray();
    }

    internal UiValueEntry CopyDetachedValueEntry(UiValueEntry entry)
    {
        ModelNormalizer copier = new ModelNormalizer();
        object? copiedValue = copier.CopyValue(entry);
        if (copiedValue == null)
        {
            throw new InvalidOperationException("The metadata entry copy could not be created.");
        }

        return (UiValueEntry)copiedValue;
    }

    internal int FindValueEntry(List<UiValueEntry> entries, string key)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (entries[index].Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void CanonicalizeObject(object? value)
    {
        if (value == null)
        {
            return;
        }

        Type type = value.GetType();
        if (IsImmutableType(type))
        {
            return;
        }

        if (CanonicalizedObjects.Contains(value))
        {
            return;
        }

        CanonicalizedObjects.Add(value);
        if (type.IsArray)
        {
            CanonicalizeArrayItems((Array)value);
            return;
        }

        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        int index = 0;
        while (index < fields.Length)
        {
            FieldInfo field = fields[index];
            object? fieldValue = field.GetValue(value);
            if (fieldValue == null && field.FieldType.IsArray && type.Name != "UiValue")
            {
                Type? elementType = field.FieldType.GetElementType();
                if (elementType != null)
                {
                    fieldValue = Array.CreateInstance(elementType, 0);
                    field.SetValue(value, fieldValue);
                }
            }

            if (fieldValue != null && Type.GetTypeCode(field.FieldType) == TypeCode.String)
            {
                fieldValue = InvariantFormatter.NormalizeLineEndings((string)fieldValue);
                field.SetValue(value, fieldValue);
            }

            CanonicalizeObject(fieldValue);
            if (fieldValue != null && field.FieldType.IsArray)
            {
                CanonicalizeArrayOrder((Array)fieldValue, type.Name, field.Name);
            }

            index++;
        }
    }

    internal void CanonicalizeArrayItems(Array values)
    {
        int index = 0;
        while (index < values.Length)
        {
            object? value = values.GetValue(index);
            if (value != null && Type.GetTypeCode(value.GetType()) == TypeCode.String)
            {
                value = InvariantFormatter.NormalizeLineEndings((string)value);
                values.SetValue(value, index);
            }

            CanonicalizeObject(value);
            index++;
        }
    }

    internal void CanonicalizeArrayOrder(Array values, string ownerTypeName, string fieldName)
    {
        if (values.Length < 2)
        {
            return;
        }

        string? keyField = GetCanonicalKeyField(values, ownerTypeName, fieldName);
        if (keyField == null)
        {
            return;
        }

        int index = 1;
        while (index < values.Length)
        {
            object? current = values.GetValue(index);
            string currentKey = GetCanonicalKey(current, keyField);
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(GetCanonicalKey(values.GetValue(insertionIndex), keyField), currentKey) > 0)
            {
                values.SetValue(values.GetValue(insertionIndex), insertionIndex + 1);
                insertionIndex--;
            }

            values.SetValue(current, insertionIndex + 1);
            index++;
        }
    }

    internal string? GetCanonicalKeyField(Array values, string ownerTypeName, string fieldName)
    {
        Type? elementType = values.GetType().GetElementType();
        if (elementType == null)
        {
            return null;
        }

        if (elementType.Name == "UiValueEntry")
        {
            return "Key";
        }

        if (ownerTypeName == "UiApplication")
        {
            if (fieldName == "Windows")
            {
                return "Key";
            }

            if (fieldName == "Commands" || fieldName == "StateEntries" || fieldName == "Routes")
            {
                return "Id";
            }

            if (fieldName == "RouteIds" || fieldName == "CommandIds")
            {
                return "$value";
            }
        }

        if (ownerTypeName == "UiWindow" && fieldName == "Pages")
        {
            return "Key";
        }

        if (ownerTypeName == "UiWindow" && fieldName == "HostCapabilities")
        {
            return "$value";
        }

        if (ownerTypeName == "UiTransportCapabilities" && fieldName == "Capabilities")
        {
            return "Name";
        }

        if (ownerTypeName == "UiPage" && (fieldName == "Resources" || fieldName == "Styles"))
        {
            return "Key";
        }

        if (ownerTypeName == "UiTheme" && fieldName == "Variants")
        {
            return "Key";
        }

        if (ownerTypeName == "UiNode")
        {
            if (fieldName == "Slots")
            {
                return "Name";
            }

            if (fieldName == "EventBindings" || fieldName == "Bindings" || fieldName == "Announcements")
            {
                return "Id";
            }
        }

        if (ownerTypeName == "UiStyle")
        {
            if (fieldName == "Declarations")
            {
                return "Property";
            }

            if (fieldName == "Rules")
            {
                return "Selector";
            }
        }

        return null;
    }

    internal string GetCanonicalKey(object? value, string keyField)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (keyField == "$value")
        {
            return (string)value;
        }

        FieldInfo? field = value.GetType().GetField(keyField, BindingFlags.Instance | BindingFlags.Public);
        if (field == null)
        {
            return string.Empty;
        }

        object? key = field.GetValue(value);
        if (key == null)
        {
            return string.Empty;
        }

        return (string)key;
    }
}
