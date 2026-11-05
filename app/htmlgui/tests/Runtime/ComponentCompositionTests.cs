using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class ComponentCompositionTests
{
    [Test]
    public void DefinitionRegistrationValidatesOptionalHandlersAndAllowsReuseAfterDisposal()
    {
        ComponentCore core = new ComponentCore();
        UiComponentDefinition invalidLifecycle = Definition("bad-lifecycle");
        invalidLifecycle.LifecycleHandlerId = " ";
        UiComponentDefinition invalidError = Definition("bad-error");
        invalidError.ErrorHandlerId = " ";
        UiComponentDefinition valid = Definition("card");
        valid.DefaultParameters = new UiValueEntry[] { Entry("tone", Text("quiet")) };

        ComponentOperationResult registered = core.RegisterComponentDefinition(valid);
        valid.DefaultParameters[0].Value.StringValue = "mutated";
        ComponentOperationResult mounted = Create(core, null, "card", "root");
        ComponentOperationResult inUse = core.UnregisterComponentDefinition("card");
        core.DisposeComponentTree(mounted.Context!.ComponentId);
        ComponentOperationResult removed = core.UnregisterComponentDefinition("card");
        ComponentOperationResult missing = core.ReadComponentDefinition("card");
        ComponentOperationResult reused = core.RegisterComponentDefinition(Definition("card"));

        Assert.Multiple(() =>
        {
            Assert.That(core.RegisterComponentDefinition(invalidLifecycle).Code, Is.EqualTo("component.invalid-lifecycle-handler"));
            Assert.That(core.RegisterComponentDefinition(invalidError).Code, Is.EqualTo("component.invalid-error-handler"));
            Assert.That(registered.Definition!.DefaultParameters.Single().Value.StringValue, Is.EqualTo("quiet"));
            Assert.That(inUse.Code, Is.EqualTo("component.definition-in-use"));
            Assert.That(removed.Success, Is.True);
            Assert.That(removed.Definition!.TypeId, Is.EqualTo("card"));
            Assert.That(missing.Code, Is.EqualTo("component.unknown-definition"));
            Assert.That(reused.Success, Is.True);
        });
    }

    [Test]
    public void ReconciliationReplacesSuppliedParametersSlotsAndContextWithoutLeakingInputs()
    {
        ComponentCore core = new ComponentCore();
        UiComponentDefinition definition = Definition("card");
        definition.DefaultParameters = new UiValueEntry[] { Entry("color", Text("blue")), Entry("size", Text("medium")) };
        core.RegisterComponentDefinition(definition);
        UiNode originalContent = Node("original", "Original");
        ComponentOperationResult mounted = core.ReconcileComponentInstance("seed", null, "card", "root", new UiValueEntry[] { Entry("size", Text("large")) }, new UiSlot[]
        {
            new UiSlot { Name = "body", Content = new UiNode[] { originalContent } }
        }, new UiValueEntry[] { Entry("theme", Text("light")) }, true, false);
        UiNode replacementFallback = Node("fallback", "Replacement");
        UiValueEntry[] replacementContext = new UiValueEntry[] { Entry("locale", Text("en")) };

        ComponentOperationResult reconciled = core.ReconcileComponentInstance("seed", null, "card", "root", new UiValueEntry[] { Entry("title", Text("Profile")) }, new UiSlot[]
        {
            new UiSlot { Name = "footer", FallbackContent = new UiNode[] { replacementFallback } }
        }, replacementContext, true, false);
        replacementFallback.Text = "mutated fallback";
        replacementContext[0].Value.StringValue = "mutated context";
        ComponentOperationResult retained = core.ReadComponentInstance(mounted.Context!.ComponentId);

        Assert.Multiple(() =>
        {
            Assert.That(reconciled.Context!.ComponentId, Is.EqualTo(mounted.Context.ComponentId));
            Assert.That(reconciled.Context.Parameters.Select(item => item.Key), Is.EqualTo(new string[] { "color", "size", "title" }));
            Assert.That(reconciled.Context.Parameters.Single(item => item.Key == "size").Value.StringValue, Is.EqualTo("medium"));
            Assert.That(reconciled.Context.Parameters.Single(item => item.Key == "title").Value.StringValue, Is.EqualTo("Profile"));
            Assert.That(retained.Context!.Slots.Single().Name, Is.EqualTo("footer"));
            Assert.That(retained.Context.Slots.Single().FallbackContent.Single().Text, Is.EqualTo("Replacement"));
            Assert.That(retained.Context.ContextValues.Single().Key, Is.EqualTo("locale"));
            Assert.That(retained.Context.ContextValues.Single().Value.StringValue, Is.EqualTo("en"));
            Assert.That(originalContent.Text, Is.EqualTo("Original"));
        });
    }

    [Test]
    public void ConditionalNestedSubtreesRetainNearestContextAndIsolatedLocalState()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = core.ReconcileComponentInstance("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), new UiValueEntry[] { Entry("locale", Text("en")), Entry("theme", Text("light")) }, true, false);
        ComponentOperationResult child = core.ReconcileComponentInstance("seed", root.Context!.ComponentId, "card", "child", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), new UiValueEntry[] { Entry("locale", Text("fr")) }, true, false);
        ComponentOperationResult grandchild = core.ReconcileComponentInstance("seed", child.Context!.ComponentId, "card", "grandchild", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>(), true, false);
        core.CreateComponentLocalState(child.Context.ComponentId, "count", Integer(2));
        core.CreateComponentLocalState(grandchild.Context!.ComponentId, "count", Integer(3));
        core.DrainComponentLifecycleInvocations();

        ComponentOperationResult hidden = core.SetComponentPresence(child.Context.ComponentId, false);
        ComponentOperationResult hiddenLocale = core.ConsumeComponentContextValue(grandchild.Context.ComponentId, "locale");
        ComponentOperationResult hiddenTheme = core.ConsumeComponentContextValue(grandchild.Context.ComponentId, "theme");
        StateOperationResult hiddenChildState = core.ReadComponentLocalState(child.Context.ComponentId, "count");
        StateOperationResult hiddenGrandchildState = core.ReadComponentLocalState(grandchild.Context.ComponentId, "count");
        ComponentOperationResult restored = core.SetComponentPresence(child.Context.ComponentId, true);
        core.WriteComponentLocalState(grandchild.Context.ComponentId, "count", Integer(4));

        Assert.Multiple(() =>
        {
            Assert.That(hidden.Applied, Is.True);
            Assert.That(hiddenLocale.ProviderComponentId, Is.EqualTo(child.Context.ComponentId));
            Assert.That(hiddenLocale.Value!.StringValue, Is.EqualTo("fr"));
            Assert.That(hiddenTheme.ProviderComponentId, Is.EqualTo(root.Context.ComponentId));
            Assert.That(hiddenTheme.Value!.StringValue, Is.EqualTo("light"));
            Assert.That(hiddenChildState.Entry!.Value.IntegerValue, Is.EqualTo(2));
            Assert.That(hiddenGrandchildState.Entry!.Value.IntegerValue, Is.EqualTo(3));
            Assert.That(restored.Applied, Is.True);
            Assert.That(core.ReadComponentLocalState(grandchild.Context.ComponentId, "count").Entry!.Value.IntegerValue, Is.EqualTo(4));
            Assert.That(core.ReadComponentLocalState(child.Context.ComponentId, "count").Entry!.Value.IntegerValue, Is.EqualTo(2));
        });
    }

    [Test]
    public void KeyedSiblingIdentityIgnoresConstructionOrderAndSelectiveRemountResetsOnlyItsSubtree()
    {
        ComponentCore first = RegisteredCore();
        ComponentOperationResult firstRoot = Create(first, null, "card", "root");
        ComponentOperationResult firstAlpha = Create(first, firstRoot.Context!.ComponentId, "card", "alpha");
        ComponentOperationResult firstBeta = Create(first, firstRoot.Context.ComponentId, "card", "beta");
        ComponentOperationResult firstNested = Create(first, firstAlpha.Context!.ComponentId, "card", "nested");
        first.CreateComponentLocalState(firstAlpha.Context.ComponentId, "value", Text("alpha state"));
        first.CreateComponentLocalState(firstBeta.Context!.ComponentId, "value", Text("beta state"));
        first.CreateComponentLocalState(firstNested.Context!.ComponentId, "value", Text("nested state"));

        ComponentCore second = RegisteredCore();
        ComponentOperationResult secondRoot = Create(second, null, "card", "root");
        ComponentOperationResult secondBeta = Create(second, secondRoot.Context!.ComponentId, "card", "beta");
        ComponentOperationResult secondAlpha = Create(second, secondRoot.Context.ComponentId, "card", "alpha");
        ComponentOperationResult secondNested = Create(second, secondAlpha.Context!.ComponentId, "card", "nested");

        ComponentOperationResult remountedAlpha = first.ReconcileComponentInstance("seed", firstRoot.Context.ComponentId, "card", "alpha", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>(), true, true);
        StateOperationResult resetAlpha = first.ReadComponentLocalState(remountedAlpha.Context!.ComponentId, "value");
        StateOperationResult retainedBeta = first.ReadComponentLocalState(firstBeta.Context.ComponentId, "value");
        StateOperationResult disposedNested = first.ReadComponentLocalState(firstNested.Context.ComponentId, "value");
        ComponentOperationResult remountedNested = Create(first, remountedAlpha.Context.ComponentId, "card", "nested");

        Assert.Multiple(() =>
        {
            Assert.That(firstRoot.Context.ComponentId, Is.EqualTo(secondRoot.Context.ComponentId));
            Assert.That(firstAlpha.Context.ComponentId, Is.EqualTo(secondAlpha.Context.ComponentId));
            Assert.That(firstBeta.Context.ComponentId, Is.EqualTo(secondBeta.Context!.ComponentId));
            Assert.That(firstNested.Context.ComponentId, Is.EqualTo(secondNested.Context!.ComponentId));
            Assert.That(remountedAlpha.Context.ComponentId, Is.EqualTo(firstAlpha.Context.ComponentId));
            Assert.That(resetAlpha.Code, Is.EqualTo("state.unknown"));
            Assert.That(retainedBeta.Entry!.Value.StringValue, Is.EqualTo("beta state"));
            Assert.That(disposedNested.Code, Is.EqualTo("component.disposed"));
            Assert.That(remountedNested.Context!.ComponentId, Is.EqualTo(firstNested.Context.ComponentId));
        });
    }

    private static ComponentCore RegisteredCore()
    {
        ComponentCore core = new ComponentCore();
        core.RegisterComponentDefinition(Definition("card"));
        return core;
    }

    private static UiComponentDefinition Definition(string typeId)
    {
        return new UiComponentDefinition { TypeId = typeId, RenderHandlerId = "render-" + typeId, LifecycleHandlerId = "lifecycle-" + typeId };
    }

    private static ComponentOperationResult Create(ComponentCore core, string? parentId, string typeId, string key)
    {
        return core.CreateComponentInstance("seed", parentId, typeId, key, System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
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

    private static UiNode Node(string key, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = key + "-id" },
            Text = text
        };
    }
}
