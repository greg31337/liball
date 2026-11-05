using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class StateSubscriptionCoreTests
{
    [Test]
    public void DeliversScopedSubscriptionsInStableOrderWithProjectedValues()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "profile", Profile("Ada", "admin"), "setup");
        core.CreateStateEntry("page", "profile", Profile("Grace", "editor"), "setup");
        core.RegisterStateSubscriptionLifetime("component");
        UiSubscription zeta = Subscription("zeta", "profile", null, "root-handler", "component");
        core.RegisterStateSubscription(null, zeta);
        core.RegisterStateSubscription(null, Subscription("alpha", "profile", "/name", "name-handler", null));
        core.RegisterStateSubscription("page", Subscription("page", "profile", null, "page-handler", null));
        zeta.HandlerId = "mutated";

        core.WriteStateEntry(null, "profile", Profile("Lin", "owner"), "input");
        StateSubscriptionDispatchResult dispatch = core.BeginStateSubscriptionDispatch();
        StateOperationResult entry = core.ReadStateEntry(null, "profile");
        core.EndStateSubscriptionDispatch();

        Assert.Multiple(() =>
        {
            Assert.That(dispatch.Success, Is.True);
            Assert.That(dispatch.Invocations.Select(item => item.SubscriptionId), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(dispatch.Invocations[0].HandlerId, Is.EqualTo("name-handler"));
            Assert.That(dispatch.Invocations[0].PreviousValue!.StringValue, Is.EqualTo("Ada"));
            Assert.That(dispatch.Invocations[0].CurrentValue!.StringValue, Is.EqualTo("Lin"));
            Assert.That(dispatch.Invocations[1].HandlerId, Is.EqualTo("root-handler"));
            Assert.That(dispatch.Invocations[1].PreviousValue!.ObjectValue!.Single(item => item.Key == "role").Value.StringValue, Is.EqualTo("admin"));
            Assert.That(dispatch.Invocations[1].CurrentValue!.ObjectValue!.Single(item => item.Key == "role").Value.StringValue, Is.EqualTo("owner"));
            Assert.That(entry.Entry!.Subscriptions.Select(item => item.Id), Is.EqualTo(new string[] { "alpha", "zeta" }));
        });
    }

    [Test]
    public void PublishesBatchNotificationsOnlyAfterCommitAndSuppressesRollback()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "message", Text("initial"), "setup");
        core.RegisterStateSubscription(null, Subscription("observer", "message", null, "handler", null));

        core.BeginStateBatch("rolled-back");
        core.WriteStateEntry(null, "message", Text("discarded"), "batch");
        StateSubscriptionDispatchResult beforeRollback = core.BeginStateSubscriptionDispatch();
        core.EndStateSubscriptionDispatch();
        core.RollbackStateBatch();
        StateSubscriptionDispatchResult afterRollback = core.BeginStateSubscriptionDispatch();
        core.EndStateSubscriptionDispatch();

        core.BeginStateBatch("committed");
        core.WriteStateEntry(null, "message", Text("first"), "batch");
        core.WriteStateEntry(null, "message", Text("second"), "batch");
        core.CommitStateBatch();
        StateSubscriptionDispatchResult afterCommit = core.BeginStateSubscriptionDispatch();
        core.EndStateSubscriptionDispatch();

        Assert.Multiple(() =>
        {
            Assert.That(beforeRollback.Invocations, Is.Empty);
            Assert.That(afterRollback.Invocations, Is.Empty);
            Assert.That(afterCommit.Invocations, Has.Length.EqualTo(1));
            Assert.That(afterCommit.Invocations[0].PreviousValue!.StringValue, Is.EqualTo("initial"));
            Assert.That(afterCommit.Invocations[0].CurrentValue!.StringValue, Is.EqualTo("second"));
            Assert.That(afterCommit.Invocations[0].Change.Version, Is.EqualTo(3));
        });
    }

    [Test]
    public void DefersReentrantWritesUntilTheNextDispatchCycle()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "counter", Text("zero"), "setup");
        core.RegisterStateSubscription(null, Subscription("observer", "counter", null, "handler", null));
        core.WriteStateEntry(null, "counter", Text("one"), "input");

        StateSubscriptionDispatchResult first = core.BeginStateSubscriptionDispatch();
        StateSubscriptionDispatchResult nested = core.BeginStateSubscriptionDispatch();
        core.WriteStateEntry(null, "counter", Text("two"), "observer");
        StateSubscriptionDispatchResult ended = core.EndStateSubscriptionDispatch();
        StateSubscriptionDispatchResult second = core.BeginStateSubscriptionDispatch();
        core.EndStateSubscriptionDispatch();
        StateSubscriptionDispatchResult extraEnd = core.EndStateSubscriptionDispatch();

        Assert.Multiple(() =>
        {
            Assert.That(first.Invocations, Has.Length.EqualTo(1));
            Assert.That(first.Invocations[0].CurrentValue!.StringValue, Is.EqualTo("one"));
            Assert.That(nested.Code, Is.EqualTo("state.subscription-dispatch-active"));
            Assert.That(ended.Success, Is.True);
            Assert.That(second.Invocations, Has.Length.EqualTo(1));
            Assert.That(second.Invocations[0].PreviousValue!.StringValue, Is.EqualTo("one"));
            Assert.That(second.Invocations[0].CurrentValue!.StringValue, Is.EqualTo("two"));
            Assert.That(extraEnd.Code, Is.EqualTo("state.subscription-dispatch-not-active"));
        });
    }

    [Test]
    public void UnsubscribeAndLifetimeDisposalRemoveRegistrationsAndQueuedCallbacks()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("global"), "setup");
        core.CreateStateEntry("page", "source", Text("page"), "setup");
        core.RegisterStateSubscriptionLifetime("component");
        core.RegisterStateSubscription(null, Subscription("attached", "source", null, "global-handler", "component"));
        core.RegisterStateSubscription("page", Subscription("attached", "source", null, "page-handler", "component"));
        core.RegisterStateSubscription(null, Subscription("durable", "source", null, "durable-handler", null));
        core.WriteStateEntry(null, "source", Text("changed"), "input");
        core.WriteStateEntry("page", "source", Text("changed"), "input");

        StateSubscriptionOperationResult unsubscribed = core.UnsubscribeStateSubscription(null, "durable");
        StateSubscriptionOperationResult disposed = core.DisposeStateSubscriptionLifetime("component");
        StateSubscriptionDispatchResult dispatch = core.BeginStateSubscriptionDispatch();
        core.EndStateSubscriptionDispatch();
        StateSubscriptionOperationResult disposedAgain = core.DisposeStateSubscriptionLifetime("component");
        StateSubscriptionOperationResult rejected = core.RegisterStateSubscription(null, Subscription("later", "source", null, "handler", "component"));
        StateOperationResult global = core.ReadStateEntry(null, "source");
        StateOperationResult page = core.ReadStateEntry("page", "source");

        Assert.Multiple(() =>
        {
            Assert.That(unsubscribed.Success, Is.True);
            Assert.That(unsubscribed.Subscription!.IsActive, Is.False);
            Assert.That(disposed.Success, Is.True);
            Assert.That(disposed.Applied, Is.True);
            Assert.That(dispatch.Invocations, Is.Empty);
            Assert.That(disposedAgain.Success, Is.True);
            Assert.That(disposedAgain.Applied, Is.False);
            Assert.That(rejected.Code, Is.EqualTo("state.subscription-scope-disposed"));
            Assert.That(global.Entry!.Subscriptions, Is.Empty);
            Assert.That(page.Entry!.Subscriptions, Is.Empty);
        });
    }

    [Test]
    public void ValidatesSubscriptionDefinitionsAndLifecycleOperations()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "source", Text("value"), "setup");
        UiSubscription blankId = Subscription(" ", "source", null, "handler", null);
        UiSubscription blankState = Subscription("blank-state", " ", null, "handler", null);
        UiSubscription blankHandler = Subscription("blank-handler", "source", null, " ", null);
        UiSubscription badPath = Subscription("bad-path", "source", "/bad~2path", "handler", null);
        UiSubscription blankLifetime = Subscription("blank-lifetime", "source", null, "handler", " ");
        UiSubscription unknownLifetime = Subscription("unknown-lifetime", "source", null, "handler", "missing");

        StateSubscriptionOperationResult lifetime = core.RegisterStateSubscriptionLifetime("owner");
        StateSubscriptionOperationResult registered = core.RegisterStateSubscription(null, Subscription("valid", "source", null, "handler", "owner"));
        core.BeginStateBatch("batch");
        StateSubscriptionOperationResult batchRegister = core.RegisterStateSubscription(null, Subscription("during-batch", "source", null, "handler", null));
        StateSubscriptionOperationResult batchUnsubscribe = core.UnsubscribeStateSubscription(null, "valid");
        StateSubscriptionOperationResult batchDispose = core.DisposeStateSubscriptionLifetime("owner");
        core.RollbackStateBatch();
        core.BeginStateRender();
        StateSubscriptionOperationResult renderRegister = core.RegisterStateSubscription(null, Subscription("during-render", "source", null, "handler", null));
        StateSubscriptionOperationResult renderLifetime = core.RegisterStateSubscriptionLifetime("render-owner");
        core.EndStateRender();

        Assert.Multiple(() =>
        {
            Assert.That(core.RegisterStateSubscription(null, null).Code, Is.EqualTo("state.null-subscription"));
            Assert.That(core.RegisterStateSubscription(null, blankId).Code, Is.EqualTo("state.invalid-subscription-id"));
            Assert.That(core.RegisterStateSubscription(null, blankState).Code, Is.EqualTo("state.invalid-subscription-state"));
            Assert.That(core.RegisterStateSubscription(null, blankHandler).Code, Is.EqualTo("state.invalid-subscription-handler"));
            Assert.That(core.RegisterStateSubscription(null, Subscription("unknown", "missing", null, "handler", null)).Code, Is.EqualTo("state.unknown-subscription-state"));
            Assert.That(core.RegisterStateSubscription(null, badPath).Code, Is.EqualTo("state.invalid-path-escape"));
            Assert.That(core.RegisterStateSubscription(null, blankLifetime).Code, Is.EqualTo("state.invalid-subscription-scope"));
            Assert.That(core.RegisterStateSubscription(null, unknownLifetime).Code, Is.EqualTo("state.unknown-subscription-scope"));
            Assert.That(core.RegisterStateSubscriptionLifetime(" ").Code, Is.EqualTo("state.invalid-subscription-scope"));
            Assert.That(lifetime.Success, Is.True);
            Assert.That(core.RegisterStateSubscriptionLifetime("owner").Code, Is.EqualTo("state.duplicate-subscription-scope"));
            Assert.That(registered.Success, Is.True);
            Assert.That(core.RegisterStateSubscription(null, Subscription("valid", "source", null, "other", null)).Code, Is.EqualTo("state.duplicate-subscription"));
            Assert.That(core.UnsubscribeStateSubscription(null, "missing").Code, Is.EqualTo("state.unknown-subscription"));
            Assert.That(core.DisposeStateSubscriptionLifetime("missing").Code, Is.EqualTo("state.unknown-subscription-scope"));
            Assert.That(batchRegister.Code, Is.EqualTo("state.batch-active"));
            Assert.That(batchUnsubscribe.Code, Is.EqualTo("state.batch-active"));
            Assert.That(batchDispose.Code, Is.EqualTo("state.batch-active"));
            Assert.That(renderRegister.Code, Is.EqualTo("state.render-write"));
            Assert.That(renderLifetime.Code, Is.EqualTo("state.render-write"));
        });
    }

    [Test]
    public void ProjectsMissingPathValuesAsNullAcrossShapeChangesAndRemoval()
    {
        StateStoreCore core = new StateStoreCore();
        core.CreateStateEntry(null, "record", ObjectValue(Entry("name", Text("Ada"))), "setup");
        core.RegisterStateSubscription(null, Subscription("name", "record", "/name", "handler", null));

        core.WriteStateEntry(null, "record", ObjectValue(Entry("role", Text("admin"))), "input");
        StateSubscriptionDispatchResult missing = core.BeginStateSubscriptionDispatch();
        core.EndStateSubscriptionDispatch();
        core.WriteStateEntry(null, "record", ObjectValue(Entry("name", Text("Lin"))), "input");
        StateSubscriptionDispatchResult restored = core.BeginStateSubscriptionDispatch();
        core.EndStateSubscriptionDispatch();
        core.RemoveStateEntry(null, "record", "remove");
        StateSubscriptionDispatchResult removed = core.BeginStateSubscriptionDispatch();
        core.EndStateSubscriptionDispatch();

        Assert.Multiple(() =>
        {
            Assert.That(missing.Invocations.Single().PreviousValue!.StringValue, Is.EqualTo("Ada"));
            Assert.That(missing.Invocations.Single().CurrentValue, Is.Null);
            Assert.That(restored.Invocations.Single().PreviousValue, Is.Null);
            Assert.That(restored.Invocations.Single().CurrentValue!.StringValue, Is.EqualTo("Lin"));
            Assert.That(removed.Invocations.Single().PreviousValue!.StringValue, Is.EqualTo("Lin"));
            Assert.That(removed.Invocations.Single().CurrentValue, Is.Null);
        });
    }

    private static UiSubscription Subscription(string id, string stateId, string? path, string handlerId, string? scopeId)
    {
        UiSubscription result = new UiSubscription();
        result.Id = id;
        result.StateId = stateId;
        result.Path = path;
        result.HandlerId = handlerId;
        result.ScopeId = scopeId;
        return result;
    }

    private static UiValue Profile(string name, string role)
    {
        return ObjectValue(Entry("name", Text(name)), Entry("role", Text(role)));
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
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
