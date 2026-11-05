using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class UiComponentRuntimeTests
{
    [Test]
    public async Task ProcessesRegisteredRenderersAndLifecycleHandlersThroughTheInterface()
    {
        IUiComponentRuntime runtime = new UiComponentRuntime();
        RecordingComponentRenderer renderer = new RecordingComponentRenderer();
        RecordingLifecycleHandler lifecycle = new RecordingLifecycleHandler();
        UiComponentDefinition definition = new UiComponentDefinition
        {
            TypeId = "card",
            RenderHandlerId = "render-card",
            LifecycleHandlerId = "lifecycle-card",
            DefaultParameters = new UiValueEntry[] { Entry("tone", Text("quiet")) }
        };
        runtime.RegisterUiComponentRenderer("render-card", renderer);
        runtime.RegisterUiLifecycleHandler("lifecycle-card", lifecycle);
        UiComponentResult registered = runtime.RegisterUiComponentDefinition(definition);
        definition.DefaultParameters[0].Value.StringValue = "mutated source";
        UiComponentResult root = runtime.MountUiComponent("seed", null, "card", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), new UiValueEntry[] { Entry("theme", Text("light")) });
        UiComponentResult child = runtime.MountUiComponent("seed", root.Context!.ComponentId, "card", "child", new UiValueEntry[] { Entry("tone", Text("strong")) }, System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiComponentResult consumed = runtime.ConsumeUiComponentContext(child.Context!.ComponentId, "theme");
        runtime.InvalidateUiComponent(child.Context.ComponentId, UiUpdatePriority.Normal, false);
        runtime.InvalidateUiComponent(root.Context.ComponentId, UiUpdatePriority.Normal, false);

        UiComponentResult processed = await runtime.ProcessUiComponentUpdates();
        UiComponentResult renderedRoot = runtime.ReadUiComponent(root.Context.ComponentId);
        UiComponentResult renderedChild = runtime.ReadUiComponent(child.Context.ComponentId);

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(consumed.Value!.StringValue, Is.EqualTo("light"));
            Assert.That(renderer.Requests.Select(item => item.Context.ComponentId), Is.EqualTo(new string[] { root.Context.ComponentId, child.Context.ComponentId }));
            Assert.That(renderer.Requests[0].Context.Parameters.Single().Value.StringValue, Is.EqualTo("quiet"));
            Assert.That(renderer.Requests[1].Context.Parameters.Single().Value.StringValue, Is.EqualTo("strong"));
            Assert.That(processed.Success, Is.True);
            Assert.That(processed.RenderRequests, Has.Length.EqualTo(2));
            Assert.That(processed.LifecycleNotifications.Select(item => item.Stage), Does.Contain(UiLifecycleStage.Create));
            Assert.That(processed.LifecycleNotifications.Select(item => item.Stage), Does.Contain(UiLifecycleStage.BeforeRender));
            Assert.That(processed.LifecycleNotifications.Select(item => item.Stage), Does.Contain(UiLifecycleStage.AfterRender));
            Assert.That(lifecycle.Notifications.Select(item => item.Stage), Is.EqualTo(processed.LifecycleNotifications.Select(item => item.Stage)));
            Assert.That(renderedRoot.Content!.Text, Is.EqualTo("root"));
            Assert.That(renderedChild.Content!.Text, Is.EqualTo("child"));
        });
    }

    [Test]
    public async Task WaitsForRendererTaskCompletionBeforePublishingContent()
    {
        IUiComponentRuntime runtime = new UiComponentRuntime();
        DelayedComponentRenderer renderer = new DelayedComponentRenderer();
        runtime.RegisterUiComponentRenderer("render", renderer);
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "async", RenderHandlerId = "render" });
        UiComponentResult mounted = runtime.MountUiComponent("seed", null, "async", "pending", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.InvalidateUiComponent(mounted.Context!.ComponentId, UiUpdatePriority.UserInteraction, false);

        Task<UiComponentResult> processing = runtime.ProcessUiComponentUpdates();
        await renderer.Started.Task;
        bool completedBeforeResult = processing.IsCompleted;
        renderer.Completion.SetResult(new UiComponentRenderResult { Content = Node("completed") });
        UiComponentResult result = await processing;

        Assert.Multiple(() =>
        {
            Assert.That(completedBeforeResult, Is.False);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Content!.Text, Is.EqualTo("completed"));
            Assert.That(runtime.ReadUiComponent(mounted.Context.ComponentId).Content!.Text, Is.EqualTo("completed"));
        });
    }

    [Test]
    public async Task RoutesRendererFailuresToRegisteredBoundaryLifecycleHandlers()
    {
        IUiComponentRuntime runtime = new UiComponentRuntime();
        RecordingLifecycleHandler errorHandler = new RecordingLifecycleHandler();
        runtime.RegisterUiComponentRenderer("render-boundary", new RecordingComponentRenderer());
        runtime.RegisterUiComponentRenderer("render-failure", new FailingComponentRenderer());
        runtime.RegisterUiLifecycleHandler("error-handler", errorHandler);
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "boundary", RenderHandlerId = "render-boundary", ErrorHandlerId = "error-handler" });
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "leaf", RenderHandlerId = "render-failure" });
        UiComponentResult boundary = runtime.MountUiComponent("seed", null, "boundary", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.ConfigureUiComponentErrorBoundary(boundary.Context!.ComponentId, Node("fallback"), "first");
        UiComponentResult leaf = runtime.MountUiComponent("seed", boundary.Context.ComponentId, "leaf", "leaf", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.InvalidateUiComponent(leaf.Context!.ComponentId, UiUpdatePriority.Immediate, false);

        UiComponentResult result = await runtime.ProcessUiComponentUpdates();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Content!.Text, Is.EqualTo("fallback"));
            Assert.That(result.ErrorBoundary!.BoundaryId, Is.EqualTo(boundary.Context.ComponentId));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("component.renderer-rejected"));
            Assert.That(errorHandler.Notifications, Has.Count.EqualTo(1));
            Assert.That(errorHandler.Notifications.Single().Stage, Is.EqualTo(UiLifecycleStage.Error));
            Assert.That(errorHandler.Notifications.Single().Diagnostic!.Code, Is.EqualTo("component.renderer-rejected"));
        });
    }

    [Test]
    public void ValidatesCallbackRegistrationAndExposesMountRemountPresenceAndDisposal()
    {
        IUiComponentRuntime runtime = new UiComponentRuntime();
        RecordingComponentRenderer renderer = new RecordingComponentRenderer();
        RecordingLifecycleHandler lifecycle = new RecordingLifecycleHandler();

        UiComponentResult rendererRegistration = runtime.RegisterUiComponentRenderer("render", renderer);
        UiComponentResult lifecycleRegistration = runtime.RegisterUiLifecycleHandler("lifecycle", lifecycle);
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "item", RenderHandlerId = "render", LifecycleHandlerId = "lifecycle" });
        UiComponentResult mounted = runtime.MountUiComponent("seed", null, "item", "key", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiComponentResult hidden = runtime.SetUiComponentPresence(mounted.Context!.ComponentId, false);
        UiComponentResult remounted = runtime.RemountUiComponent("seed", null, "item", "key", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiComponentResult disposed = runtime.DisposeUiComponent(remounted.Context!.ComponentId);
        UiComponentResult missing = runtime.ReadUiComponent(remounted.Context.ComponentId);

        Assert.Multiple(() =>
        {
            Assert.That(rendererRegistration.Success, Is.True);
            Assert.That(lifecycleRegistration.Success, Is.True);
            Assert.That(hidden.Applied, Is.True);
            Assert.That(remounted.Context.ComponentId, Is.EqualTo(mounted.Context.ComponentId));
            Assert.That(disposed.Success, Is.True);
            Assert.That(missing.Diagnostics.Single().Code, Is.EqualTo("component.disposed"));
            Assert.That(runtime.RegisterUiComponentRenderer("render", renderer).Diagnostics.Single().Code, Is.EqualTo("component.duplicate-renderer"));
            Assert.That(runtime.RegisterUiLifecycleHandler("lifecycle", lifecycle).Diagnostics.Single().Code, Is.EqualTo("component.duplicate-lifecycle-handler"));
            Assert.That(runtime.RegisterUiComponentRenderer(" ", renderer).Diagnostics.Single().Code, Is.EqualTo("component.invalid-render-handler"));
            Assert.That(runtime.RegisterUiLifecycleHandler("next", null).Diagnostics.Single().Code, Is.EqualTo("component.null-lifecycle-handler"));
            Assert.That(runtime.UnregisterUiComponentRenderer("missing").Diagnostics.Single().Code, Is.EqualTo("component.unknown-renderer"));
            Assert.That(runtime.UnregisterUiLifecycleHandler("missing").Diagnostics.Single().Code, Is.EqualTo("component.unknown-lifecycle-handler"));
        });
    }

    [Test]
    public async Task UpdatesParametersProvidesContextResetsBoundariesAndUnregistersDefinitionsThroughTheInterface()
    {
        IUiComponentRuntime runtime = new UiComponentRuntime();
        runtime.RegisterUiComponentRenderer("fail", new FailingComponentRenderer());
        runtime.RegisterUiLifecycleHandler("errors", new RecordingLifecycleHandler());
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition
        {
            TypeId = "boundary",
            RenderHandlerId = "unused",
            ErrorHandlerId = "errors"
        });
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition
        {
            TypeId = "leaf",
            RenderHandlerId = "fail",
            DefaultParameters = new UiValueEntry[] { Entry("tone", Text("quiet")) }
        });
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition
        {
            TypeId = "unused",
            RenderHandlerId = "unused"
        });
        UiComponentResult boundary = runtime.MountUiComponent("seed", null, "boundary", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiComponentResult leaf = runtime.MountUiComponent("seed", boundary.Context!.ComponentId, "leaf", "child", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());

        UiValue supplied = Text("dark");
        UiComponentResult provided = runtime.ProvideUiComponentContext(boundary.Context.ComponentId, "theme", supplied);
        supplied.StringValue = "caller mutation";
        UiComponentResult repeatedContext = runtime.ProvideUiComponentContext(boundary.Context.ComponentId, "theme", Text("dark"));
        UiComponentResult consumed = runtime.ConsumeUiComponentContext(leaf.Context!.ComponentId, "theme");
        UiComponentResult updated = runtime.UpdateUiComponentParameters(leaf.Context.ComponentId, new UiValueEntry[] { Entry("tone", Text("loud")) });
        UiComponentResult repeatedUpdate = runtime.UpdateUiComponentParameters(leaf.Context.ComponentId, new UiValueEntry[] { Entry("tone", Text("loud")) });
        UiComponentResult configured = runtime.ConfigureUiComponentErrorBoundary(boundary.Context.ComponentId, Node("fallback"), "first");
        runtime.InvalidateUiComponent(leaf.Context.ComponentId, UiUpdatePriority.Immediate, false);
        UiComponentResult failed = await runtime.ProcessUiComponentUpdates();
        UiComponentResult unchangedReset = runtime.ResetUiComponentErrorBoundary(boundary.Context.ComponentId, "first");
        UiComponentResult changedReset = runtime.ResetUiComponentErrorBoundary(boundary.Context.ComponentId, "second");
        UiComponentResult repeatedReset = runtime.ResetUiComponentErrorBoundary(boundary.Context.ComponentId, "second");
        UiComponentResult inUse = runtime.UnregisterUiComponentDefinition("leaf");
        UiComponentResult unused = runtime.UnregisterUiComponentDefinition("unused");
        runtime.DisposeUiComponent(boundary.Context.ComponentId);
        UiComponentResult leafRemoved = runtime.UnregisterUiComponentDefinition("leaf");
        UiComponentResult boundaryRemoved = runtime.UnregisterUiComponentDefinition("boundary");

        Assert.Multiple(() =>
        {
            Assert.That(provided.Success, Is.True);
            Assert.That(provided.Applied, Is.True);
            Assert.That(repeatedContext.Success, Is.True);
            Assert.That(repeatedContext.Applied, Is.False);
            Assert.That(consumed.Value!.StringValue, Is.EqualTo("dark"));
            Assert.That(updated.Success, Is.True);
            Assert.That(updated.Applied, Is.True);
            Assert.That(updated.Context!.Parameters.Single().Value.StringValue, Is.EqualTo("loud"));
            Assert.That(repeatedUpdate.Success, Is.True);
            Assert.That(repeatedUpdate.Applied, Is.False);
            Assert.That(configured.Success, Is.True);
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.ErrorBoundary!.HasError, Is.True);
            Assert.That(unchangedReset.Success, Is.True);
            Assert.That(unchangedReset.Applied, Is.False);
            Assert.That(unchangedReset.ErrorBoundary!.HasError, Is.True);
            Assert.That(changedReset.Success, Is.True);
            Assert.That(changedReset.Applied, Is.True);
            Assert.That(changedReset.ErrorBoundary!.HasError, Is.False);
            Assert.That(repeatedReset.Applied, Is.False);
            Assert.That(inUse.Diagnostics.Single().Code, Is.EqualTo("component.definition-in-use"));
            Assert.That(unused.Success, Is.True);
            Assert.That(leafRemoved.Success, Is.True);
            Assert.That(boundaryRemoved.Success, Is.True);
        });

        Assert.Multiple(() =>
        {
            Assert.That(runtime.UpdateUiComponentParameters(null, System.Array.Empty<UiValueEntry>()).Success, Is.False);
            Assert.That(runtime.UpdateUiComponentParameters("missing", null).Success, Is.False);
            Assert.That(runtime.ProvideUiComponentContext(null, "theme", Text("dark")).Success, Is.False);
            Assert.That(runtime.ProvideUiComponentContext("missing", " ", null).Success, Is.False);
            Assert.That(runtime.ResetUiComponentErrorBoundary(null, "reset").Success, Is.False);
            Assert.That(runtime.ResetUiComponentErrorBoundary("missing", " ").Success, Is.False);
            Assert.That(runtime.UnregisterUiComponentDefinition(null).Success, Is.False);
            Assert.That(runtime.UnregisterUiComponentDefinition("unused").Success, Is.False);
        });
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
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

    private class RecordingComponentRenderer : IUiComponentRenderer
    {
        public List<UiComponentRenderRequest> Requests = new List<UiComponentRenderRequest>();

        public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
        {
            Requests.Add(request);
            return Task.FromResult(new UiComponentRenderResult { Content = Node(request.Context.Key) });
        }
    }

    private class DelayedComponentRenderer : IUiComponentRenderer
    {
        public TaskCompletionSource<bool> Started = new TaskCompletionSource<bool>();
        public TaskCompletionSource<UiComponentRenderResult> Completion = new TaskCompletionSource<UiComponentRenderResult>();

        public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
        {
            Started.SetResult(true);
            return Completion.Task;
        }
    }

    private class FailingComponentRenderer : IUiComponentRenderer
    {
        public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
        {
            return Task.FromResult(new UiComponentRenderResult { Success = false });
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
}
