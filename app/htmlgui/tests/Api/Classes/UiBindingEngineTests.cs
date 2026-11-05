using System.Collections.Generic;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiBindingEngineTests
{
    [Test]
    public void OwnsStateAndExposesAttachEvaluateConverterAndDetachLifecycle()
    {
        IUiBindingEngine engine = new UiBindingEngine();
        IUiStateStore store = engine.GetBindingStateStore();
        IntegerTextConverter converter = new IntegerTextConverter();
        store.CreateState("page", "count", Integer(7), "setup");
        UiResult converterResult = engine.RegisterBindingConverter("number-text", converter);
        UiBinding binding = Binding("count", "count", "count-input", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        binding.ConverterId = "number-text";

        UiBindingResult attached = engine.AttachBinding("page", binding);
        binding.StateId = "mutated";
        attached.Binding!.TargetNodeId = "mutated";
        UiBindingResult evaluated = engine.EvaluateBinding("page", "count");
        UiBindingResult detached = engine.DetachBinding("page", "count");
        UiBindingResult missing = engine.EvaluateBinding("page", "count");

        Assert.Multiple(() =>
        {
            Assert.That(converterResult.Success, Is.True);
            Assert.That(attached.Success, Is.True);
            Assert.That(attached.Applied, Is.True);
            Assert.That(attached.Updates.Single().Value.StringValue, Is.EqualTo("7"));
            Assert.That(evaluated.Binding!.StateId, Is.EqualTo("count"));
            Assert.That(evaluated.Binding.TargetNodeId, Is.EqualTo("count-input"));
            Assert.That(evaluated.Updates.Single().Origin, Is.EqualTo("binding:page:count"));
            Assert.That(detached.Success, Is.True);
            Assert.That(detached.Applied, Is.True);
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("binding.unknown"));
        });
    }

    [Test]
    public void PushesStateThenFansOutTargetUpdatesInStableBindingOrder()
    {
        IUiBindingEngine engine = new UiBindingEngine();
        IUiStateStore store = engine.GetBindingStateStore();
        store.CreateState(null, "name", Text("Ada"), "setup");
        RecordingObserver observer = new RecordingObserver();
        store.RegisterStateSubscription(null, Subscription("state-observer", "name"), observer);
        engine.AttachBinding(null, Binding("zeta", "name", "once", "text", UiBindingMode.OneTime, UiBindingTrigger.Input));
        engine.AttachBinding(null, Binding("beta", "name", "label", "text", UiBindingMode.OneWay, UiBindingTrigger.Input));
        engine.AttachBinding(null, Binding("alpha", "name", "input", "value", UiBindingMode.TwoWay, UiBindingTrigger.Input));

        UiBindingResult mismatch = engine.PushBindingValue(null, "alpha", Text("ignored"), "browser", UiBindingTrigger.Change);
        UiBindingResult pushed = engine.PushBindingValue(null, "alpha", Text("Lin"), "browser", UiBindingTrigger.Input);
        store.WriteState(null, "name", Text("Grace"), "server");
        UiBindingResult external = engine.EvaluateStateBindings(null);

        Assert.Multiple(() =>
        {
            Assert.That(mismatch.Success, Is.True);
            Assert.That(mismatch.Applied, Is.False);
            Assert.That(observer.Contexts, Has.Count.EqualTo(2));
            Assert.That(observer.Contexts[0].CurrentValue!.StringValue, Is.EqualTo("Lin"));
            Assert.That(pushed.Success, Is.True);
            Assert.That(pushed.BindingIds, Is.EqualTo(new string[] { "alpha", "beta" }));
            Assert.That(pushed.Updates.Select(item => item.BindingId), Is.EqualTo(new string[] { "beta" }));
            Assert.That(pushed.Updates.Single().Value.StringValue, Is.EqualTo("Lin"));
            Assert.That(store.ReadState(null, "name").Entry!.Origin, Is.EqualTo("server"));
            Assert.That(external.Updates.Select(item => item.BindingId), Is.EqualTo(new string[] { "alpha", "beta" }));
            Assert.That(external.Updates, Has.All.Matches<UiBindingUpdate>(item => item.Value.StringValue == "Grace"));
        });
    }

    [Test]
    public void CommitsMatchingDomBindingsAtomicallyBeforeObserversAndTargetFanOut()
    {
        IUiBindingEngine engine = new UiBindingEngine();
        IUiStateStore store = engine.GetBindingStateStore();
        List<string> order = new List<string>();
        store.CreateState("page", "first", Text("old-first"), "setup");
        store.CreateState("page", "second", Text("old-second"), "setup");
        store.RegisterStateSubscription("page", Subscription("first-observer", "first"), new OrderedObserver("observer:first", order));
        store.RegisterStateSubscription("page", Subscription("second-observer", "second"), new OrderedObserver("observer:second", order));
        RecordingTextConverter converter = new RecordingTextConverter(order);
        engine.RegisterBindingConverter("record", converter);
        UiBinding firstView = Binding("beta-view", "first", "first-view", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        firstView.ConverterId = "record";
        UiBinding secondView = Binding("omega-view", "second", "second-view", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        secondView.ConverterId = "record";
        engine.AttachBinding("page", Binding("alpha-input", "first", "shared-input", "value", UiBindingMode.TwoWay, UiBindingTrigger.Change));
        engine.AttachBinding("page", firstView);
        engine.AttachBinding("page", secondView);
        engine.AttachBinding("page", Binding("zeta-input", "second", "shared-input", "value", UiBindingMode.OneWayToSource, UiBindingTrigger.Change));
        order.Clear();

        UiBindingResult result = engine.ProcessBindingEvent("page", Event(UiEventKind.Change, "shared-input", Text("new"), false), "browser");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Applied, Is.True);
            Assert.That(result.BindingIds, Is.EqualTo(new string[] { "alpha-input", "zeta-input", "beta-view", "omega-view" }));
            Assert.That(result.Updates.Select(item => item.BindingId), Is.EqualTo(new string[] { "beta-view", "omega-view" }));
            Assert.That(store.ReadState("page", "first").Entry!.Value.StringValue, Is.EqualTo("new"));
            Assert.That(store.ReadState("page", "second").Entry!.Value.StringValue, Is.EqualTo("new"));
            Assert.That(order, Is.EqualTo(new string[]
            {
                "observer:first", "observer:second", "target:new", "target:new"
            }));
        });
    }

    [Test]
    public void MapsDomInputChangeBlurAndFocusOutTriggersAndSkipsComposition()
    {
        IUiBindingEngine engine = new UiBindingEngine();
        IUiStateStore store = engine.GetBindingStateStore();
        UiBindingTrigger[] triggers = new UiBindingTrigger[]
        {
            UiBindingTrigger.Input, UiBindingTrigger.Change, UiBindingTrigger.Blur, UiBindingTrigger.Blur
        };
        UiEventKind[] kinds = new UiEventKind[]
        {
            UiEventKind.Input, UiEventKind.Change, UiEventKind.Blur, UiEventKind.FocusOut
        };
        int index = 0;
        while (index < triggers.Length)
        {
            string id = "item-" + index;
            store.CreateState(null, id, Text("old"), "setup");
            engine.AttachBinding(null, Binding(id, id, "node-" + index, "value", UiBindingMode.TwoWay, triggers[index]));
            index++;
        }

        UiBindingResult composing = engine.ProcessBindingEvent(null, Event(UiEventKind.Input, "node-0", Text("ignored"), true), "browser");
        UiBindingResult input = engine.ProcessBindingEvent(null, Event(kinds[0], "node-0", Text("input"), false), "browser");
        UiBindingResult change = engine.ProcessBindingEvent(null, Event(kinds[1], "node-1", Text("change"), false), "browser");
        UiBindingResult blur = engine.ProcessBindingEvent(null, Event(kinds[2], "node-2", Text("blur"), false), "browser");
        UiBindingResult focusOut = engine.ProcessBindingEvent(null, Event(kinds[3], "node-3", Text("focusout"), false), "browser");
        UiBindingResult unsupported = engine.ProcessBindingEvent(null, Event(UiEventKind.Click, "node-0", Text("click"), false), "browser");
        UiBindingResult missingValue = engine.ProcessBindingEvent(null, new UiEventData { Kind = UiEventKind.Input, TargetId = "node-0" }, "browser");
        UiBindingResult unmatched = engine.ProcessBindingEvent(null, Event(UiEventKind.Input, "missing", Text("none"), false), "browser");

        Assert.Multiple(() =>
        {
            Assert.That(composing.Success, Is.True);
            Assert.That(composing.Applied, Is.False);
            Assert.That(input.Applied, Is.True);
            Assert.That(change.Applied, Is.True);
            Assert.That(blur.Applied, Is.True);
            Assert.That(focusOut.Applied, Is.True);
            Assert.That(store.ReadState(null, "item-0").Entry!.Value.StringValue, Is.EqualTo("input"));
            Assert.That(store.ReadState(null, "item-1").Entry!.Value.StringValue, Is.EqualTo("change"));
            Assert.That(store.ReadState(null, "item-2").Entry!.Value.StringValue, Is.EqualTo("blur"));
            Assert.That(store.ReadState(null, "item-3").Entry!.Value.StringValue, Is.EqualTo("focusout"));
            Assert.That(unsupported.Diagnostics.Single().Code, Is.EqualTo("binding.unsupported-event"));
            Assert.That(missingValue.Diagnostics.Single().Code, Is.EqualTo("binding.missing-event-value"));
            Assert.That(unmatched.Success, Is.True);
            Assert.That(unmatched.Applied, Is.False);
        });
    }

    [Test]
    public void RollsBackTheWholeDomEventWhenOneBindingFails()
    {
        IUiBindingEngine engine = new UiBindingEngine();
        IUiStateStore store = engine.GetBindingStateStore();
        store.CreateState(null, "first", Text("old-first"), "setup");
        store.CreateState(null, "second", Text("old-second"), "setup");
        engine.RegisterBindingConverter("reject", new RejectingConverter());
        engine.AttachBinding(null, Binding("alpha", "first", "shared", "value", UiBindingMode.TwoWay, UiBindingTrigger.Input));
        UiBinding rejected = Binding("zeta", "second", "shared", "value", UiBindingMode.TwoWay, UiBindingTrigger.Input);
        rejected.ConverterId = "reject";
        engine.AttachBinding(null, rejected);

        UiBindingResult result = engine.ProcessBindingEvent(null, Event(UiEventKind.Input, "shared", Text("new"), false), "browser");
        store.BeginStateBatch("external");
        UiBindingResult activeBatch = engine.ProcessBindingEvent(null, Event(UiEventKind.Input, "shared", Text("new"), false), "browser");
        store.RollbackStateBatch();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("converter.rejected"));
            Assert.That(store.ReadState(null, "first").Entry!.Value.StringValue, Is.EqualTo("old-first"));
            Assert.That(store.ReadState(null, "second").Entry!.Value.StringValue, Is.EqualTo("old-second"));
            Assert.That(activeBatch.Diagnostics.Single().Code, Is.EqualTo("binding.state-batch-active"));
        });
    }

    private static UiBinding Binding(string id, string stateId, string targetNodeId, string target, UiBindingMode mode, UiBindingTrigger trigger)
    {
        return new UiBinding
        {
            Id = id,
            StateId = stateId,
            TargetNodeId = targetNodeId,
            Target = target,
            Mode = mode,
            Trigger = trigger
        };
    }

    private static UiSubscription Subscription(string id, string stateId)
    {
        return new UiSubscription { Id = id, StateId = stateId, HandlerId = id + "-handler" };
    }

    private static UiEventData Event(UiEventKind kind, string targetId, UiValue value, bool composing)
    {
        return new UiEventData
        {
            Kind = kind,
            TargetId = targetId,
            Input = new UiInputData { Value = value, IsComposing = composing }
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private class IntegerTextConverter : IUiValueConverter
    {
        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = Text(value.IntegerValue.ToString()) };
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = Integer(long.Parse(value.StringValue!)) };
        }
    }

    private class RecordingObserver : IUiStateObserver
    {
        public List<UiStateObserverContext> Contexts = new List<UiStateObserverContext>();

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            Contexts.Add(context);
            return new UiResult { Success = true };
        }
    }

    private class OrderedObserver : IUiStateObserver
    {
        public string Name;
        public List<string> Order;

        public OrderedObserver(string name, List<string> order)
        {
            Name = name;
            Order = order;
        }

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            Order.Add(Name);
            return new UiResult { Success = true };
        }
    }

    private class RecordingTextConverter : IUiValueConverter
    {
        public List<string> Order;

        public RecordingTextConverter(List<string> order)
        {
            Order = order;
        }

        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            Order.Add("target:" + value.StringValue);
            return new UiConversionResult { Success = true, Value = Text(value.StringValue!) };
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = Text(value.StringValue!) };
        }
    }

    private class RejectingConverter : IUiValueConverter
    {
        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = Text(value.StringValue!) };
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult
            {
                Success = false,
                Diagnostic = new UiDiagnostic
                {
                    Severity = UiDiagnosticSeverity.Error,
                    Code = "converter.rejected",
                    Message = "Rejected",
                    Location = "/converter"
                }
            };
        }
    }
}
