using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class StateDerivedEffectCoverageTests
{
    [Test]
    public void DynamicDependenciesPreserveDiamondOrderCyclesAndMemoizedEffects()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "first", Text("first"), "setup");
        core.CreateStateEntry(null, "second", Text("second"), "setup");
        core.RegisterDerivedState(null, Derived("left", new string[] { "first" }, "old-left"));
        core.RegisterDerivedState(null, Derived("right", new string[] { "first" }, "old-right"));
        core.RegisterDerivedState(null, Derived("combined", new string[] { "left", "right" }, "old-combined"));
        core.RegisterDerivedState(null, Derived("final", new string[] { "combined" }, "old-final"));
        core.AcceptDerivedStateValue(null, "left", Text("left"));
        core.AcceptDerivedStateValue(null, "right", Text("right"));
        core.AcceptDerivedStateValue(null, "combined", Text("combined"));
        core.AcceptDerivedStateValue(null, "final", Text("final"));
        core.RegisterStateEffect(null, Effect("publish", new string[] { "final" }, null));

        StateDerivedOperationResult rewired = core.UpdateDerivedStateDependencies(null, "right", new string[] { "second" });
        StateDerivedOperationResult cycle = core.UpdateDerivedStateDependencies(null, "left", new string[] { "final" });
        StateDerivedOrderResult rewiredOrder = core.GetDirtyDerivedStateOrder(null);
        StateDerivedOperationResult memoizedRight = core.AcceptDerivedStateValue(null, "right", Text("right"));
        StateDerivedOperationResult memoizedCombined = core.AcceptDerivedStateValue(null, "combined", Text("combined"));
        StateDerivedOperationResult memoizedFinal = core.AcceptDerivedStateValue(null, "final", Text("final"));
        StateEffectInvocation[] afterMemoization = core.DrainStateEffectInvocations();

        core.WriteStateEntry(null, "first", Text("changed first"), "input");
        StateDerivedOrderResult firstOrder = core.GetDirtyDerivedStateOrder(null);
        core.AcceptDerivedStateValue(null, "left", Text("left"));
        core.AcceptDerivedStateValue(null, "combined", Text("combined"));
        core.AcceptDerivedStateValue(null, "final", Text("final"));
        StateEffectInvocation[] afterUnchangedBranch = core.DrainStateEffectInvocations();

        core.WriteStateEntry(null, "second", Text("changed second"), "input");
        StateDerivedOrderResult secondOrder = core.GetDirtyDerivedStateOrder(null);
        core.AcceptDerivedStateValue(null, "right", Text("right two"));
        core.AcceptDerivedStateValue(null, "combined", Text("combined two"));
        StateEffectInvocation[] beforeFinal = core.DrainStateEffectInvocations();
        core.AcceptDerivedStateValue(null, "final", Text("final two"));
        StateEffectInvocation[] afterFinal = core.DrainStateEffectInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(rewired.Success, Is.True);
            Assert.That(rewired.Derived!.DependencyIds, Is.EqualTo(new string[] { "second" }));
            Assert.That(cycle.Success, Is.False);
            Assert.That(cycle.Code, Is.EqualTo("state.derived-cycle"));
            Assert.That(core.ReadDerivedState(null, "left").Derived!.DependencyIds, Is.EqualTo(new string[] { "first" }));
            Assert.That(rewiredOrder.States.Select(item => item.Id), Is.EqualTo(new string[] { "right", "combined", "final" }));
            Assert.That(memoizedRight.Applied, Is.False);
            Assert.That(memoizedRight.Derived!.Version, Is.EqualTo(1));
            Assert.That(memoizedCombined.Applied, Is.False);
            Assert.That(memoizedFinal.Applied, Is.False);
            Assert.That(afterMemoization, Is.Empty);
            Assert.That(firstOrder.States.Select(item => item.Id), Is.EqualTo(new string[] { "left", "combined", "final" }));
            Assert.That(afterUnchangedBranch, Is.Empty);
            Assert.That(secondOrder.States.Select(item => item.Id), Is.EqualTo(new string[] { "right", "combined", "final" }));
            Assert.That(beforeFinal, Is.Empty);
            Assert.That(afterFinal, Has.Length.EqualTo(1));
            Assert.That(afterFinal[0].EffectId, Is.EqualTo("publish"));
            Assert.That(afterFinal[0].Dependencies.Single().Key, Is.EqualTo("final"));
            Assert.That(afterFinal[0].Dependencies.Single().Value.StringValue, Is.EqualTo("final two"));
        });
    }

    [Test]
    public void CancellationRemovesQueuedExecutionButPreservesOrderedCleanupAndFailures()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("initial"), "setup");
        core.RegisterStateEffect(null, Effect("zeta", new string[] { "source" }, "zeta-cleanup"));
        core.RegisterStateEffect(null, Effect("alpha", new string[] { "source" }, "alpha-cleanup"));

        core.WriteStateEntry(null, "source", Text("first"), "input");
        StateEffectInvocation[] first = core.DrainStateEffectInvocations();
        core.CompleteStateEffectExecution(null, "alpha", first[0].Generation, true, null);
        core.CompleteStateEffectExecution(null, "zeta", first[1].Generation, true, null);
        core.WriteStateEntry(null, "source", Text("second"), "input");
        StateEffectOperationResult canceled = core.CancelStateEffect(null, "alpha");
        StateEffectInvocation[] second = core.DrainStateEffectInvocations();

        core.CompleteStateEffectCleanup(null, "alpha", second[0].Generation, false, "alpha.cleanup.failed");
        core.CompleteStateEffectCleanup(null, "zeta", second[1].Generation, false, null);
        core.CompleteStateEffectExecution(null, "zeta", second[2].Generation, false, "zeta.execute.failed");
        StateEffectError[] errors = core.DrainStateEffectErrors();
        core.WriteStateEntry(null, "source", Text("third"), "input");
        StateEffectInvocation[] third = core.DrainStateEffectInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(item => item.EffectId), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(canceled.Success, Is.True);
            Assert.That(canceled.Effect!.IsCancelled, Is.True);
            Assert.That(canceled.Effect.Generation, Is.EqualTo(3));
            Assert.That(second.Select(item => item.EffectId + ":" + item.Kind), Is.EqualTo(new string[]
            {
                "alpha:cleanup", "zeta:cleanup", "zeta:execute"
            }));
            Assert.That(errors.Select(item => item.Code), Is.EqualTo(new string[]
            {
                "alpha.cleanup.failed", "state.effect-handler-failed", "zeta.execute.failed"
            }));
            Assert.That(errors.Select(item => item.HandlerId), Is.EqualTo(new string[]
            {
                "alpha-cleanup", "zeta-cleanup", "zeta-handler"
            }));
            Assert.That(third, Has.Length.EqualTo(1));
            Assert.That(third[0].EffectId, Is.EqualTo("zeta"));
            Assert.That(third[0].Kind, Is.EqualTo(StateEffectCore.ExecuteInvocation));
            Assert.That(third[0].Generation, Is.EqualTo(3));
        });
    }

    [Test]
    public void RenderGuardRejectsDerivedAndEffectGraphMutationsWithoutChangingDefinitions()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("value"), "setup");
        core.RegisterDerivedState(null, Derived("derived", new string[] { "source" }, "cached"));
        core.AcceptDerivedStateValue(null, "derived", Text("computed"));
        core.RegisterStateEffect(null, Effect("effect", new string[] { "derived" }, "cleanup"));

        core.BeginStateRender();
        StateDerivedOperationResult registeredDerived = core.RegisterDerivedState(null, Derived("blocked", new string[] { "source" }, "blocked"));
        StateDerivedOperationResult updatedDerived = core.UpdateDerivedStateDependencies(null, "derived", System.Array.Empty<string>());
        StateEffectOperationResult registeredEffect = core.RegisterStateEffect(null, Effect("blocked", new string[] { "source" }, null));
        StateEffectOperationResult canceledEffect = core.CancelStateEffect(null, "effect");
        core.EndStateRender();

        StateDerivedOperationResult derived = core.ReadDerivedState(null, "derived");
        StateEffectOperationResult effect = core.ReadStateEffect(null, "effect");

        Assert.Multiple(() =>
        {
            Assert.That(registeredDerived.Code, Is.EqualTo("state.render-write"));
            Assert.That(updatedDerived.Code, Is.EqualTo("state.render-write"));
            Assert.That(registeredEffect.Code, Is.EqualTo("state.render-write"));
            Assert.That(canceledEffect.Code, Is.EqualTo("state.render-write"));
            Assert.That(core.ReadDerivedState(null, "blocked").Code, Is.EqualTo("state.unknown-derived"));
            Assert.That(core.ReadStateEffect(null, "blocked").Code, Is.EqualTo("state.unknown-effect"));
            Assert.That(derived.Derived!.DependencyIds, Is.EqualTo(new string[] { "source" }));
            Assert.That(derived.Derived.Value.StringValue, Is.EqualTo("computed"));
            Assert.That(effect.Effect!.IsCancelled, Is.False);
            Assert.That(effect.Effect.Generation, Is.EqualTo(0));
        });
    }

    private static UiDerivedState Derived(string id, string[] dependencies, string value)
    {
        return new UiDerivedState
        {
            Id = id,
            DependencyIds = dependencies,
            HandlerId = id + "-handler",
            Value = Text(value),
            IsDirty = true
        };
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

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
