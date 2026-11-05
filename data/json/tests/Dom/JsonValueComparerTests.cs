namespace json.Tests;

public class JsonValueComparerTests
{
    [Test]
    public void ComparesEveryScalarKindThroughTheInterface()
    {
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.Multiple(() =>
        {
            Assert.That(comparer.CompareValues(
                NullValue(),
                NullValue(),
                JsonComparisonMode.ExactModel), Is.True);
            Assert.That(comparer.CompareValues(
                BooleanValue(true),
                BooleanValue(true),
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                BooleanValue(true),
                BooleanValue(false),
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                StringValue("é"),
                StringValue("é"),
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                StringValue("1"),
                NumberValue("1"),
                JsonComparisonMode.JsonSemantic), Is.False);
        });
    }

    [Test]
    public void SemanticNumbersIgnoreSpellingWithoutLosingPrecision()
    {
        IJsonValueComparer comparer = new JsonValueComparer();
        JsonValue integer = NumberValue("1");
        JsonValue fraction = NumberValue("1.000");
        JsonValue exponent = NumberValue("1000e-3");
        JsonValue negativeZero = NumberValue("-0.000e999999999999999999999");
        JsonValue positiveZero = NumberValue("0");
        JsonValue huge = NumberValue("1e1000000000000000000000000");
        JsonValue equivalentHuge = NumberValue("10e999999999999999999999999");

        Assert.Multiple(() =>
        {
            Assert.That(comparer.CompareValues(
                integer,
                fraction,
                JsonComparisonMode.ExactModel), Is.False);
            Assert.That(comparer.CompareValues(
                integer,
                fraction,
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                integer,
                exponent,
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                negativeZero,
                positiveZero,
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                huge,
                equivalentHuge,
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                NumberValue("1e-1000000000000000000000000"),
                NumberValue("0"),
                JsonComparisonMode.JsonSemantic), Is.False);
        });
    }

    [TestCase("1", "1.0", true)]
    [TestCase("1", "1e0", true)]
    [TestCase("100", "1e2", true)]
    [TestCase("0.00100", "1e-3", true)]
    [TestCase("-12.50", "-125e-1", true)]
    [TestCase("0", "-0", true)]
    [TestCase("-0.0e-999", "0e+999", true)]
    [TestCase("1", "1.0001", false)]
    [TestCase("1e2", "10", false)]
    [TestCase("-1", "1", false)]
    [TestCase("1e-2", "0.1", false)]
    public void SemanticNumberComparisonUsesExactDecimalValues(
        string leftLexeme,
        string rightLexeme,
        bool expected)
    {
        IJsonValueComparer comparer = new JsonValueComparer();

        bool semantic = comparer.CompareValues(
            NumberValue(leftLexeme),
            NumberValue(rightLexeme),
            JsonComparisonMode.JsonSemantic);
        bool exact = comparer.CompareValues(
            NumberValue(leftLexeme),
            NumberValue(rightLexeme),
            JsonComparisonMode.ExactModel);

        Assert.Multiple(() =>
        {
            Assert.That(semantic, Is.EqualTo(expected));
            Assert.That(exact, Is.EqualTo(leftLexeme == rightLexeme));
        });
    }

    [Test]
    public void StringsCompareByExactUtf16CodeUnitsWithoutNormalizationOrFolding()
    {
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.Multiple(() =>
        {
            Assert.That(comparer.CompareValues(
                StringValue("𝄞\0日本語"),
                StringValue("𝄞\0日本語"),
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                StringValue("é"),
                StringValue("é"),
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                StringValue("Name"),
                StringValue("name"),
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                StringValue("a\0b"),
                StringValue("a\0c"),
                JsonComparisonMode.ExactModel), Is.False);
        });
    }

    [Test]
    public void SemanticObjectsIgnoreDistinctNameOrderAndRetainDuplicateOrder()
    {
        JsonValue left = ObjectValue(
            Member("a", NumberValue("1")),
            Member("b", BooleanValue(true)),
            Member("a", NumberValue("2")));
        JsonValue reorderedNames = ObjectValue(
            Member("b", BooleanValue(true)),
            Member("a", NumberValue("1.0")),
            Member("a", NumberValue("2e0")));
        JsonValue reorderedDuplicates = ObjectValue(
            Member("b", BooleanValue(true)),
            Member("a", NumberValue("2")),
            Member("a", NumberValue("1")));
        JsonValue arrayForward = ArrayValue(NumberValue("1"), NumberValue("2"));
        JsonValue arrayReverse = ArrayValue(NumberValue("2"), NumberValue("1"));
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.Multiple(() =>
        {
            Assert.That(comparer.CompareValues(
                left,
                reorderedNames,
                JsonComparisonMode.ExactModel), Is.False);
            Assert.That(comparer.CompareValues(
                left,
                reorderedNames,
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                left,
                reorderedDuplicates,
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                arrayForward,
                arrayReverse,
                JsonComparisonMode.JsonSemantic), Is.False);
        });
    }

    [Test]
    public void ArraysRemainOrderedAndObjectsUseExactOrdinalNames()
    {
        JsonValue nestedLeft = ArrayValue(
            NullValue(),
            ArrayValue(NumberValue("1"), StringValue("x")));
        JsonValue nestedEquivalent = ArrayValue(
            NullValue(),
            ArrayValue(NumberValue("1.0"), StringValue("x")));
        JsonValue nestedReordered = ArrayValue(
            ArrayValue(NumberValue("1"), StringValue("x")),
            NullValue());
        JsonValue composedName = ObjectValue(Member("é", NullValue()));
        JsonValue decomposedName = ObjectValue(Member("é", NullValue()));
        JsonValue upperName = ObjectValue(Member("Name", NullValue()));
        JsonValue lowerName = ObjectValue(Member("name", NullValue()));
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.Multiple(() =>
        {
            Assert.That(comparer.CompareValues(
                nestedLeft,
                nestedEquivalent,
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                nestedLeft,
                nestedEquivalent,
                JsonComparisonMode.ExactModel), Is.False);
            Assert.That(comparer.CompareValues(
                nestedLeft,
                nestedReordered,
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                nestedLeft,
                ArrayValue(NullValue()),
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                composedName,
                decomposedName,
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                upperName,
                lowerName,
                JsonComparisonMode.JsonSemantic), Is.False);
        });
    }

    [Test]
    public void DuplicateNamesRequireEqualMultiplicityAndOccurrenceOrder()
    {
        JsonValue left = ObjectValue(
            Member("a", NumberValue("1")),
            Member("b", NullValue()),
            Member("a", NumberValue("2")),
            Member("c", BooleanValue(true)));
        JsonValue interleaved = ObjectValue(
            Member("c", BooleanValue(true)),
            Member("a", NumberValue("1.0")),
            Member("a", NumberValue("2e0")),
            Member("b", NullValue()));
        JsonValue missingOccurrence = ObjectValue(
            Member("a", NumberValue("1")),
            Member("b", NullValue()),
            Member("c", BooleanValue(true)));
        JsonValue reversedOccurrences = ObjectValue(
            Member("c", BooleanValue(true)),
            Member("a", NumberValue("2")),
            Member("a", NumberValue("1")),
            Member("b", NullValue()));
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.Multiple(() =>
        {
            Assert.That(comparer.CompareValues(
                left,
                interleaved,
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                left,
                interleaved,
                JsonComparisonMode.ExactModel), Is.False);
            Assert.That(comparer.CompareValues(
                left,
                missingOccurrence,
                JsonComparisonMode.JsonSemantic), Is.False);
            Assert.That(comparer.CompareValues(
                left,
                reversedOccurrences,
                JsonComparisonMode.JsonSemantic), Is.False);
        });
    }

    [Test]
    public void SharedAcyclicSubgraphsCompareNormally()
    {
        JsonValue leftShared = ObjectValue(Member("value", NumberValue("1")));
        JsonValue rightShared = ObjectValue(Member("value", NumberValue("1.0")));
        JsonValue left = ArrayValue(leftShared, leftShared);
        JsonValue right = ArrayValue(rightShared, rightShared);
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.That(comparer.CompareValues(
            left,
            right,
            JsonComparisonMode.JsonSemantic), Is.True);
    }

    [Test]
    public void ComparisonUsesAnExplicitStackForDeepGraphs()
    {
        const int depth = 2048;
        JsonValue left = NumberValue("1");
        JsonValue right = NumberValue("1.0");
        int index = 0;
        while (index < depth)
        {
            left = ArrayValue(left);
            right = ArrayValue(right);
            index++;
        }

        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.Multiple(() =>
        {
            Assert.That(comparer.CompareValues(
                left,
                right,
                JsonComparisonMode.JsonSemantic), Is.True);
            Assert.That(comparer.CompareValues(
                left,
                right,
                JsonComparisonMode.ExactModel), Is.False);
        });
    }

    [Test]
    public void RejectsInvalidModesModelsAndCycles()
    {
        JsonValue valid = NullValue();
        JsonValue malformed = new JsonValue();
        malformed.Kind = JsonValueKind.String;
        JsonValue cyclic = ArrayValue();
        cyclic.ArrayItems!.Add(cyclic);
        IJsonValueComparer comparer = new JsonValueComparer();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                comparer.CompareValues(valid, valid, (JsonComparisonMode)999));
            Assert.Throws<InvalidOperationException>(() =>
                comparer.CompareValues(
                    malformed,
                    malformed,
                    JsonComparisonMode.ExactModel));
            Assert.Throws<InvalidOperationException>(() =>
                comparer.CompareValues(
                    cyclic,
                    cyclic,
                    JsonComparisonMode.JsonSemantic));
            Assert.Throws<ArgumentNullException>(() =>
                comparer.CompareValues(
                    null!,
                    valid,
                    JsonComparisonMode.ExactModel));
        });
    }

    [Test]
    public void RejectsEveryRepresentativeMutableModelViolation()
    {
        JsonValue uninitialized = new JsonValue();
        JsonValue undefined = new JsonValue();
        undefined.Kind = (JsonValueKind)999;
        JsonValue mismatchedPayload = NullValue();
        mismatchedPayload.BooleanValue = true;
        JsonValue malformedNumber = NumberValue("1.0");
        malformedNumber.Number!.HasFraction = false;
        JsonValue invalidString = StringValue(new string(new[] { '\ud800' }));
        JsonValue missingArrayItems = new JsonValue();
        missingArrayItems.Kind = JsonValueKind.Array;
        JsonValue nullArrayItem = ArrayValue();
        nullArrayItem.ArrayItems!.Add(null!);
        JsonValue missingObjectMembers = new JsonValue();
        missingObjectMembers.Kind = JsonValueKind.Object;
        JsonValue nullObjectMember = ObjectValue();
        nullObjectMember.ObjectMembers!.Add(null!);
        JsonMember namelessMember = new JsonMember();
        namelessMember.Value = NullValue();
        JsonValue nameless = ObjectValue(namelessMember);
        JsonMember valuelessMember = new JsonMember();
        valuelessMember.Name = "value";
        JsonValue valueless = ObjectValue(valuelessMember);
        JsonMember invalidOrdinalMember = Member("value", NullValue());
        invalidOrdinalMember.SourceOrdinal = -2;
        JsonValue invalidOrdinal = ObjectValue(invalidOrdinalMember);
        JsonValue[] invalidValues =
        {
            uninitialized,
            undefined,
            mismatchedPayload,
            malformedNumber,
            invalidString,
            missingArrayItems,
            nullArrayItem,
            missingObjectMembers,
            nullObjectMember,
            nameless,
            valueless,
            invalidOrdinal
        };
        IJsonValueComparer comparer = new JsonValueComparer();

        foreach (JsonValue invalid in invalidValues)
        {
            Assert.Multiple(() =>
            {
                Assert.Throws<InvalidOperationException>(() =>
                    comparer.CompareValues(
                        invalid,
                        NullValue(),
                        JsonComparisonMode.ExactModel),
                    invalid.Kind.ToString());
                Assert.Throws<InvalidOperationException>(() =>
                    comparer.CompareValues(
                        NullValue(),
                        invalid,
                        JsonComparisonMode.JsonSemantic),
                    invalid.Kind.ToString());
            });
        }
    }

    static JsonMember Member(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static JsonValue ObjectValue(params JsonMember[] members)
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Object;
        value.ObjectMembers = new List<JsonMember>(members);
        return value;
    }

    static JsonValue ArrayValue(params JsonValue[] items)
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Array;
        value.ArrayItems = new List<JsonValue>(items);
        return value;
    }

    static JsonValue StringValue(string text)
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.String;
        value.StringValue = text;
        return value;
    }

    static JsonValue BooleanValue(bool booleanValue)
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Boolean;
        value.BooleanValue = booleanValue;
        return value;
    }

    static JsonValue NumberValue(string lexeme)
    {
        return new JsonValueFactory().CreateNumber(lexeme);
    }

    static JsonValue NullValue()
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Null;
        return value;
    }
}
