using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class StateEffectCoreTests
{
    [Test]
    public void SchedulesExplicitDependenciesOnlyAfterCommitInStableEffectOrder()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "first", Text("first"), "setup");
        core.CreateStateEntry(null, "second", Text("second"), "setup");
        core.CreateStateEntry("other", "first", Text("other"), "setup");
        core.RegisterStateEffect(null, Effect("zeta", new string[] { "first" }, null));
        core.RegisterStateEffect(null, Effect("alpha", new string[] { "second" }, null));
        core.RegisterStateEffect("other", Effect("other-effect", new string[] { "first" }, null));
        StateEffectInvocation[] afterRegistration = core.DrainStateEffectInvocations();

        core.BeginStateBatch("update");
        core.WriteStateEntry(null, "first", Text("new first"), "batch");
        core.WriteStateEntry(null, "second", Text("new second"), "batch");
        StateEffectInvocation[] beforeCommit = core.DrainStateEffectInvocations();
        core.CommitStateBatch();
        StateEffectInvocation[] afterCommit = core.DrainStateEffectInvocations();
        StateOperationResult first = core.ReadStateEntry(null, "first");

        Assert.Multiple(() =>
        {
            Assert.That(afterRegistration, Is.Empty);
            Assert.That(beforeCommit, Is.Empty);
            Assert.That(afterCommit.Select(item => item.EffectId), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(afterCommit, Has.All.Matches<StateEffectInvocation>(item => item.Kind == StateEffectCore.ExecuteInvocation));
            Assert.That(afterCommit, Has.All.Matches<StateEffectInvocation>(item => item.Generation == 1));
            Assert.That(afterCommit[0].Dependencies.Single().Key, Is.EqualTo("second"));
            Assert.That(afterCommit[0].Dependencies.Single().Value.StringValue, Is.EqualTo("new second"));
            Assert.That(afterCommit[1].Dependencies.Single().Value.StringValue, Is.EqualTo("new first"));
            Assert.That(first.Entry!.Effects.Single().Id, Is.EqualTo("zeta"));
            Assert.That(core.ReadStateEffect("other", "other-effect").Effect!.Generation, Is.EqualTo(0));
        });
    }

    [Test]
    public void WaitsForDirtyDerivedDependenciesBeforeQueuingAnEffect()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("initial"), "setup");
        core.RegisterDerivedState(null, Derived("display", new string[] { "source" }, Text("old"), true));
        core.AcceptDerivedStateValue(null, "display", Text("ready"));
        core.RegisterStateEffect(null, Effect("render", new string[] { "source", "display" }, null));

        core.WriteStateEntry(null, "source", Text("changed"), "input");
        StateEffectInvocation[] whileDerivedDirty = core.DrainStateEffectInvocations();
        core.AcceptDerivedStateValue(null, "display", Text("computed"));
        StateEffectInvocation[] ready = core.DrainStateEffectInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(whileDerivedDirty, Is.Empty);
            Assert.That(ready, Has.Length.EqualTo(1));
            Assert.That(ready[0].EffectId, Is.EqualTo("render"));
            Assert.That(ready[0].Dependencies.Select(item => item.Key), Is.EqualTo(new string[] { "display", "source" }));
            Assert.That(ready[0].Dependencies[0].Value.StringValue, Is.EqualTo("computed"));
            Assert.That(ready[0].Dependencies[1].Value.StringValue, Is.EqualTo("changed"));
        });
    }

    [Test]
    public void QueuesCleanupBeforeRerunAndCancellationWithMonotonicGenerations()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("initial"), "setup");
        core.RegisterStateEffect(null, Effect("persist", new string[] { "source" }, "persist-cleanup"));

        core.WriteStateEntry(null, "source", Text("first"), "input");
        StateEffectInvocation first = core.DrainStateEffectInvocations().Single();
        StateEffectOperationResult firstCompletion = core.CompleteStateEffectExecution(null, "persist", first.Generation, true, null);
        core.WriteStateEntry(null, "source", Text("second"), "input");
        StateEffectInvocation[] rerun = core.DrainStateEffectInvocations();
        core.CompleteStateEffectCleanup(null, "persist", rerun[0].Generation, true, null);
        core.CompleteStateEffectExecution(null, "persist", rerun[1].Generation, true, null);
        StateEffectOperationResult canceled = core.CancelStateEffect(null, "persist");
        StateEffectInvocation[] cancellation = core.DrainStateEffectInvocations();
        core.WriteStateEntry(null, "source", Text("after cancellation"), "input");
        StateEffectOperationResult canceledAgain = core.CancelStateEffect(null, "persist");

        Assert.Multiple(() =>
        {
            Assert.That(first.Kind, Is.EqualTo(StateEffectCore.ExecuteInvocation));
            Assert.That(first.Generation, Is.EqualTo(1));
            Assert.That(firstCompletion.Success, Is.True);
            Assert.That(rerun.Select(item => item.Kind), Is.EqualTo(new string[]
            {
                StateEffectCore.CleanupInvocation, StateEffectCore.ExecuteInvocation
            }));
            Assert.That(rerun[0].HandlerId, Is.EqualTo("persist-cleanup"));
            Assert.That(rerun[0].Generation, Is.EqualTo(1));
            Assert.That(rerun[1].Generation, Is.EqualTo(2));
            Assert.That(canceled.Success, Is.True);
            Assert.That(canceled.Effect!.IsCancelled, Is.True);
            Assert.That(canceled.Effect.Generation, Is.EqualTo(3));
            Assert.That(cancellation.Single().Kind, Is.EqualTo(StateEffectCore.CleanupInvocation));
            Assert.That(cancellation.Single().Generation, Is.EqualTo(2));
            Assert.That(core.DrainStateEffectInvocations(), Is.Empty);
            Assert.That(canceledAgain.Applied, Is.False);
        });
    }

    [Test]
    public void CoalescesPendingRunsAndIgnoresStaleCompletions()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("initial"), "setup");
        core.RegisterStateEffect(null, Effect("sync", new string[] { "source" }, null));

        core.WriteStateEntry(null, "source", Text("first"), "input");
        core.WriteStateEntry(null, "source", Text("second"), "input");
        StateEffectInvocation coalesced = core.DrainStateEffectInvocations().Single();
        StateEffectOperationResult stale = core.CompleteStateEffectExecution(null, "sync", 1, true, null);
        StateEffectOperationResult current = core.CompleteStateEffectExecution(null, "sync", coalesced.Generation, true, null);

        Assert.Multiple(() =>
        {
            Assert.That(coalesced.Generation, Is.EqualTo(2));
            Assert.That(coalesced.Dependencies.Single().Value.StringValue, Is.EqualTo("second"));
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Applied, Is.False);
            Assert.That(stale.Code, Is.EqualTo("state.stale-effect-generation"));
            Assert.That(current.Success, Is.True);
            Assert.That(current.Applied, Is.True);
        });
    }

    [Test]
    public void RoutesExecutionCleanupAndGenerationFailuresDeterministically()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("initial"), "setup");
        core.RegisterStateEffect(null, Effect("failing", new string[] { "source" }, "cleanup-handler"));
        core.WriteStateEntry(null, "source", Text("first"), "input");
        StateEffectInvocation first = core.DrainStateEffectInvocations().Single();
        core.CompleteStateEffectExecution(null, "failing", first.Generation, true, null);
        core.WriteStateEntry(null, "source", Text("second"), "input");
        StateEffectInvocation[] second = core.DrainStateEffectInvocations();

        core.CompleteStateEffectCleanup(null, "failing", second[0].Generation, false, "cleanup.failed");
        core.CompleteStateEffectExecution(null, "failing", second[1].Generation, false, null);
        StateEffectError[] errors = core.DrainStateEffectErrors();
        errors[0].Code = "mutated";
        StateEffectError[] drainedAgain = core.DrainStateEffectErrors();

        UiEffect exhausted = Effect("exhausted", new string[] { "source" }, null);
        exhausted.Generation = int.MaxValue;
        core.RegisterStateEffect(null, exhausted);
        core.WriteStateEntry(null, "source", Text("third"), "input");
        StateEffectError generationError = core.DrainStateEffectErrors().Single();

        Assert.Multiple(() =>
        {
            Assert.That(errors.Select(item => item.HandlerId), Is.EqualTo(new string[] { "cleanup-handler", "failing-handler" }));
            Assert.That(errors[1].Code, Is.EqualTo("state.effect-handler-failed"));
            Assert.That(drainedAgain, Is.Empty);
            Assert.That(generationError.EffectId, Is.EqualTo("exhausted"));
            Assert.That(generationError.Code, Is.EqualTo("state.effect-generation-exhausted"));
            Assert.That(core.ReadStateEffect(null, "exhausted").Effect!.Generation, Is.EqualTo(int.MaxValue));
        });
    }

    [Test]
    public void RejectsInvalidEffectDefinitionsAndOperations()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("value"), "setup");
        UiEffect blankId = Effect(" ", new string[] { "source" }, null);
        UiEffect blankHandler = Effect("blank-handler", new string[] { "source" }, null);
        blankHandler.HandlerId = " ";
        UiEffect blankCleanup = Effect("blank-cleanup", new string[] { "source" }, " ");
        UiEffect negativeGeneration = Effect("negative", new string[] { "source" }, null);
        negativeGeneration.Generation = -1;
        UiEffect nullDependencies = Effect("null-dependencies", new string[] { "source" }, null);
        nullDependencies.DependencyIds = null!;
        UiEffect blankDependency = Effect("blank-dependency", new string[] { " " }, null);
        UiEffect duplicateDependency = Effect("duplicate-dependency", new string[] { "source", "source" }, null);
        UiEffect unknownDependency = Effect("unknown-dependency", new string[] { "missing" }, null);

        StateEffectOperationResult registered = core.RegisterStateEffect(null, Effect("valid", new string[] { "source" }, null));
        StateEffectOperationResult duplicate = core.RegisterStateEffect(null, Effect("valid", new string[] { "source" }, null));
        StateEffectOperationResult invalidCancel = core.CancelStateEffect(null, " ");
        StateEffectOperationResult unknownCancel = core.CancelStateEffect(null, "missing");

        Assert.Multiple(() =>
        {
            Assert.That(core.RegisterStateEffect(null, null).Code, Is.EqualTo("state.null-effect"));
            Assert.That(core.RegisterStateEffect(null, blankId).Code, Is.EqualTo("state.invalid-effect-id"));
            Assert.That(core.RegisterStateEffect(null, blankHandler).Code, Is.EqualTo("state.invalid-effect-handler"));
            Assert.That(core.RegisterStateEffect(null, blankCleanup).Code, Is.EqualTo("state.invalid-effect-cleanup"));
            Assert.That(core.RegisterStateEffect(null, negativeGeneration).Code, Is.EqualTo("state.invalid-effect-generation"));
            Assert.That(core.RegisterStateEffect(null, nullDependencies).Code, Is.EqualTo("state.null-effect-dependencies"));
            Assert.That(core.RegisterStateEffect(null, blankDependency).Code, Is.EqualTo("state.invalid-effect-dependency"));
            Assert.That(core.RegisterStateEffect(null, duplicateDependency).Code, Is.EqualTo("state.duplicate-effect-dependency"));
            Assert.That(core.RegisterStateEffect(null, unknownDependency).Code, Is.EqualTo("state.unknown-effect-dependency"));
            Assert.That(registered.Success, Is.True);
            Assert.That(duplicate.Code, Is.EqualTo("state.duplicate-effect"));
            Assert.That(invalidCancel.Code, Is.EqualTo("state.invalid-effect-id"));
            Assert.That(unknownCancel.Code, Is.EqualTo("state.unknown-effect"));
        });
    }

    [Test]
    public void PreventsStateAndRegistrationWritesDuringRender()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("value"), "setup");
        core.CreateStateSnapshot("baseline");
        core.RegisterDerivedState(null, Derived("derived", new string[] { "source" }, Text("value"), false));
        core.RegisterStateEffect(null, Effect("effect", new string[] { "source" }, null));

        StateRenderResult begun = core.BeginStateRender();
        StateRenderResult nested = core.BeginStateRender();
        StateOperationResult write = core.WriteStateEntry(null, "source", Text("blocked"), "render");
        StateOperationResult patch = core.PatchStateEntry(null, "source", new UiStatePatch[]
        {
            new UiStatePatch { Operation = "replace", Path = string.Empty, Value = Text("blocked") }
        }, "render");
        StateBatchResult batch = core.BeginStateBatch("render");
        StateSnapshotResult restore = core.RestoreStateSnapshot("baseline", "render");
        StateDerivedOperationResult derived = core.UpdateDerivedStateDependencies(null, "derived", new string[] { "source" });
        StateEffectOperationResult effect = core.CancelStateEffect(null, "effect");
        StateRenderResult ended = core.EndStateRender();
        StateRenderResult extraEnd = core.EndStateRender();
        StateOperationResult allowed = core.WriteStateEntry(null, "source", Text("allowed"), "input");

        Assert.Multiple(() =>
        {
            Assert.That(begun.Success, Is.True);
            Assert.That(nested.Code, Is.EqualTo("state.render-active"));
            Assert.That(write.Code, Is.EqualTo("state.render-write"));
            Assert.That(patch.Code, Is.EqualTo("state.render-write"));
            Assert.That(batch.Code, Is.EqualTo("state.render-write"));
            Assert.That(restore.Code, Is.EqualTo("state.render-write"));
            Assert.That(derived.Code, Is.EqualTo("state.render-write"));
            Assert.That(effect.Code, Is.EqualTo("state.render-write"));
            Assert.That(core.ReadStateEntry(null, "source").Entry!.Value.StringValue, Is.EqualTo("allowed"));
            Assert.That(ended.Success, Is.True);
            Assert.That(extraEnd.Code, Is.EqualTo("state.render-not-active"));
            Assert.That(allowed.Success, Is.True);
        });
    }

    private static UiEffect Effect(string id, string[] dependencies, string? cleanupHandlerId)
    {
        return new UiEffect
        {
            Id = id,
            DependencyIds = dependencies,
            HandlerId = id + "-handler",
            CleanupHandlerId = cleanupHandlerId
        };
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
