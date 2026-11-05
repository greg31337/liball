using System.Collections.Generic;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class StateComprehensiveTests
{
    [TestCase(UiValueKind.Null)]
    [TestCase(UiValueKind.Boolean)]
    [TestCase(UiValueKind.Integer)]
    [TestCase(UiValueKind.FloatingPoint)]
    [TestCase(UiValueKind.Decimal)]
    [TestCase(UiValueKind.String)]
    [TestCase(UiValueKind.DateTime)]
    [TestCase(UiValueKind.Binary)]
    [TestCase(UiValueKind.Array)]
    [TestCase(UiValueKind.Object)]
    public void PreservesEveryValueKindAcrossCopiesSnapshotsAndVersionedRestore(UiValueKind kind)
    {
        StateStoreCore core = new StateStoreCore();
        UiValue source = Value(kind);
        StateOperationResult created = core.CreateStateEntry("scope", "value", source, "create");
        Mutate(source);
        Mutate(created.Entry!.Value);
        StateOperationResult read = core.ReadStateEntry("scope", "value");
        StateSnapshotResult snapshot = core.CreateStateSnapshot("baseline");
        Mutate(snapshot.Snapshot!.Entries.Single().Entry.Value);
        UiValue replacement = kind == UiValueKind.Null ? Text("replacement") : new UiValue();
        StateOperationResult written = core.WriteStateEntry("scope", "value", replacement, "write");
        core.DrainStateChanges();

        StateSnapshotResult restored = core.RestoreStateSnapshot("baseline", "restore");
        StateOperationResult retained = core.ReadStateEntry("scope", "value");

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Entry.Version, Is.EqualTo(1));
            AssertValue(kind, read.Entry!.Value);
            Assert.That(written.Entry!.Version, Is.EqualTo(2));
            Assert.That(restored.Success, Is.True);
            Assert.That(restored.Changes, Has.Length.EqualTo(1));
            Assert.That(restored.Changes[0].Change.Version, Is.EqualTo(3));
            Assert.That(restored.Changes[0].Change.Origin, Is.EqualTo("restore"));
            Assert.That(retained.Entry!.Version, Is.EqualTo(3));
            AssertValue(kind, retained.Entry.Value);
        });
    }

    [Test]
    public void AppliesEveryPatchOperationAcrossRootObjectArrayAndEscapedPaths()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "document", ObjectValue(
            Entry("a/b", ObjectValue(Entry("m~n", Text("old")))),
            Entry("items", ArrayValue(Text("zero"), Text("one"), Text("two"))),
            Entry("source", ObjectValue(Entry("value", Text("copy"))))), "setup");
        core.DrainStateChanges();

        StateOperationResult patched = core.PatchStateEntry(null, "document", new UiStatePatch[]
        {
            Patch("test", "/a~1b/m~0n", null, Text("old")),
            Patch("replace", "/a~1b/m~0n", null, Text("new")),
            Patch("add", "/items/1", null, Text("inserted")),
            Patch("add", "/items/-", null, Text("last")),
            Patch("move", "/items/3", "/items/0", null),
            Patch("copy", "/copied", "/source", null),
            Patch("remove", "/source", null, null),
            Patch("test", "/copied/value", null, Text("copy"))
        }, "patch");
        UiValue expectedRoot = ObjectValue(Entry("done", new UiValue { Kind = UiValueKind.Boolean, BooleanValue = true }));
        StateOperationResult replacedRoot = core.PatchStateEntry(null, "document", new UiStatePatch[]
        {
            Patch("replace", string.Empty, null, expectedRoot),
            Patch("test", string.Empty, null, expectedRoot)
        }, "root");

        Assert.Multiple(() =>
        {
            Assert.That(patched.Success, Is.True);
            Assert.That(patched.Entry!.Version, Is.EqualTo(2));
            Assert.That(ObjectMember(ObjectMember(patched.Entry.Value, "a/b"), "m~n").StringValue, Is.EqualTo("new"));
            Assert.That(ObjectMember(patched.Entry.Value, "items").ArrayValue!.Select(item => item.StringValue),
                Is.EqualTo(new string[] { "inserted", "one", "two", "zero", "last" }));
            Assert.That(ObjectMember(ObjectMember(patched.Entry.Value, "copied"), "value").StringValue, Is.EqualTo("copy"));
            Assert.That(patched.Entry.Value.ObjectValue!.Select(item => item.Key), Does.Not.Contain("source"));
            Assert.That(replacedRoot.Success, Is.True);
            Assert.That(replacedRoot.Entry!.Version, Is.EqualTo(3));
            Assert.That(ObjectMember(replacedRoot.Entry.Value, "done").BooleanValue, Is.True);
        });
    }

    [Test]
    public void CasNestedBatchRollbackAndSnapshotRestoreKeepVersionsAtomic()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "counter", Text("baseline"), "create");
        core.CreateStateSnapshot("baseline");
        core.DrainStateChanges();

        StateBatchResult outer = core.BeginStateBatch("outer");
        StateOperationResult compared = core.CompareAndSetStateEntry(null, "counter", 1, Text("temporary"), "cas");
        StateBatchResult nested = core.BeginStateBatch("nested");
        StateBatchResult rollback = core.RollbackStateBatch();
        StateOperationResult afterRollback = core.ReadStateEntry(null, "counter");
        StateScopedChange[] afterRollbackChanges = core.DrainStateChanges();
        StateOperationResult stale = core.CompareAndSetStateEntry(null, "counter", 0, Text("ignored"), "stale");
        StateOperationResult accepted = core.CompareAndSetStateEntry(null, "counter", 1, Text("accepted"), "cas");
        core.DrainStateChanges();
        StateSnapshotResult restored = core.RestoreStateSnapshot("baseline", "restore");
        StateOperationResult final = core.ReadStateEntry(null, "counter");

        Assert.Multiple(() =>
        {
            Assert.That(outer.Success, Is.True);
            Assert.That(compared.Entry!.Version, Is.EqualTo(2));
            Assert.That(nested.Success, Is.False);
            Assert.That(nested.Code, Is.EqualTo("state.batch-active"));
            Assert.That(rollback.Success, Is.True);
            Assert.That(afterRollback.Entry!.Version, Is.EqualTo(1));
            Assert.That(afterRollback.Entry.Value.StringValue, Is.EqualTo("baseline"));
            Assert.That(afterRollbackChanges, Is.Empty);
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Applied, Is.False);
            Assert.That(stale.Code, Is.EqualTo("state.version-mismatch"));
            Assert.That(accepted.Entry!.Version, Is.EqualTo(2));
            Assert.That(restored.Changes.Single().Change.Version, Is.EqualTo(3));
            Assert.That(final.Entry!.Version, Is.EqualTo(3));
            Assert.That(final.Entry.Value.StringValue, Is.EqualTo("baseline"));
        });
    }

    [Test]
    public void ObserverOrderUnsubscribeAndReentrancyRemainDeterministicTogether()
    {
        IUiStateStore store = new UiStateStore();
        List<string> calls = new List<string>();
        ReentrantObserver alpha = new ReentrantObserver(store, calls);
        UnsubscribingObserver beta = new UnsubscribingObserver(store, calls);
        RecordingObserver zeta = new RecordingObserver(calls);
        store.CreateState(null, "source", Text("zero"), "setup");
        store.RegisterStateSubscription(null, Subscription("zeta"), zeta);
        store.RegisterStateSubscription(null, Subscription("beta"), beta);
        store.RegisterStateSubscription(null, Subscription("alpha"), alpha);

        UiStateResult written = store.WriteState(null, "source", Text("one"), "input");
        UiStateResult retained = store.ReadState(null, "source");

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(calls, Is.EqualTo(new string[] { "alpha:one", "beta:one", "alpha:two", "beta:two" }));
            Assert.That(zeta.Count, Is.EqualTo(0));
            Assert.That(beta.Unsubscribed.Success, Is.True);
            Assert.That(beta.Unsubscribed.Applied, Is.True);
            Assert.That(retained.Entry!.Version, Is.EqualTo(3));
            Assert.That(retained.Entry.Value.StringValue, Is.EqualTo("two"));
            Assert.That(retained.Entry.Subscriptions.Select(item => item.Id), Is.EqualTo(new string[] { "alpha", "beta" }));
        });
    }

    private static UiValue Value(UiValueKind kind)
    {
        switch (kind)
        {
            case UiValueKind.Null:
                return new UiValue();
            case UiValueKind.Boolean:
                return new UiValue { Kind = kind, BooleanValue = true };
            case UiValueKind.Integer:
                return new UiValue { Kind = kind, IntegerValue = -17 };
            case UiValueKind.FloatingPoint:
                return new UiValue { Kind = kind, FloatingPointValue = 12.5 };
            case UiValueKind.Decimal:
                return new UiValue { Kind = kind, DecimalValue = 19.25m };
            case UiValueKind.String:
                return Text("text");
            case UiValueKind.DateTime:
                return new UiValue { Kind = kind, DateTimeValue = "2026-09-15T12:34:56.0000000Z" };
            case UiValueKind.Binary:
                return new UiValue { Kind = kind, BinaryValue = new byte[] { 1, 2, 3 } };
            case UiValueKind.Array:
                return ArrayValue(Text("item"));
            default:
                return ObjectValue(Entry("key", Text("value")));
        }
    }

    private static void AssertValue(UiValueKind kind, UiValue value)
    {
        Assert.That(value.Kind, Is.EqualTo(kind));
        switch (kind)
        {
            case UiValueKind.Boolean:
                Assert.That(value.BooleanValue, Is.True);
                break;
            case UiValueKind.Integer:
                Assert.That(value.IntegerValue, Is.EqualTo(-17));
                break;
            case UiValueKind.FloatingPoint:
                Assert.That(value.FloatingPointValue, Is.EqualTo(12.5));
                break;
            case UiValueKind.Decimal:
                Assert.That(value.DecimalValue, Is.EqualTo(19.25m));
                break;
            case UiValueKind.String:
                Assert.That(value.StringValue, Is.EqualTo("text"));
                break;
            case UiValueKind.DateTime:
                Assert.That(value.DateTimeValue, Is.EqualTo("2026-09-15T12:34:56.0000000Z"));
                break;
            case UiValueKind.Binary:
                Assert.That(value.BinaryValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
                break;
            case UiValueKind.Array:
                Assert.That(value.ArrayValue!.Single().StringValue, Is.EqualTo("item"));
                break;
            case UiValueKind.Object:
                Assert.That(value.ObjectValue!.Single().Value.StringValue, Is.EqualTo("value"));
                break;
        }
    }

    private static void Mutate(UiValue value)
    {
        switch (value.Kind)
        {
            case UiValueKind.Null:
                value.Kind = UiValueKind.String;
                value.StringValue = "mutated";
                break;
            case UiValueKind.Boolean:
                value.BooleanValue = false;
                break;
            case UiValueKind.Integer:
                value.IntegerValue = 99;
                break;
            case UiValueKind.FloatingPoint:
                value.FloatingPointValue = 99.0;
                break;
            case UiValueKind.Decimal:
                value.DecimalValue = 99m;
                break;
            case UiValueKind.String:
                value.StringValue = "mutated";
                break;
            case UiValueKind.DateTime:
                value.DateTimeValue = "mutated";
                break;
            case UiValueKind.Binary:
                value.BinaryValue![0] = 99;
                break;
            case UiValueKind.Array:
                value.ArrayValue![0].StringValue = "mutated";
                break;
            case UiValueKind.Object:
                value.ObjectValue![0].Value.StringValue = "mutated";
                break;
        }
    }

    private static UiStatePatch Patch(string operation, string path, string? from, UiValue? value)
    {
        return new UiStatePatch { Operation = operation, Path = path, FromPath = from, Value = value };
    }

    private static UiSubscription Subscription(string id)
    {
        return new UiSubscription { Id = id, StateId = "source", HandlerId = id + "-handler" };
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
        return value.ObjectValue!.Single(entry => entry.Key == key).Value;
    }

    private class ReentrantObserver : IUiStateObserver
    {
        public IUiStateStore Store;
        public List<string> Calls;
        public bool Reentered;

        public ReentrantObserver(IUiStateStore store, List<string> calls)
        {
            Store = store;
            Calls = calls;
        }

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            Calls.Add("alpha:" + context.CurrentValue!.StringValue);
            if (!Reentered)
            {
                Reentered = true;
                Store.WriteState(null, "source", Text("two"), "observer");
            }

            return new UiResult { Success = true };
        }
    }

    private class UnsubscribingObserver : IUiStateObserver
    {
        public IUiStateStore Store;
        public List<string> Calls;
        public bool Removed;
        public UiStateSubscriptionResult Unsubscribed = new UiStateSubscriptionResult();

        public UnsubscribingObserver(IUiStateStore store, List<string> calls)
        {
            Store = store;
            Calls = calls;
        }

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            Calls.Add("beta:" + context.CurrentValue!.StringValue);
            if (!Removed)
            {
                Removed = true;
                Unsubscribed = Store.UnsubscribeState(null, "zeta");
            }

            return new UiResult { Success = true };
        }
    }

    private class RecordingObserver : IUiStateObserver
    {
        public List<string> Calls;
        public int Count;

        public RecordingObserver(List<string> calls)
        {
            Calls = calls;
        }

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            Count++;
            Calls.Add("zeta:" + context.CurrentValue!.StringValue);
            return new UiResult { Success = true };
        }
    }
}
