namespace json.Tests;

public class JsonPointerNavigatorTests
{
    [Test]
    public void ResolvesEveryRfc6901EvaluationExample()
    {
        JsonValue bar = StringValue("bar");
        JsonValue baz = StringValue("baz");
        JsonValue foo = ArrayValue(bar, baz);
        JsonValue zero = NumberValue("0");
        JsonValue one = NumberValue("1");
        JsonValue two = NumberValue("2");
        JsonValue three = NumberValue("3");
        JsonValue four = NumberValue("4");
        JsonValue five = NumberValue("5");
        JsonValue six = NumberValue("6");
        JsonValue seven = NumberValue("7");
        JsonValue eight = NumberValue("8");
        JsonValue root = ObjectValue(
            Member("foo", foo),
            Member("", zero),
            Member("a/b", one),
            Member("c%d", two),
            Member("e^f", three),
            Member("g|h", four),
            Member("i\\j", five),
            Member("k\"l", six),
            Member(" ", seven),
            Member("m~n", eight));
        IJsonPointerCodec codec = new JsonPointerCodec();
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        Assert.Multiple(() =>
        {
            AssertResolved(navigator, codec, root, "", root);
            AssertResolved(navigator, codec, root, "/foo", foo);
            AssertResolved(navigator, codec, root, "/foo/0", bar);
            AssertResolved(navigator, codec, root, "/", zero);
            AssertResolved(navigator, codec, root, "/a~1b", one);
            AssertResolved(navigator, codec, root, "/c%d", two);
            AssertResolved(navigator, codec, root, "/e^f", three);
            AssertResolved(navigator, codec, root, "/g|h", four);
            AssertResolved(navigator, codec, root, "/i\\j", five);
            AssertResolved(navigator, codec, root, "/k\"l", six);
            AssertResolved(navigator, codec, root, "/ ", seven);
            AssertResolved(navigator, codec, root, "/m~0n", eight);
        });
    }

    [Test]
    public void ResolvesRootAndNestedObjectArrayValues()
    {
        JsonValue first = StringValue("first");
        JsonValue second = StringValue("second");
        JsonValue items = ArrayValue(
            ObjectValue(Member("name", first)),
            ObjectValue(Member("name", second)));
        JsonValue root = ObjectValue(Member("items", items));
        IJsonPointerCodec codec = new JsonPointerCodec();
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        JsonValue rootResult = navigator.ResolvePointer(
            root,
            codec.ParseStringPointer(""),
            JsonDuplicateKeyPolicy.Preserve);
        JsonPointerResult nestedResult = navigator.TryResolvePointer(
            root,
            codec.ParseStringPointer("/items/1/name"),
            JsonDuplicateKeyPolicy.Preserve);

        Assert.Multiple(() =>
        {
            Assert.That(rootResult, Is.SameAs(root));
            Assert.That(nestedResult.Success, Is.True);
            Assert.That(nestedResult.Value, Is.SameAs(second));
            Assert.That(nestedResult.Values, Is.Null);
            Assert.That(nestedResult.FailedSegmentIndex, Is.EqualTo(-1));
            Assert.That(nestedResult.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void AppliesDuplicateSelectionPoliciesAndReturnsAllMatchesInOrder()
    {
        JsonValue first = StringValue("first");
        JsonValue second = StringValue("second");
        JsonValue root = ObjectValue(
            Member("name", first),
            Member("name", second));
        JsonPointer pointer = new JsonPointerCodec().ParseStringPointer("/name");
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        JsonValue preserve = navigator.ResolvePointer(
            root,
            pointer,
            JsonDuplicateKeyPolicy.Preserve);
        JsonValue keepFirst = navigator.ResolvePointer(
            root,
            pointer,
            JsonDuplicateKeyPolicy.KeepFirst);
        JsonValue keepLast = navigator.ResolvePointer(
            root,
            pointer,
            JsonDuplicateKeyPolicy.KeepLast);
        JsonPointerException? rejected = Assert.Throws<JsonPointerException>(() =>
            navigator.ResolvePointer(root, pointer, JsonDuplicateKeyPolicy.Reject));
        JsonPointerResult all = navigator.ResolveAllPointerMatches(root, pointer);

        Assert.Multiple(() =>
        {
            Assert.That(preserve, Is.SameAs(first));
            Assert.That(keepFirst, Is.SameAs(first));
            Assert.That(keepLast, Is.SameAs(second));
            Assert.That(rejected?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(rejected?.Diagnostic.Pointer, Is.EqualTo(""));
            Assert.That(all.Success, Is.True);
            Assert.That(all.Value, Is.SameAs(first));
            Assert.That(all.Values, Is.EqualTo(new[] { first, second }));
        });
    }

    [Test]
    public void AllMatchResolutionExpandsDuplicatesAtEveryObjectSegment()
    {
        JsonValue first = StringValue("first");
        JsonValue second = StringValue("second");
        JsonValue third = StringValue("third");
        JsonValue root = ObjectValue(
            Member(
                "branch",
                ObjectValue(Member("leaf", first), Member("leaf", second))),
            Member("branch", ObjectValue(Member("other", NullValue()))),
            Member("branch", ObjectValue(Member("leaf", third))));
        JsonPointer pointer = new JsonPointerCodec().ParseStringPointer("/branch/leaf");
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        JsonPointerResult result = navigator.ResolveAllPointerMatches(root, pointer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Value, Is.SameAs(first));
            Assert.That(result.Values, Is.EqualTo(new[] { first, second, third }));
            Assert.That(result.FailedSegmentIndex, Is.EqualTo(-1));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void ObjectNamesAreMatchedOrdinallyWithoutUnicodeNormalization()
    {
        JsonValue composed = StringValue("composed");
        JsonValue decomposed = StringValue("decomposed");
        JsonValue root = ObjectValue(
            Member("é", composed),
            Member("é", decomposed));
        IJsonPointerCodec codec = new JsonPointerCodec();
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        JsonValue composedResult = navigator.ResolvePointer(
            root,
            codec.ParseStringPointer("/é"),
            JsonDuplicateKeyPolicy.Preserve);
        JsonValue decomposedResult = navigator.ResolvePointer(
            root,
            codec.ParseStringPointer("/é"),
            JsonDuplicateKeyPolicy.Preserve);
        JsonPointerResult allComposed = navigator.ResolveAllPointerMatches(
            root,
            codec.ParseStringPointer("/é"));

        Assert.Multiple(() =>
        {
            Assert.That(composedResult, Is.SameAs(composed));
            Assert.That(decomposedResult, Is.SameAs(decomposed));
            Assert.That(composedResult, Is.Not.SameAs(decomposedResult));
            Assert.That(allComposed.Values, Is.EqualTo(new[] { composed }));
        });
    }

    [Test]
    public void ResolvesCanonicalArrayIndexesIncludingMultipleDigits()
    {
        JsonValue zero = StringValue("zero");
        JsonValue ten = StringValue("ten");
        JsonValue array = ArrayValue(
            zero,
            NullValue(),
            NullValue(),
            NullValue(),
            NullValue(),
            NullValue(),
            NullValue(),
            NullValue(),
            NullValue(),
            NullValue(),
            ten);
        JsonValue namedLeadingZero = StringValue("object member");
        JsonValue root = ObjectValue(
            Member("array", array),
            Member("01", namedLeadingZero));
        IJsonPointerCodec codec = new JsonPointerCodec();
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        Assert.Multiple(() =>
        {
            AssertResolved(navigator, codec, root, "/array/0", zero);
            AssertResolved(navigator, codec, root, "/array/10", ten);
            AssertResolved(navigator, codec, root, "/01", namedLeadingZero);
        });
    }

    [TestCase("", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("00", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("01", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("-0", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("-1", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("+0", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("+1", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase(" 1", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("1 ", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("1.0", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("1e0", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("١", JsonDiagnosticCode.InvalidPointerIndex)]
    [TestCase("2147483648", JsonDiagnosticCode.PointerIndexOutOfRange)]
    [TestCase("999999999999999999999999", JsonDiagnosticCode.PointerIndexOutOfRange)]
    [TestCase("2", JsonDiagnosticCode.PointerTargetNotFound)]
    [TestCase("2147483647", JsonDiagnosticCode.PointerTargetNotFound)]
    [TestCase("-", JsonDiagnosticCode.PointerTargetNotFound)]
    public void ArrayIndexFailuresIdentifyTheExactSegment(
        string token,
        JsonDiagnosticCode expectedCode)
    {
        JsonValue root = ObjectValue(
            Member("items", ArrayValue(NullValue(), NullValue())));
        JsonPointer pointer = new JsonPointerCodec().ParseStringPointer("/items/" + token);
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        JsonPointerResult result = navigator.TryResolvePointer(
            root,
            pointer,
            JsonDuplicateKeyPolicy.Preserve);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Values, Is.Null);
            Assert.That(result.FailedSegmentIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Diagnostic?.Pointer, Is.EqualTo("/items"));
            Assert.That(result.Diagnostic?.Depth, Is.EqualTo(1));
            Assert.That(result.Diagnostic?.ActualToken, Is.EqualTo(token));
        });
    }

    [Test]
    public void ExpectedEvaluationFailuresHaveThrowingAndResultParity()
    {
        JsonValue root = ObjectValue(
            Member("object", ObjectValue()),
            Member("scalar", StringValue("value")),
            Member("array", ArrayValue(NullValue())),
            Member("duplicate", StringValue("first")),
            Member("duplicate", StringValue("last")));
        IJsonPointerCodec codec = new JsonPointerCodec();
        IJsonPointerNavigator navigator = new JsonPointerNavigator();

        AssertFailureParity(
            navigator,
            root,
            codec.ParseStringPointer("/object/missing"),
            JsonDuplicateKeyPolicy.Preserve,
            JsonDiagnosticCode.PointerTargetNotFound,
            1,
            "/object",
            "missing");
        AssertFailureParity(
            navigator,
            root,
            codec.ParseStringPointer("/scalar/child"),
            JsonDuplicateKeyPolicy.Preserve,
            JsonDiagnosticCode.PointerTypeMismatch,
            1,
            "/scalar",
            "child");
        AssertFailureParity(
            navigator,
            root,
            codec.ParseStringPointer("/array/01"),
            JsonDuplicateKeyPolicy.Preserve,
            JsonDiagnosticCode.InvalidPointerIndex,
            1,
            "/array",
            "01");
        AssertFailureParity(
            navigator,
            root,
            codec.ParseStringPointer("/array/2147483648"),
            JsonDuplicateKeyPolicy.Preserve,
            JsonDiagnosticCode.PointerIndexOutOfRange,
            1,
            "/array",
            "2147483648");
        AssertFailureParity(
            navigator,
            root,
            codec.ParseStringPointer("/duplicate"),
            JsonDuplicateKeyPolicy.Reject,
            JsonDiagnosticCode.DuplicatePropertyName,
            0,
            "",
            "duplicate");
    }

    [Test]
    public void RejectsMalformedPointerAndValueModels()
    {
        IJsonPointerNavigator navigator = new JsonPointerNavigator();
        JsonValue validRoot = NullValue();
        JsonPointer missingTokens = new JsonPointer();
        JsonPointer rootPointer = new JsonPointerCodec().ParseStringPointer("");
        JsonValue malformedArray = new JsonValue();
        malformedArray.Kind = JsonValueKind.Array;

        JsonPointerResult malformedPointer = navigator.TryResolvePointer(
            validRoot,
            missingTokens,
            JsonDuplicateKeyPolicy.Preserve);
        JsonPointerResult malformedValue = navigator.TryResolvePointer(
            malformedArray,
            new JsonPointerCodec().ParseStringPointer("/0"),
            JsonDuplicateKeyPolicy.Preserve);
        JsonPointerResult invalidPolicy = navigator.TryResolvePointer(
            validRoot,
            rootPointer,
            (JsonDuplicateKeyPolicy)999);

        Assert.Multiple(() =>
        {
            Assert.That(malformedPointer.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidPointerSyntax));
            Assert.That(malformedPointer.FailedSegmentIndex, Is.EqualTo(-1));
            Assert.That(malformedValue.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(malformedValue.FailedSegmentIndex, Is.EqualTo(0));
            Assert.That(invalidPolicy.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
        });
    }

    static JsonMember Member(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }

    static void AssertResolved(
        IJsonPointerNavigator navigator,
        IJsonPointerCodec codec,
        JsonValue root,
        string representation,
        JsonValue expected)
    {
        JsonValue result = navigator.ResolvePointer(
            root,
            codec.ParseStringPointer(representation),
            JsonDuplicateKeyPolicy.Preserve);
        Assert.That(result, Is.SameAs(expected), representation);
    }

    static void AssertFailureParity(
        IJsonPointerNavigator navigator,
        JsonValue root,
        JsonPointer pointer,
        JsonDuplicateKeyPolicy duplicateKeyPolicy,
        JsonDiagnosticCode expectedCode,
        int expectedSegmentIndex,
        string expectedPointer,
        string expectedToken)
    {
        JsonPointerResult result = navigator.TryResolvePointer(
            root,
            pointer,
            duplicateKeyPolicy);
        JsonPointerException? exception = Assert.Throws<JsonPointerException>(() =>
            navigator.ResolvePointer(root, pointer, duplicateKeyPolicy));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(result.Values, Is.Null);
            Assert.That(result.FailedSegmentIndex, Is.EqualTo(expectedSegmentIndex));
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Diagnostic?.Pointer, Is.EqualTo(expectedPointer));
            Assert.That(result.Diagnostic?.Depth, Is.EqualTo(expectedSegmentIndex));
            Assert.That(result.Diagnostic?.ActualToken, Is.EqualTo(expectedToken));
            Assert.That(exception?.Diagnostic.Code,
                Is.EqualTo(result.Diagnostic?.Code));
            Assert.That(exception?.Diagnostic.Message,
                Is.EqualTo(result.Diagnostic?.Message));
            Assert.That(exception?.Diagnostic.Pointer,
                Is.EqualTo(result.Diagnostic?.Pointer));
            Assert.That(exception?.Diagnostic.Depth,
                Is.EqualTo(result.Diagnostic?.Depth));
            Assert.That(exception?.Diagnostic.ExpectedTokens,
                Is.EqualTo(result.Diagnostic?.ExpectedTokens));
            Assert.That(exception?.Diagnostic.ActualToken,
                Is.EqualTo(result.Diagnostic?.ActualToken));
        });
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

    static JsonValue NumberValue(string lexeme)
    {
        IJsonValueFactory factory = new JsonValueFactory();
        return factory.CreateNumber(lexeme);
    }

    static JsonValue NullValue()
    {
        JsonValue value = new JsonValue();
        value.Kind = JsonValueKind.Null;
        return value;
    }
}
