using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class StatePatchCoreTests
{
    [Test]
    public void ResolvesRootEmptyAndEscapedRfc6901ObjectTokens()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "document", ObjectValue(
            Entry("a/b", Text("slash")),
            Entry("m~n", Text("tilde")),
            Entry(string.Empty, Text("empty"))), "setup");
        core.DrainStateChanges();
        UiStatePatch slash = Replace("/a~1b", Text("new slash"));
        UiStatePatch tilde = Replace("/m~0n", Text("new tilde"));
        UiStatePatch empty = Replace("/", Text("new empty"));
        UiStatePatch test = Test("/a~1b", Text("new slash"));

        StateOperationResult result = core.PatchStateEntry(null, "document", new UiStatePatch[] { slash, tilde, empty, test }, "patch");
        slash.Value!.StringValue = "mutated input";
        result.Entry!.Patches[0].Value!.StringValue = "mutated result";
        StateOperationResult retained = core.ReadStateEntry(null, "document");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.True);
            Assert.That(result.Entry.Version, Is.EqualTo(2));
            Assert.That(result.Entry.Origin, Is.EqualTo("patch"));
            Assert.That(result.Entry.Patches, Has.Length.EqualTo(4));
            Assert.That(ObjectMember(retained.Entry!.Value, "a/b").StringValue, Is.EqualTo("new slash"));
            Assert.That(ObjectMember(retained.Entry.Value, "m~n").StringValue, Is.EqualTo("new tilde"));
            Assert.That(ObjectMember(retained.Entry.Value, string.Empty).StringValue, Is.EqualTo("new empty"));
            Assert.That(retained.Entry.Patches[0].Value!.StringValue, Is.EqualTo("new slash"));
            Assert.That(core.DrainStateChanges().Single().Change.Path, Is.Empty);
        });
    }

    [Test]
    public void AppliesObjectAndArrayAddRemoveReplaceAsOneVersionedMutation()
    {
        StateStoreCore core = new StateStoreCore();
        UiValue initial = ObjectValue(
            Entry("flag", Text("old")),
            Entry("items", ArrayValue(Text("a"), Text("b"), Text("c"))));
        core.CreateStateEntry("page", "document", initial, "setup");
        core.DrainStateChanges();

        StateOperationResult result = core.PatchStateEntry("page", "document", new UiStatePatch[]
        {
            Add("/items/1", Text("x")),
            Add("/items/-", Text("z")),
            Remove("/items/2"),
            Replace("/items/0", Text("A")),
            Add("/new", Text("created")),
            Add("/flag", Text("replaced by add")),
            Remove("/new")
        }, "batch-patch");
        StateScopedChange change = core.DrainStateChanges().Single();
        UiValue items = ObjectMember(result.Entry!.Value, "items");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Entry.Version, Is.EqualTo(2));
            Assert.That(items.ArrayValue!.Select(item => item.StringValue), Is.EqualTo(new string[] { "A", "x", "c", "z" }));
            Assert.That(ObjectMember(result.Entry.Value, "flag").StringValue, Is.EqualTo("replaced by add"));
            Assert.That(result.Entry.Value.ObjectValue!.Select(item => item.Key), Does.Not.Contain("new"));
            Assert.That(change.ScopeId, Is.EqualTo("page"));
            Assert.That(change.Change.PreviousValue, Is.Not.Null);
            Assert.That(change.Change.CurrentValue, Is.Not.Null);
            Assert.That(change.Change.Version, Is.EqualTo(2));
            Assert.That(change.Change.Origin, Is.EqualTo("batch-patch"));
        });
    }

    [Test]
    public void MovesAndCopiesValuesUsingPostRemovalArrayIndexesAndIndependentCopies()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "document", ObjectValue(
            Entry("items", ArrayValue(Text("a"), Text("b"), Text("c"))),
            Entry("nested", ObjectValue(Entry("value", Text("original"))))), "setup");

        StateOperationResult result = core.PatchStateEntry(null, "document", new UiStatePatch[]
        {
            Move("/items/0", "/items/2"),
            Copy("/nested", "/copied"),
            Replace("/copied/value", Text("copy changed")),
            Move("/nested/value", "/moved")
        }, "rearrange");
        UiValue document = result.Entry!.Value;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ObjectMember(document, "items").ArrayValue!.Select(item => item.StringValue), Is.EqualTo(new string[] { "b", "c", "a" }));
            Assert.That(ObjectMember(ObjectMember(document, "copied"), "value").StringValue, Is.EqualTo("copy changed"));
            Assert.That(ObjectMember(document, "nested").ObjectValue, Is.Empty);
            Assert.That(ObjectMember(document, "moved").StringValue, Is.EqualTo("original"));
        });
    }

    [Test]
    public void SupportsRootReplacementAndSuccessfulTestWithoutExtraVersions()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "root", Text("original"), "setup");
        core.DrainStateChanges();

        StateOperationResult testOnly = core.PatchStateEntry(null, "root", new UiStatePatch[]
        {
            Test(string.Empty, Text("original"))
        }, "test");
        StateOperationResult replaced = core.PatchStateEntry(null, "root", new UiStatePatch[]
        {
            Replace(string.Empty, ArrayValue(Text("first"))),
            Test(string.Empty, ArrayValue(Text("first"))),
            Add(string.Empty, ObjectValue(Entry("final", Text("value"))))
        }, "root-patch");
        StateScopedChange[] changes = core.DrainStateChanges();

        Assert.Multiple(() =>
        {
            Assert.That(testOnly.Success, Is.True);
            Assert.That(testOnly.Applied, Is.False);
            Assert.That(testOnly.Entry!.Version, Is.EqualTo(1));
            Assert.That(replaced.Success, Is.True);
            Assert.That(replaced.Entry!.Version, Is.EqualTo(2));
            Assert.That(ObjectMember(replaced.Entry.Value, "final").StringValue, Is.EqualTo("value"));
            Assert.That(changes, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void RejectsAFailedTestWithoutPublishingEarlierPatchOperations()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "document", ObjectValue(Entry("name", Text("Ada"))), "setup");
        core.DrainStateChanges();

        StateOperationResult result = core.PatchStateEntry(null, "document", new UiStatePatch[]
        {
            Add("/temporary", Text("created")),
            Test("/name", Text("Grace")),
            Replace("/name", Text("unreachable"))
        }, "patch");
        StateOperationResult retained = core.ReadStateEntry(null, "document");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo("state.patch-test-failed"));
            Assert.That(result.OperationIndex, Is.EqualTo(1));
            Assert.That(retained.Entry!.Version, Is.EqualTo(1));
            Assert.That(ObjectMember(retained.Entry.Value, "name").StringValue, Is.EqualTo("Ada"));
            Assert.That(retained.Entry.Value.ObjectValue!.Select(item => item.Key), Does.Not.Contain("temporary"));
            Assert.That(core.DrainStateChanges(), Is.Empty);
        });
    }

    [Test]
    public void ReturnsExceptionlessFailuresForMalformedAndBoundedPatchInputs()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "document", ObjectValue(
            Entry("name", Text("Ada")),
            Entry("items", ArrayValue(Text("a"))),
            Entry("nested", ObjectValue(Entry("child", Text("value"))))), "setup");
        core.DrainStateChanges();
        UiStatePatch nullPath = Replace("/name", Text("value"));
        nullPath.Path = null!;
        UiStatePatch invalidValue = Replace("/name", new UiValue { Kind = UiValueKind.String });
        UiStatePatch[] tooMany = new UiStatePatch[StatePatchCore.MaximumPatchOperationCount + 1];
        string deepPath = string.Concat(Enumerable.Repeat("/nested", StatePatchCore.MaximumStatePathSegmentCount + 1));

        StateOperationResult nullPatches = core.PatchStateEntry(null, "document", null, null);
        StateOperationResult excessiveCount = core.PatchStateEntry(null, "document", tooMany, null);
        StateOperationResult nullPatch = core.PatchStateEntry(null, "document", new UiStatePatch[] { null! }, null);
        StateOperationResult invalidOperation = Patch(core, new UiStatePatch { Operation = "merge", Path = "/name" });
        StateOperationResult missingValue = Patch(core, new UiStatePatch { Operation = "replace", Path = "/name" });
        StateOperationResult missingFrom = Patch(core, new UiStatePatch { Operation = "copy", Path = "/copied" });
        StateOperationResult invalidPath = Patch(core, Replace("name", Text("value")));
        StateOperationResult missingPath = Patch(core, Replace("/missing/child", Text("value")));
        StateOperationResult nullPointer = Patch(core, nullPath);
        StateOperationResult invalidEscape = Patch(core, Replace("/m~2n", Text("value")));
        StateOperationResult excessiveLength = Patch(core, Replace("/" + new string('a', StatePatchCore.MaximumStatePathLength), Text("value")));
        StateOperationResult excessiveDepth = Patch(core, Replace(deepPath, Text("value")));
        StateOperationResult leadingZero = Patch(core, Remove("/items/01"));
        StateOperationResult appendRemove = Patch(core, Remove("/items/-"));
        StateOperationResult outOfRange = Patch(core, Replace("/items/1", Text("value")));
        StateOperationResult primitiveChild = Patch(core, Replace("/name/child", Text("value")));
        StateOperationResult removeRoot = Patch(core, Remove(string.Empty));
        StateOperationResult descendantMove = Patch(core, Move("/nested", "/nested/child/grandchild"));
        StateOperationResult malformedValue = Patch(core, invalidValue);

        Assert.Multiple(() =>
        {
            Assert.That(nullPatches.Code, Is.EqualTo("state.null-patches"));
            Assert.That(excessiveCount.Code, Is.EqualTo("state.too-many-patches"));
            Assert.That(nullPatch.Code, Is.EqualTo("state.null-patch"));
            Assert.That(invalidOperation.Code, Is.EqualTo("state.invalid-patch-operation"));
            Assert.That(missingValue.Code, Is.EqualTo("state.null-patch-value"));
            Assert.That(missingFrom.Code, Is.EqualTo("state.missing-from-path"));
            Assert.That(invalidPath.Code, Is.EqualTo("state.invalid-path"));
            Assert.That(missingPath.Code, Is.EqualTo("state.path-not-found"));
            Assert.That(nullPointer.Code, Is.EqualTo("state.invalid-path"));
            Assert.That(invalidEscape.Code, Is.EqualTo("state.invalid-path-escape"));
            Assert.That(excessiveLength.Code, Is.EqualTo("state.path-too-long"));
            Assert.That(excessiveDepth.Code, Is.EqualTo("state.path-too-deep"));
            Assert.That(leadingZero.Code, Is.EqualTo("state.invalid-array-index"));
            Assert.That(appendRemove.Code, Is.EqualTo("state.invalid-array-index"));
            Assert.That(outOfRange.Code, Is.EqualTo("state.path-not-found"));
            Assert.That(primitiveChild.Code, Is.EqualTo("state.path-not-container"));
            Assert.That(removeRoot.Code, Is.EqualTo("state.cannot-remove-root"));
            Assert.That(descendantMove.Code, Is.EqualTo("state.patch-descendant-move"));
            Assert.That(malformedValue.Code, Is.EqualTo("state.invalid-value"));
            Assert.That(core.ReadStateEntry(null, "document").Entry!.Version, Is.EqualTo(1));
            Assert.That(core.DrainStateChanges(), Is.Empty);
        });
    }

    [Test]
    public void PatchFailureRollsBackOtherMutationsInAnActiveTransaction()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "first", Text("original first"), "setup");
        core.CreateStateEntry(null, "second", ObjectValue(Entry("value", Text("original second"))), "setup");
        core.DrainStateChanges();
        core.BeginStateBatch("patch-batch");
        core.WriteStateEntry(null, "first", Text("temporary"), "write");

        StateOperationResult patch = core.PatchStateEntry(null, "second", new UiStatePatch[]
        {
            Replace("/missing", Text("failure"))
        }, "patch");
        StateBatchResult committed = core.CommitStateBatch();

        Assert.Multiple(() =>
        {
            Assert.That(patch.Code, Is.EqualTo("state.path-not-found"));
            Assert.That(committed.Success, Is.False);
            Assert.That(committed.Code, Is.EqualTo("state.path-not-found"));
            Assert.That(core.ReadStateEntry(null, "first").Entry!.Value.StringValue, Is.EqualTo("original first"));
            Assert.That(ObjectMember(core.ReadStateEntry(null, "second").Entry!.Value, "value").StringValue, Is.EqualTo("original second"));
            Assert.That(core.DrainStateChanges(), Is.Empty);
        });
    }

    [Test]
    public void RejectsAnOperationWhoseResultWouldExceedThePortableValueBound()
    {
        StateStoreCore core = new StateStoreCore();
        UiValue[] maximumItems = Enumerable.Range(0, StateStoreCore.MaximumValueCollectionCount)
            .Select(index => new UiValue { Kind = UiValueKind.Null })
            .ToArray();
        core.CreateStateEntry(null, "bounded", ArrayValue(maximumItems), "setup");
        core.DrainStateChanges();

        StateOperationResult result = core.PatchStateEntry(null, "bounded", new UiStatePatch[]
        {
            Add("/-", new UiValue { Kind = UiValueKind.Null })
        }, "patch");
        StateOperationResult retained = core.ReadStateEntry(null, "bounded");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo("state.value-too-large"));
            Assert.That(result.OperationIndex, Is.EqualTo(0));
            Assert.That(retained.Entry!.Version, Is.EqualTo(1));
            Assert.That(retained.Entry.Value.ArrayValue, Has.Length.EqualTo(StateStoreCore.MaximumValueCollectionCount));
            Assert.That(core.DrainStateChanges(), Is.Empty);
        });
    }

    private static StateOperationResult Patch(StateStoreCore core, UiStatePatch patch)
    {
        return core.PatchStateEntry(null, "document", new UiStatePatch[] { patch }, null);
    }

    private static UiStatePatch Add(string path, UiValue value)
    {
        return new UiStatePatch { Operation = "add", Path = path, Value = value };
    }

    private static UiStatePatch Remove(string path)
    {
        return new UiStatePatch { Operation = "remove", Path = path };
    }

    private static UiStatePatch Replace(string path, UiValue value)
    {
        return new UiStatePatch { Operation = "replace", Path = path, Value = value };
    }

    private static UiStatePatch Move(string fromPath, string path)
    {
        return new UiStatePatch { Operation = "move", FromPath = fromPath, Path = path };
    }

    private static UiStatePatch Copy(string fromPath, string path)
    {
        return new UiStatePatch { Operation = "copy", FromPath = fromPath, Path = path };
    }

    private static UiStatePatch Test(string path, UiValue value)
    {
        return new UiStatePatch { Operation = "test", Path = path, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue ArrayValue(params UiValue[] values)
    {
        return new UiValue { Kind = UiValueKind.Array, ArrayValue = values };
    }

    private static UiValue ObjectValue(params UiValueEntry[] entries)
    {
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue ObjectMember(UiValue value, string key)
    {
        return value.ObjectValue!.Single(item => item.Key == key).Value;
    }
}
