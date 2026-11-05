namespace json.Tests;

public class JsonValueValidatorTests
{
    [Test]
    public void ValidatorAcceptsCanonicalFactoryValuesAndParsedDocuments()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonMember first = CreateMember("number", factory.CreateNumber("-0.25E+2"), 0);
        JsonMember second = CreateMember(
            "items",
            factory.CreateArray(new List<JsonValue>
            {
                factory.CreateNull(),
                factory.CreateBoolean(true),
                factory.CreateString("𝄞")
            }),
            1);
        JsonValue value = factory.CreateObject(new List<JsonMember> { first, second });
        JsonReaderOptions options = new JsonReaderOptions();
        JsonDocument document = new JsonDocumentParser().ParseString(
            "{\"a\":[0,true,\"text\"]}",
            options);
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult valueResult = validator.ValidateValue(value, new JsonLimits());
        JsonOperationResult documentResult = validator.ValidateDocument(document, new JsonLimits());

        Assert.Multiple(() =>
        {
            Assert.That(valueResult.Success, Is.True);
            Assert.That(valueResult.Diagnostic, Is.Null);
            Assert.That(valueResult.BytesConsumed, Is.Zero);
            Assert.That(valueResult.BytesWritten, Is.Zero);
            Assert.That(documentResult.Success, Is.True);
            Assert.That(documentResult.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void ValidatorRejectsUninitializedUndefinedAndMismatchedValueShapes()
    {
        JsonValue uninitialized = new JsonValue();
        JsonValue undefined = new JsonValue();
        undefined.Kind = (JsonValueKind)99;
        JsonValue nullWithBoolean = new JsonValue();
        nullWithBoolean.Kind = JsonValueKind.Null;
        nullWithBoolean.BooleanValue = true;
        JsonValue nullWithNumber = new JsonValue();
        nullWithNumber.Kind = JsonValueKind.Null;
        nullWithNumber.Number = new JsonNumber();
        JsonValue nullWithArray = new JsonValue();
        nullWithArray.Kind = JsonValueKind.Null;
        nullWithArray.ArrayItems = new List<JsonValue>();
        JsonValue nullWithObject = new JsonValue();
        nullWithObject.Kind = JsonValueKind.Null;
        nullWithObject.ObjectMembers = new List<JsonMember>();
        JsonValue booleanWithString = new JsonValue();
        booleanWithString.Kind = JsonValueKind.Boolean;
        booleanWithString.StringValue = "inactive";
        JsonValue numberWithoutPayload = new JsonValue();
        numberWithoutPayload.Kind = JsonValueKind.Number;
        JsonValue stringWithoutPayload = new JsonValue();
        stringWithoutPayload.Kind = JsonValueKind.String;
        JsonValue arrayWithoutItems = new JsonValue();
        arrayWithoutItems.Kind = JsonValueKind.Array;
        JsonValue objectWithoutMembers = new JsonValue();
        objectWithoutMembers.Kind = JsonValueKind.Object;
        JsonValue[] invalid =
        {
            uninitialized,
            undefined,
            nullWithBoolean,
            nullWithNumber,
            nullWithArray,
            nullWithObject,
            booleanWithString,
            numberWithoutPayload,
            stringWithoutPayload,
            arrayWithoutItems,
            objectWithoutMembers
        };
        IJsonValueValidator validator = new JsonValueValidator();

        foreach (JsonValue value in invalid)
        {
            JsonOperationResult result = validator.ValidateValue(value, new JsonLimits());
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False, value.Kind.ToString());
                Assert.That(result.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.InvalidModel),
                    value.Kind.ToString());
            });
        }
    }

    [Test]
    public void ValidatorChecksEveryExactNumberFieldAfterPublicMutation()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue missingLexeme = factory.CreateNumber("1");
        missingLexeme.Number!.Lexeme = null;
        JsonValue invalidLexeme = factory.CreateNumber("1");
        invalidLexeme.Number!.Lexeme = "01";
        JsonValue invalidKind = factory.CreateNumber("1.0");
        invalidKind.Number!.Kind = (JsonNumberKind)99;
        JsonValue invalidNegative = factory.CreateNumber("-1");
        invalidNegative.Number!.IsNegative = false;
        JsonValue invalidFraction = factory.CreateNumber("1.0");
        invalidFraction.Number!.HasFraction = false;
        JsonValue invalidExponent = factory.CreateNumber("1e0");
        invalidExponent.Number!.HasExponent = false;
        JsonValue invalidNegativeZero = factory.CreateNumber("-0.0e+2");
        invalidNegativeZero.Number!.IsNegativeZero = false;
        JsonValue[] invalid =
        {
            missingLexeme,
            invalidLexeme,
            invalidKind,
            invalidNegative,
            invalidFraction,
            invalidExponent,
            invalidNegativeZero
        };
        IJsonValueValidator validator = new JsonValueValidator();

        foreach (JsonValue value in invalid)
        {
            JsonOperationResult result = validator.ValidateValue(value, new JsonLimits());
            Assert.Multiple(() =>
            {
                Assert.That(result.Success, Is.False);
                Assert.That(result.Diagnostic?.Code,
                    Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            });
        }
    }

    [Test]
    public void ValidatorRejectsUnpairedUtf16InValuesAndMemberNames()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue high = factory.CreateString(new string(new[] { '\ud800' }));
        JsonValue low = factory.CreateString(new string(new[] { '\udc00' }));
        JsonMember member = CreateMember(
            new string(new[] { '\ud800' }),
            factory.CreateNull(),
            0);
        JsonValue objectValue = factory.CreateObject(new List<JsonMember> { member });
        IJsonValueValidator validator = new JsonValueValidator();

        Assert.Multiple(() =>
        {
            Assert.That(
                validator.ValidateValue(high, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(
                validator.ValidateValue(low, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(
                validator.ValidateValue(objectValue, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    [Test]
    public void ValidatorRejectsEveryNullContainerAndMemberReference()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue nullItem = factory.CreateArray(new List<JsonValue> { null! });
        JsonValue nullMember = factory.CreateObject(new List<JsonMember> { null! });
        JsonMember namelessMember = new JsonMember();
        namelessMember.Value = factory.CreateNull();
        JsonValue nameless = factory.CreateObject(new List<JsonMember> { namelessMember });
        JsonMember valuelessMember = new JsonMember();
        valuelessMember.Name = "value";
        JsonValue valueless = factory.CreateObject(new List<JsonMember> { valuelessMember });
        IJsonValueValidator validator = new JsonValueValidator();

        foreach (JsonValue value in new[] { nullItem, nullMember, nameless, valueless })
        {
            JsonOperationResult result = validator.ValidateValue(value, new JsonLimits());
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        }
    }

    [Test]
    public void ValidatorDistinguishesCyclesFromSharedAcyclicValues()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue shared = factory.CreateString("shared");
        JsonValue acyclic = factory.CreateArray(new List<JsonValue> { shared, shared });
        JsonValue cyclic = new JsonValue();
        cyclic.Kind = JsonValueKind.Array;
        cyclic.ArrayItems = new List<JsonValue> { cyclic };
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult sharedResult = validator.ValidateValue(acyclic, new JsonLimits());
        JsonOperationResult cycleResult = validator.ValidateValue(cyclic, new JsonLimits());

        Assert.Multiple(() =>
        {
            Assert.That(sharedResult.Success, Is.True);
            Assert.That(cycleResult.Success, Is.False);
            Assert.That(cycleResult.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.ModelCycle));
        });
    }

    [Test]
    public void ValidatorDetectsAnIndirectCycleThroughAnObjectMember()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue array = factory.CreateArray(new List<JsonValue>());
        JsonMember member = CreateMember("back", array, 0);
        JsonValue objectValue = factory.CreateObject(new List<JsonMember> { member });
        array.ArrayItems!.Add(objectValue);
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult result = validator.ValidateValue(array, new JsonLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.ModelCycle));
            Assert.That(result.Diagnostic?.Depth, Is.EqualTo(2));
        });
    }

    [Test]
    public void ValidatorChecksMemberSourceOrdinalIntegrity()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonMember member = CreateMember("a", factory.CreateNull(), -2);
        JsonValue value = factory.CreateObject(new List<JsonMember> { member });
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult result = validator.ValidateValue(value, new JsonLimits());

        Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidModel));
    }

    [TestCase("depth", JsonDiagnosticCode.NestingDepthLimitExceeded)]
    [TestCase("array", JsonDiagnosticCode.ArrayItemLimitExceeded)]
    [TestCase("object", JsonDiagnosticCode.ObjectMemberLimitExceeded)]
    [TestCase("nodes", JsonDiagnosticCode.DomNodeLimitExceeded)]
    [TestCase("stringBytes", JsonDiagnosticCode.DecodedStringByteLimitExceeded)]
    [TestCase("stringUnits", JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded)]
    [TestCase("number", JsonDiagnosticCode.NumberCharacterLimitExceeded)]
    public void ValidatorEnforcesEveryModelGraphLimit(
        string limitName,
        JsonDiagnosticCode expectedCode)
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonLimits limits = new JsonLimits();
        JsonValue value = CreateLimitedValue(factory, limits, limitName);
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult result = validator.ValidateValue(value, limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(expectedCode));
        });
    }

    [TestCase("array", JsonDiagnosticCode.ArrayItemLimitExceeded)]
    [TestCase("object", JsonDiagnosticCode.ObjectMemberLimitExceeded)]
    public void ValidatorAllowsTheExactCollectionCountAndRejectsTheNextEntry(
        string collectionKind,
        JsonDiagnosticCode expectedCode)
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonLimits limits = new JsonLimits();
        JsonValue exact;
        JsonValue over;
        if (collectionKind == "array")
        {
            limits.MaximumArrayItems = 1;
            exact = factory.CreateArray(new List<JsonValue> { factory.CreateNull() });
            over = factory.CreateArray(new List<JsonValue>
            {
                factory.CreateNull(),
                factory.CreateNull()
            });
        }
        else
        {
            limits.MaximumObjectMembers = 1;
            exact = factory.CreateObject(new List<JsonMember>
            {
                CreateMember("a", factory.CreateNull(), 0)
            });
            over = factory.CreateObject(new List<JsonMember>
            {
                CreateMember("a", factory.CreateNull(), 0),
                CreateMember("b", factory.CreateNull(), 1)
            });
        }

        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult exactResult = validator.ValidateValue(exact, limits);
        JsonOperationResult overResult = validator.ValidateValue(over, limits);

        Assert.Multiple(() =>
        {
            Assert.That(exactResult.Success, Is.True);
            Assert.That(exactResult.Diagnostic, Is.Null);
            Assert.That(overResult.Success, Is.False);
            Assert.That(overResult.Diagnostic?.Code, Is.EqualTo(expectedCode));
        });
    }

    [Test]
    public void DocumentValidationChecksRootMetadataProfileAndSourceByteLimit()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonValueValidator validator = new JsonValueValidator();
        JsonDocument valid = CreateStrictDocument(factory.CreateNull());
        JsonOperationResult validResult = validator.ValidateDocument(valid, new JsonLimits());

        JsonDocument missingRoot = CreateStrictDocument(factory.CreateNull());
        missingRoot.Root = null;
        JsonDocument incompatible = CreateStrictDocument(factory.CreateNull());
        incompatible.BomPolicy = JsonBomPolicy.AllowInitial;
        JsonDocument undefined = CreateStrictDocument(factory.CreateNull());
        undefined.InputMode = (JsonInputMode)99;
        JsonDocument negativeBytes = CreateStrictDocument(factory.CreateNull());
        negativeBytes.SourceByteCount = -1;
        JsonDocument oversized = CreateStrictDocument(factory.CreateNull());
        oversized.SourceByteCount = 3;
        JsonDocument jcsSequence = CreateStrictDocument(factory.CreateNull());
        jcsSequence.ConformanceProfile = JsonConformanceProfile.Jcs;
        jcsSequence.InputMode = JsonInputMode.WhitespaceSeparated;
        JsonLimits byteLimits = new JsonLimits();
        byteLimits.MaximumDocumentBytes = 2;

        Assert.Multiple(() =>
        {
            Assert.That(validResult.Success, Is.True);
            Assert.That(
                validator.ValidateDocument(null!, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(
                validator.ValidateDocument(missingRoot, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(
                validator.ValidateDocument(incompatible, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(
                validator.ValidateDocument(undefined, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(
                validator.ValidateDocument(negativeBytes, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(
                validator.ValidateDocument(oversized, byteLimits).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DocumentByteLimitExceeded));
            Assert.That(
                validator.ValidateDocument(jcsSequence, new JsonLimits()).Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    [TestCase("profile")]
    [TestCase("duplicates")]
    [TestCase("unicode")]
    [TestCase("input")]
    [TestCase("bom")]
    public void DocumentValidationRejectsEveryUndefinedMetadataEnum(string fieldName)
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonDocument document = CreateStrictDocument(factory.CreateNull());
        if (fieldName == "profile")
        {
            document.ConformanceProfile = (JsonConformanceProfile)99;
        }
        else if (fieldName == "duplicates")
        {
            document.DuplicateKeyPolicy = (JsonDuplicateKeyPolicy)99;
        }
        else if (fieldName == "unicode")
        {
            document.UnicodePolicy = (JsonUnicodePolicy)99;
        }
        else if (fieldName == "input")
        {
            document.InputMode = (JsonInputMode)99;
        }
        else
        {
            document.BomPolicy = (JsonBomPolicy)99;
        }

        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult result = validator.ValidateDocument(document, new JsonLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    [Test]
    public void ValidatorCountsMembersAndValuesAtTheExactDomNodeLimit()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue value = factory.CreateObject(new List<JsonMember>
        {
            CreateMember(
                "items",
                factory.CreateArray(new List<JsonValue> { factory.CreateNull() }),
                0)
        });
        JsonLimits exactLimits = new JsonLimits();
        exactLimits.MaximumDomNodes = 4;
        JsonLimits failingLimits = new JsonLimits();
        failingLimits.MaximumDomNodes = 3;
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult exact = validator.ValidateValue(value, exactLimits);
        JsonOperationResult failing = validator.ValidateValue(value, failingLimits);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Diagnostic, Is.Null);
            Assert.That(failing.Success, Is.False);
            Assert.That(failing.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.DomNodeLimitExceeded));
        });
    }

    [Test]
    public void StrictDocumentValidationRejectsNoncharactersButValueValidationAcceptsThem()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue value = factory.CreateString("\uFDD0");
        JsonDocument document = CreateStrictDocument(value);
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult valueResult = validator.ValidateValue(value, new JsonLimits());
        JsonOperationResult documentResult = validator.ValidateDocument(document, new JsonLimits());

        Assert.Multiple(() =>
        {
            Assert.That(valueResult.Success, Is.True);
            Assert.That(documentResult.Success, Is.False);
            Assert.That(documentResult.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    [Test]
    public void InvalidLimitsAreReturnedAsInvalidOptionsDiagnostics()
    {
        JsonLimits limits = new JsonLimits();
        limits.MaximumDomNodes = -1;
        IJsonValueFactory factory = new JsonValueFactory();
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult result = validator.ValidateValue(factory.CreateNull(), limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
        });
    }

    [Test]
    public void VeryDeepAcyclicModelsAreValidatedIteratively()
    {
        const int depth = 10_000;
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue value = factory.CreateNull();
        for (int index = 0; index < depth; index += 1)
        {
            value = factory.CreateArray(new List<JsonValue> { value });
        }

        JsonLimits limits = new JsonLimits();
        limits.MaximumNestingDepth = depth;
        limits.MaximumDomNodes = depth + 1;
        IJsonValueValidator validator = new JsonValueValidator();

        JsonOperationResult result = validator.ValidateValue(value, limits);

        Assert.That(result.Success, Is.True);
    }

    static JsonValue CreateLimitedValue(
        IJsonValueFactory factory,
        JsonLimits limits,
        string limitName)
    {
        if (limitName == "depth")
        {
            limits.MaximumNestingDepth = 1;
            return factory.CreateArray(new List<JsonValue>
            {
                factory.CreateArray(new List<JsonValue>())
            });
        }

        if (limitName == "array")
        {
            limits.MaximumArrayItems = 1;
            return factory.CreateArray(new List<JsonValue>
            {
                factory.CreateNull(), factory.CreateNull()
            });
        }

        if (limitName == "object")
        {
            limits.MaximumObjectMembers = 1;
            return factory.CreateObject(new List<JsonMember>
            {
                CreateMember("a", factory.CreateNull(), 0),
                CreateMember("b", factory.CreateNull(), 1)
            });
        }

        if (limitName == "nodes")
        {
            limits.MaximumDomNodes = 2;
            return factory.CreateObject(new List<JsonMember>
            {
                CreateMember("a", factory.CreateNull(), 0)
            });
        }

        if (limitName == "stringBytes")
        {
            limits.MaximumDecodedStringBytes = 2;
            return factory.CreateString("€");
        }

        if (limitName == "stringUnits")
        {
            limits.MaximumDecodedStringCodeUnits = 1;
            return factory.CreateString("ab");
        }

        limits.MaximumNumberCharacters = 3;
        return factory.CreateNumber("1234");
    }

    static JsonMember CreateMember(string name, JsonValue value, int sourceOrdinal)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        member.SourceOrdinal = sourceOrdinal;
        return member;
    }

    static JsonDocument CreateStrictDocument(JsonValue root)
    {
        JsonDocument document = new JsonDocument();
        document.Root = root;
        document.ConformanceProfile = JsonConformanceProfile.IJson;
        document.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        document.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        document.InputMode = JsonInputMode.SingleValue;
        document.BomPolicy = JsonBomPolicy.Reject;
        return document;
    }
}
