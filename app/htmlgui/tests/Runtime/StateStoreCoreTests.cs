using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class StateStoreCoreTests
{
    [Test]
    public void CreatesIndependentScopedEntriesAndEnumeratesThemOrdinally()
    {
        StateStoreCore core = new StateStoreCore();
        UiValue source = ObjectValue(Entry("name", Text("original")));
        StateOperationResult global = core.CreateStateEntry(null, "shared", source, "bootstrap");
        StateOperationResult scoped = core.CreateStateEntry("page", "shared", Text("page value"), "component");
        core.CreateStateEntry("zeta", "a", Text("zeta"), null);
        core.CreateStateEntry("alpha", "z", Text("alpha-z"), null);
        core.CreateStateEntry("alpha", "a", Text("alpha-a"), null);
        core.CreateStateEntry(null, "a", Text("global-a"), null);
        source.ObjectValue![0].Value.StringValue = "mutated source";
        global.Entry!.Value.ObjectValue![0].Value.StringValue = "mutated result";

        StateOperationResult retainedGlobal = core.ReadStateEntry(string.Empty, "shared");
        StateOperationResult retainedScoped = core.ReadStateEntry("page", "shared");
        StateScopedEntry[] ordered = core.GetScopedStateEntries();

        Assert.Multiple(() =>
        {
            Assert.That(global.Success, Is.True);
            Assert.That(global.Applied, Is.True);
            Assert.That(global.ScopeId, Is.Empty);
            Assert.That(global.Entry!.Path, Is.Empty);
            Assert.That(global.Entry.Version, Is.EqualTo(1));
            Assert.That(global.Entry.Origin, Is.EqualTo("bootstrap"));
            Assert.That(scoped.Success, Is.True);
            Assert.That(retainedGlobal.Entry!.Value.ObjectValue![0].Value.StringValue, Is.EqualTo("original"));
            Assert.That(retainedScoped.Entry!.Value.StringValue, Is.EqualTo("page value"));
            Assert.That(ordered.Select(item => item.ScopeId + ":" + item.Entry.Id), Is.EqualTo(new string[]
            {
                ":a", ":shared", "alpha:a", "alpha:z", "page:shared", "zeta:a"
            }));
        });
    }

    [Test]
    public void WritesRemovesAndRecreatesWithMonotonicVersionsAndOriginMetadata()
    {
        StateStoreCore core = new StateStoreCore();
        StateOperationResult created = core.CreateStateEntry("view", "counter", Integer(1), "create");
        UiValue replacement = Integer(2);
        StateOperationResult written = core.WriteStateEntry("view", "counter", replacement, "input");
        replacement.IntegerValue = 99;
        StateOperationResult removed = core.RemoveStateEntry("view", "counter", "dispose");
        StateOperationResult missing = core.ReadStateEntry("view", "counter");
        StateOperationResult recreated = core.CreateStateEntry("view", "counter", Integer(3), "restore");
        StateOperationResult retained = core.ReadStateEntry("view", "counter");

        Assert.Multiple(() =>
        {
            Assert.That(created.Entry!.Version, Is.EqualTo(1));
            Assert.That(created.Change!.PreviousValue, Is.Null);
            Assert.That(created.Change.CurrentValue!.IntegerValue, Is.EqualTo(1));
            Assert.That(created.Change.Origin, Is.EqualTo("create"));
            Assert.That(written.Entry!.Version, Is.EqualTo(2));
            Assert.That(written.Entry.Value.IntegerValue, Is.EqualTo(2));
            Assert.That(written.Entry.Origin, Is.EqualTo("input"));
            Assert.That(written.Change!.PreviousValue!.IntegerValue, Is.EqualTo(1));
            Assert.That(written.Change.CurrentValue!.IntegerValue, Is.EqualTo(2));
            Assert.That(removed.Success, Is.True);
            Assert.That(removed.Entry, Is.Null);
            Assert.That(removed.Change!.Version, Is.EqualTo(3));
            Assert.That(removed.Change.PreviousValue!.IntegerValue, Is.EqualTo(2));
            Assert.That(removed.Change.CurrentValue, Is.Null);
            Assert.That(removed.Change.Origin, Is.EqualTo("dispose"));
            Assert.That(missing.Code, Is.EqualTo("state.unknown"));
            Assert.That(recreated.Entry!.Version, Is.EqualTo(4));
            Assert.That(retained.Entry!.Version, Is.EqualTo(4));
            Assert.That(retained.Entry.Value.IntegerValue, Is.EqualTo(3));
            Assert.That(retained.Entry.Origin, Is.EqualTo("restore"));
        });
    }

    [Test]
    public void CompareAndSetAppliesOnlyToTheExpectedScopedVersion()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry("left", "selection", Text("first"), "create-left");
        core.CreateStateEntry("right", "selection", Text("right"), "create-right");

        StateOperationResult mismatch = core.CompareAndSetStateEntry("left", "selection", 0, Text("ignored"), "stale");
        StateOperationResult applied = core.CompareAndSetStateEntry("left", "selection", 1, Text("second"), "cas");
        StateOperationResult stale = core.CompareAndSetStateEntry("left", "selection", 1, Text("third"), "stale-again");
        StateOperationResult left = core.ReadStateEntry("left", "selection");
        StateOperationResult right = core.ReadStateEntry("right", "selection");

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Success, Is.True);
            Assert.That(mismatch.Applied, Is.False);
            Assert.That(mismatch.Code, Is.EqualTo("state.version-mismatch"));
            Assert.That(mismatch.Entry!.Version, Is.EqualTo(1));
            Assert.That(mismatch.Change, Is.Null);
            Assert.That(applied.Success, Is.True);
            Assert.That(applied.Applied, Is.True);
            Assert.That(applied.Entry!.Version, Is.EqualTo(2));
            Assert.That(applied.Change!.Origin, Is.EqualTo("cas"));
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Applied, Is.False);
            Assert.That(stale.Entry!.Version, Is.EqualTo(2));
            Assert.That(left.Entry!.Value.StringValue, Is.EqualTo("second"));
            Assert.That(left.Entry.Origin, Is.EqualTo("cas"));
            Assert.That(right.Entry!.Value.StringValue, Is.EqualTo("right"));
            Assert.That(right.Entry.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void RejectsInvalidAddressesOriginsValuesAndMissingOperationsWithoutMutation()
    {
        StateStoreCore core = new StateStoreCore();
        UiValue invalid = new UiValue { Kind = UiValueKind.String };
        UiValue cycle = new UiValue { Kind = UiValueKind.Array, ArrayValue = Array.Empty<UiValue>() };
        cycle.ArrayValue = new UiValue[] { cycle };
        StateOperationResult valid = core.CreateStateEntry(null, "valid", Text("retained"), null);

        StateOperationResult nullId = core.CreateStateEntry(null, null, Text("value"), null);
        StateOperationResult blankId = core.CreateStateEntry(null, " ", Text("value"), null);
        StateOperationResult blankScope = core.CreateStateEntry(" ", "id", Text("value"), null);
        StateOperationResult blankOrigin = core.CreateStateEntry(null, "origin", Text("value"), " ");
        StateOperationResult nullValue = core.CreateStateEntry(null, "null", null, null);
        StateOperationResult invalidValue = core.CreateStateEntry(null, "invalid", invalid, null);
        StateOperationResult cyclicValue = core.CreateStateEntry(null, "cycle", cycle, null);
        StateOperationResult duplicate = core.CreateStateEntry(null, "valid", Text("other"), null);
        StateOperationResult unknownWrite = core.WriteStateEntry(null, "missing", Text("value"), null);
        StateOperationResult unknownRemove = core.RemoveStateEntry(null, "missing", null);
        StateOperationResult invalidVersion = core.CompareAndSetStateEntry(null, "valid", -1, Text("value"), null);
        StateOperationResult retained = core.ReadStateEntry(null, "valid");

        Assert.Multiple(() =>
        {
            Assert.That(nullId.Code, Is.EqualTo("state.invalid-id"));
            Assert.That(blankId.Code, Is.EqualTo("state.invalid-id"));
            Assert.That(blankScope.Code, Is.EqualTo("state.invalid-scope"));
            Assert.That(blankOrigin.Code, Is.EqualTo("state.invalid-origin"));
            Assert.That(nullValue.Code, Is.EqualTo("state.null-value"));
            Assert.That(invalidValue.Code, Is.EqualTo("state.invalid-value"));
            Assert.That(cyclicValue.Code, Is.EqualTo("state.value-cycle"));
            Assert.That(duplicate.Code, Is.EqualTo("state.duplicate"));
            Assert.That(unknownWrite.Code, Is.EqualTo("state.unknown"));
            Assert.That(unknownRemove.Code, Is.EqualTo("state.unknown"));
            Assert.That(invalidVersion.Code, Is.EqualTo("state.invalid-version"));
            Assert.That(valid.Entry!.Origin, Is.Empty);
            Assert.That(retained.Entry!.Version, Is.EqualTo(1));
            Assert.That(retained.Entry.Value.StringValue, Is.EqualTo("retained"));
            Assert.That(core.GetScopedStateEntries(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void StopsBeforeAStateVersionWouldOverflow()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "bounded", Text("initial"), null);
        core.Versions.Single().Version = int.MaxValue;

        StateOperationResult write = core.WriteStateEntry(null, "bounded", Text("write"), null);
        StateOperationResult remove = core.RemoveStateEntry(null, "bounded", null);
        StateOperationResult retained = core.ReadStateEntry(null, "bounded");

        Assert.Multiple(() =>
        {
            Assert.That(write.Code, Is.EqualTo("state.version-exhausted"));
            Assert.That(remove.Code, Is.EqualTo("state.version-exhausted"));
            Assert.That(retained.Success, Is.True);
            Assert.That(retained.Entry!.Version, Is.EqualTo(1));
            Assert.That(retained.Entry.Value.StringValue, Is.EqualTo("initial"));
        });
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue ObjectValue(params UiValueEntry[] entries)
    {
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }
}
