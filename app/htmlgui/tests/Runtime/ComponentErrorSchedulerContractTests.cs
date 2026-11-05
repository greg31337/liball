using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class ComponentErrorSchedulerContractTests
{
    [Test]
    public void SelectsTheNearestHealthyBoundaryAndMakesItEligibleAfterReset()
    {
        ComponentCore core = BoundaryCore();
        ComponentOperationResult outer = CreateComponent(core, null, "boundary", "outer");
        ComponentOperationResult inner = CreateComponent(core, outer.Context!.ComponentId, "boundary", "inner");
        ComponentOperationResult leaf = CreateComponent(core, inner.Context!.ComponentId, "leaf", "leaf");
        core.ConfigureComponentErrorBoundary(outer.Context.ComponentId, Node("outer fallback"), "outer-1");
        core.ConfigureComponentErrorBoundary(inner.Context.ComponentId, Node("inner fallback"), "inner-1");
        core.AttachComponentTree(outer.Context.ComponentId);
        core.ActivateComponentTree(outer.Context.ComponentId);
        core.DrainComponentLifecycleInvocations();

        ComponentOperationResult first = core.ReportComponentError(leaf.Context!.ComponentId, UiLifecycleStage.BeforeRender, Diagnostic("component.first", "First failure"));
        ComponentErrorInvocation firstInvocation = core.DrainComponentErrorInvocations().Single();
        ComponentOperationResult unchanged = core.ResetComponentErrorBoundary(inner.Context.ComponentId, "inner-1");
        ComponentOperationResult second = core.ReportComponentError(leaf.Context.ComponentId, UiLifecycleStage.AfterRender, Diagnostic("component.second", "Second failure"));
        ComponentErrorInvocation secondInvocation = core.DrainComponentErrorInvocations().Single();
        ComponentOperationResult reset = core.ResetComponentErrorBoundary(inner.Context.ComponentId, "inner-2");
        ComponentOperationResult third = core.ReportComponentError(leaf.Context.ComponentId, UiLifecycleStage.HydrationComplete, Diagnostic("component.third", "Third failure"));
        ComponentErrorInvocation thirdInvocation = core.DrainComponentErrorInvocations().Single();
        ComponentOperationResult retainedInner = core.ReadComponentErrorBoundary(inner.Context.ComponentId);
        ComponentOperationResult retainedOuter = core.ReadComponentErrorBoundary(outer.Context.ComponentId);

        Assert.Multiple(() =>
        {
            Assert.That(first.ErrorBoundary!.BoundaryId, Is.EqualTo(inner.Context.ComponentId));
            Assert.That(first.Node!.Text, Is.EqualTo("inner fallback"));
            Assert.That(firstInvocation.Boundary.BoundaryId, Is.EqualTo(inner.Context.ComponentId));
            Assert.That(firstInvocation.FailedComponentId, Is.EqualTo(leaf.Context.ComponentId));
            Assert.That(firstInvocation.Stage, Is.EqualTo(UiLifecycleStage.BeforeRender));
            Assert.That(unchanged.Applied, Is.False);
            Assert.That(unchanged.ErrorBoundary!.HasError, Is.True);
            Assert.That(second.ErrorBoundary!.BoundaryId, Is.EqualTo(outer.Context.ComponentId));
            Assert.That(second.Node!.Text, Is.EqualTo("outer fallback"));
            Assert.That(secondInvocation.Boundary.BoundaryId, Is.EqualTo(outer.Context.ComponentId));
            Assert.That(secondInvocation.Stage, Is.EqualTo(UiLifecycleStage.AfterRender));
            Assert.That(reset.Applied, Is.True);
            Assert.That(third.ErrorBoundary!.BoundaryId, Is.EqualTo(inner.Context.ComponentId));
            Assert.That(thirdInvocation.Boundary.BoundaryId, Is.EqualTo(inner.Context.ComponentId));
            Assert.That(thirdInvocation.Stage, Is.EqualTo(UiLifecycleStage.HydrationComplete));
            Assert.That(retainedInner.ErrorBoundary!.HasError, Is.True);
            Assert.That(retainedInner.ErrorBoundary.Diagnostic!.Code, Is.EqualTo("component.third"));
            Assert.That(retainedOuter.ErrorBoundary!.HasError, Is.True);
            Assert.That(retainedOuter.ErrorBoundary.Diagnostic!.Code, Is.EqualTo("component.second"));
        });
    }

    [Test]
    public async Task ReportsLifecycleRendererAndBoundaryHandlerFailures()
    {
        UiComponentRuntime lifecycleRuntime = new UiComponentRuntime();
        ThrowingLifecycleHandler lifecycleHandler = new ThrowingLifecycleHandler();
        lifecycleRuntime.RegisterUiLifecycleHandler("lifecycle", lifecycleHandler);
        lifecycleRuntime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "lifecycle", RenderHandlerId = "unused", LifecycleHandlerId = "lifecycle" });
        lifecycleRuntime.MountUiComponent("lifecycle-seed", null, "lifecycle", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        UiComponentResult lifecycleResult = await lifecycleRuntime.ProcessUiComponentUpdates();

        UiComponentRuntime errorRuntime = new UiComponentRuntime();
        ThrowingRenderer renderer = new ThrowingRenderer();
        ThrowingLifecycleHandler errorHandler = new ThrowingLifecycleHandler();
        errorRuntime.RegisterUiComponentRenderer("render-leaf", renderer);
        errorRuntime.RegisterUiLifecycleHandler("error-handler", errorHandler);
        errorRuntime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "boundary", RenderHandlerId = "unused", ErrorHandlerId = "error-handler" });
        errorRuntime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "leaf", RenderHandlerId = "render-leaf" });
        UiComponentResult boundary = errorRuntime.MountUiComponent("error-seed", null, "boundary", "boundary", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        errorRuntime.ConfigureUiComponentErrorBoundary(boundary.Context!.ComponentId, Node("fallback"), "reset-1");
        UiComponentResult leaf = errorRuntime.MountUiComponent("error-seed", boundary.Context.ComponentId, "leaf", "leaf", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        errorRuntime.InvalidateUiComponent(leaf.Context!.ComponentId, UiUpdatePriority.Immediate, false);
        UiComponentResult errorResult = await errorRuntime.ProcessUiComponentUpdates();

        Assert.Multiple(() =>
        {
            Assert.That(lifecycleResult.Success, Is.False);
            Assert.That(lifecycleHandler.Notifications.Select(item => item.Stage), Is.EqualTo(new UiLifecycleStage[] { UiLifecycleStage.Create, UiLifecycleStage.Attach, UiLifecycleStage.Activate }));
            Assert.That(lifecycleResult.Diagnostics.Select(item => item.Code), Is.EqualTo(new string[]
            {
                "component.lifecycle-handler-failed",
                "component.lifecycle-handler-failed",
                "component.lifecycle-handler-failed"
            }));
            Assert.That(errorResult.Success, Is.False);
            Assert.That(errorResult.Content!.Text, Is.EqualTo("fallback"));
            Assert.That(errorResult.ErrorBoundary!.BoundaryId, Is.EqualTo(boundary.Context.ComponentId));
            Assert.That(errorResult.Diagnostics.Select(item => item.Code), Is.EqualTo(new string[] { "component.renderer-failed", "component.error-handler-failed" }));
            Assert.That(errorHandler.Notifications.Single().Stage, Is.EqualTo(UiLifecycleStage.Error));
            Assert.That(errorHandler.Notifications.Single().Diagnostic!.Code, Is.EqualTo("component.renderer-failed"));
        });
    }

    [Test]
    public async Task DefersRendererInvalidationToASecondPublicUpdateCycle()
    {
        UiComponentRuntime runtime = new UiComponentRuntime();
        ReentrantRenderer renderer = new ReentrantRenderer(runtime, false);
        runtime.RegisterUiComponentRenderer("render", renderer);
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "item", RenderHandlerId = "render" });
        UiComponentResult mounted = runtime.MountUiComponent("seed", null, "item", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.InvalidateUiComponent(mounted.Context!.ComponentId, UiUpdatePriority.Normal, false);

        UiComponentResult first = await runtime.ProcessUiComponentUpdates();
        ComponentInvalidation deferred = runtime.Scheduler.PendingInvalidations.Single();
        int depthAfterFirst = runtime.Scheduler.UpdateDepth;
        UiComponentResult second = await runtime.ProcessUiComponentUpdates();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.RenderRequests.Single().Generation, Is.EqualTo(1));
            Assert.That(deferred.ComponentId, Is.EqualTo(mounted.Context.ComponentId));
            Assert.That(deferred.Priority, Is.EqualTo(UiUpdatePriority.Immediate));
            Assert.That(depthAfterFirst, Is.EqualTo(1));
            Assert.That(second.Success, Is.True);
            Assert.That(second.RenderRequests.Single().Generation, Is.EqualTo(2));
            Assert.That(second.RenderRequests.Single().Priority, Is.EqualTo(UiUpdatePriority.Immediate));
            Assert.That(renderer.RenderCount, Is.EqualTo(2));
            Assert.That(runtime.Scheduler.PendingInvalidations, Is.Empty);
            Assert.That(runtime.Scheduler.UpdateDepth, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task ReportsMaximumPublicUpdateDepthAndRecoversForLaterWork()
    {
        UiComponentRuntime runtime = new UiComponentRuntime();
        ReentrantRenderer renderer = new ReentrantRenderer(runtime, true);
        runtime.RegisterUiComponentRenderer("render", renderer);
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "loop", RenderHandlerId = "render" });
        UiComponentResult mounted = runtime.MountUiComponent("seed", null, "loop", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.InvalidateUiComponent(mounted.Context!.ComponentId, UiUpdatePriority.Normal, false);

        int cycle = 0;
        while (cycle < ComponentSchedulerCore.MaximumUpdateDepth)
        {
            UiComponentResult processed = await runtime.ProcessUiComponentUpdates();
            Assert.That(processed.Success, Is.True);
            cycle++;
        }

        UiComponentResult exhausted = await runtime.ProcessUiComponentUpdates();
        renderer.ReenterEveryTime = false;
        UiComponentResult recoveredInvalidation = runtime.InvalidateUiComponent(mounted.Context.ComponentId, UiUpdatePriority.Background, false);
        UiComponentResult recovered = await runtime.ProcessUiComponentUpdates();

        Assert.Multiple(() =>
        {
            Assert.That(exhausted.Success, Is.False);
            Assert.That(exhausted.Diagnostics.Single().Code, Is.EqualTo("component.maximum-update-depth"));
            Assert.That(renderer.RenderCount, Is.EqualTo(ComponentSchedulerCore.MaximumUpdateDepth + 1));
            Assert.That(recoveredInvalidation.Success, Is.True);
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.RenderRequests.Single().Generation, Is.EqualTo(ComponentSchedulerCore.MaximumUpdateDepth + 1));
            Assert.That(runtime.Scheduler.UpdateDepth, Is.EqualTo(0));
        });
    }

    [Test]
    public void PropagatesAnUnrecoverableRootRendererFailureFromTheProcessingTask()
    {
        UiComponentRuntime runtime = new UiComponentRuntime();
        runtime.RegisterUiComponentRenderer("render-root", new ThrowingRenderer());
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "root", RenderHandlerId = "render-root" });
        UiComponentResult mounted = runtime.MountUiComponent("seed", null, "root", "root", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.InvalidateUiComponent(mounted.Context!.ComponentId, UiUpdatePriority.Normal, false);

        UiComponentException? failure = Assert.ThrowsAsync<UiComponentException>(() => runtime.ProcessUiComponentUpdates());

        Assert.Multiple(() =>
        {
            Assert.That(failure!.ComponentId, Is.EqualTo(mounted.Context.ComponentId));
            Assert.That(failure.Stage, Is.EqualTo(UiLifecycleStage.BeforeRender));
            Assert.That(failure.Message, Does.Contain("component.renderer-failed"));
            Assert.That(runtime.Core.DrainComponentErrorInvocations(), Is.Empty);
        });
    }

    private static ComponentCore BoundaryCore()
    {
        ComponentCore core = new ComponentCore();
        core.RegisterComponentDefinition(new UiComponentDefinition { TypeId = "boundary", RenderHandlerId = "render-boundary", ErrorHandlerId = "error-handler" });
        core.RegisterComponentDefinition(new UiComponentDefinition { TypeId = "leaf", RenderHandlerId = "render-leaf" });
        return core;
    }

    private static ComponentOperationResult CreateComponent(ComponentCore core, string? parentId, string typeId, string key)
    {
        return core.CreateComponentInstance("seed", parentId, typeId, key, System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
    }

    private static UiDiagnostic Diagnostic(string code, string message)
    {
        return new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = code, Message = message };
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

    private class ThrowingRenderer : IUiComponentRenderer
    {
        public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
        {
            throw new System.InvalidOperationException("renderer failed");
        }
    }

    private class ThrowingLifecycleHandler : IUiLifecycleHandler
    {
        public List<UiLifecycleNotification> Notifications = new List<UiLifecycleNotification>();

        public Task<UiLifecycleHandlerResult> HandleUiComponentLifecycle(UiLifecycleNotification notification)
        {
            Notifications.Add(notification);
            throw new System.InvalidOperationException("handler failed");
        }
    }

    private class ReentrantRenderer : IUiComponentRenderer
    {
        public UiComponentRuntime Runtime;
        public bool ReenterEveryTime;
        public int RenderCount;

        public ReentrantRenderer(UiComponentRuntime runtime, bool reenterEveryTime)
        {
            Runtime = runtime;
            ReenterEveryTime = reenterEveryTime;
        }

        public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
        {
            RenderCount++;
            if (ReenterEveryTime || RenderCount == 1)
            {
                Runtime.InvalidateUiComponent(request.Context.ComponentId, UiUpdatePriority.Immediate, false);
            }

            return Task.FromResult(new UiComponentRenderResult { Content = Node("render-" + RenderCount) });
        }
    }
}
