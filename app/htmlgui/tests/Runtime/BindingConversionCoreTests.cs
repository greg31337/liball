using System;
using System.Collections.Generic;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class BindingConversionCoreTests
{
    [Test]
    public void InvokesConvertersInBothDirectionsWithCopiedContextsAndValues()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        IntegerTextConverter converter = new IntegerTextConverter();
        store.CreateStateEntry("page", "count", Integer(7), "setup");
        BindingOperationResult converterRegistration = core.RegisterBindingConverter("number-text", converter);
        UiBinding binding = Binding("count-binding", "count", string.Empty, "count-input", "text", UiBindingMode.TwoWay, UiBindingTrigger.Input);
        binding.ConverterId = "number-text";
        core.AttachBinding(store, "page", binding);

        BindingTargetResult target = core.EvaluateBindingTarget(store, "page", "count-binding");
        converter.TargetValues[0].IntegerValue = 99;
        BindingOperationResult source = core.UpdateTriggeredBindingSource(store, "page", "count-binding", Text("12"), "input", UiBindingTrigger.Input);
        converter.SourceValues[0].StringValue = "mutated";
        StateOperationResult stored = store.ReadStateEntry("page", "count");

        Assert.Multiple(() =>
        {
            Assert.That(converterRegistration.Success, Is.True);
            Assert.That(target.Success, Is.True);
            Assert.That(target.Text, Is.EqualTo("7"));
            Assert.That(target.Update!.Value.StringValue, Is.EqualTo("7"));
            Assert.That(target.Binding!.Conversion!.Success, Is.True);
            Assert.That(converter.TargetContexts.Single().BindingId, Is.EqualTo("count-binding"));
            Assert.That(converter.TargetContexts.Single().ScopeId, Is.EqualTo("page"));
            Assert.That(converter.TargetContexts.Single().ToSource, Is.False);
            Assert.That(source.Success, Is.True);
            Assert.That(source.Binding!.Conversion!.Value!.IntegerValue, Is.EqualTo(12));
            Assert.That(converter.SourceContexts.Single().ToSource, Is.True);
            Assert.That(converter.SourceContexts.Single().Origin, Is.EqualTo("input"));
            Assert.That(stored.Entry!.Value.IntegerValue, Is.EqualTo(12));
            Assert.That(stored.Entry.Origin, Is.EqualTo("binding:page:count-binding"));
        });
    }

    [Test]
    public void AppliesFallbacksForMissingNullIncompatibleAndFailedValues()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry(null, "record", ObjectValue(Entry("name", Text("Ada"))), "setup");
        store.CreateStateEntry(null, "empty", Null(), "setup");
        store.CreateStateEntry(null, "number", Integer(5), "setup");
        FailingConverter failingConverter = new FailingConverter();
        core.RegisterBindingConverter("failing", failingConverter);

        UiBinding missingBinding = Binding("missing", "record", "/missing", "missing-node", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        missingBinding.FallbackValue = Text("missing fallback");
        core.AttachBinding(store, null, missingBinding);
        UiBinding nullBinding = Binding("null", "empty", string.Empty, "null-node", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        nullBinding.FallbackValue = Text("null fallback");
        core.AttachBinding(store, null, nullBinding);
        UiBinding incompatibleBinding = Binding("incompatible", "number", string.Empty, "number-node", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        incompatibleBinding.FallbackValue = Text("type fallback");
        core.AttachBinding(store, null, incompatibleBinding);
        UiBinding failedBinding = Binding("failed", "record", "/name", "failed-node", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        failedBinding.ConverterId = "failing";
        failedBinding.FallbackValue = Text("converter fallback");
        core.AttachBinding(store, null, failedBinding);
        UiBinding noFallbackBinding = Binding("no-fallback", "record", "/name", "failed-node", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        noFallbackBinding.ConverterId = "failing";
        core.AttachBinding(store, null, noFallbackBinding);

        BindingTargetResult missing = core.EvaluateBindingTarget(store, null, "missing");
        BindingTargetResult nullValue = core.EvaluateBindingTarget(store, null, "null");
        BindingTargetResult incompatible = core.EvaluateBindingTarget(store, null, "incompatible");
        BindingTargetResult failed = core.EvaluateBindingTarget(store, null, "failed");
        failingConverter.Diagnostic.Message = "mutated";
        BindingTargetResult noFallback = core.EvaluateBindingTarget(store, null, "no-fallback");

        Assert.Multiple(() =>
        {
            Assert.That(missing.Success, Is.True);
            Assert.That(missing.Text, Is.EqualTo("missing fallback"));
            Assert.That(missing.Update!.Version, Is.EqualTo(1));
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("state.path-not-found"));
            Assert.That(nullValue.Success, Is.True);
            Assert.That(nullValue.Text, Is.EqualTo("null fallback"));
            Assert.That(nullValue.Diagnostics, Is.Empty);
            Assert.That(incompatible.Success, Is.True);
            Assert.That(incompatible.Text, Is.EqualTo("type fallback"));
            Assert.That(incompatible.Diagnostics.Single().Code, Is.EqualTo("binding.target-requires-string"));
            Assert.That(failed.Success, Is.True);
            Assert.That(failed.Text, Is.EqualTo("converter fallback"));
            Assert.That(failed.Diagnostics.Single().Code, Is.EqualTo("converter.rejected"));
            Assert.That(failed.Diagnostics.Single().Message, Is.EqualTo("Rejected value"));
            Assert.That(failed.Binding!.Conversion!.Success, Is.False);
            Assert.That(noFallback.Success, Is.False);
            Assert.That(noFallback.Code, Is.EqualTo("binding.conversion-failed"));
        });
    }

    [Test]
    public void GivesNullValuesDeterministicSemanticsForEveryTargetFamily()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry(null, "empty", Null(), "setup");
        string[] targets = new string[]
        {
            "text", "attribute:title", "property:value", "class:active", "style:color", "visibility",
            "enabled", "selection", "value", "items", "command"
        };
        int index = 0;
        while (index < targets.Length)
        {
            core.AttachBinding(store, null, Binding("null-" + index, "empty", string.Empty, "node-" + index, targets[index], UiBindingMode.OneWay, UiBindingTrigger.Change));
            index++;
        }

        BindingTargetResult text = core.EvaluateBindingTarget(store, null, "null-0");
        BindingTargetResult attribute = core.EvaluateBindingTarget(store, null, "null-1");
        BindingTargetResult property = core.EvaluateBindingTarget(store, null, "null-2");
        BindingTargetResult classTarget = core.EvaluateBindingTarget(store, null, "null-3");
        BindingTargetResult style = core.EvaluateBindingTarget(store, null, "null-4");
        BindingTargetResult visibility = core.EvaluateBindingTarget(store, null, "null-5");
        BindingTargetResult enabled = core.EvaluateBindingTarget(store, null, "null-6");
        BindingTargetResult selection = core.EvaluateBindingTarget(store, null, "null-7");
        BindingTargetResult value = core.EvaluateBindingTarget(store, null, "null-8");
        BindingTargetResult items = core.EvaluateBindingTarget(store, null, "null-9");
        BindingTargetResult command = core.EvaluateBindingTarget(store, null, "null-10");

        Assert.Multiple(() =>
        {
            Assert.That(text.Text, Is.EqualTo(string.Empty));
            Assert.That(attribute.RemoveTarget, Is.True);
            Assert.That(attribute.Attribute!.Name, Is.EqualTo("title"));
            Assert.That(property.Property!.Value.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(classTarget.BooleanValue, Is.False);
            Assert.That(style.RemoveTarget, Is.True);
            Assert.That(visibility.BooleanValue, Is.False);
            Assert.That(enabled.BooleanValue, Is.False);
            Assert.That(selection.Value!.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(value.Value!.Kind, Is.EqualTo(UiValueKind.Null));
            Assert.That(items.Items, Is.Empty);
            Assert.That(command.RemoveTarget, Is.True);
            Assert.That(command.CommandId, Is.Null);
        });
    }

    [Test]
    public void MatchesInputChangeBlurExplicitAndImmediateTriggersExactly()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        UiBindingTrigger[] triggers = new UiBindingTrigger[]
        {
            UiBindingTrigger.Input, UiBindingTrigger.Change, UiBindingTrigger.Blur, UiBindingTrigger.Explicit, UiBindingTrigger.Immediate
        };
        int index = 0;
        while (index < triggers.Length)
        {
            string stateId = "state-" + index;
            store.CreateStateEntry(null, stateId, Text("initial"), "setup");
            core.AttachBinding(store, null, Binding("binding-" + index, stateId, string.Empty, "node-" + index, "value", UiBindingMode.TwoWay, triggers[index]));
            index++;
        }

        BindingOperationResult mismatch = core.UpdateTriggeredBindingSource(store, null, "binding-0", Text("ignored"), "input", UiBindingTrigger.Change);
        BindingOperationResult input = core.UpdateTriggeredBindingSource(store, null, "binding-0", Text("input"), "input", UiBindingTrigger.Input);
        BindingOperationResult change = core.UpdateTriggeredBindingSource(store, null, "binding-1", Text("change"), "change", UiBindingTrigger.Change);
        BindingOperationResult blur = core.UpdateTriggeredBindingSource(store, null, "binding-2", Text("blur"), "blur", UiBindingTrigger.Blur);
        BindingOperationResult explicitUpdate = core.UpdateTriggeredBindingSource(store, null, "binding-3", Text("explicit"), "explicit", UiBindingTrigger.Explicit);
        BindingOperationResult immediate = core.UpdateTriggeredBindingSource(store, null, "binding-4", Text("immediate"), "immediate", UiBindingTrigger.Immediate);
        BindingOperationResult invalid = core.UpdateTriggeredBindingSource(store, null, "binding-4", Text("invalid"), "input", (UiBindingTrigger)99);

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Success, Is.True);
            Assert.That(mismatch.Applied, Is.False);
            Assert.That(mismatch.Code, Is.EqualTo("binding.trigger-mismatch"));
            Assert.That(input.Applied, Is.True);
            Assert.That(change.Applied, Is.True);
            Assert.That(blur.Applied, Is.True);
            Assert.That(explicitUpdate.Applied, Is.True);
            Assert.That(immediate.Applied, Is.True);
            Assert.That(store.ReadStateEntry(null, "state-0").Entry!.Value.StringValue, Is.EqualTo("input"));
            Assert.That(store.ReadStateEntry(null, "state-1").Entry!.Value.StringValue, Is.EqualTo("change"));
            Assert.That(store.ReadStateEntry(null, "state-2").Entry!.Value.StringValue, Is.EqualTo("blur"));
            Assert.That(store.ReadStateEntry(null, "state-3").Entry!.Value.StringValue, Is.EqualTo("explicit"));
            Assert.That(store.ReadStateEntry(null, "state-4").Entry!.Value.StringValue, Is.EqualTo("immediate"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics.Single().Code, Is.EqualTo("binding.invalid-trigger"));
        });
    }

    [Test]
    public void SuppressesTwoWayOriginEchoesUntilAnExternalStateChangeArrives()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry("page", "name", Text("Ada"), "setup");
        core.AttachBinding(store, "page", Binding("name-binding", "name", string.Empty, "name-input", "value", UiBindingMode.TwoWay, UiBindingTrigger.Input));

        BindingTargetResult initial = core.EvaluateBindingTarget(store, "page", "name-binding");
        BindingOperationResult echoed = core.UpdateTriggeredBindingSource(store, "page", "name-binding", Text("ignored"), initial.Update!.Origin, UiBindingTrigger.Input);
        BindingOperationResult input = core.UpdateTriggeredBindingSource(store, "page", "name-binding", Text("Lin"), "browser", UiBindingTrigger.Input);
        BindingTargetResult loop = core.EvaluateBindingTarget(store, "page", "name-binding");
        store.WriteStateEntry("page", "name", Text("Grace"), "server");
        BindingTargetResult external = core.EvaluateBindingTarget(store, "page", "name-binding");

        Assert.Multiple(() =>
        {
            Assert.That(initial.Update.Origin, Is.EqualTo("binding:page:name-binding"));
            Assert.That(echoed.Success, Is.True);
            Assert.That(echoed.Applied, Is.False);
            Assert.That(echoed.Code, Is.EqualTo("binding.origin-loop"));
            Assert.That(input.Applied, Is.True);
            Assert.That(loop.Success, Is.True);
            Assert.That(loop.Applied, Is.False);
            Assert.That(loop.Code, Is.EqualTo("binding.origin-loop"));
            Assert.That(external.Applied, Is.True);
            Assert.That(external.Value!.StringValue, Is.EqualTo("Grace"));
        });
    }

    [Test]
    public void ReportsConverterAndFallbackDefinitionDiagnostics()
    {
        StateStoreCore store = new StateStoreCore();
        BindingCore core = new BindingCore();
        store.CreateStateEntry(null, "source", Text("value"), "setup");
        UiBinding unknown = Binding("unknown", "source", string.Empty, "node", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        unknown.ConverterId = "missing";
        UiBinding invalidFallback = Binding("fallback", "source", string.Empty, "node", "visibility", UiBindingMode.OneWay, UiBindingTrigger.Change);
        invalidFallback.FallbackValue = Text("not boolean");
        NullResultConverter nullConverter = new NullResultConverter();
        core.RegisterBindingConverter("null-result", nullConverter);
        UiBinding nullResult = Binding("null-result", "source", string.Empty, "node", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        nullResult.ConverterId = "null-result";

        BindingOperationResult invalidId = core.RegisterBindingConverter(" ", nullConverter);
        BindingOperationResult nullHandler = core.RegisterBindingConverter("null", null);
        BindingOperationResult duplicate = core.RegisterBindingConverter("null-result", nullConverter);
        BindingOperationResult unknownResult = core.AttachBinding(store, null, unknown);
        BindingOperationResult fallbackResult = core.AttachBinding(store, null, invalidFallback);
        core.AttachBinding(store, null, nullResult);
        BindingTargetResult converted = core.EvaluateBindingTarget(store, null, "null-result");

        Assert.Multiple(() =>
        {
            Assert.That(invalidId.Diagnostics.Single().Code, Is.EqualTo("binding.invalid-converter"));
            Assert.That(nullHandler.Diagnostics.Single().Code, Is.EqualTo("binding.null-converter"));
            Assert.That(duplicate.Diagnostics.Single().Code, Is.EqualTo("binding.duplicate-converter"));
            Assert.That(unknownResult.Diagnostics.Single().Code, Is.EqualTo("binding.unknown-converter"));
            Assert.That(fallbackResult.Diagnostics.Single().Code, Is.EqualTo("binding.invalid-fallback"));
            Assert.That(converted.Success, Is.False);
            Assert.That(converted.Code, Is.EqualTo("binding.null-conversion-result"));
            Assert.That(converted.Diagnostics.Single().Location, Is.EqualTo("/binding"));
        });
    }

    private static UiBinding Binding(string id, string stateId, string path, string targetNodeId, string target, UiBindingMode mode, UiBindingTrigger trigger)
    {
        UiBinding result = new UiBinding();
        result.Id = id;
        result.StateId = stateId;
        result.Path = path;
        result.TargetNodeId = targetNodeId;
        result.Target = target;
        result.Mode = mode;
        result.Trigger = trigger;
        return result;
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static UiValue Null()
    {
        return new UiValue { Kind = UiValueKind.Null };
    }

    private static UiValue ObjectValue(params UiValueEntry[] entries)
    {
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private class IntegerTextConverter : IUiValueConverter
    {
        public List<UiValue> TargetValues = new List<UiValue>();
        public List<UiValue> SourceValues = new List<UiValue>();
        public List<UiBindingContext> TargetContexts = new List<UiBindingContext>();
        public List<UiBindingContext> SourceContexts = new List<UiBindingContext>();

        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            TargetValues.Add(value);
            TargetContexts.Add(context);
            return new UiConversionResult { Success = true, Value = Text(value.IntegerValue.ToString()) };
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            SourceValues.Add(value);
            SourceContexts.Add(context);
            return new UiConversionResult { Success = true, Value = Integer(long.Parse(value.StringValue!)) };
        }
    }

    private class FailingConverter : IUiValueConverter
    {
        public UiDiagnostic Diagnostic = new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Warning,
            Code = "converter.rejected",
            Message = "Rejected value",
            Location = "/converter"
        };

        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = false, Diagnostic = Diagnostic };
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = false, Diagnostic = Diagnostic };
        }
    }

    private class NullResultConverter : IUiValueConverter
    {
        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            return null!;
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            return null!;
        }
    }
}
