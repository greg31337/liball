using System.Reflection;
using System.Runtime.CompilerServices;

namespace ftp.Tests;

public class PublicDataClassCoverageTests
{
    [Test]
    public void Every_public_data_class_constructs_and_all_fields_are_writable()
    {
        Type[] dataTypes = typeof(FtpCommand).Assembly.GetTypes()
            .Where(type =>
                type.IsClass &&
                type.IsPublic &&
                !type.IsAbstract &&
                GetPublicFields(type).Length > 0)
            .OrderBy(type => type.Name)
            .ToArray();

        Assert.That(dataTypes, Has.Length.EqualTo(60));
        foreach (Type dataType in dataTypes)
        {
            object? instance = Activator.CreateInstance(dataType);
            Assert.That(instance, Is.Not.Null, dataType.Name);
            foreach (FieldInfo field in GetPublicFields(dataType))
            {
                object? original = field.GetValue(instance);
                object? alternate = CreateAlternateValue(
                    field.FieldType, original);
                field.SetValue(instance, alternate);
                AssertFieldValue(field, instance!, alternate);
                field.SetValue(instance, original);
                AssertFieldValue(field, instance!, original);
            }
        }
    }

    [Test]
    public void Optional_fields_use_false_Has_flags_or_nullable_references()
    {
        NullabilityInfoContext nullability = new NullabilityInfoContext();
        Type[] dataTypes = typeof(FtpCommand).Assembly.GetTypes()
            .Where(type =>
                type.IsClass &&
                type.IsPublic &&
                !type.IsAbstract &&
                GetPublicFields(type).Length > 0)
            .ToArray();

        foreach (Type dataType in dataTypes)
        {
            object first = Activator.CreateInstance(dataType)!;
            object second = Activator.CreateInstance(dataType)!;
            FieldInfo[] fields = GetPublicFields(dataType);
            foreach (FieldInfo field in fields)
            {
                object? value = field.GetValue(first);
                if (field.FieldType == typeof(bool) &&
                    field.Name.StartsWith("Has", StringComparison.Ordinal))
                {
                    Assert.That(value, Is.False,
                        dataType.Name + "." + field.Name);
                    Assert.That(FindOptionalValueField(fields, field.Name),
                        Is.Not.Null,
                        dataType.Name + "." + field.Name);
                }
                if (!field.FieldType.IsValueType)
                {
                    NullabilityInfo info = nullability.Create(field);
                    if (info.ReadState == NullabilityState.Nullable)
                    {
                        Assert.That(value, Is.Null,
                            dataType.Name + "." + field.Name);
                    }
                    else
                    {
                        Assert.That(value, Is.Not.Null,
                            dataType.Name + "." + field.Name);
                    }
                }
                if (value is Array array)
                {
                    Assert.That(array, Is.Empty,
                        dataType.Name + "." + field.Name);
                    Assert.That(value, Is.Not.SameAs(field.GetValue(second)),
                        dataType.Name + "." + field.Name);
                }
                else if (value != null &&
                    dataTypes.Contains(field.FieldType))
                {
                    Assert.That(value, Is.Not.SameAs(field.GetValue(second)),
                        dataType.Name + "." + field.Name);
                }
            }
        }
    }

    private static FieldInfo[] GetPublicFields(Type type)
    {
        return type.GetFields(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);
    }

    private static FieldInfo? FindOptionalValueField(
        FieldInfo[] fields,
        string hasFieldName)
    {
        string valueName = hasFieldName.Substring(3);
        return fields.FirstOrDefault(field =>
            field.Name == valueName ||
            field.Name.StartsWith(valueName, StringComparison.Ordinal));
    }

    private static object? CreateAlternateValue(
        Type fieldType,
        object? original)
    {
        if (fieldType == typeof(bool))
        {
            return !((bool)original!);
        }
        if (fieldType == typeof(int))
        {
            return ((int)original!) + 1;
        }
        if (fieldType == typeof(long))
        {
            return ((long)original!) + 1;
        }
        if (fieldType == typeof(string))
        {
            return original == null ? "value" : original + "-changed";
        }
        if (fieldType == typeof(DateTime))
        {
            return new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        }
        if (fieldType == typeof(TimeSpan))
        {
            return TimeSpan.FromSeconds(1);
        }
        if (fieldType == typeof(CancellationToken))
        {
            return new CancellationToken(true);
        }
        if (fieldType.IsEnum)
        {
            Array values = Enum.GetValues(fieldType);
            return values.GetValue(values.Length - 1);
        }
        if (fieldType.IsArray)
        {
            return Array.CreateInstance(fieldType.GetElementType()!, 1);
        }
        if (fieldType.IsInterface)
        {
            return DispatchProxy.Create(
                fieldType, typeof(FtpServerLoginProviderProxy));
        }
        if (fieldType == typeof(Stream))
        {
            return new MemoryStream();
        }
        if (!fieldType.IsAbstract)
        {
            ConstructorInfo? constructor = fieldType.GetConstructor(Type.EmptyTypes);
            if (constructor != null)
            {
                return constructor.Invoke(null);
            }
            return RuntimeHelpers.GetUninitializedObject(fieldType);
        }
        return original;
    }

    private static void AssertFieldValue(
        FieldInfo field,
        object instance,
        object? expected)
    {
        object? actual = field.GetValue(instance);
        if (!field.FieldType.IsValueType && field.FieldType != typeof(string))
        {
            Assert.That(actual, Is.SameAs(expected),
                field.DeclaringType!.Name + "." + field.Name);
            return;
        }
        Assert.That(actual, Is.EqualTo(expected),
            field.DeclaringType!.Name + "." + field.Name);
    }
}
