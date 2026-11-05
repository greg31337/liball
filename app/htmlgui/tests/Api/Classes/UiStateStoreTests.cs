using System.Collections.Generic;
using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class UiStateStoreTests
{
    [Test]
    public void ExposesCrudCompareAndSetAndPatchOperationsThroughThePublicInterface()
    {
        IUiStateStore store = new UiStateStore();
        UiStateResult created = store.CreateState("page", "profile", Profile("Ada"), "setup");
        created.Entry!.Value.ObjectValue![0].Value.StringValue = "mutated";
        UiStateResult read = store.ReadState("page", "profile");
        UiStateResult written = store.WriteState("page", "profile", Profile("Grace"), "input");
        UiStateResult mismatch = store.CompareAndSetState("page", "profile", 1, Profile("ignored"), "cas");
        UiStateResult compared = store.CompareAndSetState("page", "profile", 2, Profile("Lin"), "cas");
        UiStateResult patched = store.PatchState("page", "profile", new UiStatePatch[]
        {
            new UiStatePatch { Operation = "replace", Path = "/name", Value = Text("Quinn") }
        }, "patch");
        UiStateResult removed = store.RemoveState("page", "profile", "remove");
        UiStateResult missing = store.ReadState("page", "profile");

        Assert.Multiple(() =>
        {
            Assert.That(created.Success, Is.True);
            Assert.That(created.Applied, Is.True);
            Assert.That(read.Entry!.Value.ObjectValue![0].Value.StringValue, Is.EqualTo("Ada"));
            Assert.That(written.Entry!.Version, Is.EqualTo(2));
            Assert.That(mismatch.Success, Is.True);
            Assert.That(mismatch.Applied, Is.False);
            Assert.That(mismatch.Diagnostics.Single().Code, Is.EqualTo("state.version-mismatch"));
            Assert.That(compared.Entry!.Version, Is.EqualTo(3));
            Assert.That(patched.Entry!.Version, Is.EqualTo(4));
            Assert.That(patched.Entry.Patches.Single().Operation, Is.EqualTo("replace"));
            Assert.That(patched.Entry.Value.ObjectValue![0].Value.StringValue, Is.EqualTo("Quinn"));
            Assert.That(removed.Change!.Version, Is.EqualTo(5));
            Assert.That(removed.Entry, Is.Null);
            Assert.That(missing.Success, Is.False);
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("state.unknown"));
        });
    }

    [Test]
    public void DeliversOnlyCommittedBatchAndSnapshotChangesToObservers()
    {
        IUiStateStore store = new UiStateStore();
        RecordingStateObserver observer = new RecordingStateObserver("observer");
        store.CreateState(null, "message", Text("initial"), "setup");
        UiStateSnapshotResult snapshot = store.CreateStateSnapshot("baseline");
        store.RegisterStateSubscription(null, Subscription("message-observer", "message", null, "handler", null), observer);

        store.BeginStateBatch("commit");
        store.WriteState(null, "message", Text("first"), "batch");
        store.WriteState(null, "message", Text("second"), "batch");
        int beforeCommit = observer.Contexts.Count;
        UiStateBatchResult committed = store.CommitStateBatch();

        store.BeginStateBatch("rollback");
        store.WriteState(null, "message", Text("discarded"), "batch");
        UiStateBatchResult rolledBack = store.RollbackStateBatch();
        int afterRollback = observer.Contexts.Count;

        store.WriteState(null, "message", Text("changed"), "input");
        UiStateSnapshotResult restored = store.RestoreStateSnapshot("baseline", "restore");
        snapshot.Entries[0].Entry.Value.StringValue = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Success, Is.True);
            Assert.That(snapshot.SnapshotId, Is.EqualTo("baseline"));
            Assert.That(beforeCommit, Is.EqualTo(0));
            Assert.That(committed.Success, Is.True);
            Assert.That(committed.Changes, Has.Length.EqualTo(1));
            Assert.That(observer.Contexts[0].PreviousValue!.StringValue, Is.EqualTo("initial"));
            Assert.That(observer.Contexts[0].CurrentValue!.StringValue, Is.EqualTo("second"));
            Assert.That(rolledBack.Success, Is.True);
            Assert.That(afterRollback, Is.EqualTo(1));
            Assert.That(observer.Contexts, Has.Count.EqualTo(3));
            Assert.That(observer.Contexts[1].CurrentValue!.StringValue, Is.EqualTo("changed"));
            Assert.That(observer.Contexts[2].Change.Origin, Is.EqualTo("restore"));
            Assert.That(observer.Contexts[2].CurrentValue!.StringValue, Is.EqualTo("initial"));
            Assert.That(restored.Changes.Single().ScopeId, Is.EqualTo(string.Empty));
            Assert.That(store.ReadState(null, "message").Entry!.Value.StringValue, Is.EqualTo("initial"));
        });
    }

    [Test]
    public void DefersReentrantObserverWritesIntoStableFollowingCycles()
    {
        IUiStateStore store = new UiStateStore();
        List<string> calls = new List<string>();
        ReentrantStateObserver alpha = new ReentrantStateObserver("alpha", calls, store);
        RecordingStateObserver zeta = new RecordingStateObserver("zeta", calls);
        store.CreateState(null, "counter", Text("zero"), "setup");
        store.RegisterStateSubscription(null, Subscription("zeta", "counter", null, "zeta-handler", null), zeta);
        store.RegisterStateSubscription(null, Subscription("alpha", "counter", null, "alpha-handler", null), alpha);

        UiStateResult written = store.WriteState(null, "counter", Text("one"), "input");

        Assert.Multiple(() =>
        {
            Assert.That(written.Success, Is.True);
            Assert.That(calls, Is.EqualTo(new string[] { "alpha:one", "zeta:one", "alpha:two", "zeta:two" }));
            Assert.That(alpha.Contexts, Has.Count.EqualTo(2));
            Assert.That(alpha.Contexts[1].PreviousValue!.StringValue, Is.EqualTo("one"));
            Assert.That(alpha.Contexts[1].CurrentValue!.StringValue, Is.EqualTo("two"));
            Assert.That(store.ReadState(null, "counter").Entry!.Value.StringValue, Is.EqualTo("two"));
        });
    }

    [Test]
    public void LifetimeDisposalAndUnsubscribeRemovePublicCallbacksDuringDispatch()
    {
        IUiStateStore store = new UiStateStore();
        RecordingStateObserver attached = new RecordingStateObserver("attached");
        DisposingStateObserver disposer = new DisposingStateObserver(store, "component");
        RecordingStateObserver removable = new RecordingStateObserver("removable");
        store.CreateState(null, "source", Text("initial"), "setup");
        store.RegisterStateSubscriptionLifetime("component");
        store.RegisterStateSubscription(null, Subscription("zeta", "source", null, "attached-handler", "component"), attached);
        store.RegisterStateSubscription(null, Subscription("alpha", "source", null, "dispose-handler", null), disposer);
        store.RegisterStateSubscription(null, Subscription("omega", "source", null, "remove-handler", null), removable);

        store.WriteState(null, "source", Text("first"), "input");
        UiStateSubscriptionResult unsubscribed = store.UnsubscribeState(null, "omega");
        store.WriteState(null, "source", Text("second"), "input");
        UiStateSubscriptionResult disposedAgain = store.DisposeStateSubscriptionLifetime("component");

        Assert.Multiple(() =>
        {
            Assert.That(disposer.Contexts, Has.Count.EqualTo(2));
            Assert.That(attached.Contexts, Is.Empty);
            Assert.That(removable.Contexts, Has.Count.EqualTo(1));
            Assert.That(unsubscribed.Success, Is.True);
            Assert.That(unsubscribed.Subscription!.IsActive, Is.False);
            Assert.That(disposedAgain.Success, Is.True);
            Assert.That(disposedAgain.Applied, Is.False);
            Assert.That(store.ReadState(null, "source").Entry!.Subscriptions.Select(item => item.Id), Is.EqualTo(new string[] { "alpha" }));
        });
    }

    [Test]
    public void SurfacesDerivedAndEffectOperationsWithCopiedResults()
    {
        IUiStateStore store = new UiStateStore();
        store.CreateState("page", "first", Text("one"), "setup");
        store.CreateState("page", "second", Text("two"), "setup");
        UiDerivedState derived = new UiDerivedState
        {
            Id = "summary",
            DependencyIds = new string[] { "first" },
            HandlerId = "derive-handler",
            Value = Text("old")
        };
        UiDerivedStateResult registeredDerived = store.RegisterDerivedState("page", derived);
        derived.DependencyIds[0] = "mutated";
        UiDerivedStateResult dependencies = store.UpdateDerivedStateDependencies("page", "summary", new string[] { "first", "second" });
        UiDerivedStateResult accepted = store.AcceptDerivedStateValue("page", "summary", Text("computed"));

        UiEffect effect = new UiEffect
        {
            Id = "persist",
            DependencyIds = new string[] { "first" },
            HandlerId = "persist-handler",
            CleanupHandlerId = "cleanup-handler"
        };
        UiStateEffectResult registeredEffect = store.RegisterStateEffect("page", effect);
        effect.HandlerId = "mutated";
        UiStateEffectResult readEffect = store.ReadStateEffect("page", "persist");
        UiStateEffectResult canceled = store.CancelStateEffect("page", "persist");

        Assert.Multiple(() =>
        {
            Assert.That(registeredDerived.Success, Is.True);
            Assert.That(store.ReadDerivedState("page", "summary").Derived!.DependencyIds, Is.EqualTo(new string[] { "first", "second" }));
            Assert.That(dependencies.Applied, Is.True);
            Assert.That(accepted.Derived!.Value.StringValue, Is.EqualTo("computed"));
            Assert.That(accepted.Derived.Version, Is.EqualTo(1));
            Assert.That(registeredEffect.Success, Is.True);
            Assert.That(readEffect.Effect!.HandlerId, Is.EqualTo("persist-handler"));
            Assert.That(canceled.Success, Is.True);
            Assert.That(canceled.Effect!.IsCancelled, Is.True);
            Assert.That(store.ReadStateEffect("page", "missing").Diagnostics.Single().Code, Is.EqualTo("state.unknown-effect"));
        });
    }

    [Test]
    public void ReportsObserverRegistrationAndCallbackFailuresWithoutLosingCommittedState()
    {
        IUiStateStore store = new UiStateStore();
        store.CreateState(null, "source", Text("initial"), "setup");
        UiSubscription subscription = Subscription("observer", "source", "/bad~2path", "handler", null);
        UiStateSubscriptionResult nullObserver = store.RegisterStateSubscription(null, subscription, null);
        UiStateSubscriptionResult invalidPath = store.RegisterStateSubscription(null, subscription, new RecordingStateObserver("unused"));
        FailingStateObserver failing = new FailingStateObserver();
        UiStateSubscriptionResult registered = store.RegisterStateSubscription(null, Subscription("failing", "source", null, "handler", null), failing);

        UiStateResult written = store.WriteState(null, "source", Text("committed"), "input");
        failing.Diagnostic.Message = "mutated";
        UiStateResult retained = store.ReadState(null, "source");

        Assert.Multiple(() =>
        {
            Assert.That(nullObserver.Success, Is.False);
            Assert.That(nullObserver.Diagnostics.Single().Code, Is.EqualTo("state.null-observer"));
            Assert.That(invalidPath.Diagnostics.Single().Code, Is.EqualTo("state.invalid-path-escape"));
            Assert.That(registered.Success, Is.True);
            Assert.That(written.Success, Is.False);
            Assert.That(written.Applied, Is.True);
            Assert.That(written.Diagnostics.Single().Code, Is.EqualTo("observer.rejected"));
            Assert.That(written.Diagnostics.Single().Message, Is.EqualTo("Rejected callback"));
            Assert.That(retained.Success, Is.True);
            Assert.That(retained.Entry!.Value.StringValue, Is.EqualTo("committed"));
        });
    }

    private static UiSubscription Subscription(string id, string stateId, string? path, string handlerId, string? lifetimeId)
    {
        return new UiSubscription { Id = id, StateId = stateId, Path = path, HandlerId = handlerId, ScopeId = lifetimeId };
    }

    private static UiValue Profile(string name)
    {
        return new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[] { new UiValueEntry { Key = "name", Value = Text(name) } }
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private class RecordingStateObserver : IUiStateObserver
    {
        public string Name;
        public List<string> Calls;
        public List<UiStateObserverContext> Contexts = new List<UiStateObserverContext>();

        public RecordingStateObserver(string name)
        {
            Name = name;
            Calls = new List<string>();
        }

        public RecordingStateObserver(string name, List<string> calls)
        {
            Name = name;
            Calls = calls;
        }

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            Contexts.Add(context);
            Calls.Add(Name + ":" + context.CurrentValue!.StringValue);
            return new UiResult { Success = true };
        }
    }

    private class ReentrantStateObserver : IUiStateObserver
    {
        public string Name;
        public List<string> Calls;
        public IUiStateStore Store;
        public bool Reentered;
        public List<UiStateObserverContext> Contexts = new List<UiStateObserverContext>();

        public ReentrantStateObserver(string name, List<string> calls, IUiStateStore store)
        {
            Name = name;
            Calls = calls;
            Store = store;
        }

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            Contexts.Add(context);
            Calls.Add(Name + ":" + context.CurrentValue!.StringValue);
            if (!Reentered)
            {
                Reentered = true;
                Store.WriteState(null, "counter", Text("two"), "observer");
            }

            return new UiResult { Success = true };
        }
    }

    private class DisposingStateObserver : IUiStateObserver
    {
        public IUiStateStore Store;
        public string LifetimeId;
        public List<UiStateObserverContext> Contexts = new List<UiStateObserverContext>();

        public DisposingStateObserver(IUiStateStore store, string lifetimeId)
        {
            Store = store;
            LifetimeId = lifetimeId;
        }

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            Contexts.Add(context);
            Store.DisposeStateSubscriptionLifetime(LifetimeId);
            return new UiResult { Success = true };
        }
    }

    private class FailingStateObserver : IUiStateObserver
    {
        public UiDiagnostic Diagnostic = new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Warning,
            Code = "observer.rejected",
            Message = "Rejected callback",
            Location = "/observer"
        };

        public UiResult ObserveStateChange(UiStateObserverContext context)
        {
            return new UiResult { Success = false, Diagnostics = new UiDiagnostic[] { Diagnostic } };
        }
    }
}
