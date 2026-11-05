using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class StateStoreTransactionTests
{
    [Test]
    public void CommitsAtomicallyWithFirstTouchOrderedDeduplicatedChanges()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "alpha", Text("initial"), "setup");
        core.CreateStateEntry(null, "beta", Text("remove me"), "setup");
        core.DrainStateChanges();

        StateBatchResult begun = core.BeginStateBatch("update");
        core.WriteStateEntry(null, "alpha", Text("first"), "first write");
        core.WriteStateEntry(null, "alpha", Text("final"), "second write");
        core.RemoveStateEntry(null, "beta", "remove");
        core.CreateStateEntry("page", "gamma", Text("created"), "create");
        StateScopedChange[] unpublished = core.DrainStateChanges();
        StateBatchResult committed = core.CommitStateBatch();
        committed.Changes[0].Change.CurrentValue!.StringValue = "mutated result";
        StateScopedChange[] published = core.DrainStateChanges();

        Assert.Multiple(() =>
        {
            Assert.That(begun.Success, Is.True);
            Assert.That(begun.Applied, Is.False);
            Assert.That(unpublished, Is.Empty);
            Assert.That(committed.Success, Is.True);
            Assert.That(committed.Applied, Is.True);
            Assert.That(committed.BatchId, Is.EqualTo("update"));
            Assert.That(committed.Changes.Select(item => item.ScopeId + ":" + item.Change.StateId), Is.EqualTo(new string[]
            {
                ":alpha", ":beta", "page:gamma"
            }));
            Assert.That(committed.Changes[0].Change.PreviousValue!.StringValue, Is.EqualTo("initial"));
            Assert.That(committed.Changes[0].Change.Version, Is.EqualTo(3));
            Assert.That(committed.Changes[0].Change.Origin, Is.EqualTo("second write"));
            Assert.That(published.Select(item => item.ScopeId + ":" + item.Change.StateId), Is.EqualTo(new string[]
            {
                ":alpha", ":beta", "page:gamma"
            }));
            Assert.That(published[0].Change.CurrentValue!.StringValue, Is.EqualTo("final"));
            Assert.That(core.ReadStateEntry(null, "alpha").Entry!.Value.StringValue, Is.EqualTo("final"));
            Assert.That(core.ReadStateEntry(null, "beta").Code, Is.EqualTo("state.unknown"));
            Assert.That(core.ReadStateEntry("page", "gamma").Success, Is.True);
            Assert.That(core.BatchActive, Is.False);
        });
    }

    [Test]
    public void RollsBackTheWholeBatchAfterValidationOrCompareAndSetFailure()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "value", Text("original"), "setup");
        core.DrainStateChanges();

        core.BeginStateBatch("invalid");
        core.WriteStateEntry(null, "value", Text("temporary"), "write");
        StateOperationResult invalid = core.CreateStateEntry(null, "bad", null, "invalid");
        StateOperationResult rejectedAfterFailure = core.WriteStateEntry(null, "value", Text("ignored"), "ignored");
        StateBatchResult invalidCommit = core.CommitStateBatch();
        StateOperationResult afterInvalid = core.ReadStateEntry(null, "value");

        core.BeginStateBatch("cas");
        core.WriteStateEntry(null, "value", Text("temporary again"), "write");
        StateOperationResult mismatch = core.CompareAndSetStateEntry(null, "value", 99, Text("ignored"), "cas");
        StateBatchResult casCommit = core.CommitStateBatch();
        StateOperationResult afterCas = core.ReadStateEntry(null, "value");

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Code, Is.EqualTo("state.null-value"));
            Assert.That(rejectedAfterFailure.Code, Is.EqualTo("state.batch-failed"));
            Assert.That(invalidCommit.Success, Is.False);
            Assert.That(invalidCommit.Code, Is.EqualTo("state.null-value"));
            Assert.That(afterInvalid.Entry!.Value.StringValue, Is.EqualTo("original"));
            Assert.That(afterInvalid.Entry.Version, Is.EqualTo(1));
            Assert.That(mismatch.Success, Is.True);
            Assert.That(mismatch.Applied, Is.False);
            Assert.That(mismatch.Code, Is.EqualTo("state.version-mismatch"));
            Assert.That(casCommit.Success, Is.False);
            Assert.That(casCommit.Code, Is.EqualTo("state.version-mismatch"));
            Assert.That(afterCas.Entry!.Value.StringValue, Is.EqualTo("original"));
            Assert.That(afterCas.Entry.Version, Is.EqualTo(1));
            Assert.That(core.DrainStateChanges(), Is.Empty);
        });
    }

    [Test]
    public void RejectsNestedBatchesAndSupportsExplicitRollbackWithoutDisturbingTheOuterBatch()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "value", Text("original"), null);
        core.DrainStateChanges();

        StateBatchResult invalid = core.BeginStateBatch(" ");
        StateBatchResult outer = core.BeginStateBatch("outer");
        StateBatchResult nested = core.BeginStateBatch("nested");
        StateOperationResult write = core.WriteStateEntry(null, "value", Text("temporary"), "outer");
        StateSnapshotResult snapshotDuringBatch = core.CreateStateSnapshot("not-allowed");
        StateBatchResult rolledBack = core.RollbackStateBatch();
        StateOperationResult retained = core.ReadStateEntry(null, "value");
        StateBatchResult noCommit = core.CommitStateBatch();
        StateBatchResult noRollback = core.RollbackStateBatch();

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Code, Is.EqualTo("state.invalid-batch"));
            Assert.That(outer.Success, Is.True);
            Assert.That(nested.Code, Is.EqualTo("state.batch-active"));
            Assert.That(write.Success, Is.True);
            Assert.That(snapshotDuringBatch.Code, Is.EqualTo("state.batch-active"));
            Assert.That(rolledBack.Success, Is.True);
            Assert.That(rolledBack.Applied, Is.False);
            Assert.That(rolledBack.BatchId, Is.EqualTo("outer"));
            Assert.That(retained.Entry!.Value.StringValue, Is.EqualTo("original"));
            Assert.That(retained.Entry.Version, Is.EqualTo(1));
            Assert.That(noCommit.Code, Is.EqualTo("state.no-batch"));
            Assert.That(noRollback.Code, Is.EqualTo("state.no-batch"));
            Assert.That(core.DrainStateChanges(), Is.Empty);
        });
    }

    [Test]
    public void RestoresNamedSnapshotsAtomicallyWithNewMonotonicVersions()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "alpha", Text("snapshot alpha"), "setup");
        core.CreateStateEntry("page", "beta", Text("snapshot beta"), "setup");
        core.DrainStateChanges();
        StateSnapshotResult snapshot = core.CreateStateSnapshot("baseline");
        snapshot.Snapshot!.Entries[0].Entry.Value.StringValue = "mutated snapshot result";

        core.WriteStateEntry(null, "alpha", Text("changed"), "change");
        core.RemoveStateEntry("page", "beta", "remove");
        core.CreateStateEntry(null, "gamma", Text("extra"), "create");
        core.DrainStateChanges();
        StateSnapshotResult restored = core.RestoreStateSnapshot("baseline", "restore");
        StateScopedChange[] published = core.DrainStateChanges();
        StateOperationResult alpha = core.ReadStateEntry(null, "alpha");
        StateOperationResult beta = core.ReadStateEntry("page", "beta");
        StateOperationResult gamma = core.ReadStateEntry(null, "gamma");

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Success, Is.True);
            Assert.That(restored.Success, Is.True);
            Assert.That(restored.Applied, Is.True);
            Assert.That(restored.Changes.Select(item => item.ScopeId + ":" + item.Change.StateId), Is.EqualTo(new string[]
            {
                ":alpha", ":gamma", "page:beta"
            }));
            Assert.That(published.Select(item => item.ScopeId + ":" + item.Change.StateId), Is.EqualTo(new string[]
            {
                ":alpha", ":gamma", "page:beta"
            }));
            Assert.That(published, Has.All.Matches<StateScopedChange>(item => item.Change.Origin == "restore"));
            Assert.That(alpha.Entry!.Value.StringValue, Is.EqualTo("snapshot alpha"));
            Assert.That(alpha.Entry.Version, Is.EqualTo(3));
            Assert.That(alpha.Entry.Origin, Is.EqualTo("restore"));
            Assert.That(beta.Entry!.Value.StringValue, Is.EqualTo("snapshot beta"));
            Assert.That(beta.Entry.Version, Is.EqualTo(3));
            Assert.That(gamma.Code, Is.EqualTo("state.unknown"));
            Assert.That(restored.Changes.Single(item => item.Change.StateId == "gamma").Change.Version, Is.EqualTo(2));
            Assert.That(core.CreateStateSnapshot("baseline").Code, Is.EqualTo("state.duplicate-snapshot"));
            Assert.That(core.RestoreStateSnapshot("missing", "restore").Code, Is.EqualTo("state.unknown-snapshot"));
            Assert.That(core.RestoreStateSnapshot(" ", "restore").Code, Is.EqualTo("state.invalid-snapshot"));
        });
    }

    [Test]
    public void RollsBackARejectedSnapshotRestoreAndSuppressesItsNotifications()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "bounded", Text("snapshot"), "setup");
        core.CreateStateSnapshot("baseline");
        core.WriteStateEntry(null, "bounded", Text("current"), "change");
        core.DrainStateChanges();
        core.Versions.Single().Version = int.MaxValue;

        StateSnapshotResult restored = core.RestoreStateSnapshot("baseline", "restore");
        StateOperationResult retained = core.ReadStateEntry(null, "bounded");
        StateBatchResult nextBatch = core.BeginStateBatch("next");
        core.RollbackStateBatch();

        Assert.Multiple(() =>
        {
            Assert.That(restored.Success, Is.False);
            Assert.That(restored.Code, Is.EqualTo("state.version-exhausted"));
            Assert.That(retained.Entry!.Value.StringValue, Is.EqualTo("current"));
            Assert.That(retained.Entry.Version, Is.EqualTo(2));
            Assert.That(core.Versions.Single().Version, Is.EqualTo(int.MaxValue));
            Assert.That(core.DrainStateChanges(), Is.Empty);
            Assert.That(nextBatch.Success, Is.True);
        });
    }

    [Test]
    public void OmitsACreateThenRemoveNetChangeFromBatchNotifications()
    {
        StateStoreCore core = new StateStoreCore();
        core.BeginStateBatch("transient");
        core.CreateStateEntry(null, "transient", Text("value"), "create");
        core.RemoveStateEntry(null, "transient", "remove");

        StateBatchResult committed = core.CommitStateBatch();
        StateOperationResult missing = core.ReadStateEntry(null, "transient");
        StateOperationResult recreated = core.CreateStateEntry(null, "transient", Text("later"), "recreate");

        Assert.Multiple(() =>
        {
            Assert.That(committed.Success, Is.True);
            Assert.That(committed.Changes, Is.Empty);
            Assert.That(missing.Code, Is.EqualTo("state.unknown"));
            Assert.That(recreated.Entry!.Version, Is.EqualTo(3));
        });
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
