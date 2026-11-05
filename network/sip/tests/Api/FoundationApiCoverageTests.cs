namespace sip.Tests;

public class FoundationApiCoverageTests
{
    [Test]
    public void EveryPublicDataFieldRetainsItsAssignedValue()
    {
        Type[] dataTypes = GetPublicDataTypes();

        Assert.That(dataTypes, Is.Not.Empty);
        foreach (Type dataType in dataTypes)
        {
            object instance = Activator.CreateInstance(dataType)!;
            System.Reflection.FieldInfo[] fields = dataType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            Assert.That(fields, Is.Not.Empty, dataType.Name);
            foreach (System.Reflection.FieldInfo field in fields)
            {
                object sample = CreateSampleValue(field.FieldType);
                field.SetValue(instance, sample);
                object? actual = field.GetValue(instance);

                if (field.FieldType.IsValueType || field.FieldType == typeof(string))
                {
                    Assert.That(actual, Is.EqualTo(sample), $"{dataType.Name}.{field.Name}");
                }
                else
                {
                    Assert.That(actual, Is.SameAs(sample), $"{dataType.Name}.{field.Name}");
                }
            }
        }
    }

    [Test]
    public void MutableDefaultsAreOwnedByEachDataObject()
    {
        Type[] dataTypes = GetPublicDataTypes();

        foreach (Type dataType in dataTypes)
        {
            object first = Activator.CreateInstance(dataType)!;
            object second = Activator.CreateInstance(dataType)!;
            System.Reflection.FieldInfo[] fields = dataType.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

            foreach (System.Reflection.FieldInfo field in fields)
            {
                object? firstValue = field.GetValue(first);
                object? secondValue = field.GetValue(second);

                if (firstValue != null && !field.FieldType.IsValueType && field.FieldType != typeof(string))
                {
                    Assert.That(secondValue, Is.Not.Null, $"{dataType.Name}.{field.Name}");
                    Assert.That(secondValue, Is.Not.SameAs(firstValue), $"{dataType.Name}.{field.Name}");
                }

                if (field.FieldType.IsArray)
                {
                    Assert.That(firstValue, Is.InstanceOf<Array>(), $"{dataType.Name}.{field.Name}");
                    Assert.That(((Array)firstValue!).Length, Is.Zero, $"{dataType.Name}.{field.Name}");
                }
            }
        }
    }

    static Type[] GetPublicDataTypes()
    {
        return typeof(SipMessage).Assembly.GetExportedTypes()
            .Where(type => type.IsClass)
            .Where(type => !type.IsAbstract)
            .Where(type => !typeof(Exception).IsAssignableFrom(type))
            .Where(type => type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).Length > 0)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
    }

    static object CreateSampleValue(Type valueType)
    {
        if (valueType == typeof(string))
        {
            return "assigned-value";
        }

        if (valueType == typeof(bool))
        {
            return true;
        }

        if (valueType == typeof(byte))
        {
            return (byte)37;
        }

        if (valueType == typeof(int))
        {
            return 37;
        }

        if (valueType == typeof(long))
        {
            return 3700L;
        }

        if (valueType.IsEnum)
        {
            Array values = Enum.GetValues(valueType);
            return values.GetValue(values.Length - 1)!;
        }

        if (valueType.IsArray)
        {
            Type elementType = valueType.GetElementType()!;
            Array array = Array.CreateInstance(elementType, 1);
            array.SetValue(CreateSampleValue(elementType), 0);
            return array;
        }

        return Activator.CreateInstance(valueType)!;
    }
}
