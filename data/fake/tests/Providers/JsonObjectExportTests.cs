using System.Collections;
using System.Globalization;
using System.Text.Json;
using fake;

namespace fake.Tests;

[TestFixture]
public class JsonObjectExportTests
{
    public enum SampleKind
    {
        First = 0,
        Second = 1
    }

    public class NestedValue
    {
        public bool Ready;
        public string Label;
    }

    public class FieldValue
    {
        public string Zebra;
        public int Alpha;
        public NestedValue Nested;

        public string IgnoredProperty { get; set; }
    }

    public class CyclicValue
    {
        public object Next;
    }

    [Test]
    public void PublicFieldsUseDeterministicOrdinalOrderAndValidEscaping()
    {
        NestedValue nested = new NestedValue();
        nested.Ready = true;
        nested.Label = "line\n\"quoted\"\\slash\u2028é😀";
        FieldValue value = new FieldValue();
        value.Zebra = "last";
        value.Alpha = 7;
        value.Nested = nested;
        value.IgnoredProperty = "not exported";
        ExportProvider export = Fake.Create(14100).Export();

        string first = export.Json(value);
        string second = export.Json(value);
        using JsonDocument document = JsonDocument.Parse(first);
        JsonElement root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(root.EnumerateObject().Select(item => item.Name),
                Is.EqualTo(new string[] { "Alpha", "Nested", "Zebra" }));
            Assert.That(root.GetProperty("Alpha").GetInt32(), Is.EqualTo(7));
            Assert.That(root.GetProperty("Nested").EnumerateObject()
                    .Select(item => item.Name),
                Is.EqualTo(new string[] { "Label", "Ready" }));
            Assert.That(root.GetProperty("Nested").GetProperty("Label")
                    .GetString(),
                Is.EqualTo(nested.Label));
            Assert.That(root.TryGetProperty("IgnoredProperty", out _),
                Is.False);
        });
    }

    [Test]
    public void SchemaDictionaryAndCollectionOrderArePreserved()
    {
        Fake fake = Fake.Create(14101);
        SchemaGenerator schema = fake.Schema();
        SchemaNode root = schema.Object(
            new SchemaField[]
            {
                schema.Field("zeta", schema.Constant(1)),
                schema.Field("alpha", schema.Constant(2)),
                schema.Field(
                    "items",
                    schema.Array(schema.Constant("item"), 2))
            });
        object generated = schema.GenerateFromRoot(root);

        string json = fake.Export().Json(generated);

        Assert.That(json,
            Is.EqualTo("{\"zeta\":1,\"alpha\":2,\"items\":[\"item\",\"item\"]}"));
        using JsonDocument document = JsonDocument.Parse(json);
        Assert.That(document.RootElement.EnumerateObject()
                .Select(item => item.Name),
            Is.EqualTo(new string[] { "zeta", "alpha", "items" }));
    }

    [Test]
    public void NumbersDatesAndOtherMachineValuesAreCultureInvariant()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = new CultureInfo("ar-EG");
        string json;
        try
        {
            object[] values = new object[]
            {
                null,
                true,
                long.MinValue,
                ulong.MaxValue,
                1234.5m,
                0.125d,
                new DateTime(
                    2024,
                    1,
                    2,
                    3,
                    4,
                    5,
                    DateTimeKind.Utc),
                new DateTimeOffset(
                    2024,
                    1,
                    2,
                    3,
                    4,
                    5,
                    TimeSpan.FromHours(5.5)),
                new DateOnly(2024, 1, 2),
                new TimeOnly(6, 7, 8),
                new TimeSpan(1, 2, 3, 4),
                new Guid("00112233-4455-6677-8899-aabbccddeeff"),
                SampleKind.Second
            };
            json = Fake.Create(14102).Export().Json(values);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;

        Assert.Multiple(() =>
        {
            Assert.That(root[0].ValueKind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(root[1].GetBoolean(), Is.True);
            Assert.That(root[2].GetInt64(), Is.EqualTo(long.MinValue));
            Assert.That(root[3].GetUInt64(), Is.EqualTo(ulong.MaxValue));
            Assert.That(root[4].GetDecimal(), Is.EqualTo(1234.5m));
            Assert.That(root[5].GetDouble(), Is.EqualTo(0.125d));
            Assert.That(root[6].GetString(),
                Is.EqualTo("2024-01-02T03:04:05.0000000Z"));
            Assert.That(root[7].GetString(),
                Is.EqualTo("2024-01-02T03:04:05.0000000+05:30"));
            Assert.That(root[8].GetString(), Is.EqualTo("2024-01-02"));
            Assert.That(root[9].GetString(),
                Is.EqualTo("06:07:08.0000000"));
            Assert.That(root[10].GetString(), Is.EqualTo("1.02:03:04"));
            Assert.That(root[11].GetString(),
                Is.EqualTo("00112233-4455-6677-8899-aabbccddeeff"));
            Assert.That(root[12].GetString(), Is.EqualTo("Second"));
            Assert.That(json, Does.Contain("1234.5").And.Not.Contain("1234,5"));
        });
    }

    [Test]
    public void GeneratedProfilesExportTheirPublicFields()
    {
        Fake fake = Fake.Create(14103);
        DeviceProfile profile = fake.Profile().Device();

        string json = fake.Export().Json(profile);
        using JsonDocument document = JsonDocument.Parse(json);
        string[] names = document.RootElement.EnumerateObject()
            .Select(item => item.Name).ToArray();
        string[] sortedNames = (string[])names.Clone();
        Array.Sort(sortedNames, StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(document.RootElement.GetProperty("Manufacturer")
                    .GetString(),
                Is.EqualTo(profile.Manufacturer));
            Assert.That(document.RootElement.GetProperty("ScreenWidth")
                    .GetInt32(),
                Is.EqualTo(profile.ScreenWidth));
            Assert.That(document.RootElement.GetProperty("UsesAndroid")
                    .GetBoolean(),
                Is.EqualTo(profile.UsesAndroid));
            Assert.That(names, Is.EqualTo(sortedNames));
        });
    }

    [Test]
    public void InvalidGraphsAndUnsupportedJsonShapesAreRejected()
    {
        ExportProvider export = Fake.Create(14104).Export();
        CyclicValue cycle = new CyclicValue();
        cycle.Next = cycle;
        Array multidimensional = Array.CreateInstance(
            typeof(string),
            new int[] { 1, 1 });
        Hashtable invalidDictionary = new Hashtable();
        invalidDictionary[1] = "value";
        object nested = "end";
        int depth = 0;
        while (depth < 130)
        {
            nested = new object[] { nested };
            depth++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(() => export.Json(cycle),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("cycle"));
            Assert.That(() => export.Json(multidimensional),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("one-dimensional"));
            Assert.That(() => export.Json(invalidDictionary),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("keys must be strings"));
            Assert.That(() => export.Json(nested),
                Throws.TypeOf<FakeException>()
                    .With.Message.Contains("maximum depth"));
            Assert.That(() => export.Json(double.NaN),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => export.Json(float.PositiveInfinity),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => export.Json("\ud800"),
                Throws.TypeOf<ArgumentException>());
        });
    }

    [Test]
    public void CountAndOutputLimitsFailWithoutConsumingExportEntropy()
    {
        FakeOptions countOptions = FakeOptions.Create(14105);
        countOptions.MaximumCount = 1;
        Fake countInspectedFake = Fake.Create(countOptions);
        Fake countUntouchedFake = Fake.Create(countOptions);
        ExportProvider countInspected = countInspectedFake.Export();
        ExportProvider countUntouched = countUntouchedFake.Export();
        FakeOptions outputOptions = FakeOptions.Create(14106);
        outputOptions.MaximumOutputSize = 7;
        Fake outputInspectedFake = Fake.Create(outputOptions);
        Fake outputUntouchedFake = Fake.Create(outputOptions);
        ExportProvider outputInspected = outputInspectedFake.Export();
        ExportProvider outputUntouched = outputUntouchedFake.Export();

        Assert.Multiple(() =>
        {
            Assert.That(() => countInspected.Json(
                    new object[] { 1, 2 }),
                Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => outputInspected.Json("abcdef"),
                Throws.TypeOf<ArgumentOutOfRangeException>()
                    .With.Message.Contains("configured maximum"));
            Assert.That(countInspected.RandomValue.Long(),
                Is.EqualTo(countUntouched.RandomValue.Long()));
            Assert.That(outputInspected.RandomValue.Long(),
                Is.EqualTo(outputUntouched.RandomValue.Long()));
        });
    }
}
