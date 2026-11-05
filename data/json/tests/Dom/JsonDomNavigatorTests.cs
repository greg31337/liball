namespace json.Tests;

public class JsonDomNavigatorTests
{
    [Test]
    public void ArrayReadsAndMutationsPreserveStoredOrderAndIdentity()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue first = factory.CreateString("first");
        JsonValue second = factory.CreateString("second");
        JsonValue inserted = factory.CreateString("inserted");
        JsonValue replacement = factory.CreateString("replacement");
        JsonValue appended = factory.CreateString("appended");
        JsonValue array = factory.CreateArray(new List<JsonValue> { first, second });
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonOperationResult insertResult = navigator.InsertArrayItem(array, 1, inserted);
        JsonOperationResult replaceResult = navigator.ReplaceArrayItem(array, 2, replacement);
        JsonOperationResult appendResult = navigator.AppendArrayItem(array, appended);
        JsonOperationResult removeResult = navigator.RemoveArrayItem(array, 0);
        List<JsonValue> items = array.ArrayItems
            ?? throw new InvalidOperationException("Expected array items.");

        Assert.Multiple(() =>
        {
            Assert.That(navigator.GetArrayItem(array, 0), Is.SameAs(inserted));
            Assert.That(items, Is.EqualTo(new[] { inserted, replacement, appended }));
            Assert.That(insertResult.Success, Is.True);
            Assert.That(replaceResult.Success, Is.True);
            Assert.That(appendResult.Success, Is.True);
            Assert.That(removeResult.Success, Is.True);
            Assert.That(insertResult.Diagnostic, Is.Null);
            Assert.That(insertResult.BytesConsumed, Is.Zero);
            Assert.That(insertResult.BytesWritten, Is.Zero);
        });

        JsonOperationResult clearResult = navigator.ClearArrayItems(array);

        Assert.Multiple(() =>
        {
            Assert.That(clearResult.Success, Is.True);
            Assert.That(items, Is.Empty);
        });
    }

    [Test]
    public void ArrayMutationFailuresDoNotChangeTheArray()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue retained = factory.CreateNull();
        JsonValue array = factory.CreateArray(new List<JsonValue> { retained });
        JsonValue scalar = factory.CreateBoolean(true);
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonOperationResult negativeInsert = navigator.InsertArrayItem(array, -1, scalar);
        JsonOperationResult lateInsert = navigator.InsertArrayItem(array, 2, scalar);
        JsonOperationResult negativeReplace = navigator.ReplaceArrayItem(array, -1, scalar);
        JsonOperationResult lateReplace = navigator.ReplaceArrayItem(array, 1, scalar);
        JsonOperationResult negativeRemove = navigator.RemoveArrayItem(array, -1);
        JsonOperationResult lateRemove = navigator.RemoveArrayItem(array, 1);
        JsonOperationResult wrongType = navigator.AppendArrayItem(scalar, retained);
        JsonOperationResult nullItem = navigator.AppendArrayItem(array, null!);

        Assert.Multiple(() =>
        {
            Assert.That(array.ArrayItems, Is.EqualTo(new[] { retained }));
            Assert.That(negativeInsert.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(lateInsert.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(negativeReplace.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(lateReplace.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(negativeRemove.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(lateRemove.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(wrongType.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTypeMismatch));
            Assert.That(nullItem.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    [Test]
    public void ArrayMutationsAcceptEveryLegalEdgeAndCanClearAnEmptyArray()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue middle = factory.CreateString("middle");
        JsonValue first = factory.CreateString("first");
        JsonValue last = factory.CreateString("last");
        JsonValue firstReplacement = factory.CreateString("first replacement");
        JsonValue lastReplacement = factory.CreateString("last replacement");
        JsonValue array = factory.CreateArray(new List<JsonValue> { middle });
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonOperationResult insertFirst = navigator.InsertArrayItem(array, 0, first);
        JsonOperationResult insertLast = navigator.InsertArrayItem(
            array,
            array.ArrayItems!.Count,
            last);
        JsonOperationResult replaceFirst = navigator.ReplaceArrayItem(
            array,
            0,
            firstReplacement);
        JsonOperationResult replaceLast = navigator.ReplaceArrayItem(
            array,
            array.ArrayItems.Count - 1,
            lastReplacement);

        Assert.Multiple(() =>
        {
            Assert.That(insertFirst.Success, Is.True);
            Assert.That(insertLast.Success, Is.True);
            Assert.That(replaceFirst.Success, Is.True);
            Assert.That(replaceLast.Success, Is.True);
            Assert.That(navigator.GetArrayItem(array, 0), Is.SameAs(firstReplacement));
            Assert.That(navigator.GetArrayItem(array, 2), Is.SameAs(lastReplacement));
            Assert.That(array.ArrayItems,
                Is.EqualTo(new[] { firstReplacement, middle, lastReplacement }));
        });

        JsonOperationResult removeLast = navigator.RemoveArrayItem(array, 2);
        JsonOperationResult removeFirst = navigator.RemoveArrayItem(array, 0);
        JsonOperationResult clearOnce = navigator.ClearArrayItems(array);
        JsonOperationResult clearAgain = navigator.ClearArrayItems(array);

        Assert.Multiple(() =>
        {
            Assert.That(removeLast.Success, Is.True);
            Assert.That(removeFirst.Success, Is.True);
            Assert.That(clearOnce.Success, Is.True);
            Assert.That(clearAgain.Success, Is.True);
            Assert.That(array.ArrayItems, Is.Empty);
        });
    }

    [Test]
    public void ArrayReadsReportBoundsAndContainerFailures()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue array = factory.CreateArray(new List<JsonValue>());
        JsonValue scalar = factory.CreateNull();
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonPointerException? negative = Assert.Throws<JsonPointerException>(() =>
            navigator.GetArrayItem(array, -1));
        JsonPointerException? missing = Assert.Throws<JsonPointerException>(() =>
            navigator.GetArrayItem(array, 0));
        JsonPointerException? mismatch = Assert.Throws<JsonPointerException>(() =>
            navigator.GetArrayItem(scalar, 0));

        Assert.Multiple(() =>
        {
            Assert.That(negative?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(missing?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(mismatch?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTypeMismatch));
        });
    }

    [Test]
    public void ArrayOperationsReportMalformedModelsWithoutThrowingFromMutations()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue missingItems = new JsonValue();
        missingItems.Kind = JsonValueKind.Array;
        JsonValue nullItem = factory.CreateArray(new List<JsonValue> { null! });
        JsonValue replacement = factory.CreateNull();
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonPointerException? missingListRead = Assert.Throws<JsonPointerException>(() =>
            navigator.GetArrayItem(missingItems, 0));
        JsonPointerException? nullItemRead = Assert.Throws<JsonPointerException>(() =>
            navigator.GetArrayItem(nullItem, 0));
        JsonOperationResult append = navigator.AppendArrayItem(missingItems, replacement);
        JsonOperationResult insert = navigator.InsertArrayItem(missingItems, 0, replacement);
        JsonOperationResult replace = navigator.ReplaceArrayItem(missingItems, 0, replacement);
        JsonOperationResult remove = navigator.RemoveArrayItem(missingItems, 0);
        JsonOperationResult clear = navigator.ClearArrayItems(missingItems);

        Assert.Multiple(() =>
        {
            Assert.That(missingListRead?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            Assert.That(nullItemRead?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            AssertFailureResult(append, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(insert, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(replace, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(remove, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(clear, JsonDiagnosticCode.InvalidModel);
        });
    }

    [TestCase(JsonDuplicateKeyPolicy.Preserve, "first")]
    [TestCase(JsonDuplicateKeyPolicy.KeepFirst, "first")]
    [TestCase(JsonDuplicateKeyPolicy.KeepLast, "last")]
    public void ObjectLookupAppliesRetainingDuplicatePolicies(
        JsonDuplicateKeyPolicy policy,
        string expected)
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue first = factory.CreateString("first");
        JsonValue last = factory.CreateString("last");
        JsonValue objectValue = factory.CreateObject(new List<JsonMember>
        {
            CreateMember("name", first),
            CreateMember("other", factory.CreateNull()),
            CreateMember("name", last)
        });
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonValue selected = navigator.GetObjectValue(objectValue, "name", policy);

        Assert.That(selected.StringValue, Is.EqualTo(expected));
    }

    [Test]
    public void ObjectLookupRejectsDuplicatesAndReportsMissingAndWrongTypes()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue objectValue = factory.CreateObject(new List<JsonMember>
        {
            CreateMember("name", factory.CreateNull()),
            CreateMember("name", factory.CreateBoolean(true))
        });
        JsonValue scalar = factory.CreateString("scalar");
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonPointerException? duplicate = Assert.Throws<JsonPointerException>(() =>
            navigator.GetObjectValue(
                objectValue,
                "name",
                JsonDuplicateKeyPolicy.Reject));
        JsonPointerException? missing = Assert.Throws<JsonPointerException>(() =>
            navigator.GetObjectValue(
                objectValue,
                "missing",
                JsonDuplicateKeyPolicy.KeepFirst));
        JsonPointerException? mismatch = Assert.Throws<JsonPointerException>(() =>
            navigator.GetObjectMatches(scalar, "name"));

        Assert.Multiple(() =>
        {
            Assert.That(duplicate?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.DuplicatePropertyName));
            Assert.That(missing?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(mismatch?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTypeMismatch));
        });
    }

    [Test]
    public void ObjectLookupHandlesUniqueRejectEmptyMatchesAndUndefinedPolicy()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue selected = factory.CreateString("selected");
        JsonValue objectValue = factory.CreateObject(new List<JsonMember>
        {
            CreateMember("name", selected)
        });
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonValue rejectValue = navigator.GetObjectValue(
            objectValue,
            "name",
            JsonDuplicateKeyPolicy.Reject);
        List<JsonMember> missingMatches = navigator.GetObjectMatches(objectValue, "missing");
        JsonPointerException? invalidPolicy = Assert.Throws<JsonPointerException>(() =>
            navigator.GetObjectValue(
                objectValue,
                "name",
                (JsonDuplicateKeyPolicy)99));
        JsonPointerException? nullName = Assert.Throws<JsonPointerException>(() =>
            navigator.GetObjectMatches(objectValue, null!));

        Assert.Multiple(() =>
        {
            Assert.That(rejectValue, Is.SameAs(selected));
            Assert.That(missingMatches, Is.Empty);
            Assert.That(invalidPolicy?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(nullName?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
        });
    }

    [Test]
    public void ObjectMatchesUseExactNamesAndReturnAnIndependentOrderedList()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonMember first = CreateMember("é", factory.CreateNumber("1"));
        JsonMember distinct = CreateMember("e\u0301", factory.CreateNumber("2"));
        JsonMember last = CreateMember("é", factory.CreateNumber("3"));
        JsonValue objectValue = factory.CreateObject(new List<JsonMember>
        {
            first,
            distinct,
            last
        });
        IJsonDomNavigator navigator = new JsonDomNavigator();

        List<JsonMember> matches = navigator.GetObjectMatches(objectValue, "é");
        matches.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(navigator.GetObjectMatches(objectValue, "é"),
                Is.EqualTo(new[] { first, last }));
            Assert.That(navigator.GetObjectMatches(objectValue, "e\u0301"),
                Is.EqualTo(new[] { distinct }));
            Assert.That(objectValue.ObjectMembers, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void ObjectMutationsPreserveStoredOrderDuplicatesAndIdentity()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonMember first = CreateMember("a", factory.CreateNumber("1"));
        JsonMember second = CreateMember("b", factory.CreateNumber("2"));
        JsonMember inserted = CreateMember("a", factory.CreateNumber("3"));
        JsonMember replacement = CreateMember("c", factory.CreateNumber("4"));
        JsonMember appended = CreateMember("a", factory.CreateNumber("5"));
        JsonValue objectValue = factory.CreateObject(new List<JsonMember> { first, second });
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonOperationResult insertResult = navigator.InsertObjectMember(
            objectValue,
            1,
            inserted);
        JsonOperationResult replaceResult = navigator.ReplaceObjectMember(
            objectValue,
            2,
            replacement);
        JsonOperationResult appendResult = navigator.AppendObjectMember(objectValue, appended);
        JsonOperationResult removeResult = navigator.RemoveObjectMember(objectValue, 0);
        List<JsonMember> members = objectValue.ObjectMembers
            ?? throw new InvalidOperationException("Expected object members.");

        Assert.Multiple(() =>
        {
            Assert.That(members, Is.EqualTo(new[] { inserted, replacement, appended }));
            Assert.That(insertResult.Success, Is.True);
            Assert.That(replaceResult.Success, Is.True);
            Assert.That(appendResult.Success, Is.True);
            Assert.That(removeResult.Success, Is.True);
            Assert.That(navigator.GetObjectMatches(objectValue, "a"),
                Is.EqualTo(new[] { inserted, appended }));
        });

        JsonOperationResult clearResult = navigator.ClearObjectMembers(objectValue);

        Assert.Multiple(() =>
        {
            Assert.That(clearResult.Success, Is.True);
            Assert.That(members, Is.Empty);
        });
    }

    [Test]
    public void ObjectMutationFailuresDoNotChangeTheObject()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonMember retained = CreateMember("a", factory.CreateNull());
        JsonMember valid = CreateMember("b", factory.CreateBoolean(true));
        JsonMember nameless = new JsonMember();
        nameless.Value = factory.CreateNull();
        JsonValue objectValue = factory.CreateObject(new List<JsonMember> { retained });
        JsonValue scalar = factory.CreateNull();
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonOperationResult negativeInsert = navigator.InsertObjectMember(
            objectValue,
            -1,
            valid);
        JsonOperationResult lateInsert = navigator.InsertObjectMember(
            objectValue,
            2,
            valid);
        JsonOperationResult negativeReplace = navigator.ReplaceObjectMember(
            objectValue,
            -1,
            valid);
        JsonOperationResult lateReplace = navigator.ReplaceObjectMember(
            objectValue,
            1,
            valid);
        JsonOperationResult negativeRemove = navigator.RemoveObjectMember(objectValue, -1);
        JsonOperationResult lateRemove = navigator.RemoveObjectMember(objectValue, 1);
        JsonOperationResult wrongType = navigator.AppendObjectMember(scalar, valid);
        JsonOperationResult invalidMember = navigator.AppendObjectMember(
            objectValue,
            nameless);

        Assert.Multiple(() =>
        {
            Assert.That(objectValue.ObjectMembers, Is.EqualTo(new[] { retained }));
            Assert.That(negativeInsert.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(lateInsert.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(negativeReplace.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(lateReplace.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(negativeRemove.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(lateRemove.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTargetNotFound));
            Assert.That(wrongType.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.PointerTypeMismatch));
            Assert.That(invalidMember.Diagnostic?.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        });
    }

    [Test]
    public void ObjectMutationsAcceptEveryLegalEdgeAndCanClearAnEmptyObject()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonMember middle = CreateMember("middle", factory.CreateNull());
        JsonMember first = CreateMember("first", factory.CreateNull());
        JsonMember last = CreateMember("last", factory.CreateNull());
        JsonMember firstReplacement = CreateMember("first replacement", factory.CreateNull());
        JsonMember lastReplacement = CreateMember("last replacement", factory.CreateNull());
        JsonValue objectValue = factory.CreateObject(new List<JsonMember> { middle });
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonOperationResult insertFirst = navigator.InsertObjectMember(objectValue, 0, first);
        JsonOperationResult insertLast = navigator.InsertObjectMember(
            objectValue,
            objectValue.ObjectMembers!.Count,
            last);
        JsonOperationResult replaceFirst = navigator.ReplaceObjectMember(
            objectValue,
            0,
            firstReplacement);
        JsonOperationResult replaceLast = navigator.ReplaceObjectMember(
            objectValue,
            objectValue.ObjectMembers.Count - 1,
            lastReplacement);

        Assert.Multiple(() =>
        {
            Assert.That(insertFirst.Success, Is.True);
            Assert.That(insertLast.Success, Is.True);
            Assert.That(replaceFirst.Success, Is.True);
            Assert.That(replaceLast.Success, Is.True);
            Assert.That(objectValue.ObjectMembers,
                Is.EqualTo(new[] { firstReplacement, middle, lastReplacement }));
        });

        JsonOperationResult removeLast = navigator.RemoveObjectMember(objectValue, 2);
        JsonOperationResult removeFirst = navigator.RemoveObjectMember(objectValue, 0);
        JsonOperationResult clearOnce = navigator.ClearObjectMembers(objectValue);
        JsonOperationResult clearAgain = navigator.ClearObjectMembers(objectValue);

        Assert.Multiple(() =>
        {
            Assert.That(removeLast.Success, Is.True);
            Assert.That(removeFirst.Success, Is.True);
            Assert.That(clearOnce.Success, Is.True);
            Assert.That(clearAgain.Success, Is.True);
            Assert.That(objectValue.ObjectMembers, Is.Empty);
        });
    }

    [Test]
    public void ObjectOperationsRejectEveryMalformedContainerAndMemberShape()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonValue missingMembers = new JsonValue();
        missingMembers.Kind = JsonValueKind.Object;
        JsonValue validObject = factory.CreateObject(new List<JsonMember>());
        JsonMember valid = CreateMember("valid", factory.CreateNull());
        JsonMember valueless = new JsonMember();
        valueless.Name = "valueless";
        JsonMember invalidOrdinal = CreateMember("ordinal", factory.CreateNull());
        invalidOrdinal.SourceOrdinal = -2;
        IJsonDomNavigator navigator = new JsonDomNavigator();

        JsonPointerException? readFailure = Assert.Throws<JsonPointerException>(() =>
            navigator.GetObjectMatches(missingMembers, "name"));
        JsonOperationResult appendMissing = navigator.AppendObjectMember(
            missingMembers,
            valid);
        JsonOperationResult insertMissing = navigator.InsertObjectMember(
            missingMembers,
            0,
            valid);
        JsonOperationResult replaceMissing = navigator.ReplaceObjectMember(
            missingMembers,
            0,
            valid);
        JsonOperationResult removeMissing = navigator.RemoveObjectMember(missingMembers, 0);
        JsonOperationResult clearMissing = navigator.ClearObjectMembers(missingMembers);
        JsonOperationResult nullMember = navigator.AppendObjectMember(validObject, null!);
        JsonOperationResult valuelessMember = navigator.AppendObjectMember(
            validObject,
            valueless);
        JsonOperationResult badOrdinalMember = navigator.AppendObjectMember(
            validObject,
            invalidOrdinal);

        Assert.Multiple(() =>
        {
            Assert.That(readFailure?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
            AssertFailureResult(appendMissing, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(insertMissing, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(replaceMissing, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(removeMissing, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(clearMissing, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(nullMember, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(valuelessMember, JsonDiagnosticCode.InvalidModel);
            AssertFailureResult(badOrdinalMember, JsonDiagnosticCode.InvalidModel);
            Assert.That(validObject.ObjectMembers, Is.Empty);
        });
    }

    [Test]
    public void ObjectReadsRejectMalformedStoredMembers()
    {
        IJsonValueFactory factory = new JsonValueFactory();
        JsonMember nameless = new JsonMember();
        nameless.Value = factory.CreateNull();
        JsonMember valueless = new JsonMember();
        valueless.Name = "name";
        JsonMember invalidOrdinal = CreateMember("name", factory.CreateNull());
        invalidOrdinal.SourceOrdinal = -2;
        JsonValue[] malformed =
        {
            factory.CreateObject(new List<JsonMember> { null! }),
            factory.CreateObject(new List<JsonMember> { nameless }),
            factory.CreateObject(new List<JsonMember> { valueless }),
            factory.CreateObject(new List<JsonMember> { invalidOrdinal })
        };
        IJsonDomNavigator navigator = new JsonDomNavigator();

        foreach (JsonValue objectValue in malformed)
        {
            JsonPointerException? failure = Assert.Throws<JsonPointerException>(() =>
                navigator.GetObjectMatches(objectValue, "name"));
            Assert.That(failure?.Diagnostic.Code,
                Is.EqualTo(JsonDiagnosticCode.InvalidModel));
        }
    }

    static void AssertFailureResult(
        JsonOperationResult result,
        JsonDiagnosticCode expectedCode)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.That(result.Diagnostic?.Code, Is.EqualTo(expectedCode));
        Assert.That(result.Diagnostic?.Message, Is.Not.Empty);
        Assert.That(result.BytesConsumed, Is.Zero);
        Assert.That(result.BytesWritten, Is.Zero);
    }

    static JsonMember CreateMember(string name, JsonValue value)
    {
        JsonMember member = new JsonMember();
        member.Name = name;
        member.Value = value;
        return member;
    }
}
