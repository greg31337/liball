using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class ComponentLifecycleContractTests
{
    [Test]
    public void OrdersEveryStructuralStageAndSuppressesRepeatedTransitions()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = CreateComponent(core, null, "root");
        ComponentOperationResult child = CreateComponent(core, root.Context!.ComponentId, "child");
        ComponentOperationResult grandchild = CreateComponent(core, child.Context!.ComponentId, "grandchild");
        string[] parentFirst = new string[] { root.Context.ComponentId, child.Context.ComponentId, grandchild.Context!.ComponentId };
        string[] childFirst = parentFirst.Reverse().ToArray();
        ComponentLifecycleInvocation[] created = core.DrainComponentLifecycleInvocations();

        ComponentOperationResult attached = core.AttachComponentTree(root.Context.ComponentId);
        ComponentOperationResult repeatedAttach = core.AttachComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] attachment = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult activated = core.ActivateComponentTree(root.Context.ComponentId);
        ComponentOperationResult repeatedActivate = core.ActivateComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] activation = core.DrainComponentLifecycleInvocations();

        core.UpdateComponentParameters(root.Context.ComponentId, new UiValueEntry[] { Entry("value", "root") });
        core.UpdateComponentParameters(child.Context.ComponentId, new UiValueEntry[] { Entry("value", "child") });
        core.UpdateComponentParameters(grandchild.Context.ComponentId, new UiValueEntry[] { Entry("value", "grandchild") });
        ComponentLifecycleInvocation[] parameters = core.DrainComponentLifecycleInvocations();

        ComponentOperationResult deactivated = core.DeactivateComponentTree(root.Context.ComponentId);
        ComponentOperationResult repeatedDeactivate = core.DeactivateComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] deactivation = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult detached = core.DetachComponentTree(root.Context.ComponentId);
        ComponentOperationResult repeatedDetach = core.DetachComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] detachment = core.DrainComponentLifecycleInvocations();

        ComponentOperationResult reattached = core.AttachComponentTree(root.Context.ComponentId);
        ComponentOperationResult repeatedReattach = core.AttachComponentTree(root.Context.ComponentId);
        core.ActivateComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] restored = core.DrainComponentLifecycleInvocations();
        ComponentOperationResult disposed = core.DisposeComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] disposal = core.DrainComponentLifecycleInvocations();

        Assert.Multiple(() =>
        {
            AssertStage(created, UiLifecycleStage.Create, parentFirst);
            Assert.That(attached.Applied, Is.True);
            Assert.That(repeatedAttach.Applied, Is.False);
            AssertStage(attachment, UiLifecycleStage.Attach, parentFirst);
            Assert.That(activated.Applied, Is.True);
            Assert.That(repeatedActivate.Applied, Is.False);
            AssertStage(activation, UiLifecycleStage.Activate, parentFirst);
            AssertStage(parameters, UiLifecycleStage.ParametersChanged, parentFirst);
            Assert.That(deactivated.Applied, Is.True);
            Assert.That(repeatedDeactivate.Applied, Is.False);
            AssertStage(deactivation, UiLifecycleStage.Deactivate, childFirst);
            Assert.That(detached.Applied, Is.True);
            Assert.That(repeatedDetach.Applied, Is.False);
            AssertStage(detachment, UiLifecycleStage.Detach, childFirst);
            Assert.That(reattached.Applied, Is.True);
            Assert.That(repeatedReattach.Applied, Is.False);
            Assert.That(restored.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[]
            {
                UiLifecycleStage.Attach,
                UiLifecycleStage.Attach,
                UiLifecycleStage.Attach,
                UiLifecycleStage.Activate,
                UiLifecycleStage.Activate,
                UiLifecycleStage.Activate
            }));
            Assert.That(restored.Select(item => item.Notification.ComponentId), Is.EqualTo(parentFirst.Concat(parentFirst)));
            Assert.That(disposed.Applied, Is.True);
            Assert.That(disposal.Select(item => item.Notification.Stage), Is.EqualTo(new UiLifecycleStage[]
            {
                UiLifecycleStage.Deactivate,
                UiLifecycleStage.Deactivate,
                UiLifecycleStage.Deactivate,
                UiLifecycleStage.Detach,
                UiLifecycleStage.Detach,
                UiLifecycleStage.Detach,
                UiLifecycleStage.Dispose,
                UiLifecycleStage.Dispose,
                UiLifecycleStage.Dispose
            }));
            Assert.That(disposal.Select(item => item.Notification.ComponentId), Is.EqualTo(childFirst.Concat(childFirst).Concat(childFirst)));
            Assert.That(core.DrainComponentLifecycleInvocations(), Is.Empty);
        });
    }

    [Test]
    public async Task BatchesHydrationInParentFirstOrderWithExactRenderStages()
    {
        UiComponentRuntime runtime = new UiComponentRuntime();
        RecordingRenderer renderer = new RecordingRenderer();
        RecordingLifecycleHandler lifecycle = new RecordingLifecycleHandler();
        runtime.RegisterUiComponentRenderer("render-card", renderer);
        runtime.RegisterUiLifecycleHandler("lifecycle-card", lifecycle);
        runtime.RegisterUiComponentDefinition(Definition());
        UiComponentResult root = runtime.MountUiComponent("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiComponentResult child = runtime.MountUiComponent("seed", root.Context!.ComponentId, "card", "child", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiComponentResult grandchild = runtime.MountUiComponent("seed", child.Context!.ComponentId, "card", "grandchild", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        await runtime.ProcessUiComponentUpdates();
        lifecycle.Notifications.Clear();

        runtime.Scheduler.BeginComponentInvalidationBatch("hydrate-tree");
        runtime.InvalidateUiComponent(grandchild.Context!.ComponentId, UiUpdatePriority.Normal, true);
        runtime.InvalidateUiComponent(child.Context.ComponentId, UiUpdatePriority.Background, false);
        runtime.InvalidateUiComponent(child.Context.ComponentId, UiUpdatePriority.Normal, true);
        runtime.InvalidateUiComponent(root.Context.ComponentId, UiUpdatePriority.Normal, true);
        ComponentScheduleResult committed = runtime.Scheduler.CommitComponentInvalidationBatch();
        UiComponentResult processed = await runtime.ProcessUiComponentUpdates();
        string[] parentFirst = new string[] { root.Context.ComponentId, child.Context.ComponentId, grandchild.Context.ComponentId };

        Assert.Multiple(() =>
        {
            Assert.That(committed.Applied, Is.True);
            Assert.That(processed.Success, Is.True);
            Assert.That(processed.RenderRequests.Select(item => item.Context.ComponentId), Is.EqualTo(parentFirst));
            Assert.That(processed.RenderRequests.Select(item => item.Priority), Is.All.EqualTo(UiUpdatePriority.Normal));
            Assert.That(processed.RenderRequests.Select(item => item.IsHydration), Is.All.True);
            Assert.That(renderer.Requests.Select(item => item.Context.ComponentId), Is.EqualTo(parentFirst));
            Assert.That(processed.LifecycleNotifications.Select(item => item.NotificationKey()), Is.EqualTo(new string[]
            {
                Key(root.Context.ComponentId, UiLifecycleStage.HydrationStart),
                Key(root.Context.ComponentId, UiLifecycleStage.BeforeRender),
                Key(child.Context.ComponentId, UiLifecycleStage.HydrationStart),
                Key(child.Context.ComponentId, UiLifecycleStage.BeforeRender),
                Key(grandchild.Context.ComponentId, UiLifecycleStage.HydrationStart),
                Key(grandchild.Context.ComponentId, UiLifecycleStage.BeforeRender),
                Key(root.Context.ComponentId, UiLifecycleStage.AfterRender),
                Key(root.Context.ComponentId, UiLifecycleStage.HydrationComplete),
                Key(child.Context.ComponentId, UiLifecycleStage.AfterRender),
                Key(child.Context.ComponentId, UiLifecycleStage.HydrationComplete),
                Key(grandchild.Context.ComponentId, UiLifecycleStage.AfterRender),
                Key(grandchild.Context.ComponentId, UiLifecycleStage.HydrationComplete)
            }));
            Assert.That(processed.LifecycleNotifications.Select(item => item.Generation), Is.All.EqualTo(1));
            Assert.That(lifecycle.Notifications.Select(item => item.NotificationKey()), Is.EqualTo(processed.LifecycleNotifications.Select(item => item.NotificationKey())));
        });
    }

    [Test]
    public void DisposesNestedEffectCleanupsInExactChildFirstOrder()
    {
        ComponentCore core = RegisteredCore();
        ComponentOperationResult root = CreateComponent(core, null, "root");
        ComponentOperationResult child = CreateComponent(core, root.Context!.ComponentId, "child");
        ComponentOperationResult grandchild = CreateComponent(core, child.Context!.ComponentId, "grandchild");
        ComponentOperationResult[] components = new ComponentOperationResult[] { root, child, grandchild };
        int index = 0;
        while (index < components.Length)
        {
            UiComponentContext context = components[index].Context!;
            core.CreateComponentLocalState(context.ComponentId, "value", Text("initial"));
            core.RegisterComponentEffect(context.ComponentId, Effect(context.Key));
            core.WriteComponentLocalState(context.ComponentId, "value", Text("changed"));
            index++;
        }

        StateEffectInvocation[] executions = core.StateStore.DrainStateEffectInvocations();
        index = 0;
        while (index < executions.Length)
        {
            StateEffectInvocation execution = executions[index];
            core.StateStore.CompleteStateEffectExecution(execution.ScopeId, execution.EffectId, execution.Generation, true, null);
            index++;
        }

        core.DrainComponentLifecycleInvocations();
        core.DisposeComponentTree(root.Context.ComponentId);
        ComponentLifecycleInvocation[] lifecycle = core.DrainComponentLifecycleInvocations();
        StateEffectInvocation[] cleanups = core.StateStore.DrainStateEffectInvocations();
        string[] childFirstIds = new string[] { grandchild.Context!.ComponentId, child.Context.ComponentId, root.Context.ComponentId };

        Assert.Multiple(() =>
        {
            Assert.That(cleanups.Select(item => item.Kind), Is.All.EqualTo(StateEffectCore.CleanupInvocation));
            Assert.That(cleanups.Select(item => item.HandlerId), Is.EqualTo(new string[] { "cleanup-grandchild", "cleanup-child", "cleanup-root" }));
            Assert.That(cleanups.Select(item => item.ScopeId), Is.EqualTo(new string[] { grandchild.Context.StateScopeId, child.Context.StateScopeId, root.Context.StateScopeId }));
            Assert.That(lifecycle.Select(item => item.Notification.Stage), Is.All.EqualTo(UiLifecycleStage.Dispose));
            Assert.That(lifecycle.Select(item => item.Notification.ComponentId), Is.EqualTo(childFirstIds));
            Assert.That(core.StateStore.EffectCore.Effects, Is.Empty);
        });
    }

    [Test]
    public async Task WaitsForRendererAndLifecycleTasksBeforeCompletingTheCycle()
    {
        UiComponentRuntime runtime = new UiComponentRuntime();
        DelayedRenderer renderer = new DelayedRenderer();
        DelayedLifecycleHandler lifecycle = new DelayedLifecycleHandler();
        runtime.RegisterUiComponentRenderer("render-card", renderer);
        runtime.RegisterUiLifecycleHandler("lifecycle-card", lifecycle);
        runtime.RegisterUiComponentDefinition(Definition());
        UiComponentResult mounted = runtime.MountUiComponent("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.InvalidateUiComponent(mounted.Context!.ComponentId, UiUpdatePriority.Normal, false);

        Task<UiComponentResult> processing = runtime.ProcessUiComponentUpdates();
        await renderer.Started.Task;
        bool completedWhileRendering = processing.IsCompleted;
        bool lifecycleStartedWhileRendering = lifecycle.Started.Task.IsCompleted;
        renderer.Completion.SetResult(new UiComponentRenderResult { Content = Node("rendered") });
        await lifecycle.Started.Task;
        bool completedDuringLifecycle = processing.IsCompleted;
        lifecycle.Completion.SetResult(new UiLifecycleHandlerResult());
        UiComponentResult processed = await processing;

        Assert.Multiple(() =>
        {
            Assert.That(completedWhileRendering, Is.False);
            Assert.That(lifecycleStartedWhileRendering, Is.False);
            Assert.That(completedDuringLifecycle, Is.False);
            Assert.That(processed.Success, Is.True);
            Assert.That(processed.Content!.Text, Is.EqualTo("rendered"));
            Assert.That(lifecycle.Notifications.Select(item => item.Stage), Is.EqualTo(new UiLifecycleStage[]
            {
                UiLifecycleStage.Create,
                UiLifecycleStage.Attach,
                UiLifecycleStage.Activate,
                UiLifecycleStage.BeforeRender,
                UiLifecycleStage.AfterRender
            }));
        });
    }

    private static void AssertStage(ComponentLifecycleInvocation[] invocations, UiLifecycleStage stage, string[] componentIds)
    {
        Assert.That(invocations.Select(item => item.Notification.Stage), Is.All.EqualTo(stage));
        Assert.That(invocations.Select(item => item.Notification.ComponentId), Is.EqualTo(componentIds));
    }

    private static ComponentCore RegisteredCore()
    {
        ComponentCore core = new ComponentCore();
        core.RegisterComponentDefinition(Definition());
        return core;
    }

    private static UiComponentDefinition Definition()
    {
        return new UiComponentDefinition { TypeId = "card", RenderHandlerId = "render-card", LifecycleHandlerId = "lifecycle-card" };
    }

    private static ComponentOperationResult CreateComponent(ComponentCore core, string? parentId, string key)
    {
        return core.CreateComponentInstance("seed", parentId, "card", key, System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
    }

    private static UiValueEntry Entry(string key, string value)
    {
        return new UiValueEntry { Key = key, Value = Text(value) };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiEffect Effect(string key)
    {
        return new UiEffect { Id = "effect-" + key, DependencyIds = new string[] { "value" }, HandlerId = "run-" + key, CleanupHandlerId = "cleanup-" + key };
    }

    private static UiNode Node(string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = text, SiblingIdentity = text, StableId = text + "-id" },
            Text = text
        };
    }

    private static string Key(string componentId, UiLifecycleStage stage)
    {
        return componentId + ":" + stage;
    }

    private class RecordingRenderer : IUiComponentRenderer
    {
        public List<UiComponentRenderRequest> Requests = new List<UiComponentRenderRequest>();

        public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
        {
            Requests.Add(request);
            return Task.FromResult(new UiComponentRenderResult { Content = Node(request.Context.Key) });
        }
    }

    private class RecordingLifecycleHandler : IUiLifecycleHandler
    {
        public List<UiLifecycleNotification> Notifications = new List<UiLifecycleNotification>();

        public Task<UiLifecycleHandlerResult> HandleUiComponentLifecycle(UiLifecycleNotification notification)
        {
            Notifications.Add(notification);
            return Task.FromResult(new UiLifecycleHandlerResult());
        }
    }

    private class DelayedRenderer : IUiComponentRenderer
    {
        public TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>();
        public TaskCompletionSource<UiComponentRenderResult> Completion = new TaskCompletionSource<UiComponentRenderResult>();

        public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
        {
            Started.SetResult(true);
            return Completion.Task;
        }
    }

    private class DelayedLifecycleHandler : IUiLifecycleHandler
    {
        public List<UiLifecycleNotification> Notifications = new List<UiLifecycleNotification>();
        public TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>();
        public TaskCompletionSource<UiLifecycleHandlerResult> Completion = new TaskCompletionSource<UiLifecycleHandlerResult>();
        private bool DelayNext = true;

        public Task<UiLifecycleHandlerResult> HandleUiComponentLifecycle(UiLifecycleNotification notification)
        {
            Notifications.Add(notification);
            if (DelayNext)
            {
                DelayNext = false;
                Started.SetResult(true);
                return Completion.Task;
            }

            return Task.FromResult(new UiLifecycleHandlerResult());
        }
    }
}

internal static class UiLifecycleNotificationTestExtensions
{
    internal static string NotificationKey(this UiLifecycleNotification notification)
    {
        return notification.ComponentId + ":" + notification.Stage;
    }
}
