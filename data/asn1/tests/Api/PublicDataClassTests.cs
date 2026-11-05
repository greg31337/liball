using System.Numerics;
using System.Reflection;
using asn1;

namespace asn1.Tests;

public class PublicDataClassTests
{
    [Test]
    public void EveryPublicDataClassConstructsWithUsableReferenceDefaults()
    {
        Type[] dataTypes = GetDataTypes();

        Assert.That(dataTypes, Has.Length.EqualTo(82));
        foreach (Type dataType in dataTypes)
        {
            object? instance = Activator.CreateInstance(dataType);
            Assert.That(instance, Is.Not.Null, dataType.FullName);

            NullabilityInfoContext nullability = new NullabilityInfoContext();
            foreach (FieldInfo field in GetPublicFields(dataType))
            {
                if (!field.FieldType.IsValueType
                    && nullability.Create(field).ReadState == NullabilityState.NotNull)
                {
                    Assert.That(field.GetValue(instance), Is.Not.Null,
                        dataType.Name + "." + field.Name
                            + " has a non-null public contract but a null default.");
                }
            }
        }
    }

    [Test]
    public void EveryPublicDataFieldRoundTripsAnAssignedValue()
    {
        foreach (Type dataType in GetDataTypes())
        {
            object instance = Activator.CreateInstance(dataType)
                ?? throw new InvalidOperationException(dataType.FullName);
            foreach (FieldInfo field in GetPublicFields(dataType))
            {
                object? sample = CreateSampleValue(field.FieldType);

                field.SetValue(instance, sample);

                if (field.FieldType.IsValueType)
                {
                    Assert.That(field.GetValue(instance), Is.EqualTo(sample),
                        dataType.Name + "." + field.Name);
                }
                else
                {
                    Assert.That(field.GetValue(instance), Is.SameAs(sample),
                        dataType.Name + "." + field.Name);
                }
            }
        }
    }

    [Test]
    public void EveryPresenceFlagDistinguishesAbsentFromPresentDefault()
    {
        int presenceFlagCount = 0;
        foreach (Type dataType in GetDataTypes())
        {
            object instance = Activator.CreateInstance(dataType)
                ?? throw new InvalidOperationException(dataType.FullName);
            foreach (FieldInfo presence in GetPublicFields(dataType).Where(field =>
                field.FieldType == typeof(bool)
                    && field.Name.StartsWith("Has", StringComparison.Ordinal)))
            {
                string valueName = presence.Name.Substring(3);
                FieldInfo[] values = GetPresenceValues(dataType, valueName);

                Assert.That(values, Is.Not.Empty,
                    dataType.Name + "." + presence.Name
                        + " has no corresponding value fields.");
                Assert.That(presence.GetValue(instance), Is.False,
                    dataType.Name + "." + presence.Name);

                foreach (FieldInfo value in values)
                {
                    object? defaultValue = value.FieldType.IsValueType
                        ? Activator.CreateInstance(value.FieldType)
                        : null;
                    value.SetValue(instance, defaultValue);
                }
                presence.SetValue(instance, true);

                Assert.That(presence.GetValue(instance), Is.True,
                    dataType.Name + "." + presence.Name);
                presenceFlagCount = presenceFlagCount + 1;
            }
        }

        Assert.That(presenceFlagCount, Is.EqualTo(41));
    }

    private static Type[] GetDataTypes()
    {
        Assembly apiAssembly = typeof(AsnCompilationInput).Assembly;
        return apiAssembly.GetExportedTypes()
            .Where(type => type.Namespace == "asn1"
                && type.IsClass
                && !type.IsAbstract
                && !typeof(Exception).IsAssignableFrom(type)
                && !type.GetInterfaces().Any(contract =>
                    contract.IsPublic && contract.Assembly == apiAssembly))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    private static FieldInfo[] GetPublicFields(Type dataType)
    {
        return dataType.GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private static object? CreateSampleValue(Type fieldType)
    {
        if (fieldType == typeof(string))
        {
            return "coverage-value";
        }

        if (fieldType == typeof(bool))
        {
            return true;
        }

        if (fieldType == typeof(byte))
        {
            return (byte)17;
        }

        if (fieldType == typeof(int))
        {
            return 17;
        }

        if (fieldType == typeof(long))
        {
            return 17L;
        }

        if (fieldType == typeof(BigInteger))
        {
            return new BigInteger(17);
        }

        if (fieldType.IsEnum)
        {
            Array values = Enum.GetValues(fieldType);
            return values.GetValue(values.Length - 1);
        }

        if (fieldType.IsArray)
        {
            Type elementType = fieldType.GetElementType()
                ?? throw new InvalidOperationException(fieldType.FullName);
            return Array.CreateInstance(elementType, 1);
        }

        if (fieldType == typeof(IAsnObjectSetRegistry))
        {
            return new AsnObjectSetRegistry();
        }

        if (fieldType == typeof(IPerInstructionRegistry))
        {
            return new EmptyPerInstructionRegistry();
        }

        return Activator.CreateInstance(fieldType);
    }

    private static FieldInfo[] GetPresenceValues(Type dataType, string valueName)
    {
        string[] names;
        if (valueName == "Exception")
        {
            names = ["ExceptionType", "ExceptionValue", "ExceptionIdentifier"];
        }
        else if (valueName == "EncodedLocation")
        {
            names = ["EncodedOffset", "EncodedLength"];
        }
        else if (valueName == "EncodedBitLocation")
        {
            names = ["EncodedBitOffset", "EncodedBitLength"];
        }
        else if (valueName == "BinaryValue")
        {
            names = ["Mantissa", "Radix", "Exponent"];
        }
        else if (valueName == "DecimalValue")
        {
            names = ["DecimalText", "DecimalCoefficient", "DecimalExponent"];
        }
        else
        {
            names = [valueName];
        }

        return names.Select(name => dataType.GetField(
                name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(field => field != null)
            .Cast<FieldInfo>()
            .ToArray();
    }

    private class EmptyPerInstructionRegistry : IPerInstructionRegistry
    {
        public void RegisterHandler(
            AsnPerInstructionRegistration registration,
            IPerInstructionHandler handler)
        {
        }

        public IPerInstructionHandler? ResolveHandler(string keyword)
        {
            return null;
        }

        public AsnPerInstructionRegistration[] GetRegistrations()
        {
            return [];
        }
    }
}
