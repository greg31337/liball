namespace json.Tests;

public class JsonValueTests
{
    [Test]
    public void DirectlyConstructedValueStartsUninitializedWithoutPayloads()
    {
        JsonValue value = new JsonValue();

        Assert.Multiple(() =>
        {
            Assert.That(value.Kind, Is.EqualTo(JsonValueKind.Uninitialized));
            Assert.That(value.BooleanValue, Is.False);
            Assert.That(value.Number, Is.Null);
            Assert.That(value.StringValue, Is.Null);
            Assert.That(value.ArrayItems, Is.Null);
            Assert.That(value.ObjectMembers, Is.Null);
            Assert.That(value.Range, Is.Null);
        });
    }

    [Test]
    public void ScalarKindsRetainTheirCanonicalPayloads()
    {
        JsonValue nullValue = new JsonValue();
        nullValue.Kind = JsonValueKind.Null;
        JsonValue falseValue = new JsonValue();
        falseValue.Kind = JsonValueKind.Boolean;
        falseValue.BooleanValue = false;
        JsonValue trueValue = new JsonValue();
        trueValue.Kind = JsonValueKind.Boolean;
        trueValue.BooleanValue = true;
        JsonValue stringValue = new JsonValue();
        stringValue.Kind = JsonValueKind.String;
        stringValue.StringValue = "e\u0301";

        Assert.Multiple(() =>
        {
            Assert.That(nullValue.Kind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(falseValue.Kind, Is.EqualTo(JsonValueKind.Boolean));
            Assert.That(falseValue.BooleanValue, Is.False);
            Assert.That(trueValue.BooleanValue, Is.True);
            Assert.That(stringValue.Kind, Is.EqualTo(JsonValueKind.String));
            Assert.That(stringValue.StringValue, Is.EqualTo("e\u0301"));
        });
    }

    [Test]
    public void NumberRetainsExactLexemeAndClassificationFlags()
    {
        JsonNumber number = new JsonNumber();
        number.Lexeme = "-0.00E+02";
        number.Kind = JsonNumberKind.FractionWithExponent;
        number.IsNegative = true;
        number.HasFraction = true;
        number.HasExponent = true;
        number.IsNegativeZero = true;
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Number;
        value.Number = number;

        Assert.Multiple(() =>
        {
            Assert.That(value.Kind, Is.EqualTo(JsonValueKind.Number));
            Assert.That(value.Number, Is.SameAs(number));
            Assert.That(number.Lexeme, Is.EqualTo("-0.00E+02"));
            Assert.That(number.Kind, Is.EqualTo(JsonNumberKind.FractionWithExponent));
            Assert.That(number.IsNegative, Is.True);
            Assert.That(number.HasFraction, Is.True);
            Assert.That(number.HasExponent, Is.True);
            Assert.That(number.IsNegativeZero, Is.True);
        });
    }

    [Test]
    public void ArrayRetainsCallerSuppliedItemOrder()
    {
        JsonValue first = new JsonValue();
        first.Kind = JsonValueKind.Null;
        JsonValue second = new JsonValue();
        second.Kind = JsonValueKind.Boolean;
        second.BooleanValue = true;
        List<JsonValue> items = new List<JsonValue> { first, second };
        JsonValue array = new JsonValue();
        array.Kind = JsonValueKind.Array;
        array.ArrayItems = items;

        Assert.Multiple(() =>
        {
            Assert.That(array.ArrayItems, Is.SameAs(items));
            Assert.That(array.ArrayItems, Is.EqualTo(new[] { first, second }));
        });

        items.Reverse();

        Assert.That(array.ArrayItems, Is.EqualTo(new[] { second, first }));
    }

    [Test]
    public void ObjectRetainsDuplicateNamesAndSourceOrder()
    {
        JsonSourceRange nameRange = CreateRange(2, 5);
        JsonSourceRange memberRange = CreateRange(2, 8);
        JsonValue firstValue = new JsonValue();
        firstValue.Kind = JsonValueKind.Boolean;
        firstValue.BooleanValue = true;
        JsonMember first = new JsonMember();

        Assert.That(first.SourceOrdinal, Is.EqualTo(-1));

        first.Name = "duplicate";
        first.Value = firstValue;
        first.NameRange = nameRange;
        first.MemberRange = memberRange;
        first.SourceOrdinal = 0;
        JsonMember second = new JsonMember();
        second.Name = "duplicate";
        second.Value = new JsonValue();
        second.Value.Kind = JsonValueKind.Null;
        second.SourceOrdinal = 1;
        List<JsonMember> members = new List<JsonMember> { first, second };
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Object;
        value.ObjectMembers = members;

        Assert.Multiple(() =>
        {
            Assert.That(value.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(value.ObjectMembers, Is.SameAs(members));
            Assert.That(value.ObjectMembers, Is.EqualTo(new[] { first, second }));
            Assert.That(value.ObjectMembers?[0].Name, Is.EqualTo("duplicate"));
            Assert.That(value.ObjectMembers?[1].Name, Is.EqualTo("duplicate"));
            Assert.That(first.Value, Is.SameAs(firstValue));
            Assert.That(first.NameRange, Is.SameAs(nameRange));
            Assert.That(first.MemberRange, Is.SameAs(memberRange));
            Assert.That(first.SourceOrdinal, Is.EqualTo(0));
            Assert.That(second.SourceOrdinal, Is.EqualTo(1));
        });
    }

    [Test]
    public void DocumentRetainsRootAndParseProfileMetadata()
    {
        JsonValue root = new JsonValue();
        root.Kind = JsonValueKind.Array;
        root.ArrayItems = new List<JsonValue>();
        JsonDocument document = new JsonDocument();
        document.Root = root;
        document.ConformanceProfile = JsonConformanceProfile.IJson;
        document.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        document.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        document.InputMode = JsonInputMode.JsonTextSequence;
        document.BomPolicy = JsonBomPolicy.AllowInitial;
        document.SourceByteCount = 42;

        Assert.Multiple(() =>
        {
            Assert.That(document.Root, Is.SameAs(root));
            Assert.That(document.ConformanceProfile, Is.EqualTo(JsonConformanceProfile.IJson));
            Assert.That(document.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.Reject));
            Assert.That(document.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.UnicodeScalar));
            Assert.That(document.InputMode, Is.EqualTo(JsonInputMode.JsonTextSequence));
            Assert.That(document.BomPolicy, Is.EqualTo(JsonBomPolicy.AllowInitial));
            Assert.That(document.SourceByteCount, Is.EqualTo(42));
        });
    }

    private static JsonSourceRange CreateRange(long startOffset, long endOffset)
    {
        JsonSourcePosition start = new JsonSourcePosition();
        start.ByteOffset = startOffset;
        JsonSourcePosition end = new JsonSourcePosition();
        end.ByteOffset = endOffset;
        JsonSourceRange range = new JsonSourceRange();
        range.Start = start;
        range.End = end;
        return range;
    }
}
