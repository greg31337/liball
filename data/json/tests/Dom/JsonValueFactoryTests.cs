namespace json.Tests;

public class JsonValueFactoryTests
{
    [Test]
    public void FactoryCreatesAndClonesAllSevenValueKindsThroughItsInterface()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue uninitialized = factory.CloneValue(new JsonValue());
        JsonValue nullValue = factory.CreateNull();
        JsonValue falseValue = factory.CreateBoolean(false);
        JsonValue number = factory.CreateNumber("-0.00E+2");
        JsonValue text = factory.CreateString("text");
        JsonValue array = factory.CreateArray(new List<JsonValue>());
        JsonValue objectValue = factory.CreateObject(new List<JsonMember>());

        Assert.Multiple(() =>
        {
            Assert.That(uninitialized.Kind, Is.EqualTo(JsonValueKind.Uninitialized));
            Assert.That(nullValue.Kind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(falseValue.Kind, Is.EqualTo(JsonValueKind.Boolean));
            Assert.That(falseValue.BooleanValue, Is.False);
            Assert.That(number.Kind, Is.EqualTo(JsonValueKind.Number));
            Assert.That(number.Number?.Lexeme, Is.EqualTo("-0.00E+2"));
            Assert.That(text.Kind, Is.EqualTo(JsonValueKind.String));
            Assert.That(text.StringValue, Is.EqualTo("text"));
            Assert.That(array.Kind, Is.EqualTo(JsonValueKind.Array));
            Assert.That(array.ArrayItems, Is.Empty);
            Assert.That(objectValue.Kind, Is.EqualTo(JsonValueKind.Object));
            Assert.That(objectValue.ObjectMembers, Is.Empty);
        });
    }

    [TestCase("0", JsonNumberKind.Integer, false, false, false, false)]
    [TestCase("-0", JsonNumberKind.Integer, true, false, false, true)]
    [TestCase("12.340", JsonNumberKind.Fraction, false, true, false, false)]
    [TestCase("6E-10", JsonNumberKind.Exponent, false, false, true, false)]
    [TestCase("-0.00e-2", JsonNumberKind.FractionWithExponent, true, true, true, true)]
    public void NumberCreationPreservesAndClassifiesTheExactValidatedLexeme(
        string lexeme,
        JsonNumberKind kind,
        bool negative,
        bool fraction,
        bool exponent,
        bool negativeZero)
    {
        IJsonValueFactory factory = new JsonValueFactory();

        JsonValue throwing = factory.CreateNumber(lexeme);
        JsonValueResult result = factory.TryCreateNumber(lexeme);

        Assert.Multiple(() =>
        {
            Assert.That(throwing.Number?.Lexeme, Is.EqualTo(lexeme));
            Assert.That(throwing.Number?.Kind, Is.EqualTo(kind));
            Assert.That(throwing.Number?.IsNegative, Is.EqualTo(negative));
            Assert.That(throwing.Number?.HasFraction, Is.EqualTo(fraction));
            Assert.That(throwing.Number?.HasExponent, Is.EqualTo(exponent));
            Assert.That(throwing.Number?.IsNegativeZero, Is.EqualTo(negativeZero));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.DocumentComplete));
            Assert.That(result.Value?.Number?.Lexeme, Is.EqualTo(lexeme));
            Assert.That(result.Value, Is.Not.SameAs(throwing));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [TestCase("")]
    [TestCase("-")]
    [TestCase("+1")]
    [TestCase("01")]
    [TestCase("1.")]
    [TestCase("1e")]
    [TestCase("NaN")]
    [TestCase("1 2")]
    [TestCase("١")]
    public void InvalidNumberHasThrowingAndResultDiagnosticParity(string lexeme)
    {
        IJsonValueFactory factory = new JsonValueFactory();

        JsonValueResult result = factory.TryCreateNumber(lexeme);
        JsonParseException failure = Assert.Throws<JsonParseException>(() =>
            factory.CreateNumber(lexeme))
            ?? throw new InvalidOperationException("Expected an invalid-number failure.");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Status, Is.EqualTo(JsonReadStatus.Error));
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidNumber));
            Assert.That(failure.Diagnostic.Code, Is.EqualTo(result.Diagnostic?.Code));
            Assert.That(failure.Diagnostic.Message, Is.EqualTo(result.Diagnostic?.Message));
            Assert.That(failure.Diagnostic.SourceExcerpt,
                Is.EqualTo(result.Diagnostic?.SourceExcerpt));
        });
    }

    [Test]
    public void ArrayAndObjectCreationOwnTheirOrderedListContainers()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue first = factory.CreateNull();
        JsonValue second = factory.CreateBoolean(true);
        List<JsonValue> items = new List<JsonValue> { first, second };
        JsonValue array = factory.CreateArray(items);
        JsonMember firstMember = new JsonMember();
        firstMember.Name = "duplicate";
        firstMember.Value = first;
        firstMember.SourceOrdinal = 3;
        JsonMember secondMember = new JsonMember();
        secondMember.Name = "duplicate";
        secondMember.Value = second;
        secondMember.SourceOrdinal = 4;
        List<JsonMember> members = new List<JsonMember> { firstMember, secondMember };
        JsonValue objectValue = factory.CreateObject(members);

        items.Clear();
        members.Reverse();

        Assert.Multiple(() =>
        {
            Assert.That(array.ArrayItems, Is.Not.SameAs(items));
            Assert.That(array.ArrayItems, Is.EqualTo(new[] { first, second }));
            Assert.That(array.ArrayItems?[0], Is.SameAs(first));
            Assert.That(array.ArrayItems?[1], Is.SameAs(second));
            Assert.That(objectValue.ObjectMembers, Is.Not.SameAs(members));
            Assert.That(objectValue.ObjectMembers, Is.EqualTo(new[] { firstMember, secondMember }));
            Assert.That(objectValue.ObjectMembers?[0], Is.SameAs(firstMember));
            Assert.That(objectValue.ObjectMembers?[1], Is.SameAs(secondMember));
            Assert.That(objectValue.ObjectMembers?.Select(member => member.Name),
                Is.EqualTo(new[] { "duplicate", "duplicate" }));
            Assert.That(objectValue.ObjectMembers?.Select(member => member.SourceOrdinal),
                Is.EqualTo(new[] { 3, 4 }));
        });
    }

    [Test]
    public void DeepCloneCopiesEveryMutablePayloadAndSourceMetadata()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureSourceRanges = true;
        options.TrackLineInformation = true;
        JsonDocumentParser parser = new JsonDocumentParser();
        JsonDocument document = parser.ParseString(
            "\n{\"a\":[-0.00E+2,\"text\"],\"a\":true}",
            options);
        JsonValue source = document.Root
            ?? throw new InvalidOperationException("Expected a source root.");
        IJsonValueFactory factory = new JsonValueFactory();

        JsonValue clone = factory.CloneValue(source);
        List<JsonMember> sourceMembers = source.ObjectMembers
            ?? throw new InvalidOperationException("Expected source members.");
        List<JsonMember> cloneMembers = clone.ObjectMembers
            ?? throw new InvalidOperationException("Expected cloned members.");
        JsonValue sourceArray = sourceMembers[0].Value
            ?? throw new InvalidOperationException("Expected a source array.");
        JsonValue cloneArray = cloneMembers[0].Value
            ?? throw new InvalidOperationException("Expected a cloned array.");

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(cloneMembers, Is.Not.SameAs(sourceMembers));
            Assert.That(cloneMembers, Has.Count.EqualTo(2));
            Assert.That(cloneMembers.Select(member => member.Name),
                Is.EqualTo(new[] { "a", "a" }));
            Assert.That(cloneMembers.Select(member => member.SourceOrdinal),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(cloneMembers[0], Is.Not.SameAs(sourceMembers[0]));
            Assert.That(cloneMembers[0].NameRange, Is.Not.SameAs(sourceMembers[0].NameRange));
            Assert.That(cloneMembers[0].MemberRange, Is.Not.SameAs(sourceMembers[0].MemberRange));
            Assert.That(cloneArray, Is.Not.SameAs(sourceArray));
            Assert.That(cloneArray.ArrayItems, Is.Not.SameAs(sourceArray.ArrayItems));
            Assert.That(cloneArray.ArrayItems?[0].Number,
                Is.Not.SameAs(sourceArray.ArrayItems?[0].Number));
            Assert.That(cloneArray.ArrayItems?[0].Number?.Lexeme, Is.EqualTo("-0.00E+2"));
            Assert.That(cloneArray.ArrayItems?[1].StringValue, Is.EqualTo("text"));
            Assert.That(cloneMembers[1].Value?.BooleanValue, Is.True);
            Assert.That(clone.Range, Is.Not.SameAs(source.Range));
            Assert.That(clone.Range?.Start, Is.Not.SameAs(source.Range?.Start));
            Assert.That(clone.Range?.End, Is.Not.SameAs(source.Range?.End));
            Assert.That(clone.Range?.Start?.ByteOffset, Is.EqualTo(1));
            Assert.That(clone.Range?.Start?.Line, Is.EqualTo(2));
        });

        sourceMembers[0].Name = "changed";
        sourceMembers[0].SourceOrdinal = 99;
        sourceMembers[0].NameRange?.Start?.ByteOffset = 999;
        sourceArray.ArrayItems?[0].Number?.Lexeme = "7";
        sourceArray.ArrayItems?[1].StringValue = "changed";
        sourceArray.ArrayItems?.Clear();
        sourceMembers.Clear();
        source.Range?.Start?.ByteOffset = 999;

        Assert.Multiple(() =>
        {
            Assert.That(cloneMembers.Select(member => member.Name),
                Is.EqualTo(new[] { "a", "a" }));
            Assert.That(cloneMembers.Select(member => member.SourceOrdinal),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(cloneMembers[0].NameRange?.Start?.ByteOffset, Is.EqualTo(2));
            Assert.That(cloneArray.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(cloneArray.ArrayItems?[0].Number?.Lexeme, Is.EqualTo("-0.00E+2"));
            Assert.That(cloneArray.ArrayItems?[1].StringValue, Is.EqualTo("text"));
            Assert.That(clone.Range?.Start?.ByteOffset, Is.EqualTo(1));
        });
    }

    [Test]
    public void MutatingADeepCloneDoesNotChangeSourcePayloadsOrLocations()
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.CaptureSourceRanges = true;
        options.TrackLineInformation = true;
        options.TrackUnicodeScalarColumn = true;
        JsonDocument sourceDocument = new JsonDocumentParser().ParseString(
            "\n{\"duplicate\":[-0.00E+2,\"€\"],\"duplicate\":null}",
            options);
        JsonValue source = sourceDocument.Root
            ?? throw new InvalidOperationException("Expected a source root.");
        IJsonValueFactory factory = new JsonValueFactory();

        JsonValue clone = factory.CloneValue(source);
        List<JsonMember> cloneMembers = clone.ObjectMembers
            ?? throw new InvalidOperationException("Expected cloned members.");
        JsonValue cloneArray = cloneMembers[0].Value
            ?? throw new InvalidOperationException("Expected a cloned array.");

        clone.Range!.Start!.ByteOffset = 500;
        clone.Range.Start.Line = 500;
        clone.Range.Start.ByteColumn = 500;
        clone.Range.Start.ScalarColumn = 500;
        cloneMembers[0].Name = "changed";
        cloneMembers[0].SourceOrdinal = 500;
        cloneMembers[0].NameRange!.Start!.ByteOffset = 500;
        cloneMembers[0].MemberRange!.End!.ByteOffset = 500;
        cloneArray.Range!.Start!.ByteOffset = 500;
        cloneArray.ArrayItems![0].Number!.Lexeme = "500";
        cloneArray.ArrayItems[1].StringValue = "changed";
        cloneArray.ArrayItems.Clear();
        cloneMembers.RemoveAt(1);

        List<JsonMember> sourceMembers = source.ObjectMembers
            ?? throw new InvalidOperationException("Expected source members.");
        JsonValue sourceArray = sourceMembers[0].Value
            ?? throw new InvalidOperationException("Expected a source array.");

        Assert.Multiple(() =>
        {
            Assert.That(source.Range?.Start?.ByteOffset, Is.EqualTo(1));
            Assert.That(source.Range?.Start?.Line, Is.EqualTo(2));
            Assert.That(source.Range?.Start?.ByteColumn, Is.EqualTo(1));
            Assert.That(source.Range?.Start?.ScalarColumn, Is.EqualTo(1));
            Assert.That(sourceMembers, Has.Count.EqualTo(2));
            Assert.That(sourceMembers.Select(member => member.Name),
                Is.EqualTo(new[] { "duplicate", "duplicate" }));
            Assert.That(sourceMembers.Select(member => member.SourceOrdinal),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(sourceMembers[0].NameRange?.Start?.ByteOffset, Is.EqualTo(2));
            Assert.That(sourceMembers[0].MemberRange?.End?.ByteOffset, Is.EqualTo(30));
            Assert.That(sourceArray.Range?.Start?.ByteOffset, Is.EqualTo(14));
            Assert.That(sourceArray.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(sourceArray.ArrayItems?[0].Number?.Lexeme, Is.EqualTo("-0.00E+2"));
            Assert.That(sourceArray.ArrayItems?[1].StringValue, Is.EqualTo("€"));
        });
    }

    [Test]
    public void DeepClonePreservesSharedValueIdentityWithoutSharingTheSource()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue shared = factory.CreateString("shared");
        JsonValue source = factory.CreateArray(new List<JsonValue> { shared, shared });

        JsonValue clone = factory.CloneValue(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone.ArrayItems, Has.Count.EqualTo(2));
            Assert.That(clone.ArrayItems?[0], Is.SameAs(clone.ArrayItems?[1]));
            Assert.That(clone.ArrayItems?[0], Is.Not.SameAs(shared));
            Assert.That(clone.ArrayItems?[0].StringValue, Is.EqualTo("shared"));
        });
    }

    [Test]
    public void DeepCloneHandlesVeryDeepContainersIteratively()
    {
        const int depth = 10_000;
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue source = factory.CreateNull();
        for (int index = 0; index < depth; index += 1)
        {
            source = factory.CreateArray(new List<JsonValue> { source });
        }

        JsonValue clone = factory.CloneValue(source);
        JsonValue sourceCursor = source;
        JsonValue cloneCursor = clone;
        for (int index = 0; index < depth; index += 1)
        {
            Assert.That(cloneCursor, Is.Not.SameAs(sourceCursor), "depth " + index);
            sourceCursor = sourceCursor.ArrayItems?[0]
                ?? throw new InvalidOperationException("Expected a source array item.");
            cloneCursor = cloneCursor.ArrayItems?[0]
                ?? throw new InvalidOperationException("Expected a cloned array item.");
        }

        Assert.Multiple(() =>
        {
            Assert.That(sourceCursor.Kind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(cloneCursor.Kind, Is.EqualTo(JsonValueKind.Null));
            Assert.That(cloneCursor, Is.Not.SameAs(sourceCursor));
        });
    }

    [Test]
    public void IterativeCloneTerminatesAndPreservesASelfReference()
    {
        JsonValue source = new JsonValue();
        source.Kind = JsonValueKind.Array;
        source.ArrayItems = new List<JsonValue> { source };
        IJsonValueFactory factory = new JsonValueFactory();

        JsonValue clone = factory.CloneValue(source);

        Assert.Multiple(() =>
        {
            Assert.That(clone, Is.Not.SameAs(source));
            Assert.That(clone.ArrayItems, Has.Count.EqualTo(1));
            Assert.That(clone.ArrayItems?[0], Is.SameAs(clone));
        });
    }
}
