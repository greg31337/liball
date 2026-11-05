using fake;

namespace fake.Tests;

[TestFixture]
public class EdgeProviderTests
{
    [Test]
    public void EveryCategoryReturnsNamedRecordsInItsRequestedCategory()
    {
        EdgeProvider edge = Fake.Create(147L).Edge();

        foreach (EdgeCaseCategory category in Enum.GetValues<EdgeCaseCategory>())
        {
            EdgeCaseRecord[] records = edge.Cases(category);

            Assert.That(records, Is.Not.Empty, category.ToString());
            Assert.That(records.Select(record => record.Name), Is.Unique, category.ToString());
            Assert.That(records.All(record => record.Category == category), Is.True, category.ToString());
            Assert.That(records.All(record => !string.IsNullOrWhiteSpace(record.Name)), Is.True, category.ToString());
            Assert.That(records.All(record => !string.IsNullOrWhiteSpace(record.Description)), Is.True, category.ToString());
        }
    }

    [Test]
    public void EmptyNullWhitespaceIncludesDistinctNullEmptyAndWhitespaceCases()
    {
        EdgeCaseRecord[] records = Fake.Create(147L).Edge().EmptyNullWhitespace();

        Assert.Multiple(() =>
        {
            Assert.That(records.Select(record => record.Name), Is.EqualTo(new string[]
            {
                "null-reference", "empty-string", "ascii-space", "mixed-whitespace"
            }));
            Assert.That(records[0].Value, Is.Null);
            Assert.That(records[1].Value, Is.EqualTo(string.Empty));
            Assert.That(((string)records[2].Value).All(char.IsWhiteSpace), Is.True);
            Assert.That(((string)records[3].Value).All(char.IsWhiteSpace), Is.True);
        });
    }

    [Test]
    public void BoundaryNumbersRetainLimitsAndSpecialFloatingPointValues()
    {
        EdgeCaseRecord[] records = Fake.Create(147L).Edge().BoundaryNumbers();

        Assert.Multiple(() =>
        {
            Assert.That(Value(records, "int32-minimum"), Is.EqualTo(int.MinValue));
            Assert.That(Value(records, "int32-maximum"), Is.EqualTo(int.MaxValue));
            Assert.That(Value(records, "int64-minimum"), Is.EqualTo(long.MinValue));
            Assert.That(Value(records, "int64-maximum"), Is.EqualTo(long.MaxValue));
            Assert.That(Value(records, "decimal-minimum"), Is.EqualTo(decimal.MinValue));
            Assert.That(Value(records, "decimal-maximum"), Is.EqualTo(decimal.MaxValue));
            Assert.That(Value(records, "double-minimum"), Is.EqualTo(double.MinValue));
            Assert.That(Value(records, "double-maximum"), Is.EqualTo(double.MaxValue));
            Assert.That(BitConverter.DoubleToInt64Bits((double)Value(records, "negative-zero")), Is.EqualTo(long.MinValue));
            Assert.That((double)Value(records, "double-epsilon"), Is.EqualTo(double.Epsilon));
            Assert.That((double)Value(records, "positive-infinity"), Is.EqualTo(double.PositiveInfinity));
            Assert.That((double)Value(records, "negative-infinity"), Is.EqualTo(double.NegativeInfinity));
            Assert.That(double.IsNaN((double)Value(records, "not-a-number")), Is.True);
            Assert.That(Value(records, "binary-rounding"), Is.Not.EqualTo(RelatedValue(records, "binary-rounding")));
        });
    }

    [Test]
    public void LongRepeatedTextUsesExactRequestedLengthAndStableShapes()
    {
        EdgeProvider edge = Fake.Create(147L).Edge();

        EdgeCaseRecord[] records = edge.LongRepeatedTextFromLength(17);

        Assert.Multiple(() =>
        {
            Assert.That(records.Select(record => ((string)record.Value).Length), Is.All.EqualTo(17));
            Assert.That(Value(records, "repeated-letter"), Is.EqualTo("xxxxxxxxxxxxxxxxx"));
            Assert.That(Value(records, "repeated-digit"), Is.EqualTo("00000000000000000"));
            Assert.That(Value(records, "repeated-pattern"), Is.EqualTo("ababababababababa"));
            Assert.That(edge.LongRepeatedText()[0].Value, Has.Length.EqualTo(256));
        });
    }

    [Test]
    public void LongRepeatedTextEnforcesPositiveCountAndOutputLimits()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 8;
        options.MaximumOutputSize = 7;
        EdgeProvider edge = Fake.Create(options).Edge();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => edge.LongRepeatedTextFromLength(0)).ParamName, Is.EqualTo("length"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => edge.LongRepeatedTextFromLength(9)).ParamName, Is.EqualTo("length"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => edge.LongRepeatedTextFromLength(8)).ParamName, Is.EqualTo("length"));
        });
    }

    [Test]
    public void UnicodeCasesCoverTextAndCharacterArrayOnlyMalformedUtf16()
    {
        EdgeCaseRecord[] records = Fake.Create(147L).Edge().Unicode();
        EdgeCaseRecord malformed = records.Single(record => record.Name == "unpaired-high-surrogate");
        char[] codeUnits = (char[])malformed.Value;

        Assert.Multiple(() =>
        {
            Assert.That(Value(records, "right-to-left"), Is.EqualTo("مرحبا"));
            Assert.That((string)Value(records, "bidi-control"), Does.Contain("\u202E"));
            Assert.That(Value(records, "combining-mark"), Is.EqualTo("e\u0301"));
            Assert.That((string)Value(records, "emoji-sequence"), Does.Contain("\u200D"));
            Assert.That(((string)Value(records, "non-bmp")).Length, Is.EqualTo(2));
            Assert.That(codeUnits, Has.Length.EqualTo(1));
            Assert.That(char.IsHighSurrogate(codeUnits[0]), Is.True);
            Assert.That(records.Where(record => record != malformed).All(record => IsWellFormed((string)record.Value)), Is.True);
        });
    }

    [Test]
    public void TemporalCasesCoverRuntimeEpochCalendarOffsetAndDstBoundaries()
    {
        EdgeCaseRecord[] records = Fake.Create(147L).Edge().Temporal();

        Assert.Multiple(() =>
        {
            Assert.That(Value(records, "runtime-minimum"), Is.EqualTo(DateTimeOffset.MinValue));
            Assert.That(Value(records, "runtime-maximum"), Is.EqualTo(DateTimeOffset.MaxValue));
            Assert.That(Value(records, "unix-epoch"), Is.EqualTo(DateTimeOffset.UnixEpoch));
            Assert.That(((DateTimeOffset)Value(records, "leap-day")).Date, Is.EqualTo(new DateTime(2000, 2, 29)));
            Assert.That(((DateTimeOffset)Value(records, "offset-minimum")).Offset, Is.EqualTo(TimeSpan.FromHours(-14)));
            Assert.That(((DateTimeOffset)Value(records, "offset-maximum")).Offset, Is.EqualTo(TimeSpan.FromHours(14)));
            Assert.That(Value(records, "year-boundary"), Is.LessThan(RelatedValue(records, "year-boundary")));
            Assert.That(((DateTime)Value(records, "dst-gap-local")).Kind, Is.EqualTo(DateTimeKind.Unspecified));
            Assert.That(((DateTime)Value(records, "dst-overlap-local")).Kind, Is.EqualTo(DateTimeKind.Unspecified));
        });
    }

    [Test]
    public void IdentifierCasesReuseDocumentedMutationsAndAddCrossDomainShapes()
    {
        Fake fake = Fake.Create(147L);
        EdgeCaseRecord[] records = fake.Edge().Identifiers();
        IdentifierMutationKind[] mutations = Enum.GetValues<IdentifierMutationKind>();

        int index = 0;
        while (index < mutations.Length)
        {
            Assert.That(records[index].Value, Is.EqualTo(fake.Identifier().Invalid(mutations[index])));
            index++;
        }
        Assert.Multiple(() =>
        {
            Assert.That(records.Select(record => record.Name), Does.Contain("invalid-iban-country"));
            Assert.That(records.Select(record => record.Name), Does.Contain("malformed-idn"));
            Assert.That(records.Select(record => record.Name), Does.Contain("ambiguous-phone"));
            Assert.That(fake.Identifier().ValidateIdentifier((string)records[0].Value, IdentifierKind.Isbn10).ChecksumValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(fake.Identifier().ValidateIdentifier((string)records[1].Value, IdentifierKind.Ean8).StructuralValidity, Is.EqualTo(ValidityMode.Invalid));
        });
    }

    [Test]
    public void InjectionCasesAreExactOptInInertStrings()
    {
        EdgeCaseRecord[] records = Fake.Create(147L).Edge().Injections();

        Assert.Multiple(() =>
        {
            Assert.That(Value(records, "sql"), Is.EqualTo("' OR 1=1 --"));
            Assert.That(Value(records, "html"), Is.EqualTo("<script>alert(1)</script>"));
            Assert.That(Value(records, "spreadsheet"), Is.EqualTo("=1+1"));
            Assert.That(Value(records, "shell"), Is.EqualTo("$(touch /tmp/fake-edge-probe)"));
            Assert.That(Value(records, "path-traversal"), Is.EqualTo("../../etc/passwd"));
            Assert.That(Value(records, "header-splitting"), Is.EqualTo("value\r\nX-Fake: injected"));
            Assert.That(records.All(record => record.Value is string), Is.True);
        });
    }

    [Test]
    public void ContradictoryCasesRetainBothConflictingValues()
    {
        EdgeCaseRecord[] records = Fake.Create(147L).Edge().ContradictoryInputs();

        Assert.Multiple(() =>
        {
            Assert.That(Value(records, "reversed-numeric-range"), Is.EqualTo(10));
            Assert.That(RelatedValue(records, "reversed-numeric-range"), Is.EqualTo(1));
            Assert.That(Value(records, "negative-count"), Is.EqualTo(-1));
            Assert.That(Value(records, "empty-exclusive-time-range"), Is.EqualTo(RelatedValue(records, "empty-exclusive-time-range")));
            Assert.That(Value(records, "conflicting-required-value"), Is.Not.EqualTo(RelatedValue(records, "conflicting-required-value")));
            Assert.That(records.All(record => record.RelatedValue != null), Is.True);
        });
    }

    [Test]
    public void CallsReturnIndependentRecordsAndMutableValues()
    {
        EdgeProvider edge = Fake.Create(147L).Edge();
        EdgeCaseRecord[] first = edge.Unicode();
        EdgeCaseRecord[] second = edge.Unicode();
        first[0].Name = "changed";
        ((char[])first[^1].Value)[0] = 'x';

        Assert.Multiple(() =>
        {
            Assert.That(second[0].Name, Is.EqualTo("right-to-left"));
            Assert.That(char.IsHighSurrogate(((char[])second[^1].Value)[0]), Is.True);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(second[0], Is.Not.SameAs(first[0]));
        });
    }

    [Test]
    public void CategoryAndConfiguredCountLimitsAreValidated()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 3;
        EdgeProvider limited = Fake.Create(options).Edge();
        EdgeProvider edge = Fake.Create(147L).Edge();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => limited.EmptyNullWhitespace()).ParamName, Is.EqualTo("category"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => edge.Cases((EdgeCaseCategory)999)).ParamName, Is.EqualTo("category"));
        });
    }

    static object Value(EdgeCaseRecord[] records, string name)
    {
        return records.Single(record => record.Name == name).Value;
    }

    static object RelatedValue(EdgeCaseRecord[] records, string name)
    {
        return records.Single(record => record.Name == name).RelatedValue;
    }

    static bool IsWellFormed(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (char.IsHighSurrogate(value[index]))
            {
                if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                    return false;
                index += 2;
                continue;
            }
            if (char.IsLowSurrogate(value[index]))
                return false;
            index++;
        }
        return true;
    }
}
