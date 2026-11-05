using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;

namespace gltf.Tests.Api.Data;

[TestFixture]
public class PublicDataModelContractTests
{
    private const int ExpectedDataModelCount = 131;
    private const int ExpectedPropertyJsonOwnerCount = 76;

    [Test]
    public void PublicDataModelInventory_ContainsEveryDataHolder()
    {
        Type[] types = GetDataModelTypes().ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(types, Has.Length.EqualTo(ExpectedDataModelCount));
            Assert.That(types.Select(type => type.Name), Is.Ordered);
            Assert.That(types, Has.All.Matches<Type>(type =>
                type.GetConstructor(Type.EmptyTypes) != null));
            Assert.That(GetPropertyJsonOwnerTypes().Count(),
                Is.EqualTo(ExpectedPropertyJsonOwnerCount));
        });
    }

    [TestCaseSource(nameof(GetDataModelTypes))]
    public void DataModel_DefaultsPresenceAndMutableOwnershipAreIndependent(Type type)
    {
        object first = Activator.CreateInstance(type)!;
        object second = Activator.CreateInstance(type)!;
        FieldInfo[] fields = GetPublicFields(type);

        Assert.Multiple(() =>
        {
            foreach (FieldInfo field in fields)
            {
                object? firstValue = field.GetValue(first);
                object? secondValue = field.GetValue(second);

                Assert.That(
                    DescribeDefault(firstValue),
                    Is.EqualTo(DescribeDefault(secondValue)),
                    $"{type.Name}.{field.Name} must have a deterministic default");

                if (field.FieldType == typeof(bool)
                    && field.Name.StartsWith("Has", StringComparison.Ordinal))
                {
                    Assert.That(
                        firstValue,
                        Is.False,
                        $"{type.Name}.{field.Name} must default to absent");
                    field.SetValue(first, true);
                    Assert.That(
                        field.GetValue(first),
                        Is.True,
                        $"{type.Name}.{field.Name} must independently track presence");
                    Assert.That(field.GetValue(second), Is.False);
                }

                AssertInitializedMutableFieldOwnership(
                    type,
                    field,
                    firstValue,
                    secondValue);
            }
        });
    }

    [TestCaseSource(nameof(GetPropertyJsonOwnerTypes))]
    public void PropertyJsonOwner_RetainsIndependentExtensionExtrasAndUnknownState(Type type)
    {
        object first = Activator.CreateInstance(type)!;
        object second = Activator.CreateInstance(type)!;
        FieldInfo field = type.GetField("PropertyJson")!;
        GltfPropertyJson firstJson = (GltfPropertyJson)field.GetValue(first)!;
        GltfPropertyJson secondJson = (GltfPropertyJson)field.GetValue(second)!;
        GltfTypedExtensionData typed = new GltfTypedExtensionData
        {
            MaterialsUnlit = new GltfMaterialsUnlitExtension()
        };

        firstJson.Extensions["EXT_model_test"] = new JsonObject
        {
            ["owner"] = type.Name
        };
        firstJson.TypedExtensions["KHR_materials_unlit"] = typed;
        firstJson.HasExtras = true;
        firstJson.Extras = new JsonArray(type.Name, 7);
        firstJson.UnknownMembers["future"] = new JsonObject { ["kept"] = true };
        firstJson.UnknownMembers["futureNull"] = null;

        Assert.Multiple(() =>
        {
            Assert.That(firstJson.Extensions["EXT_model_test"]!["owner"]!
                .GetValue<string>(), Is.EqualTo(type.Name));
            Assert.That(firstJson.TypedExtensions["KHR_materials_unlit"],
                Is.SameAs(typed));
            Assert.That(firstJson.HasExtras, Is.True);
            Assert.That(firstJson.Extras![0]!.GetValue<string>(), Is.EqualTo(type.Name));
            Assert.That(firstJson.UnknownMembers["future"]!["kept"]!
                .GetValue<bool>(), Is.True);
            Assert.That(firstJson.UnknownMembers.ContainsKey("futureNull"), Is.True);
            Assert.That(firstJson.UnknownMembers["futureNull"], Is.Null);
            Assert.That(secondJson.Extensions, Is.Empty);
            Assert.That(secondJson.TypedExtensions, Is.Empty);
            Assert.That(secondJson.HasExtras, Is.False);
            Assert.That(secondJson.Extras, Is.Null);
            Assert.That(secondJson.UnknownMembers, Is.Empty);
            Assert.That(firstJson, Is.Not.SameAs(secondJson));
        });
    }

    [Test]
    public void CloneAndMerge_DeepCopyJsonStateAcrossCompleteModelGraph()
    {
        GltfDocument source = CreateCompleteModelGraph();
        List<GltfPropertyJson> sourceProperties = FindPropertyJson(source);
        AddDistinctJsonState(sourceProperties);
        GltfEditOptions options = new GltfEditOptions
        {
            RequireSafeUnknownExtensionRemap = false,
            ValidateResult = false
        };

        GltfDocument clone = new GltfDocumentEditor().CloneDocument(source, options);
        GltfDocument merged = new GltfDocumentEditor().MergeDocument(
            [source],
            options).Document!;
        List<GltfPropertyJson> cloneProperties = FindPropertyJson(clone);
        List<GltfPropertyJson> mergedProperties = FindPropertyJson(merged);

        Assert.Multiple(() =>
        {
            Assert.That(sourceProperties, Has.Count.GreaterThanOrEqualTo(76));
            Assert.That(cloneProperties, Has.Count.EqualTo(sourceProperties.Count));
            Assert.That(mergedProperties, Has.Count.EqualTo(sourceProperties.Count));
            AssertJsonStateIsIndependent(sourceProperties, cloneProperties);
            AssertJsonStateIsIndependent(sourceProperties, mergedProperties);
            AssertMutableGraphIsIndependent(source, clone, false);
            AssertMutableGraphIsIndependent(source, merged, true);
        });

        MutateJsonState(cloneProperties, "clone");
        MutateJsonState(mergedProperties, "merge");

        Assert.Multiple(() =>
        {
            AssertOriginalJsonState(sourceProperties);
        });
    }

    private static IEnumerable<Type> GetDataModelTypes()
    {
        return typeof(GltfDocument).Assembly.GetExportedTypes()
            .Where(type => type.IsClass)
            .Where(type => !type.IsAbstract)
            .Where(type => type.BaseType == typeof(object))
            .Where(type => type.GetInterfaces().Length == 0)
            .Where(type => GetPublicFields(type).Length > 0)
            .OrderBy(type => type.Name);
    }

    private static IEnumerable<Type> GetPropertyJsonOwnerTypes()
    {
        return GetDataModelTypes().Where(type =>
        {
            FieldInfo? field = type.GetField("PropertyJson");
            return field != null && field.FieldType == typeof(GltfPropertyJson);
        });
    }

    private static FieldInfo[] GetPublicFields(Type type)
    {
        return type.GetFields(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(field => field.Name)
            .ToArray();
    }

    private static string DescribeDefault(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is string text)
        {
            return "string:" + text;
        }

        if (value.GetType().IsValueType)
        {
            return "value:" + value;
        }

        if (value is ICollection collection)
        {
            return value.GetType().FullName + ":count=" + collection.Count;
        }

        return "object:" + value.GetType().FullName;
    }

    private static void AssertInitializedMutableFieldOwnership(
        Type ownerType,
        FieldInfo field,
        object? firstValue,
        object? secondValue)
    {
        if (firstValue == null || secondValue == null || firstValue is string)
        {
            return;
        }

        Type valueType = firstValue.GetType();
        if (valueType.IsValueType)
        {
            return;
        }

        Array? array = firstValue as Array;
        if (array != null && array.Length == 0)
        {
            return;
        }

        Assert.That(
            firstValue,
            Is.Not.SameAs(secondValue),
            $"{ownerType.Name}.{field.Name} must not share mutable default state");

        if (firstValue is IDictionary dictionary)
        {
            Assert.That(
                dictionary,
                Is.Empty,
                $"{ownerType.Name}.{field.Name} must start empty");
        }
        else if (firstValue is IList list && array == null)
        {
            Assert.That(
                list,
                Is.Empty,
                $"{ownerType.Name}.{field.Name} must start empty");
        }
    }

    private static GltfDocument CreateCompleteModelGraph()
    {
        GltfDocument document = new GltfDocument();
        PopulateOwnedModels(document, new HashSet<object>(ReferenceEqualityComparer.Instance));

        GltfTypedExtensionData typed = new GltfTypedExtensionData();
        PopulateOwnedModels(typed, new HashSet<object>(ReferenceEqualityComparer.Instance));
        document.PropertyJson.TypedExtensions["EXT_complete_model_graph"] = typed;
        return document;
    }

    private static void PopulateOwnedModels(object value, HashSet<object> visited)
    {
        if (!visited.Add(value) || value is GltfPropertyJson || value is JsonNode)
        {
            return;
        }

        foreach (FieldInfo field in GetPublicFields(value.GetType()))
        {
            object? fieldValue = field.GetValue(value);
            Type fieldType = field.FieldType;
            if (fieldValue == null && IsDataModelType(fieldType))
            {
                fieldValue = Activator.CreateInstance(fieldType)!;
                field.SetValue(value, fieldValue);
            }

            if (fieldValue is IList list && !fieldType.IsArray)
            {
                Type? elementType = GetGenericArgument(fieldType, 0);
                if (elementType != null && IsDataModelType(elementType)
                    && list.Count == 0)
                {
                    object item = Activator.CreateInstance(elementType)!;
                    list.Add(item);
                    PopulateOwnedModels(item, visited);
                }

                continue;
            }

            if (fieldValue is IDictionary dictionary)
            {
                Type? keyType = GetGenericArgument(fieldType, 0);
                Type? valueType = GetGenericArgument(fieldType, 1);
                if (keyType == typeof(string) && valueType != null
                    && IsDataModelType(valueType) && dictionary.Count == 0)
                {
                    object item = Activator.CreateInstance(valueType)!;
                    dictionary.Add("model", item);
                    PopulateOwnedModels(item, visited);
                }

                continue;
            }

            if (fieldValue != null && IsDataModelType(fieldValue.GetType()))
            {
                PopulateOwnedModels(fieldValue, visited);
            }
        }
    }

    private static bool IsDataModelType(Type type)
    {
        return type.IsClass
            && !type.IsAbstract
            && type.BaseType == typeof(object)
            && type.GetInterfaces().Length == 0
            && GetPublicFields(type).Length > 0;
    }

    private static Type? GetGenericArgument(Type type, int index)
    {
        if (!type.IsGenericType)
        {
            return null;
        }

        Type[] arguments = type.GetGenericArguments();
        if (index >= arguments.Length)
        {
            return null;
        }

        return arguments[index];
    }

    private static List<GltfPropertyJson> FindPropertyJson(object root)
    {
        List<GltfPropertyJson> result = new List<GltfPropertyJson>();
        CollectPropertyJson(
            root,
            result,
            new HashSet<object>(ReferenceEqualityComparer.Instance));
        return result;
    }

    private static void CollectPropertyJson(
        object? value,
        List<GltfPropertyJson> result,
        HashSet<object> visited)
    {
        if (value == null || value is string || value is JsonNode
            || value.GetType().IsValueType || !visited.Add(value))
        {
            return;
        }

        if (value is GltfPropertyJson propertyJson)
        {
            result.Add(propertyJson);
            foreach (KeyValuePair<string, GltfTypedExtensionData> entry
                in propertyJson.TypedExtensions.OrderBy(entry => entry.Key))
            {
                CollectPropertyJson(entry.Value, result, visited);
            }

            return;
        }

        if (value is IDictionary dictionary)
        {
            List<DictionaryEntry> entries = new List<DictionaryEntry>();
            IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                entries.Add(enumerator.Entry);
            }

            entries.Sort((left, right) => StringComparer.Ordinal.Compare(
                left.Key?.ToString(),
                right.Key?.ToString()));
            foreach (DictionaryEntry entry in entries)
            {
                CollectPropertyJson(entry.Value, result, visited);
            }

            return;
        }

        if (value is IEnumerable enumerable)
        {
            foreach (object? item in enumerable)
            {
                CollectPropertyJson(item, result, visited);
            }

            return;
        }

        foreach (FieldInfo field in GetPublicFields(value.GetType()))
        {
            CollectPropertyJson(field.GetValue(value), result, visited);
        }
    }

    private static void AddDistinctJsonState(List<GltfPropertyJson> properties)
    {
        for (int index = 0; index < properties.Count; index++)
        {
            GltfPropertyJson property = properties[index];
            property.Extensions["EXT_model_state"] = new JsonObject
            {
                ["index"] = index
            };
            property.HasExtras = true;
            property.Extras = new JsonObject { ["index"] = index };
            property.UnknownMembers["future"] = new JsonObject { ["index"] = index };
            property.UnknownMembers["futureNull"] = null;
        }
    }

    private static void AssertJsonStateIsIndependent(
        List<GltfPropertyJson> source,
        List<GltfPropertyJson> copy)
    {
        for (int index = 0; index < source.Count; index++)
        {
            GltfPropertyJson sourceProperty = source[index];
            GltfPropertyJson copyProperty = copy[index];
            Assert.That(copyProperty, Is.Not.SameAs(sourceProperty));
            Assert.That(copyProperty.Extensions, Is.Not.SameAs(sourceProperty.Extensions));
            Assert.That(copyProperty.Extras, Is.Not.SameAs(sourceProperty.Extras));
            Assert.That(copyProperty.UnknownMembers,
                Is.Not.SameAs(sourceProperty.UnknownMembers));
            Assert.That(copyProperty.Extensions["EXT_model_state"],
                Is.Not.SameAs(sourceProperty.Extensions["EXT_model_state"]));
            Assert.That(copyProperty.UnknownMembers["future"],
                Is.Not.SameAs(sourceProperty.UnknownMembers["future"]));
            Assert.That(JsonNode.DeepEquals(
                copyProperty.Extensions["EXT_model_state"],
                sourceProperty.Extensions["EXT_model_state"]), Is.True);
            Assert.That(JsonNode.DeepEquals(copyProperty.Extras, sourceProperty.Extras),
                Is.True);
            Assert.That(JsonNode.DeepEquals(
                copyProperty.UnknownMembers["future"],
                sourceProperty.UnknownMembers["future"]), Is.True);
            Assert.That(copyProperty.UnknownMembers.ContainsKey("futureNull"), Is.True);
            Assert.That(copyProperty.UnknownMembers["futureNull"], Is.Null);
        }
    }

    private static void MutateJsonState(
        List<GltfPropertyJson> properties,
        string owner)
    {
        for (int index = 0; index < properties.Count; index++)
        {
            properties[index].Extensions["EXT_model_state"]!["index"] = -1;
            properties[index].Extras!["owner"] = owner;
            properties[index].UnknownMembers["future"]!["index"] = -1;
        }
    }

    private static void AssertOriginalJsonState(List<GltfPropertyJson> properties)
    {
        for (int index = 0; index < properties.Count; index++)
        {
            Assert.That(properties[index].Extensions["EXT_model_state"]!["index"]!
                .GetValue<int>(), Is.EqualTo(index));
            Assert.That(properties[index].Extras!["owner"], Is.Null);
            Assert.That(properties[index].UnknownMembers["future"]!["index"]!
                .GetValue<int>(), Is.EqualTo(index));
        }
    }

    private static void AssertMutableGraphIsIndependent(
        object source,
        object copy,
        bool mergedDocument)
    {
        AssertMutableValueIsIndependent(
            source,
            copy,
            new Dictionary<object, object>(ReferenceEqualityComparer.Instance),
            mergedDocument,
            "document");
    }

    private static void AssertMutableValueIsIndependent(
        object? source,
        object? copy,
        Dictionary<object, object> copies,
        bool mergedDocument,
        string path)
    {
        if (source == null || source is string || source.GetType().IsValueType)
        {
            return;
        }

        if (copy == null)
        {
            Assert.That(
                mergedDocument && path == "document.Container",
                Is.True,
                $"{path} must be retained");
            return;
        }

        if (copies.ContainsKey(source))
        {
            Assert.That(copy, Is.SameAs(copies[source]), $"{path} must preserve aliases");
            return;
        }

        copies[source] = copy;
        Assert.That(copy, Is.Not.SameAs(source), $"{path} must be independently owned");
        Assert.That(copy.GetType(), Is.EqualTo(source.GetType()),
            $"{path} must retain its runtime type");

        if (source is JsonNode sourceNode)
        {
            Assert.That(JsonNode.DeepEquals(sourceNode, (JsonNode)copy), Is.True,
                $"{path} must retain JSON content");
            return;
        }

        if (source is Array sourceArray)
        {
            Array copyArray = (Array)copy;
            Assert.That(copyArray.Length, Is.EqualTo(sourceArray.Length),
                $"{path} must retain array length");
            for (int index = 0; index < sourceArray.Length; index++)
            {
                AssertMutableValueIsIndependent(
                    sourceArray.GetValue(index),
                    copyArray.GetValue(index),
                    copies,
                    mergedDocument,
                    path + "[" + index + "]");
            }

            return;
        }

        if (source is IDictionary sourceDictionary)
        {
            IDictionary copyDictionary = (IDictionary)copy;
            Assert.That(copyDictionary.Count, Is.EqualTo(sourceDictionary.Count),
                $"{path} must retain dictionary entries");
            IDictionaryEnumerator enumerator = sourceDictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                object key = enumerator.Key;
                Assert.That(copyDictionary.Contains(key), Is.True,
                    $"{path} must retain key '{key}'");
                AssertMutableValueIsIndependent(
                    enumerator.Value,
                    copyDictionary[key],
                    copies,
                    mergedDocument,
                    path + "[" + key + "]");
            }

            return;
        }

        if (source is IList sourceList)
        {
            IList copyList = (IList)copy;
            Assert.That(copyList.Count, Is.EqualTo(sourceList.Count),
                $"{path} must retain list items");
            for (int index = 0; index < sourceList.Count; index++)
            {
                AssertMutableValueIsIndependent(
                    sourceList[index],
                    copyList[index],
                    copies,
                    mergedDocument,
                    path + "[" + index + "]");
            }

            return;
        }

        foreach (FieldInfo field in GetPublicFields(source.GetType()))
        {
            AssertMutableValueIsIndependent(
                field.GetValue(source),
                field.GetValue(copy),
                copies,
                mergedDocument,
                path + "." + field.Name);
        }
    }
}
