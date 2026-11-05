using System.Reflection;
using System.Text.Json;

namespace currencycodes.Tests;

public class CompleteDefensiveSnapshotTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
        IncludeFields = true
    };

    private static readonly Type[] PublicSnapshotTypes =
    {
        typeof(AmountTextResult),
        typeof(AmountValidationResult),
        typeof(CurrencyFormatMetadata),
        typeof(CurrencyInfo),
        typeof(CurrencyLookupResult),
        typeof(CurrencyUsage),
        typeof(CurrencyValidationResult),
        typeof(DataVersionInfo),
        typeof(MinorUnitConversionResult),
        typeof(NormalizationResult),
        typeof(PartialDate),
        typeof(TerritoryInfo),
        typeof(TerritoryLookupResult)
    };

    [Test]
    public void EveryPublicSnapshotFieldIsDetachedAcrossCallsAndServiceInstances()
    {
        HashSet<string> mutatedFields = new HashSet<string>(StringComparer.Ordinal);

        AssertDetached(
            "data version",
            service => service.GetCurrencyDataVersion(),
            mutatedFields);
        AssertDetached(
            "currency catalog",
            service => service.GetCurrencyCatalog(CurrencySet.All),
            mutatedFields);
        AssertDetached(
            "territory catalog",
            service => service.GetTerritoryCatalog(),
            mutatedFields);
        AssertDetached(
            "usage catalog",
            service => service.GetCurrencyUsageCatalog(),
            mutatedFields);
        AssertDetached(
            "currency lookup",
            service => service.FindCurrencyByAlphabeticCode("USD", CurrencySet.ActiveOnly),
            mutatedFields);
        AssertDetached(
            "territory lookup",
            service => service.FindTerritoryByAlpha2Code("US"),
            mutatedFields);
        AssertDetached(
            "format metadata",
            service => service.GetCurrencyFormatMetadata("CHF"),
            mutatedFields);
        AssertDetached(
            "normalization",
            service => service.NormalizeCurrencyAlphabeticCode(" usd "),
            mutatedFields);
        AssertDetached(
            "currency validation",
            service => service.ValidateCurrencyAssignment("USD", CurrencySet.ActiveOnly),
            mutatedFields);
        AssertDetached(
            "amount validation",
            service => service.ValidateCurrencyAmount(12.34m, "USD", AmountUsage.IsoMinorUnit),
            mutatedFields);
        AssertDetached(
            "major to minor conversion",
            service => service.ConvertCurrencyMajorToMinor(12.34m, "USD", AmountUsage.IsoMinorUnit),
            mutatedFields);
        AssertDetached(
            "amount formatting",
            service => service.FormatInvariantCurrencyAmount(
                12.34m,
                "USD",
                AmountUsage.IsoMinorUnit,
                MidpointRounding.ToEven),
            mutatedFields);

        foreach (Type type in PublicSnapshotTypes)
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (FieldInfo field in fields)
            {
                Assert.That(
                    mutatedFields,
                    Does.Contain(GetFieldKey(field)),
                    type.Name + "." + field.Name + " was not exercised");
            }
        }
    }

    private static void AssertDetached(
        string operation,
        Func<CurrencyCodeService, object> getSnapshot,
        HashSet<string> mutatedFields)
    {
        CurrencyCodeService service = new CurrencyCodeService();
        object snapshot = getSnapshot(service);
        string expected = Serialize(snapshot);

        MutateSnapshot(snapshot, mutatedFields, new HashSet<object>(ReferenceEqualityComparer.Instance));

        Assert.Multiple(() =>
        {
            Assert.That(
                Serialize(snapshot),
                Is.Not.EqualTo(expected),
                operation + " returned value was not mutated by the test");
            Assert.That(
                Serialize(getSnapshot(service)),
                Is.EqualTo(expected),
                operation + " changed a later call on the same service");
            Assert.That(
                Serialize(getSnapshot(new CurrencyCodeService())),
                Is.EqualTo(expected),
                operation + " changed a separate service instance");
        });
    }

    private static string Serialize(object value)
    {
        return JsonSerializer.Serialize(value, value.GetType(), SerializerOptions);
    }

    private static void MutateSnapshot(
        object value,
        HashSet<string> mutatedFields,
        HashSet<object> visited)
    {
        if (!visited.Add(value))
        {
            return;
        }

        if (value is Array array)
        {
            MutateArray(array, mutatedFields, visited);
            return;
        }

        FieldInfo[] fields = value.GetType().GetFields(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        foreach (FieldInfo field in fields)
        {
            mutatedFields.Add(GetFieldKey(field));
            object? current = field.GetValue(value);

            if (current is Array fieldArray)
            {
                MutateArray(fieldArray, mutatedFields, visited);
                Type elementType = field.FieldType.GetElementType()
                    ?? throw new InvalidOperationException("Array field has no element type.");
                field.SetValue(value, Array.CreateInstance(elementType, 0));
                continue;
            }

            if (current != null && IsPublicSnapshotType(current.GetType()))
            {
                MutateSnapshot(current, mutatedFields, visited);
                field.SetValue(value, null);
                continue;
            }

            field.SetValue(value, CreateChangedScalar(field.FieldType, current));
        }
    }

    private static void MutateArray(
        Array array,
        HashSet<string> mutatedFields,
        HashSet<object> visited)
    {
        Type elementType = array.GetType().GetElementType()
            ?? throw new InvalidOperationException("Array has no element type.");
        for (int index = 0; index < array.Length; index++)
        {
            object? element = array.GetValue(index);
            if (element != null && IsPublicSnapshotType(element.GetType()))
            {
                MutateSnapshot(element, mutatedFields, visited);
            }
            else
            {
                array.SetValue(CreateChangedScalar(elementType, element), index);
            }
        }

        if (array.Length > 0 && !elementType.IsValueType)
        {
            array.SetValue(null, 0);
        }
    }

    private static object CreateChangedScalar(Type type, object? current)
    {
        if (type == typeof(string))
        {
            return "__mutated__";
        }

        if (type == typeof(bool))
        {
            return !(bool)(current ?? false);
        }

        if (type == typeof(int))
        {
            return (int)(current ?? 0) == int.MinValue ? int.MaxValue : int.MinValue;
        }

        if (type == typeof(long))
        {
            return (long)(current ?? 0L) == long.MinValue ? long.MaxValue : long.MinValue;
        }

        if (type == typeof(decimal))
        {
            return (decimal)(current ?? 0m) == decimal.MinValue
                ? decimal.MaxValue
                : decimal.MinValue;
        }

        if (type == typeof(DateOnly))
        {
            return (DateOnly)(current ?? DateOnly.MinValue) == DateOnly.MinValue
                ? DateOnly.MaxValue
                : DateOnly.MinValue;
        }

        if (type.IsEnum)
        {
            return Enum.ToObject(type, int.MaxValue);
        }

        throw new InvalidOperationException("Unsupported public snapshot field type: " + type.FullName);
    }

    private static bool IsPublicSnapshotType(Type type)
    {
        return Array.IndexOf(PublicSnapshotTypes, type) >= 0;
    }

    private static string GetFieldKey(FieldInfo field)
    {
        return field.DeclaringType?.FullName + "." + field.Name;
    }
}
