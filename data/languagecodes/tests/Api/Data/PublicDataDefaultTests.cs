using System.Reflection;
using languagecodes;

namespace languagecodes.Tests;

public sealed class PublicDataDefaultTests
{
    [TestCaseSource(nameof(PublicDataTypes))]
    public void PublicDataTypesHaveWorkingParameterlessConstructors(Type dataType)
    {
        ConstructorInfo? constructor = dataType.GetConstructor(Type.EmptyTypes);

        Assert.That(constructor, Is.Not.Null, $"{dataType.Name} has no public parameterless constructor.");
        Assert.That(constructor?.Invoke(Array.Empty<object>()), Is.TypeOf(dataType));
    }

    [TestCaseSource(nameof(PublicDataTypes))]
    public void ConstructorsInitializeNullableReferenceFieldsToNull(Type dataType)
    {
        object instance = Activator.CreateInstance(dataType)!;

        Assert.Multiple(() =>
        {
            foreach (FieldInfo field in PublicFields(dataType))
            {
                if (!field.FieldType.IsValueType && !field.FieldType.IsArray)
                {
                    Assert.That(
                        field.GetValue(instance),
                        Is.Null,
                        $"{dataType.Name}.{field.Name} should default to null.");
                }
            }
        });
    }

    [TestCaseSource(nameof(PublicDataTypes))]
    public void ConstructorsInitializeScalarFieldsToTheirDocumentedDefaults(Type dataType)
    {
        object instance = Activator.CreateInstance(dataType)!;

        Assert.Multiple(() =>
        {
            foreach (FieldInfo field in PublicFields(dataType))
            {
                if (!field.FieldType.IsValueType)
                {
                    continue;
                }

                object expected = dataType == typeof(LanguageTagMatchResult) && field.Name == "MatchedIndex"
                    ? -1
                    : Activator.CreateInstance(field.FieldType)!;
                Assert.That(
                    field.GetValue(instance),
                    Is.EqualTo(expected),
                    $"{dataType.Name}.{field.Name} has an unexpected constructor default.");
            }
        });
    }

    [TestCaseSource(nameof(PublicDataTypes))]
    public void ConstructorsCreateIndependentEmptyArraySnapshots(Type dataType)
    {
        object first = Activator.CreateInstance(dataType)!;
        object second = Activator.CreateInstance(dataType)!;

        Assert.Multiple(() =>
        {
            foreach (FieldInfo field in PublicFields(dataType))
            {
                if (!field.FieldType.IsArray)
                {
                    continue;
                }

                Array? firstArray = field.GetValue(first) as Array;
                Array? secondArray = field.GetValue(second) as Array;
                Assert.That(
                    firstArray,
                    Is.Not.Null.And.Empty,
                    $"{dataType.Name}.{field.Name} should start with an empty array.");
                Assert.That(
                    secondArray,
                    Is.Not.SameAs(firstArray),
                    $"{dataType.Name}.{field.Name} is shared between instances.");
            }
        });
    }

    private static Type[] PublicDataTypes()
    {
        return new[]
        {
            typeof(LanguageAlias),
            typeof(LanguageCodeEntry),
            typeof(LanguageLookupOptions),
            typeof(LanguageLookupResult),
            typeof(LanguageMatch),
            typeof(LanguageName),
            typeof(LanguageNormalizationOptions),
            typeof(LanguageNormalizationResult),
            typeof(LanguageRelationship),
            typeof(LanguageRetirement),
            typeof(LanguageSubtagMetadata),
            typeof(LanguageTag),
            typeof(LanguageTagMatchResult),
            typeof(LanguageValidationIssue),
            typeof(LanguageValidationOptions),
            typeof(LanguageValidationResult)
        };
    }

    private static FieldInfo[] PublicFields(Type dataType)
    {
        return dataType.GetFields(BindingFlags.Instance | BindingFlags.Public);
    }
}
