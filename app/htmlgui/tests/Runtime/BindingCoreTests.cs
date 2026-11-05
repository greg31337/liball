using System;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class BindingCoreTests
{
    [Test]
    public void AttachesScopedBindingsInStableOrderAndCopiesDefinitions()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry(null, "source", Text("global"), "setup");
        store.CreateStateEntry("page", "source", Text("page"), "setup");
        UiBinding zeta = Binding("zeta", "source", string.Empty, "zeta-node", "text", UiBindingMode.OneWay);

        BindingOperationResult attachedZeta = core.AttachBinding(store, null, zeta);
        BindingOperationResult attachedAlpha = core.AttachBinding(store, null, Binding("alpha", "source", string.Empty, "alpha-node", "value", UiBindingMode.TwoWay));
        BindingOperationResult attachedPage = core.AttachBinding(store, "page", Binding("alpha", "source", string.Empty, "page-node", "text", UiBindingMode.OneTime));
        zeta.StateId = "mutated";
        BindingOperationResult read = core.ReadBinding(store, null, "zeta");
        read.Binding!.TargetNodeId = "mutated";
        BindingOperationResult retained = core.ReadBinding(store, null, "zeta");

        Assert.Multiple(() =>
        {
            Assert.That(attachedZeta.Success, Is.True);
            Assert.That(attachedAlpha.Success, Is.True);
            Assert.That(attachedPage.Success, Is.True);
            Assert.That(core.Bindings.Select(item => item.ScopeId + ":" + item.Binding.Id), Is.EqualTo(new string[] { ":alpha", ":zeta", "page:alpha" }));
            Assert.That(retained.Binding!.StateId, Is.EqualTo("source"));
            Assert.That(retained.Binding.TargetNodeId, Is.EqualTo("zeta-node"));
            Assert.That(retained.Binding.Context, Is.Null);
            Assert.That(retained.Binding.Update, Is.Null);
            Assert.That(retained.Binding.Conversion, Is.Null);
        });
    }

    [Test]
    public void EnforcesAllFourBindingDirections()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry(null, "source", Text("one"), "setup");
        core.AttachBinding(store, null, Binding("once", "source", string.Empty, "once-node", "text", UiBindingMode.OneTime));
        core.AttachBinding(store, null, Binding("forward", "source", string.Empty, "forward-node", "text", UiBindingMode.OneWay));
        core.AttachBinding(store, null, Binding("both", "source", string.Empty, "both-node", "value", UiBindingMode.TwoWay));
        core.AttachBinding(store, null, Binding("backward", "source", string.Empty, "backward-node", "value", UiBindingMode.OneWayToSource));

        BindingTargetResult onceFirst = core.EvaluateBindingTarget(store, null, "once");
        BindingTargetResult forwardFirst = core.EvaluateBindingTarget(store, null, "forward");
        BindingTargetResult backwardRead = core.EvaluateBindingTarget(store, null, "backward");
        store.WriteStateEntry(null, "source", Text("two"), "state");
        BindingTargetResult onceSecond = core.EvaluateBindingTarget(store, null, "once");
        BindingTargetResult forwardSecond = core.EvaluateBindingTarget(store, null, "forward");
        BindingOperationResult readOnlyWrite = core.UpdateBindingSource(store, null, "forward", Text("ignored"), "target");
        BindingOperationResult twoWayWrite = core.UpdateBindingSource(store, null, "both", Text("three"), "target");
        BindingOperationResult sourceOnlyWrite = core.UpdateBindingSource(store, null, "backward", Text("four"), "target");
        StateOperationResult current = store.ReadStateEntry(null, "source");

        Assert.Multiple(() =>
        {
            Assert.That(onceFirst.Applied, Is.True);
            Assert.That(onceFirst.Text, Is.EqualTo("one"));
            Assert.That(onceSecond.Success, Is.True);
            Assert.That(onceSecond.Applied, Is.False);
            Assert.That(forwardFirst.Text, Is.EqualTo("one"));
            Assert.That(forwardSecond.Text, Is.EqualTo("two"));
            Assert.That(backwardRead.Success, Is.True);
            Assert.That(backwardRead.Applied, Is.False);
            Assert.That(backwardRead.Code, Is.EqualTo("binding.source-only"));
            Assert.That(readOnlyWrite.Success, Is.True);
            Assert.That(readOnlyWrite.Applied, Is.False);
            Assert.That(readOnlyWrite.Code, Is.EqualTo("binding.target-read-only"));
            Assert.That(twoWayWrite.Success, Is.True);
            Assert.That(twoWayWrite.Entry!.Value.StringValue, Is.EqualTo("three"));
            Assert.That(twoWayWrite.Binding!.Context!.ToSource, Is.True);
            Assert.That(sourceOnlyWrite.Entry!.Version, Is.EqualTo(4));
            Assert.That(current.Entry!.Value.StringValue, Is.EqualTo("four"));
        });
    }

    [Test]
    public void ProjectsEverySupportedTargetKind()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry(null, "text", Text("hello"), "setup");
        store.CreateStateEntry(null, "flag", Boolean(true), "setup");
        store.CreateStateEntry(null, "number", Integer(7), "setup");
        store.CreateStateEntry(null, "items", ArrayValue(Text("first"), Text("second")), "setup");
        string[] targets = new string[]
        {
            "text", "attribute:title", "attribute:hidden", "property:tabIndex", "class:active", "style:color",
            "visibility", "enabled", "selection", "value", "items", "command"
        };
        string[] states = new string[]
        {
            "text", "text", "flag", "number", "flag", "text", "flag", "flag", "text", "number", "items", "text"
        };
        int index = 0;
        while (index < targets.Length)
        {
            core.AttachBinding(store, null, Binding("binding-" + index, states[index], string.Empty, "node-" + index, targets[index], UiBindingMode.OneWay));
            index++;
        }

        BindingTargetResult text = core.EvaluateBindingTarget(store, null, "binding-0");
        BindingTargetResult attribute = core.EvaluateBindingTarget(store, null, "binding-1");
        BindingTargetResult booleanAttribute = core.EvaluateBindingTarget(store, null, "binding-2");
        BindingTargetResult property = core.EvaluateBindingTarget(store, null, "binding-3");
        BindingTargetResult classTarget = core.EvaluateBindingTarget(store, null, "binding-4");
        BindingTargetResult style = core.EvaluateBindingTarget(store, null, "binding-5");
        BindingTargetResult visibility = core.EvaluateBindingTarget(store, null, "binding-6");
        BindingTargetResult enabled = core.EvaluateBindingTarget(store, null, "binding-7");
        BindingTargetResult selection = core.EvaluateBindingTarget(store, null, "binding-8");
        BindingTargetResult value = core.EvaluateBindingTarget(store, null, "binding-9");
        BindingTargetResult items = core.EvaluateBindingTarget(store, null, "binding-10");
        BindingTargetResult command = core.EvaluateBindingTarget(store, null, "binding-11");

        Assert.Multiple(() =>
        {
            Assert.That(text.Text, Is.EqualTo("hello"));
            Assert.That(text.Update!.TargetNodeId, Is.EqualTo("node-0"));
            Assert.That(attribute.Attribute!.Name, Is.EqualTo("title"));
            Assert.That(attribute.Attribute.Value, Is.EqualTo("hello"));
            Assert.That(booleanAttribute.Attribute!.IsBoolean, Is.True);
            Assert.That(booleanAttribute.RemoveTarget, Is.False);
            Assert.That(property.Property!.Name, Is.EqualTo("tabIndex"));
            Assert.That(property.Property.Value.IntegerValue, Is.EqualTo(7));
            Assert.That(classTarget.Name, Is.EqualTo("active"));
            Assert.That(classTarget.BooleanValue, Is.True);
            Assert.That(style.Name, Is.EqualTo("color"));
            Assert.That(style.Text, Is.EqualTo("hello"));
            Assert.That(visibility.BooleanValue, Is.True);
            Assert.That(enabled.BooleanValue, Is.True);
            Assert.That(selection.Value!.StringValue, Is.EqualTo("hello"));
            Assert.That(value.Value!.IntegerValue, Is.EqualTo(7));
            Assert.That(items.Items.Select(item => item.StringValue), Is.EqualTo(new string?[] { "first", "second" }));
            Assert.That(command.CommandId, Is.EqualTo("hello"));
            Assert.That(command.Binding!.Context!.ToSource, Is.False);
        });
    }

    [Test]
    public void ReadsAndWritesNestedPathsAndSupportsReadOnlyDerivedSources()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry("page", "profile", ObjectValue(Entry("name", Text("Ada"))), "setup");
        core.AttachBinding(store, "page", Binding("name", "profile", "/name", "input", "value", UiBindingMode.TwoWay));
        core.AttachBinding(store, "page", Binding("missing", "profile", "/missing", "missing", "text", UiBindingMode.OneWay));
        UiDerivedState derived = new UiDerivedState
        {
            Id = "display",
            DependencyIds = new string[] { "profile" },
            HandlerId = "display-handler",
            Value = Text("Derived"),
            IsDirty = false
        };
        store.RegisterDerivedState("page", derived);
        BindingOperationResult derivedAttached = core.AttachBinding(store, "page", Binding("derived", "display", string.Empty, "display", "text", UiBindingMode.OneWay));
        BindingOperationResult derivedWrite = core.AttachBinding(store, "page", Binding("derived-write", "display", string.Empty, "display-input", "value", UiBindingMode.TwoWay));

        BindingTargetResult initial = core.EvaluateBindingTarget(store, "page", "name");
        BindingOperationResult updated = core.UpdateBindingSource(store, "page", "name", Text("Lin"), "input");
        BindingTargetResult missing = core.EvaluateBindingTarget(store, "page", "missing");
        BindingTargetResult derivedValue = core.EvaluateBindingTarget(store, "page", "derived");
        StateOperationResult profile = store.ReadStateEntry("page", "profile");

        Assert.Multiple(() =>
        {
            Assert.That(initial.Value!.StringValue, Is.EqualTo("Ada"));
            Assert.That(initial.Update!.Version, Is.EqualTo(1));
            Assert.That(updated.Success, Is.True);
            Assert.That(profile.Entry!.Value.ObjectValue!.Single().Value.StringValue, Is.EqualTo("Lin"));
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Code, Is.EqualTo("state.path-not-found"));
            Assert.That(derivedAttached.Success, Is.True);
            Assert.That(derivedValue.Text, Is.EqualTo("Derived"));
            Assert.That(derivedValue.Update!.Origin, Is.EqualTo("binding:page:derived"));
            Assert.That(derivedWrite.Success, Is.False);
            Assert.That(derivedWrite.Code, Is.EqualTo("binding.derived-source-read-only"));
        });
    }

    [Test]
    public void RejectsInvalidDefinitionsDuplicatesAndTargetValueKinds()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry(null, "text", Text("value"), "setup");
        store.CreateStateEntry(null, "number", Integer(1), "setup");
        store.CreateStateEntry(null, "flag", Boolean(true), "setup");
        store.CreateStateEntry(null, "object", ObjectValue(Entry("key", Text("value"))), "setup");
        UiBinding valid = Binding("valid", "text", string.Empty, "node", "text", UiBindingMode.OneWay);
        BindingOperationResult attached = core.AttachBinding(store, null, valid);

        UiBinding badMode = Binding("bad-mode", "text", string.Empty, "node", "text", UiBindingMode.OneWay);
        badMode.Mode = (UiBindingMode)99;
        UiBinding badTrigger = Binding("bad-trigger", "text", string.Empty, "node", "text", UiBindingMode.OneWay);
        badTrigger.Trigger = (UiBindingTrigger)99;
        UiBinding badConverter = Binding("bad-converter", "text", string.Empty, "node", "text", UiBindingMode.OneWay);
        badConverter.ConverterId = " ";

        core.AttachBinding(store, null, Binding("wrong-text", "number", string.Empty, "node", "text", UiBindingMode.OneWay));
        core.AttachBinding(store, null, Binding("wrong-boolean", "text", string.Empty, "node", "visibility", UiBindingMode.OneWay));
        core.AttachBinding(store, null, Binding("wrong-items", "object", string.Empty, "node", "items", UiBindingMode.OneWay));
        core.AttachBinding(store, null, Binding("wrong-attribute", "number", string.Empty, "node", "attribute:title", UiBindingMode.OneWay));
        core.AttachBinding(store, null, Binding("wrong-style", "flag", string.Empty, "node", "style:color", UiBindingMode.OneWay));

        Assert.Multiple(() =>
        {
            Assert.That(attached.Success, Is.True);
            Assert.That(core.AttachBinding(store, null, valid).Code, Is.EqualTo("binding.duplicate"));
            Assert.That(core.AttachBinding(store, null, null).Code, Is.EqualTo("binding.null"));
            Assert.That(core.AttachBinding(store, null, Binding(" ", "text", string.Empty, "node", "text", UiBindingMode.OneWay)).Code, Is.EqualTo("binding.invalid-id"));
            Assert.That(core.AttachBinding(store, null, Binding("unknown", "missing", string.Empty, "node", "text", UiBindingMode.OneWay)).Code, Is.EqualTo("binding.unknown-state"));
            Assert.That(core.AttachBinding(store, null, Binding("bad-path", "text", "/bad~2path", "node", "text", UiBindingMode.OneWay)).Code, Is.EqualTo("state.invalid-path-escape"));
            Assert.That(core.AttachBinding(store, null, Binding("bad-target", "text", string.Empty, "node", "attribute", UiBindingMode.OneWay)).Code, Is.EqualTo("binding.invalid-target"));
            Assert.That(core.AttachBinding(store, null, Binding("bad-target-kind", "text", string.Empty, "node", "unknown:name", UiBindingMode.OneWay)).Code, Is.EqualTo("binding.invalid-target"));
            Assert.That(core.AttachBinding(store, null, badMode).Code, Is.EqualTo("binding.invalid-mode"));
            Assert.That(core.AttachBinding(store, null, badTrigger).Code, Is.EqualTo("binding.invalid-trigger"));
            Assert.That(core.AttachBinding(store, null, badConverter).Code, Is.EqualTo("binding.invalid-converter"));
            Assert.That(core.EvaluateBindingTarget(store, null, "wrong-text").Code, Is.EqualTo("binding.target-requires-string"));
            Assert.That(core.EvaluateBindingTarget(store, null, "wrong-boolean").Code, Is.EqualTo("binding.target-requires-boolean"));
            Assert.That(core.EvaluateBindingTarget(store, null, "wrong-items").Code, Is.EqualTo("binding.target-requires-array"));
            Assert.That(core.EvaluateBindingTarget(store, null, "wrong-attribute").Code, Is.EqualTo("binding.target-requires-text-or-boolean"));
            Assert.That(core.EvaluateBindingTarget(store, null, "wrong-style").Code, Is.EqualTo("binding.target-requires-string"));
            Assert.That(core.ReadBinding(store, null, "missing").Code, Is.EqualTo("binding.unknown"));
            Assert.That(core.UpdateBindingSource(store, null, "missing", Text("value"), "input").Code, Is.EqualTo("binding.unknown"));
        });
    }

    private static UiBinding Binding(string id, string stateId, string path, string targetNodeId, string target, UiBindingMode mode)
    {
        UiBinding result = new UiBinding();
        result.Id = id;
        result.StateId = stateId;
        result.Path = path;
        result.TargetNodeId = targetNodeId;
        result.Target = target;
        result.Mode = mode;
        result.Trigger = UiBindingTrigger.Change;
        return result;
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Boolean(bool value)
    {
        return new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
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
}
