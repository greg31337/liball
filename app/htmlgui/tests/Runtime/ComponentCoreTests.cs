using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class ComponentCoreTests
{
    [Test]
    public void RegistersCopiedDefinitionsInStableTypeOrder()
    {
        ComponentCore core = new ComponentCore();
        UiComponentDefinition zeta = Definition("zeta", "render-zeta", Entry("size", Text("medium")));
        UiComponentDefinition alpha = Definition("alpha", "render-alpha", Entry("tone", Text("quiet")));

        ComponentOperationResult first = core.RegisterComponentDefinition(zeta);
        ComponentOperationResult second = core.RegisterComponentDefinition(alpha);
        zeta.DefaultParameters[0].Value.StringValue = "mutated source";
        first.Definition!.DefaultParameters[0].Value.StringValue = "mutated result";
        ComponentOperationResult retained = core.ReadComponentDefinition("zeta");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Applied, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(core.Definitions.Select(item => item.TypeId), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(retained.Definition!.DefaultParameters.Single().Value.StringValue, Is.EqualTo("medium"));
            Assert.That(core.RegisterComponentDefinition(Definition("alpha", "other")).Code, Is.EqualTo("component.duplicate-definition"));
            Assert.That(core.RegisterComponentDefinition(null).Code, Is.EqualTo("component.null-definition"));
            Assert.That(core.RegisterComponentDefinition(Definition(string.Empty, "render")).Code, Is.EqualTo("component.invalid-type"));
            Assert.That(core.RegisterComponentDefinition(Definition("missing-handler", string.Empty)).Code, Is.EqualTo("component.invalid-render-handler"));
            Assert.That(core.ReadComponentDefinition("missing").Code, Is.EqualTo("component.unknown-definition"));
        });
    }

    [Test]
    public void CreatesStableTypeKeyIdentityWithMergedParametersAndCopiedSlots()
    {
        ComponentCore core = RegisteredCore();
        UiValueEntry[] parameters = new UiValueEntry[] { Entry("title", Text("Profile")), Entry("size", Text("large")) };
        UiNode suppliedNode = TextNode("supplied", "Supplied");
        UiSlot[] slots = new UiSlot[]
        {
            new UiSlot { Name = "zeta", Content = new UiNode[] { suppliedNode } },
            new UiSlot { Name = "alpha", FallbackContent = new UiNode[] { TextNode("fallback", "Fallback") } }
        };
        UiValueEntry[] context = new UiValueEntry[] { Entry("theme", Text("light")) };

        ComponentOperationResult created = core.CreateComponentInstance("application-seed", null, "card", "profile", parameters, slots, context);
        parameters[0].Value.StringValue = "mutated parameter";
        suppliedNode.Text = "mutated slot";
        context[0].Value.StringValue = "mutated context";
        ComponentOperationResult retained = core.ReadComponentInstance(created.Context!.ComponentId);
        ComponentOperationResult child = core.CreateComponentInstance("application-seed", created.Context.ComponentId, "card", "details", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentCore repeatedCore = RegisteredCore();
        ComponentOperationResult repeated = repeatedCore.CreateComponentInstance("application-seed", null, "card", "profile", new UiValueEntry[] { Entry("size", Text("large")), Entry("title", Text("Profile")) }, slots, new UiValueEntry[] { Entry("theme", Text("light")) });

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(retained.Context!.Parameters.Select(item => item.Key), Is.EqualTo(new string[] { "color", "size", "title" }));
            Assert.That(retained.Context.Parameters.Select(item => item.Value.StringValue), Is.EqualTo(new string[] { "blue", "large", "Profile" }));
            Assert.That(retained.Context.Slots.Select(item => item.Name), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(retained.Context.Slots.Single(item => item.Name == "zeta").Content.Single().Text, Is.EqualTo("Supplied"));
            Assert.That(retained.Context.ContextValues.Single().Value.StringValue, Is.EqualTo("light"));
            Assert.That(created.Context.StateScopeId, Is.Not.Empty);
            Assert.That(child.Context!.ComponentId, Is.Not.EqualTo(created.Context.ComponentId));
            Assert.That(child.Context.StateScopeId, Is.Not.EqualTo(created.Context.StateScopeId));
            Assert.That(repeated.Context!.ComponentId, Is.EqualTo(created.Context.ComponentId));
            Assert.That(repeated.Context.StateScopeId, Is.EqualTo(created.Context.StateScopeId));
            Assert.That(core.CreateComponentInstance("application-seed", null, "card", "profile", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.duplicate-instance"));
            Assert.That(core.UnregisterComponentDefinition("card").Code, Is.EqualTo("component.definition-in-use"));
        });
    }

    [Test]
    public void LocalStateUsesAnIsolatedDeterministicScopePerInstance()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult first = core.CreateComponentInstance("seed", null, "card", "first", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult second = core.CreateComponentInstance("seed", null, "card", "second", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());

        StateOperationResult firstCreated = core.CreateComponentLocalState(first.Context!.ComponentId, "count", Integer(1));
        StateOperationResult secondCreated = core.CreateComponentLocalState(second.Context!.ComponentId, "count", Integer(10));
        StateOperationResult firstWritten = core.WriteComponentLocalState(first.Context.ComponentId, "count", Integer(2));
        StateOperationResult firstRead = core.ReadComponentLocalState(first.Context.ComponentId, "count");
        StateOperationResult secondRead = core.ReadComponentLocalState(second.Context.ComponentId, "count");

        Assert.Multiple(() =>
        {
            Assert.That(firstCreated.Success, Is.True);
            Assert.That(secondCreated.Success, Is.True);
            Assert.That(firstCreated.ScopeId, Is.EqualTo(first.Context.StateScopeId));
            Assert.That(secondCreated.ScopeId, Is.EqualTo(second.Context.StateScopeId));
            Assert.That(firstWritten.Entry!.Version, Is.EqualTo(2));
            Assert.That(firstRead.Entry!.Value.IntegerValue, Is.EqualTo(2));
            Assert.That(secondRead.Entry!.Value.IntegerValue, Is.EqualTo(10));
            Assert.That(core.ReadComponentLocalState("missing", "count").Code, Is.EqualTo("component.unknown"));
        });
    }

    [Test]
    public void ContextConsumptionUsesTheNearestAncestorProvider()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = core.CreateComponentInstance("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), new UiValueEntry[]
        {
            Entry("locale", Text("en")),
            Entry("theme", Text("light"))
        });
        ComponentOperationResult child = core.CreateComponentInstance("seed", root.Context!.ComponentId, "card", "child", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), new UiValueEntry[]
        {
            Entry("locale", Text("fr"))
        });
        ComponentOperationResult grandchild = core.CreateComponentInstance("seed", child.Context!.ComponentId, "card", "grandchild", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());

        ComponentOperationResult locale = core.ConsumeComponentContextValue(grandchild.Context!.ComponentId, "locale");
        ComponentOperationResult theme = core.ConsumeComponentContextValue(grandchild.Context.ComponentId, "theme");
        ComponentOperationResult provided = core.ProvideComponentContextValue(child.Context.ComponentId, "locale", Text("de"));
        provided.Value!.StringValue = "mutated result";
        ComponentOperationResult updatedLocale = core.ConsumeComponentContextValue(grandchild.Context.ComponentId, "locale");

        Assert.Multiple(() =>
        {
            Assert.That(locale.Value!.StringValue, Is.EqualTo("fr"));
            Assert.That(locale.ProviderComponentId, Is.EqualTo(child.Context.ComponentId));
            Assert.That(theme.Value!.StringValue, Is.EqualTo("light"));
            Assert.That(theme.ProviderComponentId, Is.EqualTo(root.Context.ComponentId));
            Assert.That(provided.Applied, Is.True);
            Assert.That(updatedLocale.Value!.StringValue, Is.EqualTo("de"));
            Assert.That(core.ConsumeComponentContextValue(root.Context.ComponentId, "locale").Code, Is.EqualTo("component.unknown-context"));
            Assert.That(core.ConsumeComponentContextValue(grandchild.Context.ComponentId, "missing").Code, Is.EqualTo("component.unknown-context"));
        });
    }

    [Test]
    public void DispatchesCopiedVersionedRenderRequestsAndRejectsStaleCompletion()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult created = core.CreateComponentInstance("seed", null, "card", "profile", new UiValueEntry[] { Entry("title", Text("First")) }, System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());

        ComponentOperationResult first = core.BeginComponentRender(created.Context!.ComponentId, UiUpdatePriority.Immediate, true);
        first.RenderRequest!.Context.Parameters.Single(item => item.Key == "title").Value.StringValue = "mutated request";
        core.UpdateComponentParameters(created.Context.ComponentId, new UiValueEntry[] { Entry("title", Text("Second")) });
        ComponentOperationResult second = core.BeginComponentRender(created.Context.ComponentId, UiUpdatePriority.Background, false);
        ComponentRenderInvocation[] invocations = core.DrainComponentRenderInvocations();
        invocations[0].Request.Context.Parameters.Single(item => item.Key == "title").Value.StringValue = "mutated invocation";
        UiNode rendered = TextNode("rendered", "Second render");
        ComponentOperationResult stale = core.CompleteComponentRender(created.Context.ComponentId, first.RenderRequest.Generation, TextNode("stale", "Stale"));
        ComponentOperationResult completed = core.CompleteComponentRender(created.Context.ComponentId, second.RenderRequest!.Generation, rendered);
        rendered.Text = "mutated source";
        completed.Node!.Text = "mutated result";
        ComponentOperationResult retained = core.ReadComponentInstance(created.Context.ComponentId);

        Assert.Multiple(() =>
        {
            Assert.That(invocations.Select(item => item.HandlerId), Is.EqualTo(new string[] { "render-card", "render-card" }));
            Assert.That(invocations.Select(item => item.Request.Generation), Is.EqualTo(new int[] { 1, 2 }));
            Assert.That(invocations[0].Request.IsHydration, Is.True);
            Assert.That(invocations[0].Request.Priority, Is.EqualTo(UiUpdatePriority.Immediate));
            Assert.That(invocations[1].Request.Context.Parameters.Single(item => item.Key == "title").Value.StringValue, Is.EqualTo("Second"));
            Assert.That(second.RenderRequest.Generation, Is.EqualTo(2));
            Assert.That(stale.Code, Is.EqualTo("component.stale-render"));
            Assert.That(completed.Success, Is.True);
            Assert.That(retained.Node!.Text, Is.EqualTo("Second render"));
            Assert.That(core.DrainComponentRenderInvocations(), Is.Empty);
            Assert.That(core.BeginComponentRender(created.Context.ComponentId, (UiUpdatePriority)999, false).Code, Is.EqualTo("component.invalid-priority"));
            Assert.That(core.CompleteComponentRender(created.Context.ComponentId, 2, null).Code, Is.EqualTo("component.null-render"));
        });
    }

    [Test]
    public void EmitsCreateParameterAttachAndActivateLifecycleInParentFirstOrder()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = core.CreateComponentInstance("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult child = core.CreateComponentInstance("seed", root.Context!.ComponentId, "card", "child", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult grandchild = core.CreateComponentInstance("seed", child.Context!.ComponentId, "card", "grandchild", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());

        ComponentLifecycleInvocation[] created = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult parentDetached = core.AttachComponentTree(child.Context.ComponentId);
        ComponentOperationResult attached = core.AttachComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] attachment = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult activated = core.ActivateComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] activation = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult changed = core.UpdateComponentParameters(child.Context.ComponentId, new UiValueEntry[] { Entry("size", Text("large")) });
        ComponentOperationResult unchanged = core.UpdateComponentParameters(child.Context.ComponentId, new UiValueEntry[] { Entry("size", Text("large")) });
        ComponentLifecycleInvocation[] parameters = core.DrainComponentLifecycleInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(created.Select(item => item.Notification.ComponentId), Is.EqualTo(new string[] { root.Context.ComponentId, child.Context.ComponentId, grandchild.Context!.ComponentId }));
            Assert.That(created.Select(item => item.Notification.Stage), Is.All.EqualTo(UiLifecycleStage.Create));
            Assert.That(created.Select(item => item.HandlerId), Is.All.EqualTo("lifecycle-card"));
            Assert.That(parentDetached.Code, Is.EqualTo("component.parent-detached"));
            Assert.That(attached.Applied, Is.True);
            Assert.That(attachment.Select(item => item.Notification.ComponentId), Is.EqualTo(new string[] { root.Context.ComponentId, child.Context.ComponentId, grandchild.Context.ComponentId }));
            Assert.That(attachment.Select(item => item.Notification.Stage), Is.All.EqualTo(UiLifecycleStage.Attach));
            Assert.That(activated.Applied, Is.True);
            Assert.That(activation.Select(item => item.Notification.ComponentId), Is.EqualTo(new string[] { root.Context.ComponentId, child.Context.ComponentId, grandchild.Context.ComponentId }));
            Assert.That(activation.Select(item => item.Notification.Stage), Is.All.EqualTo(UiLifecycleStage.Activate));
            Assert.That(changed.Applied, Is.True);
            Assert.That(unchanged.Applied, Is.False);
            Assert.That(parameters.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.ParametersChanged }));
            Assert.That(parameters.Single().Notification.ComponentId, Is.EqualTo(child.Context.ComponentId));
        });
    }

    [Test]
    public void OrdersHydrationAndRenderLifecycleAroundTheRenderCallback()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult created = core.CreateComponentInstance("seed", null, "card", "hydrated", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        core.DrainComponentLifecycleInvocations();

        ComponentOperationResult started = core.BeginComponentRender(created.Context!.ComponentId, UiUpdatePriority.Immediate, true);
        ComponentLifecycleInvocation[] before = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult completed = core.CompleteComponentRender(created.Context.ComponentId, started.RenderRequest!.Generation, TextNode("result", "Hydrated"));
        ComponentLifecycleInvocation[] after = core.DrainComponentLifecycleInvocations();
        before[0].Notification.ComponentId = "mutated copy";
        ComponentOperationResult normalStarted = core.BeginComponentRender(created.Context.ComponentId, UiUpdatePriority.Normal, false);
        ComponentOperationResult normalCompleted = core.CompleteComponentRender(created.Context.ComponentId, normalStarted.RenderRequest!.Generation, TextNode("normal", "Normal"));
        ComponentLifecycleInvocation[] normal = core.DrainComponentLifecycleInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(started.Success, Is.True);
            Assert.That(completed.Success, Is.True);
            Assert.That(before.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.HydrationStart, UiLifecycleStage.BeforeRender }));
            Assert.That(before.Select(item => item.Notification.Generation), Is.All.EqualTo(1));
            Assert.That(after.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.AfterRender, UiLifecycleStage.HydrationComplete }));
            Assert.That(after.Select(item => item.Notification.Generation), Is.All.EqualTo(1));
            Assert.That(normalStarted.Success, Is.True);
            Assert.That(normalCompleted.Success, Is.True);
            Assert.That(normal.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.BeforeRender, UiLifecycleStage.AfterRender }));
            Assert.That(normal.Select(item => item.Notification.Generation), Is.All.EqualTo(2));
            Assert.That(core.LifecycleInvocations, Is.Empty);
        });
    }

    [Test]
    public void DeactivatesDetachesAndDisposesChildrenBeforeParentsWithoutRepeatingStages()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = core.CreateComponentInstance("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult child = core.CreateComponentInstance("seed", root.Context!.ComponentId, "card", "child", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult grandchild = core.CreateComponentInstance("seed", child.Context!.ComponentId, "card", "grandchild", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        string[] parentFirst = new string[] { root.Context.ComponentId, child.Context.ComponentId, grandchild.Context!.ComponentId };
        string[] childFirst = new string[] { grandchild.Context.ComponentId, child.Context.ComponentId, root.Context.ComponentId };
        core.AttachComponentTree(root.Context.ComponentId);
        core.ActivateComponentTree(root.Context.ComponentId);
        core.DrainComponentLifecycleInvocations();

        ComponentOperationResult deactivated = core.DeactivateComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] deactivation = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult detached = core.DetachComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] detachment = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult repeatedDetach = core.DetachComponentTree(root.Context.ComponentId);
        core.AttachComponentTree(root.Context.ComponentId);
        core.ActivateComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] restored = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult disposed = core.DisposeComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] disposal = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult repeatedDispose = core.DisposeComponentTree(root.Context.ComponentId);

        Assert.Multiple(() =>
        {
            Assert.That(deactivated.Applied, Is.True);
            Assert.That(deactivation.Select(item => item.Notification.ComponentId), Is.EqualTo(childFirst));
            Assert.That(deactivation.Select(item => item.Notification.Stage), Is.All.EqualTo(UiLifecycleStage.Deactivate));
            Assert.That(detached.Applied, Is.True);
            Assert.That(detachment.Select(item => item.Notification.ComponentId), Is.EqualTo(childFirst));
            Assert.That(detachment.Select(item => item.Notification.Stage), Is.All.EqualTo(UiLifecycleStage.Detach));
            Assert.That(repeatedDetach.Applied, Is.False);
            Assert.That(restored.Select(item => item.Notification.ComponentId), Is.EqualTo(parentFirst.Concat(parentFirst)));
            Assert.That(restored.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.Attach, UiLifecycleStage.Attach, UiLifecycleStage.Attach, UiLifecycleStage.Activate, UiLifecycleStage.Activate, UiLifecycleStage.Activate }));
            Assert.That(disposed.Applied, Is.True);
            Assert.That(disposal.Select(item => item.Notification.ComponentId), Is.EqualTo(childFirst.Concat(childFirst).Concat(childFirst)));
            Assert.That(disposal.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.Deactivate, UiLifecycleStage.Deactivate, UiLifecycleStage.Deactivate, UiLifecycleStage.Detach, UiLifecycleStage.Detach, UiLifecycleStage.Detach, UiLifecycleStage.Dispose, UiLifecycleStage.Dispose, UiLifecycleStage.Dispose }));
            Assert.That(repeatedDispose.Success, Is.True);
            Assert.That(repeatedDispose.Applied, Is.False);
            Assert.That(core.DrainComponentLifecycleInvocations(), Is.Empty);
            Assert.That(core.UpdateComponentParameters(child.Context.ComponentId, System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.disposed"));
            Assert.That(core.BeginComponentRender(root.Context.ComponentId, UiUpdatePriority.Normal, false).Code, Is.EqualTo("component.disposed"));
            Assert.That(core.CreateComponentInstance("seed", root.Context.ComponentId, "card", "late", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.parent-disposed"));
        });
    }

    [Test]
    public void ReconciliationPreservesKeyedStateUntilAnExplicitRemount()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult mounted = core.ReconcileComponentInstance("seed", null, "card", "profile", new UiValueEntry[] { Entry("size", Text("large")) }, System.Array.Empty<UiSlot>(), new UiValueEntry[] { Entry("theme", Text("light")) }, true, false);
        core.CreateComponentLocalState(mounted.Context!.ComponentId, "count", Integer(7));
        core.DrainComponentLifecycleInvocations();

        ComponentOperationResult preserved = core.ReconcileComponentInstance("seed", null, "card", "profile", new UiValueEntry[] { Entry("size", Text("large")) }, System.Array.Empty<UiSlot>(), new UiValueEntry[] { Entry("theme", Text("dark")) }, true, false);
        StateOperationResult preservedState = core.ReadComponentLocalState(preserved.Context!.ComponentId, "count");
        ComponentOperationResult hidden = core.SetComponentPresence(preserved.Context.ComponentId, false);
        ComponentLifecycleInvocation[] hiddenLifecycle = core.DrainComponentLifecycleInvocations();
        StateOperationResult hiddenState = core.ReadComponentLocalState(preserved.Context.ComponentId, "count");
        ComponentOperationResult restored = core.SetComponentPresence(preserved.Context.ComponentId, true);
        ComponentLifecycleInvocation[] restoredLifecycle = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult remounted = core.ReconcileComponentInstance("seed", null, "card", "profile", new UiValueEntry[] { Entry("size", Text("small")) }, System.Array.Empty<UiSlot>(), new UiValueEntry[] { Entry("theme", Text("contrast")) }, true, true);
        ComponentLifecycleInvocation[] remountLifecycle = core.DrainComponentLifecycleInvocations();
        StateOperationResult resetState = core.ReadComponentLocalState(remounted.Context!.ComponentId, "count");
        StateOperationResult recreatedState = core.CreateComponentLocalState(remounted.Context.ComponentId, "count", Integer(1));

        Assert.Multiple(() =>
        {
            Assert.That(preserved.Success, Is.True);
            Assert.That(preserved.Context.ComponentId, Is.EqualTo(mounted.Context.ComponentId));
            Assert.That(preserved.Context.ContextValues.Single().Value.StringValue, Is.EqualTo("dark"));
            Assert.That(preservedState.Entry!.Value.IntegerValue, Is.EqualTo(7));
            Assert.That(hidden.Applied, Is.True);
            Assert.That(hiddenLifecycle.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.Deactivate }));
            Assert.That(hiddenState.Entry!.Value.IntegerValue, Is.EqualTo(7));
            Assert.That(restored.Applied, Is.True);
            Assert.That(restoredLifecycle.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.Activate }));
            Assert.That(remounted.Success, Is.True);
            Assert.That(remounted.Context.ComponentId, Is.EqualTo(mounted.Context.ComponentId));
            Assert.That(remounted.Context.Parameters.Single(item => item.Key == "size").Value.StringValue, Is.EqualTo("small"));
            Assert.That(remountLifecycle.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.Deactivate, UiLifecycleStage.Detach, UiLifecycleStage.Dispose, UiLifecycleStage.Create, UiLifecycleStage.Attach, UiLifecycleStage.Activate }));
            Assert.That(resetState.Code, Is.EqualTo("state.unknown"));
            Assert.That(recreatedState.Entry!.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void DisposalTearsDownChildResourcesBeforeParentResources()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = core.CreateComponentInstance("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), new UiValueEntry[] { Entry("theme", Text("light")) });
        ComponentOperationResult child = core.CreateComponentInstance("seed", root.Context!.ComponentId, "card", "child", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), new UiValueEntry[] { Entry("theme", Text("dark")) });
        core.CreateComponentLocalState(root.Context.ComponentId, "value", Text("root"));
        core.CreateComponentLocalState(child.Context!.ComponentId, "value", Text("child"));
        core.RegisterComponentSubscription(root.Context.ComponentId, Subscription("watch", "value", "root-watch"));
        core.RegisterComponentSubscription(child.Context.ComponentId, Subscription("watch", "value", "child-watch"));
        core.RegisterComponentEffect(root.Context.ComponentId, Effect("sync", "value", "root-cleanup"));
        core.RegisterComponentEffect(child.Context.ComponentId, Effect("sync", "value", "child-cleanup"));
        core.WriteComponentLocalState(root.Context.ComponentId, "value", Text("updated root"));
        core.WriteComponentLocalState(child.Context.ComponentId, "value", Text("updated child"));
        StateEffectInvocation[] executions = core.StateStore.DrainStateEffectInvocations();
        int executionIndex = 0;
        while (executionIndex < executions.Length)
        {
            core.StateStore.CompleteStateEffectExecution(executions[executionIndex].ScopeId, executions[executionIndex].EffectId, executions[executionIndex].Generation, true, null);
            executionIndex++;
        }

        core.AttachComponentTree(root.Context.ComponentId);
        core.ActivateComponentTree(root.Context.ComponentId);
        core.DrainComponentLifecycleInvocations();
        ComponentOperationResult disposed = core.DisposeComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] lifecycle = core.DrainComponentLifecycleInvocations();
        StateEffectInvocation[] cleanups = core.StateStore.DrainStateEffectInvocations();
        StateSubscriptionDispatchResult subscriptionDispatch = core.StateStore.BeginStateSubscriptionDispatch();
        core.StateStore.EndStateSubscriptionDispatch();

        Assert.Multiple(() =>
        {
            Assert.That(disposed.Success, Is.True);
            Assert.That(core.Instances, Is.Empty);
            Assert.That(core.StateStore.Entries, Is.Empty);
            Assert.That(core.StateStore.Versions, Is.Empty);
            Assert.That(core.StateStore.EffectCore.Effects, Is.Empty);
            Assert.That(core.StateStore.SubscriptionCore.Subscriptions, Is.Empty);
            Assert.That(core.StateStore.SubscriptionCore.Lifetimes, Is.Empty);
            Assert.That(subscriptionDispatch.Invocations, Is.Empty);
            Assert.That(cleanups.Select(item => item.HandlerId), Is.EqualTo(new string[] { "child-cleanup", "root-cleanup" }));
            Assert.That(cleanups.Select(item => item.Kind), Is.All.EqualTo(StateEffectCore.CleanupInvocation));
            Assert.That(lifecycle.Where(item => item.Notification.Stage == UiLifecycleStage.Dispose).Select(item => item.Notification.ComponentId), Is.EqualTo(new string[] { child.Context.ComponentId, root.Context.ComponentId }));
            Assert.That(core.ReadComponentLocalState(child.Context.ComponentId, "value").Code, Is.EqualTo("component.disposed"));
        });
    }

    [Test]
    public void ChildCleanupUsesStableIdentityOrderForSiblings()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = core.CreateComponentInstance("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult zeta = core.CreateComponentInstance("seed", root.Context!.ComponentId, "card", "zeta", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult alpha = core.CreateComponentInstance("seed", root.Context.ComponentId, "card", "alpha", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        string[] orderedChildren = new string[] { zeta.Context!.ComponentId, alpha.Context!.ComponentId }.OrderBy(item => item, System.StringComparer.Ordinal).ToArray();
        core.DrainComponentLifecycleInvocations();

        core.DisposeComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] lifecycle = core.DrainComponentLifecycleInvocations();
        string[] disposedIds = new string[] { root.Context.ComponentId, zeta.Context.ComponentId, alpha.Context.ComponentId }.OrderBy(item => item, System.StringComparer.Ordinal).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.Select(item => item.Notification.ComponentId), Is.EqualTo(orderedChildren.Concat(new string[] { root.Context.ComponentId })));
            Assert.That(lifecycle.Select(item => item.Notification.Stage), Is.All.EqualTo(UiLifecycleStage.Dispose));
            Assert.That(core.Instances, Is.Empty);
            Assert.That(core.DisposedComponentIds, Is.EqualTo(disposedIds));
        });
    }

    [Test]
    public void RoutesNestedErrorsToTheNearestAncestorBoundaryAndRendersItsFallback()
    {
        ComponentCore core = BoundaryCore();
        ComponentOperationResult outer = core.CreateComponentInstance("seed", null, "boundary", "outer", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult inner = core.CreateComponentInstance("seed", outer.Context!.ComponentId, "boundary", "inner", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult leaf = core.CreateComponentInstance("seed", inner.Context!.ComponentId, "leaf", "leaf", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiNode outerFallback = TextNode("outer-error", "Outer fallback");
        UiNode innerFallback = TextNode("inner-error", "Inner fallback");
        core.ConfigureComponentErrorBoundary(outer.Context.ComponentId, outerFallback, "outer-1");
        core.ConfigureComponentErrorBoundary(inner.Context.ComponentId, innerFallback, "inner-1");
        outerFallback.Text = "mutated outer source";
        innerFallback.Text = "mutated inner source";
        core.AttachComponentTree(outer.Context.ComponentId);
        core.ActivateComponentTree(outer.Context.ComponentId);
        core.DrainComponentLifecycleInvocations();
        UiDiagnostic leafDiagnostic = Diagnostic("component.render-failed", "Leaf render failed", "/leaf");

        ComponentOperationResult caught = core.ReportComponentError(leaf.Context!.ComponentId, UiLifecycleStage.AfterRender, leafDiagnostic);
        leafDiagnostic.Message = "mutated diagnostic source";
        ComponentErrorInvocation firstInvocation = core.DrainComponentErrorInvocations().Single();
        firstInvocation.Boundary.Diagnostic!.Message = "mutated invocation";
        ComponentOperationResult retainedInner = core.ReadComponentErrorBoundary(inner.Context.ComponentId);
        ComponentOperationResult retainedOuter = core.ReadComponentErrorBoundary(outer.Context.ComponentId);
        ComponentOperationResult renderedInner = core.ReadComponentInstance(inner.Context.ComponentId);
        ComponentOperationResult propagated = core.ReportComponentError(inner.Context.ComponentId, UiLifecycleStage.Error, Diagnostic("component.boundary-failed", "Inner boundary failed", "/inner"));
        ComponentErrorInvocation secondInvocation = core.DrainComponentErrorInvocations().Single();

        Assert.Multiple(() =>
        {
            Assert.That(caught.Success, Is.True);
            Assert.That(caught.ErrorBoundary!.BoundaryId, Is.EqualTo(inner.Context.ComponentId));
            Assert.That(caught.Node!.Text, Is.EqualTo("Inner fallback"));
            Assert.That(firstInvocation.HandlerId, Is.EqualTo("error-boundary"));
            Assert.That(firstInvocation.FailedComponentId, Is.EqualTo(leaf.Context.ComponentId));
            Assert.That(firstInvocation.Stage, Is.EqualTo(UiLifecycleStage.AfterRender));
            Assert.That(retainedInner.ErrorBoundary!.HasError, Is.True);
            Assert.That(retainedInner.ErrorBoundary.Diagnostic!.Message, Is.EqualTo("Leaf render failed"));
            Assert.That(retainedOuter.ErrorBoundary!.HasError, Is.False);
            Assert.That(renderedInner.Node!.Text, Is.EqualTo("Inner fallback"));
            Assert.That(propagated.ErrorBoundary!.BoundaryId, Is.EqualTo(outer.Context.ComponentId));
            Assert.That(propagated.Node!.Text, Is.EqualTo("Outer fallback"));
            Assert.That(secondInvocation.FailedComponentId, Is.EqualTo(inner.Context.ComponentId));
            Assert.That(secondInvocation.Boundary.BoundaryId, Is.EqualTo(outer.Context.ComponentId));
        });
    }

    [Test]
    public void ResetKeysClearBoundaryStateAndReactivatePreservedDescendants()
    {
        ComponentCore core = BoundaryCore();
        ComponentOperationResult boundary = core.CreateComponentInstance("seed", null, "boundary", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult leaf = core.CreateComponentInstance("seed", boundary.Context!.ComponentId, "leaf", "content", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        core.ConfigureComponentErrorBoundary(boundary.Context.ComponentId, TextNode("error", "Fallback"), "version-1");
        core.AttachComponentTree(boundary.Context.ComponentId);
        core.ActivateComponentTree(boundary.Context.ComponentId);
        core.DrainComponentLifecycleInvocations();
        core.ReportComponentError(leaf.Context!.ComponentId, UiLifecycleStage.BeforeRender, Diagnostic("component.failed", "Failed", null));
        core.DrainComponentErrorInvocations();
        core.DrainComponentLifecycleInvocations();

        ComponentOperationResult unchanged = core.ResetComponentErrorBoundary(boundary.Context.ComponentId, "version-1");
        ComponentOperationResult reset = core.ResetComponentErrorBoundary(boundary.Context.ComponentId, "version-2");
        ComponentLifecycleInvocation[] lifecycle = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult retained = core.ReadComponentErrorBoundary(boundary.Context.ComponentId);
        ComponentOperationResult rendered = core.ReadComponentInstance(boundary.Context.ComponentId);

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Applied, Is.False);
            Assert.That(unchanged.ErrorBoundary!.HasError, Is.True);
            Assert.That(reset.Applied, Is.True);
            Assert.That(reset.ErrorBoundary!.HasError, Is.False);
            Assert.That(reset.ErrorBoundary.Diagnostic, Is.Null);
            Assert.That(reset.ErrorBoundary.ResetKey, Is.EqualTo("version-2"));
            Assert.That(retained.ErrorBoundary!.HasError, Is.False);
            Assert.That(rendered.Node, Is.Null);
            Assert.That(lifecycle.Select(item => item.Notification.ComponentId), Is.EqualTo(new string[] { leaf.Context.ComponentId }));
            Assert.That(lifecycle.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.Activate }));
            Assert.That(core.ResetComponentErrorBoundary(boundary.Context.ComponentId, " ").Code, Is.EqualTo("component.invalid-reset-key"));
            Assert.That(core.ConfigureComponentErrorBoundary(leaf.Context.ComponentId, null, null).Code, Is.EqualTo("component.not-error-boundary"));
        });
    }

    [Test]
    public void ThrowsUnrecoverableComponentFailuresWhenNoAncestorBoundaryExists()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = core.CreateComponentInstance("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        ComponentOperationResult child = core.CreateComponentInstance("seed", root.Context!.ComponentId, "card", "child", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        string childId = child.Context!.ComponentId;
        core.DrainComponentLifecycleInvocations();

        UiComponentException? failure = Assert.Throws<UiComponentException>(() => core.ReportComponentError(childId, UiLifecycleStage.BeforeRender, Diagnostic("component.unhandled", "Unhandled render failure", "/child")));
        ComponentLifecycleInvocation lifecycle = core.DrainComponentLifecycleInvocations().Single();
        ComponentOperationResult nullDiagnostic = core.ReportComponentError(childId, UiLifecycleStage.BeforeRender, null);
        ComponentOperationResult badStage = core.ReportComponentError(childId, (UiLifecycleStage)999, Diagnostic("component.invalid-stage", "Invalid stage", null));

        Assert.Multiple(() =>
        {
            Assert.That(failure!.ComponentId, Is.EqualTo(childId));
            Assert.That(failure.Stage, Is.EqualTo(UiLifecycleStage.BeforeRender));
            Assert.That(failure.Message, Does.Contain("Unhandled render failure"));
            Assert.That(lifecycle.Notification.Stage, Is.EqualTo(UiLifecycleStage.Error));
            Assert.That(lifecycle.Notification.Diagnostic!.Code, Is.EqualTo("component.unhandled"));
            Assert.That(nullDiagnostic.Code, Is.EqualTo("component.null-diagnostic"));
            Assert.That(badStage.Code, Is.EqualTo("component.invalid-lifecycle-stage"));
            Assert.That(core.DrainComponentErrorInvocations(), Is.Empty);
        });
    }

    [Test]
    public void RejectsMalformedParametersSlotsContextsAndParentRelationships()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult parent = core.CreateComponentInstance("seed", null, "card", "parent", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiValueEntry duplicate = Entry("same", Text("value"));
        UiSlot repeated = new UiSlot { Name = "same" };

        Assert.Multiple(() =>
        {
            Assert.That(core.CreateComponentInstance(null, null, "card", "item", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.invalid-root-seed"));
            Assert.That(core.CreateComponentInstance("seed", null, "missing", "item", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.unknown-definition"));
            Assert.That(core.CreateComponentInstance("seed", "missing", "card", "item", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.unknown-parent"));
            Assert.That(core.CreateComponentInstance("other-seed", parent.Context!.ComponentId, "card", "item", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.root-mismatch"));
            Assert.That(core.CreateComponentInstance("seed", null, "card", "item", null, System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.null-parameters"));
            Assert.That(core.CreateComponentInstance("seed", null, "card", "item", new UiValueEntry[] { duplicate, duplicate }, System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.duplicate-parameter"));
            Assert.That(core.CreateComponentInstance("seed", null, "card", "item", System.Array.Empty<UiValueEntry>(), new UiSlot[] { repeated, repeated }, System.Array.Empty<UiValueEntry>()).Code, Is.EqualTo("component.duplicate-slot"));
            Assert.That(core.CreateComponentInstance("seed", null, "card", "item", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), new UiValueEntry[] { duplicate, duplicate }).Code, Is.EqualTo("component.duplicate-context"));
            Assert.That(core.ProvideComponentContextValue(parent.Context.ComponentId, " ", Text("value")).Code, Is.EqualTo("component.invalid-context-key"));
        });
    }

    private static ComponentCore RegisteredCore()
    {
        ComponentCore core = new ComponentCore();
        core.RegisterComponentDefinition(Definition("card", "render-card", Entry("color", Text("blue")), Entry("size", Text("medium"))));
        return core;
    }

    private static ComponentCore BoundaryCore()
    {
        ComponentCore core = new ComponentCore();
        UiComponentDefinition boundary = Definition("boundary", "render-boundary");
        boundary.ErrorHandlerId = "error-boundary";
        core.RegisterComponentDefinition(boundary);
        core.RegisterComponentDefinition(Definition("leaf", "render-leaf"));
        return core;
    }

    private static UiComponentDefinition Definition(string typeId, string renderHandlerId, params UiValueEntry[] defaults)
    {
        return new UiComponentDefinition { TypeId = typeId, RenderHandlerId = renderHandlerId, LifecycleHandlerId = "lifecycle-" + typeId, DefaultParameters = defaults };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiSubscription Subscription(string id, string stateId, string handlerId)
    {
        return new UiSubscription { Id = id, StateId = stateId, HandlerId = handlerId };
    }

    private static UiEffect Effect(string id, string dependencyId, string cleanupHandlerId)
    {
        return new UiEffect { Id = id, DependencyIds = new string[] { dependencyId }, HandlerId = id + "-handler", CleanupHandlerId = cleanupHandlerId };
    }

    private static UiDiagnostic Diagnostic(string code, string message, string? location)
    {
        return new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = code, Message = message, Location = location };
    }

    private static UiNode TextNode(string key, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = key + "-id" },
            Text = text
        };
    }
}
