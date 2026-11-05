using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace jmap.Tests;

public class PublicDataDtoTests
{
    [TestCaseSource(nameof(DataDtoCases))]
    public void EveryDataDtoConstructsWithIndependentMutableDefaults(Type dtoType)
    {
        object first = CreateDto(dtoType);
        object second = CreateDto(dtoType);

        foreach (FieldInfo field in DataFields(dtoType))
        {
            object? firstValue = field.GetValue(first);
            object? secondValue = field.GetValue(second);

            if (firstValue is ICollection firstCollection)
            {
                Assert.That(firstCollection, Is.Empty, dtoType.Name + "." + field.Name);
                Assert.That(secondValue, Is.Not.SameAs(firstValue),
                    dtoType.Name + "." + field.Name);
            }
            else if (firstValue != null && IsMutableDataObject(firstValue.GetType()))
            {
                Assert.That(secondValue, Is.Not.SameAs(firstValue),
                    dtoType.Name + "." + field.Name);
            }
        }
    }

    [TestCaseSource(nameof(DataDtoCases))]
    public void EveryDataDtoFieldStoresAnAssignedValueWithoutChangingAnotherInstance(Type dtoType)
    {
        object first = CreateDto(dtoType);
        object second = CreateDto(dtoType);

        foreach (FieldInfo field in DataFields(dtoType))
        {
            object? secondValue = field.GetValue(second);
            object assigned = DifferentValue(field.FieldType, field.GetValue(first));

            field.SetValue(first, assigned);

            if (field.FieldType.IsValueType)
            {
                Assert.That(field.GetValue(first), Is.EqualTo(assigned),
                    dtoType.Name + "." + field.Name);
            }
            else
            {
                Assert.That(field.GetValue(first), Is.SameAs(assigned),
                    dtoType.Name + "." + field.Name);
            }

            Assert.That(field.GetValue(second), Is.EqualTo(secondValue),
                dtoType.Name + "." + field.Name + " leaked to another instance");

            if (!field.FieldType.IsValueType && secondValue == null)
            {
                field.SetValue(first, null);
                Assert.That(field.GetValue(first), Is.Null,
                    dtoType.Name + "." + field.Name + " must retain explicit null");
            }
        }
    }

    [TestCaseSource(nameof(OptionalScalarCases))]
    public void OptionalScalarFlagsRepresentMissingPresentAndExplicitNull(
        Type dtoType,
        string presenceName,
        string valueName,
        string? nullName)
    {
        object dto = CreateDto(dtoType);
        FieldInfo presence = RequireField(dtoType, presenceName);
        FieldInfo value = RequireField(dtoType, valueName);

        Assert.That(presence.GetValue(dto), Is.False, dtoType.Name + "." + presenceName);

        object assigned = DifferentValue(value.FieldType, value.GetValue(dto));
        value.SetValue(dto, assigned);
        presence.SetValue(dto, true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(presence.GetValue(dto), Is.True);
            Assert.That(value.GetValue(dto), Is.EqualTo(assigned));
        }

        if (nullName != null)
        {
            FieldInfo nullFlag = RequireField(dtoType, nullName);
            Assert.That(nullFlag.GetValue(dto), Is.False, dtoType.Name + "." + nullName);
            nullFlag.SetValue(dto, true);
            Assert.That(nullFlag.GetValue(dto), Is.True, dtoType.Name + "." + nullName);
        }
        else if (!value.FieldType.IsValueType)
        {
            value.SetValue(dto, null);
            Assert.That(value.GetValue(dto), Is.Null, dtoType.Name + "." + valueName);
            Assert.That(presence.GetValue(dto), Is.True, dtoType.Name + "." + presenceName);
        }
    }

    [TestCaseSource(nameof(UnknownPropertyCases))]
    public void EveryUnknownPropertyMapStoresNullAndStructuredExtensionJson(Type dtoType)
    {
        object first = CreateDto(dtoType);
        object second = CreateDto(dtoType);
        Dictionary<string, JsonElement> firstUnknown =
            (Dictionary<string, JsonElement>)RequireField(dtoType, "UnknownProperties").GetValue(first)!;
        Dictionary<string, JsonElement> secondUnknown =
            (Dictionary<string, JsonElement>)RequireField(dtoType, "UnknownProperties").GetValue(second)!;

        firstUnknown.Add("vendorNull", PublicApiTestHelper.Json("null"));
        firstUnknown.Add("vendorObject", PublicApiTestHelper.Json("{\"enabled\":true}"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(firstUnknown["vendorNull"].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(firstUnknown["vendorObject"].GetProperty("enabled").GetBoolean(), Is.True);
            Assert.That(secondUnknown, Is.Empty);
            Assert.That(secondUnknown, Is.Not.SameAs(firstUnknown));
        }
    }

    [TestCaseSource(nameof(RecursiveCollectionCases))]
    public void RecursiveDtoCollectionsRetainChildIdentity(Type dtoType, string fieldName)
    {
        object parent = CreateDto(dtoType);
        object child = CreateDto(dtoType);
        FieldInfo field = RequireField(dtoType, fieldName);
        IList children = (IList)Activator.CreateInstance(field.FieldType)!;
        field.SetValue(parent, children);

        children.Add(child);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(children, Has.Count.EqualTo(1));
            Assert.That(children[0], Is.SameAs(child));
            Assert.That(field.GetValue(parent), Is.SameAs(children));
        }
    }

    private static IEnumerable<TestCaseData> DataDtoCases()
    {
        foreach (Type type in DataDtoTypes())
        {
            yield return new TestCaseData(type).SetName("DataDto_" + type.Name);
        }
    }

    private static IEnumerable<TestCaseData> OptionalScalarCases()
    {
        foreach (Type type in DataDtoTypes())
        {
            FieldInfo[] fields = DataFields(type);
            foreach (FieldInfo presence in fields)
            {
                if (presence.FieldType != typeof(bool) ||
                    !presence.Name.StartsWith("Has", StringComparison.Ordinal))
                {
                    continue;
                }

                string valueName = presence.Name.Substring(3);
                FieldInfo? value = fields.SingleOrDefault(field => field.Name == valueName);
                if (value == null)
                {
                    continue;
                }

                string nullName = "Is" + valueName + "Null";
                FieldInfo? nullFlag = fields.SingleOrDefault(field => field.Name == nullName);
                yield return new TestCaseData(
                    type, presence.Name, value.Name, nullFlag?.Name)
                    .SetName("Optional_" + type.Name + "_" + value.Name);
            }
        }
    }

    private static IEnumerable<TestCaseData> UnknownPropertyCases()
    {
        foreach (Type type in DataDtoTypes())
        {
            if (DataFields(type).Any(field => field.Name == "UnknownProperties"))
            {
                yield return new TestCaseData(type).SetName("UnknownJson_" + type.Name);
            }
        }
    }

    private static IEnumerable<TestCaseData> RecursiveCollectionCases()
    {
        foreach (Type type in DataDtoTypes())
        {
            foreach (FieldInfo field in DataFields(type))
            {
                if (field.FieldType.IsGenericType &&
                    field.FieldType.GetGenericTypeDefinition() == typeof(List<>) &&
                    field.FieldType.GetGenericArguments()[0] == type)
                {
                    yield return new TestCaseData(type, field.Name)
                        .SetName("Recursive_" + type.Name + "_" + field.Name);
                }
            }
        }
    }

    private static Type[] DataDtoTypes()
    {
        return typeof(JmapSession).Assembly
            .GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract &&
                !typeof(Exception).IsAssignableFrom(type) && DataFields(type).Length > 0)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static FieldInfo[] DataFields(Type type)
    {
        return type.GetFields(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    private static object CreateDto(Type type)
    {
        object? value = Activator.CreateInstance(type);
        Assert.That(value, Is.Not.Null, type.Name + " must be constructible");
        return value!;
    }

    private static FieldInfo RequireField(Type type, string name)
    {
        FieldInfo? field = type.GetField(
            name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Assert.That(field, Is.Not.Null, type.Name + "." + name);
        return field!;
    }

    private static bool IsMutableDataObject(Type type)
    {
        return type.Assembly == typeof(JmapSession).Assembly && type.IsClass;
    }

    private static object DifferentValue(Type type, object? current)
    {
        if (type == typeof(string))
        {
            return "assigned-value";
        }

        if (type == typeof(bool))
        {
            return !(bool)current!;
        }

        if (type == typeof(int))
        {
            return (int)current! == 17 ? 18 : 17;
        }

        if (type == typeof(long))
        {
            return (long)current! == 17L ? 18L : 17L;
        }

        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        }

        if (type == typeof(JsonElement))
        {
            return PublicApiTestHelper.Json("{\"assigned\":true}");
        }

        if (type == typeof(byte[]))
        {
            return new byte[] { 1, 2, 3 };
        }

        if (type == typeof(Stream))
        {
            return new MemoryStream(new byte[] { 1, 2, 3 }, false);
        }

        if (type.IsEnum)
        {
            object[] values = Enum.GetValues(type).Cast<object>().ToArray();
            return values.First(value => !value.Equals(current));
        }

        if (type.IsInterface)
        {
            return DispatchProxy.Create(type, typeof(DataDtoDispatchProxy));
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 1);
        }

        object? value = Activator.CreateInstance(type);
        Assert.That(value, Is.Not.Null, "No field sample for " + type.FullName);
        return value!;
    }

    public class DataDtoDispatchProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? arguments)
        {
            throw new NotSupportedException("The DTO field proxy is only an identity value.");
        }
    }
}
