using System.Reflection;

namespace jpeg.Tests;

internal class PublicModelContractTests
{
    [Test]
    public void EveryPublicEnumValue_RoundTripsByNameAndNumericValue()
    {
        Type[] types = typeof(global::jpeg.JpegImage).Assembly.GetExportedTypes();
        int enumCount = 0;
        int valueCount = 0;

        foreach (Type type in types)
        {
            if (!type.IsEnum)
            {
                continue;
            }

            enumCount = enumCount + 1;
            Array values = Enum.GetValues(type);
            foreach (object value in values)
            {
                string? name = Enum.GetName(type, value);
                Assert.That(name, Is.Not.Null.And.Not.Empty, type.FullName);
                Assert.That(Enum.Parse(type, name!), Is.EqualTo(value), type.FullName + "." + name);
                Assert.That(Enum.ToObject(type, Convert.ToInt32(value)), Is.EqualTo(value), type.FullName + "." + name);
                valueCount = valueCount + 1;
            }
        }

        Assert.That(enumCount, Is.GreaterThan(0));
        Assert.That(valueCount, Is.GreaterThan(enumCount));
    }

    [Test]
    public void EveryPublicDataModelField_RoundTripsItsValue()
    {
        Type[] types = typeof(global::jpeg.JpegImage).Assembly.GetExportedTypes();
        int modelCount = 0;
        int fieldCount = 0;

        foreach (Type type in types)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            if (!type.IsClass || fields.Length == 0)
            {
                continue;
            }

            object? model = Activator.CreateInstance(type);
            Assert.That(model, Is.Not.Null, type.FullName);
            modelCount = modelCount + 1;
            foreach (FieldInfo field in fields)
            {
                object value = CreateRepresentativeValue(field.FieldType);
                field.SetValue(model, value);
                Assert.That(field.GetValue(model), Is.EqualTo(value), type.FullName + "." + field.Name);
                fieldCount = fieldCount + 1;
            }
        }

        Assert.That(modelCount, Is.GreaterThan(0));
        Assert.That(fieldCount, Is.GreaterThan(modelCount));
    }

    [Test]
    public void EveryPublicConstructor_CreatesItsDeclaredType()
    {
        Type[] types = typeof(global::jpeg.JpegImage).Assembly.GetExportedTypes();
        int constructorCount = 0;

        foreach (Type type in types)
        {
            if (!type.IsClass)
            {
                continue;
            }

            ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (ConstructorInfo constructor in constructors)
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                object?[] arguments = new object?[parameters.Length];
                int index = 0;
                while (index < parameters.Length)
                {
                    arguments[index] = CreateConstructorArgument(parameters[index].ParameterType);
                    index = index + 1;
                }

                object instance = constructor.Invoke(arguments);
                Assert.That(instance, Is.TypeOf(type), type.FullName);
                constructorCount = constructorCount + 1;
            }
        }

        Assert.That(constructorCount, Is.GreaterThan(0));
    }

    private static object CreateRepresentativeValue(Type type)
    {
        if (type == typeof(string))
        {
            return "public field value";
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(int))
        {
            return 17;
        }

        if (type == typeof(long))
        {
            return 19L;
        }

        if (type == typeof(double))
        {
            return 2.5;
        }

        if (type.IsEnum)
        {
            Array values = Enum.GetValues(type);
            return values.GetValue(values.Length - 1)!;
        }

        if (type.IsArray)
        {
            return Array.CreateInstance(type.GetElementType()!, 1);
        }

        return Activator.CreateInstance(type)!;
    }

    private static object CreateConstructorArgument(Type type)
    {
        if (type == typeof(string))
        {
            return "constructor message";
        }

        if (type == typeof(System.Exception))
        {
            return new System.Exception("inner");
        }

        return CreateRepresentativeValue(type);
    }
}
