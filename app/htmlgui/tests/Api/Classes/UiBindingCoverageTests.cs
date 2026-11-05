using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiBindingCoverageTests
{
    [Test]
    public void PropagatesCommittedBatchesAcrossModesWithoutEchoingTheWritingBinding()
    {
        IUiBindingEngine engine = new UiBindingEngine();
        IUiStateStore store = engine.GetBindingStateStore();
        store.CreateState(null, "name", Text("initial"), "setup");
        engine.AttachBinding(null, Binding("once", "name", "once-node", "text", UiBindingMode.OneTime, UiBindingTrigger.Input));
        engine.AttachBinding(null, Binding("forward", "name", "forward-node", "text", UiBindingMode.OneWay, UiBindingTrigger.Input));
        engine.AttachBinding(null, Binding("both", "name", "both-node", "value", UiBindingMode.TwoWay, UiBindingTrigger.Input));
        engine.AttachBinding(null, Binding("source-only", "name", "source-node", "value", UiBindingMode.OneWayToSource, UiBindingTrigger.Explicit));

        store.BeginStateBatch("external");
        store.WriteState(null, "name", Text("batched"), "server");
        UiStateBatchResult committed = store.CommitStateBatch();
        UiBindingResult afterBatch = engine.EvaluateStateBindings(null);
        UiBindingResult twoWay = engine.PushBindingValue(null, "both", Text("two-way"), "browser", UiBindingTrigger.Input);
        UiBindingResult sourceOnly = engine.PushBindingValue(null, "source-only", Text("source-only"), "browser", UiBindingTrigger.Explicit);

        Assert.Multiple(() =>
        {
            Assert.That(committed.Success, Is.True);
            Assert.That(afterBatch.BindingIds, Is.EqualTo(new string[] { "both", "forward" }));
            Assert.That(afterBatch.Updates.Select(item => item.Value.StringValue), Is.EqualTo(new string[] { "batched", "batched" }));
            Assert.That(twoWay.Success, Is.True);
            Assert.That(twoWay.BindingIds, Is.EqualTo(new string[] { "both", "forward" }));
            Assert.That(twoWay.Updates.Select(item => item.BindingId), Is.EqualTo(new string[] { "forward" }));
            Assert.That(sourceOnly.Success, Is.True);
            Assert.That(sourceOnly.BindingIds, Is.EqualTo(new string[] { "source-only", "both", "forward" }));
            Assert.That(sourceOnly.Updates.Select(item => item.BindingId), Is.EqualTo(new string[] { "both", "forward" }));
            Assert.That(store.ReadState(null, "name").Entry!.Value.StringValue, Is.EqualTo("source-only"));
            Assert.That(store.ReadState(null, "name").Entry!.Version, Is.EqualTo(4));
        });
    }

    [Test]
    public void ValidatesConvertedValuesBeforeFallbackOrSourceMutation()
    {
        IUiBindingEngine engine = new UiBindingEngine();
        IUiStateStore store = engine.GetBindingStateStore();
        store.CreateState(null, "count", Integer(7), "setup");
        engine.RegisterBindingConverter("malformed-target", new MalformedTargetConverter());
        engine.RegisterBindingConverter("malformed-source", new MalformedSourceConverter());
        UiBinding display = Binding("display", "count", "display-node", "text", UiBindingMode.OneWay, UiBindingTrigger.Change);
        display.ConverterId = "malformed-target";
        display.FallbackValue = Text("safe fallback");
        UiBinding input = Binding("input", "count", "input-node", "value", UiBindingMode.TwoWay, UiBindingTrigger.Input);
        input.ConverterId = "malformed-source";

        UiBindingResult attachedDisplay = engine.AttachBinding(null, display);
        UiBindingResult attachedInput = engine.AttachBinding(null, input);
        UiBindingResult pushed = engine.PushBindingValue(null, "input", Text("12"), "browser", UiBindingTrigger.Input);
        UiStateResult retained = store.ReadState(null, "count");

        Assert.Multiple(() =>
        {
            Assert.That(attachedDisplay.Success, Is.True);
            Assert.That(attachedDisplay.Updates.Single().Value.StringValue, Is.EqualTo("safe fallback"));
            Assert.That(attachedDisplay.Diagnostics.Single().Code, Is.EqualTo("state.invalid-value"));
            Assert.That(attachedDisplay.Binding!.Conversion, Is.Not.Null);
            Assert.That(attachedInput.Success, Is.True);
            Assert.That(pushed.Success, Is.False);
            Assert.That(pushed.Applied, Is.False);
            Assert.That(pushed.Diagnostics.Single().Code, Is.EqualTo("state.invalid-value"));
            Assert.That(pushed.Binding!.Conversion, Is.Not.Null);
            Assert.That(retained.Entry!.Value.IntegerValue, Is.EqualTo(7));
            Assert.That(retained.Entry.Version, Is.EqualTo(1));
        });
    }

    [Test]
    public void DetachClearsOneTimeAndEventStateBeforeACleanReattach()
    {
        IUiBindingEngine engine = new UiBindingEngine();
        IUiStateStore store = engine.GetBindingStateStore();
        store.CreateState("page", "name", Text("initial"), "setup");
        UiBinding once = Binding("once", "name", "once-node", "text", UiBindingMode.OneTime, UiBindingTrigger.Change);
        UiBinding input = Binding("input", "name", "input-node", "value", UiBindingMode.TwoWay, UiBindingTrigger.Input);
        engine.AttachBinding("page", once);
        engine.AttachBinding("page", input);

        UiBindingResult detachedOnce = engine.DetachBinding("page", "once");
        UiBindingResult detachedInput = engine.DetachBinding("page", "input");
        store.WriteState("page", "name", Text("latest"), "server");
        UiBindingResult detachedEvaluation = engine.EvaluateStateBindings("page");
        UiBindingResult detachedEvent = engine.ProcessBindingEvent("page", Event("input-node", Text("ignored")), "browser");
        UiBindingResult reattachedOnce = engine.AttachBinding("page", once);
        UiBindingResult reattachedInput = engine.AttachBinding("page", input);
        UiBindingResult secondOnceEvaluation = engine.EvaluateBinding("page", "once");

        Assert.Multiple(() =>
        {
            Assert.That(detachedOnce.Success, Is.True);
            Assert.That(detachedOnce.Binding!.Context, Is.Not.Null);
            Assert.That(detachedInput.Success, Is.True);
            Assert.That(detachedEvaluation.Applied, Is.False);
            Assert.That(detachedEvaluation.BindingIds, Is.Empty);
            Assert.That(detachedEvent.Success, Is.True);
            Assert.That(detachedEvent.Applied, Is.False);
            Assert.That(store.ReadState("page", "name").Entry!.Value.StringValue, Is.EqualTo("latest"));
            Assert.That(reattachedOnce.Updates.Single().Value.StringValue, Is.EqualTo("latest"));
            Assert.That(reattachedOnce.Binding!.Context!.Version, Is.EqualTo(2));
            Assert.That(reattachedInput.Updates.Single().Value.StringValue, Is.EqualTo("latest"));
            Assert.That(secondOnceEvaluation.Success, Is.True);
            Assert.That(secondOnceEvaluation.Applied, Is.False);
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

    private static UiEventData Event(string targetId, UiValue value)
    {
        return new UiEventData
        {
            Kind = UiEventKind.Input,
            TargetId = targetId,
            Input = new UiInputData { Value = value }
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

    private class MalformedTargetConverter : IUiValueConverter
    {
        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = new UiValue { Kind = UiValueKind.String } };
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = value };
        }
    }

    private class MalformedSourceConverter : IUiValueConverter
    {
        public UiConversionResult ConvertBindingValueToTarget(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = value };
        }

        public UiConversionResult ConvertBindingValueToSource(UiValue value, UiBindingContext context)
        {
            return new UiConversionResult { Success = true, Value = new UiValue { Kind = UiValueKind.String } };
        }
    }
}
