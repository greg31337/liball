using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class StateDerivedCoreTests
{
    [Test]
    public void RegistersIndependentDerivedStateAndMemoizesAcceptedValues()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("Ada"), "setup");
        string[] dependencies = new string[] { "source" };
        UiDerivedState definition = Derived("display", dependencies, Text("pending"), true);

        StateDerivedOperationResult registered = core.RegisterDerivedState(null, definition);
        dependencies[0] = "mutated";
        definition.Value.StringValue = "mutated";
        registered.Derived!.Value.StringValue = "mutated result";
        StateDerivedOrderResult initialOrder = core.GetDirtyDerivedStateOrder(null);
        StateDerivedOperationResult accepted = core.AcceptDerivedStateValue(null, "display", Text("Ada Lovelace"));
        StateDerivedOperationResult ignoredCleanEvaluation = core.AcceptDerivedStateValue(null, "display", Text("ignored"));
        StateDerivedOperationResult retained = core.ReadDerivedState(null, "display");
        StateOperationResult source = core.ReadStateEntry(null, "source");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(registered.Applied, Is.True);
            Assert.That(initialOrder.Success, Is.True);
            Assert.That(initialOrder.States.Select(item => item.Id), Is.EqualTo(new string[] { "display" }));
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Applied, Is.True);
            Assert.That(accepted.Derived!.Version, Is.EqualTo(1));
            Assert.That(accepted.Derived.IsDirty, Is.False);
            Assert.That(ignoredCleanEvaluation.Success, Is.True);
            Assert.That(ignoredCleanEvaluation.Applied, Is.False);
            Assert.That(ignoredCleanEvaluation.Derived!.Value.StringValue, Is.EqualTo("Ada Lovelace"));
            Assert.That(retained.Derived!.DependencyIds, Is.EqualTo(new string[] { "source" }));
            Assert.That(retained.Derived.Value.StringValue, Is.EqualTo("Ada Lovelace"));
            Assert.That(source.Entry!.Derived.Single().Id, Is.EqualTo("display"));
            Assert.That(source.Entry.Derived.Single().IsDirty, Is.False);
            Assert.That(core.GetDirtyDerivedStateOrder(null).States, Is.Empty);
        });
    }

    [Test]
    public void PropagatesDirtyStateTransitivelyAndOrdersADiamondTopologically()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("initial"), "setup");
        core.CreateStateEntry("other", "source", Text("other"), "setup");
        core.RegisterDerivedState(null, Derived("left", new string[] { "source" }, Text("old-left"), true));
        core.RegisterDerivedState(null, Derived("right", new string[] { "source" }, Text("old-right"), true));
        core.RegisterDerivedState(null, Derived("combined", new string[] { "right", "left" }, Text("old-combined"), true));
        core.RegisterDerivedState(null, Derived("final", new string[] { "combined" }, Text("old-final"), true));
        core.RegisterDerivedState("other", Derived("other-display", new string[] { "source" }, Text("cached"), false));
        core.AcceptDerivedStateValue(null, "left", Text("left"));
        core.AcceptDerivedStateValue(null, "right", Text("right"));
        core.AcceptDerivedStateValue(null, "combined", Text("combined"));
        core.AcceptDerivedStateValue(null, "final", Text("final"));

        core.WriteStateEntry(null, "source", Text("changed"), "input");
        StateDerivedOrderResult order = core.GetDirtyDerivedStateOrder(null);
        StateDerivedOperationResult other = core.ReadDerivedState("other", "other-display");
        StateOperationResult source = core.ReadStateEntry(null, "source");

        Assert.Multiple(() =>
        {
            Assert.That(order.Success, Is.True);
            Assert.That(order.States.Select(item => item.Id), Is.EqualTo(new string[] { "left", "right", "combined", "final" }));
            Assert.That(order.States.Single(item => item.Id == "combined").DependencyIds, Is.EqualTo(new string[] { "left", "right" }));
            Assert.That(other.Derived!.IsDirty, Is.False);
            Assert.That(source.Entry!.Derived.Select(item => item.Id), Is.EqualTo(new string[] { "left", "right" }));
            Assert.That(source.Entry.Derived, Has.All.Matches<UiDerivedState>(item => item.IsDirty));
        });
    }

    [Test]
    public void DefersDirtyPropagationUntilCommitAndDiscardsItOnRollback()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("initial"), "setup");
        core.RegisterDerivedState(null, Derived("display", new string[] { "source" }, Text("cached"), true));
        core.AcceptDerivedStateValue(null, "display", Text("ready"));
        core.DrainStateChanges();

        core.BeginStateBatch("rollback");
        core.WriteStateEntry(null, "source", Text("temporary"), "batch");
        StateDerivedOperationResult duringRollbackBatch = core.ReadDerivedState(null, "display");
        core.RollbackStateBatch();
        StateDerivedOperationResult afterRollback = core.ReadDerivedState(null, "display");

        core.BeginStateBatch("commit");
        core.WriteStateEntry(null, "source", Text("committed"), "batch");
        StateDerivedOperationResult beforeCommit = core.ReadDerivedState(null, "display");
        core.CommitStateBatch();
        StateDerivedOperationResult afterCommit = core.ReadDerivedState(null, "display");

        Assert.Multiple(() =>
        {
            Assert.That(duringRollbackBatch.Derived!.IsDirty, Is.False);
            Assert.That(afterRollback.Derived!.IsDirty, Is.False);
            Assert.That(beforeCommit.Derived!.IsDirty, Is.False);
            Assert.That(afterCommit.Derived!.IsDirty, Is.True);
            Assert.That(core.GetDirtyDerivedStateOrder(null).States.Select(item => item.Id), Is.EqualTo(new string[] { "display" }));
        });
    }

    [Test]
    public void UpdatesDependenciesAndRejectsCyclesWithoutChangingTheGraph()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "first-source", Text("first"), "setup");
        core.CreateStateEntry(null, "second-source", Text("second"), "setup");
        core.RegisterDerivedState(null, Derived("first", new string[] { "first-source" }, Text("first value"), false));
        core.RegisterDerivedState(null, Derived("second", new string[] { "first" }, Text("second value"), false));

        StateDerivedOperationResult cyclic = core.UpdateDerivedStateDependencies(null, "first", new string[] { "second" });
        StateDerivedOperationResult updated = core.UpdateDerivedStateDependencies(null, "first", new string[] { "second-source" });
        core.AcceptDerivedStateValue(null, "first", Text("updated first"));
        core.AcceptDerivedStateValue(null, "second", Text("updated second"));
        core.WriteStateEntry(null, "first-source", Text("no longer used"), "input");
        StateDerivedOrderResult afterOldSource = core.GetDirtyDerivedStateOrder(null);
        core.WriteStateEntry(null, "second-source", Text("used"), "input");
        StateDerivedOrderResult afterNewSource = core.GetDirtyDerivedStateOrder(null);

        Assert.Multiple(() =>
        {
            Assert.That(cyclic.Success, Is.False);
            Assert.That(cyclic.Code, Is.EqualTo("state.derived-cycle"));
            Assert.That(updated.Success, Is.True);
            Assert.That(updated.Derived!.DependencyIds, Is.EqualTo(new string[] { "second-source" }));
            Assert.That(afterOldSource.States, Is.Empty);
            Assert.That(afterNewSource.States.Select(item => item.Id), Is.EqualTo(new string[] { "first", "second" }));
        });
    }

    [Test]
    public void RejectsInvalidDefinitionsDependenciesAndIdentityConflicts()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("value"), "setup");
        UiDerivedState blankId = Derived(" ", new string[] { "source" }, Text("value"), true);
        UiDerivedState blankHandler = Derived("blank-handler", new string[] { "source" }, Text("value"), true);
        blankHandler.HandlerId = " ";
        UiDerivedState negativeVersion = Derived("negative", new string[] { "source" }, Text("value"), true);
        negativeVersion.Version = -1;
        UiDerivedState nullDependencies = Derived("null-dependencies", new string[] { "source" }, Text("value"), true);
        nullDependencies.DependencyIds = null!;
        UiDerivedState invalidDependency = Derived("invalid-dependency", new string[] { " " }, Text("value"), true);
        UiDerivedState duplicateDependency = Derived("duplicate-dependency", new string[] { "source", "source" }, Text("value"), true);
        UiDerivedState unknownDependency = Derived("unknown-dependency", new string[] { "missing" }, Text("value"), true);
        UiDerivedState selfDependency = Derived("self", new string[] { "self" }, Text("value"), true);
        UiDerivedState stateConflict = Derived("source", System.Array.Empty<string>(), Text("value"), true);

        StateDerivedOperationResult valid = core.RegisterDerivedState(null, Derived("valid", new string[] { "source" }, Text("value"), true));
        StateDerivedOperationResult duplicate = core.RegisterDerivedState(null, Derived("valid", new string[] { "source" }, Text("value"), true));
        StateOperationResult derivedConflict = core.CreateStateEntry(null, "valid", Text("value"), null);
        core.BeginStateBatch("batch");
        StateDerivedOperationResult duringBatch = core.RegisterDerivedState(null, Derived("batch-derived", new string[] { "source" }, Text("value"), true));
        StateDerivedOperationResult evaluationDuringBatch = core.AcceptDerivedStateValue(null, "valid", Text("value"));
        core.RollbackStateBatch();

        Assert.Multiple(() =>
        {
            Assert.That(core.RegisterDerivedState(null, null).Code, Is.EqualTo("state.null-derived"));
            Assert.That(core.RegisterDerivedState(null, blankId).Code, Is.EqualTo("state.invalid-derived-id"));
            Assert.That(core.RegisterDerivedState(null, blankHandler).Code, Is.EqualTo("state.invalid-derived-handler"));
            Assert.That(core.RegisterDerivedState(null, negativeVersion).Code, Is.EqualTo("state.invalid-derived-version"));
            Assert.That(core.RegisterDerivedState(null, nullDependencies).Code, Is.EqualTo("state.null-derived-dependencies"));
            Assert.That(core.RegisterDerivedState(null, invalidDependency).Code, Is.EqualTo("state.invalid-derived-dependency"));
            Assert.That(core.RegisterDerivedState(null, duplicateDependency).Code, Is.EqualTo("state.duplicate-derived-dependency"));
            Assert.That(core.RegisterDerivedState(null, unknownDependency).Code, Is.EqualTo("state.unknown-derived-dependency"));
            Assert.That(core.RegisterDerivedState(null, selfDependency).Code, Is.EqualTo("state.derived-cycle"));
            Assert.That(core.RegisterDerivedState(null, stateConflict).Code, Is.EqualTo("state.derived-id-conflict"));
            Assert.That(valid.Success, Is.True);
            Assert.That(duplicate.Code, Is.EqualTo("state.duplicate-derived"));
            Assert.That(derivedConflict.Code, Is.EqualTo("state.state-id-conflict"));
            Assert.That(duringBatch.Code, Is.EqualTo("state.batch-active"));
            Assert.That(evaluationDuringBatch.Code, Is.EqualTo("state.batch-active"));
        });
    }

    [Test]
    public void ClearsDirtyWithoutChangingTheVersionForAnUnchangedMemoizedValue()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("value"), "setup");
        UiDerivedState definition = Derived("memo", new string[] { "source" }, Text("same"), true);
        definition.Version = 7;
        core.RegisterDerivedState(null, definition);

        StateDerivedOperationResult unchanged = core.AcceptDerivedStateValue(null, "memo", Text("same"));
        core.WriteStateEntry(null, "source", Text("changed"), "input");
        StateDerivedOperationResult unchangedAgain = core.AcceptDerivedStateValue(null, "memo", Text("same"));
        UiDerivedState exhaustedDefinition = Derived("exhausted", new string[] { "source" }, Text("old"), true);
        exhaustedDefinition.Version = int.MaxValue;
        core.RegisterDerivedState(null, exhaustedDefinition);
        StateDerivedOperationResult exhausted = core.AcceptDerivedStateValue(null, "exhausted", Text("new"));

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Success, Is.True);
            Assert.That(unchanged.Applied, Is.False);
            Assert.That(unchanged.Derived!.Version, Is.EqualTo(7));
            Assert.That(unchanged.Derived.IsDirty, Is.False);
            Assert.That(unchangedAgain.Applied, Is.False);
            Assert.That(unchangedAgain.Derived!.Version, Is.EqualTo(7));
            Assert.That(exhausted.Success, Is.False);
            Assert.That(exhausted.Code, Is.EqualTo("state.derived-version-exhausted"));
            Assert.That(core.ReadDerivedState(null, "exhausted").Derived!.IsDirty, Is.True);
        });
    }

    private static UiDerivedState Derived(string id, string[] dependencies, UiValue value, bool dirty)
    {
        return new UiDerivedState
        {
            Id = id,
            DependencyIds = dependencies,
            HandlerId = id + "-handler",
            Value = value,
            IsDirty = dirty
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
