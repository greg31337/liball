using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class UiHostRecoveryTests
{
    [Test]
    public void RejectsPendingBatchResynchronizesReconnectsAndCleansOwnedRegistrations()
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiBindingEngine bindings = new UiBindingEngine();
        IUiValidationEngine validation = new UiValidationEngine();
        IUiRouter router = new UiRouter();
        IUiFocusController focus = new UiFocusController();
        IUiEventDispatcher events = new UiEventDispatcher();
        UiApplication application = Application(false);
        string inputId = InputId(application);
        events.RegisterEventHandler(inputId, EventBinding(), new RecordingUiEventHandler());
        IUiHost host = Host(transport, events, bindings, validation, new UiCommandDispatcher(), new UiComponentRuntime(), router, focus);
        Assert.That(host.InitializeUiHost(application).Success, Is.True);
        Assert.That(host.MountUiHost("session-1", "nonce-1", 100).Snapshot, Is.Not.Null);
        BridgeSession browser = CompleteReady(host, transport, 101, 102);

        UiEventDispatchResult dispatched = host.DispatchUiHostEvent(InputEvent(inputId, "changed"));
        UiUpdateBatch batch = dispatched.RenderResult!.UpdateBatch!;
        UiUpdateResult rejected = host.RejectUiHostUpdate(batch.Id, batch.Revision, 110);
        UiBridgeEnvelope resync = transport.Messages.Last();
        UiResult reconnecting = host.RecoverUiHostTransport("session-2", "nonce-2", 200);
        UiBridgeEnvelope reconnectInitialize = transport.Messages.Last();
        BridgeSession replacementBrowser = BrowserSession();
        BridgeSessionOperation replacementReady = replacementBrowser.AcceptBridgeInitialize(reconnectInitialize, true, 201);
        UiResult recovered = host.ReceiveUiHostMessage(replacementReady.Envelope, true, 202);
        UiBridgeEnvelope recoveredSnapshot = transport.Messages.Last();
        UiResult shutdown = host.ShutdownUiHost(210);

        Assert.Multiple(() =>
        {
            Assert.That(browser.State, Is.EqualTo(UiTransportState.Ready));
            Assert.That(dispatched.Success, Is.True);
            Assert.That(host.GetUiHostSnapshot(), Is.Null);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RequiresResync, Is.True);
            Assert.That(rejected.Snapshot!.Revision, Is.EqualTo(0));
            Assert.That(resync.Kind, Is.EqualTo(UiBridgeMessageKind.RenderSnapshot));
            Assert.That(resync.Payload!.ObjectValue!.Single(item => item.Key == "revision").Value.IntegerValue, Is.EqualTo(0));
            Assert.That(reconnecting.Success, Is.True, string.Join(",", reconnecting.Diagnostics.Select(item => item.Code)));
            Assert.That(reconnectInitialize.Kind, Is.EqualTo(UiBridgeMessageKind.Initialize));
            Assert.That(recovered.Success, Is.True, string.Join(",", recovered.Diagnostics.Select(item => item.Code)));
            Assert.That(recoveredSnapshot.Kind, Is.EqualTo(UiBridgeMessageKind.RenderSnapshot));
            Assert.That(shutdown.Success, Is.True);
            Assert.That(bindings.GetBindingStateStore().ReadState(application.Id, "value").Success, Is.False);
            Assert.That(bindings.EvaluateBinding(application.Id, "input-binding").Success, Is.False);
            Assert.That(validation.ReadValidationForm("profile").Success, Is.False);
            Assert.That(router.ResolveUiRoute("/next").Status, Is.EqualTo(UiNavigationStatus.NotFound));
            Assert.That(focus.ApplyFocusRequest(new UiFocusState(), new UiFocusRequest { Operation = UiFocusOperation.Focus, ScopeId = "main" }).Success, Is.False);
            Assert.That(host.GetUiHostDiagnostics().Select(item => item.Code), Does.Contain("update.batch-rejected"));
        });
    }

    [Test]
    public void RollsBackFailedTransactionsAndRoutesDetachedDiagnostics()
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiBindingEngine bindings = new UiBindingEngine();
        IUiEventDispatcher events = new UiEventDispatcher();
        UiApplication application = Application(true);
        string inputId = InputId(application);
        events.RegisterEventHandler(inputId, EventBinding(), new RecordingUiEventHandler());
        IUiHost host = Host(transport, events, bindings, new UiValidationEngine(), new UiCommandDispatcher(), new UiComponentRuntime(), new UiRouter(), new UiFocusController());
        host.InitializeUiHost(application);
        host.MountUiHost("session-1", "nonce-1", 100);
        CompleteReady(host, transport, 101, 102);

        UiEventDispatchResult failed = host.DispatchUiHostEvent(InputEvent(inputId, "should roll back"));
        UiDiagnostic[] first = host.GetUiHostDiagnostics();
        first[0].Code = "mutated";
        UiDiagnostic[] second = host.GetUiHostDiagnostics();

        Assert.Multiple(() =>
        {
            Assert.That(failed.Success, Is.False);
            Assert.That(failed.TransactionStages, Is.EqualTo(new string[] { "event", "state", "bindings", "validation", "commands" }));
            Assert.That(failed.RenderResult, Is.Null);
            Assert.That(failed.BridgeResult, Is.Null);
            Assert.That(bindings.GetBindingStateStore().ReadState(application.Id, "value").Entry!.Value.StringValue, Is.EqualTo("initial"));
            Assert.That(host.GetUiHostSnapshot()!.Revision, Is.EqualTo(0));
            Assert.That(second.Select(item => item.Code), Does.Contain("command.unknown"));
            Assert.That(second[0].Code, Is.Not.EqualTo("mutated"));
        });
    }

    [Test]
    public void ContinuesAfterComponentBoundaryRecoveryAndCommitsOnAcknowledgement()
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiBindingEngine bindings = new UiBindingEngine();
        IUiEventDispatcher events = new UiEventDispatcher();
        IUiComponentRuntime components = BoundaryRuntime();
        UiApplication application = Application(false);
        string inputId = InputId(application);
        events.RegisterEventHandler(inputId, EventBinding(), new RecordingUiEventHandler());
        IUiHost host = Host(transport, events, bindings, new UiValidationEngine(), new UiCommandDispatcher(), components, new UiRouter(), new UiFocusController());
        host.InitializeUiHost(application);
        host.MountUiHost("session-1", "nonce-1", 100);
        BridgeSession browser = CompleteReady(host, transport, 101, 102);

        UiEventDispatchResult recovered = host.DispatchUiHostEvent(InputEvent(inputId, "boundary value"));
        UiBridgeEnvelope patch = transport.Messages.Last();
        BridgeMessageFlowResult browserPatch = new BridgeMessageFlow(browser).ReceiveBridgeMessage(patch, true, 104);
        UiResult acknowledged = host.ReceiveUiHostMessage(browserPatch.Envelope, true, 105);

        Assert.Multiple(() =>
        {
            Assert.That(recovered.Success, Is.True);
            Assert.That(recovered.ComponentResult!.Success, Is.False);
            Assert.That(recovered.ComponentResult.ErrorBoundary!.HasError, Is.True);
            Assert.That(recovered.RecoveredByComponentBoundary, Is.True);
            Assert.That(recovered.TransactionStages.Last(), Is.EqualTo("bridge"));
            Assert.That(recovered.BridgeResult!.Success, Is.True);
            Assert.That(acknowledged.Success, Is.True);
            Assert.That(host.GetUiHostSnapshot()!.Revision, Is.EqualTo(1));
            Assert.That(host.GetUiHostDiagnostics().Select(item => item.Code), Does.Contain("component.renderer-rejected"));
        });
    }

    private static IUiHost Host(HostRecordingTransport transport, IUiEventDispatcher events, IUiBindingEngine bindings, IUiValidationEngine validation, IUiCommandDispatcher commands, IUiComponentRuntime components, IUiRouter router, IUiFocusController focus)
    {
        return new UiHost(new UiModelValidator(), new UiRenderer(), new UiUpdateEngine(), events, bindings, validation, commands, components, router, focus, transport);
    }

    private static BridgeSession CompleteReady(IUiHost host, HostRecordingTransport transport, long browserMilliseconds, long hostMilliseconds)
    {
        BridgeSession browser = BrowserSession();
        BridgeSessionOperation ready = browser.AcceptBridgeInitialize(transport.Messages.Last(), true, browserMilliseconds);
        Assert.That(host.ReceiveUiHostMessage(ready.Envelope, true, hostMilliseconds).Success, Is.True);
        return browser;
    }

    private static IUiComponentRuntime BoundaryRuntime()
    {
        IUiComponentRuntime runtime = new UiComponentRuntime();
        runtime.RegisterUiComponentRenderer("fail", new RecoveryFailingRenderer());
        runtime.RegisterUiLifecycleHandler("boundary-errors", new RecoveryLifecycleHandler());
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "boundary", RenderHandlerId = "unused", ErrorHandlerId = "boundary-errors" });
        runtime.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "leaf", RenderHandlerId = "fail" });
        UiComponentResult boundary = runtime.MountUiComponent("host-seed", null, "boundary", "boundary", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.ConfigureUiComponentErrorBoundary(boundary.Context!.ComponentId, new UiNode { Kind = UiNodeKind.Text, Text = "fallback" }, "reset");
        UiComponentResult leaf = runtime.MountUiComponent("host-seed", boundary.Context.ComponentId, "leaf", "leaf", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        runtime.InvalidateUiComponent(leaf.Context!.ComponentId, UiUpdatePriority.UserInteraction, false);
        return runtime;
    }

    private static UiApplication Application(bool unknownCommand)
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("host-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        UiNode input = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "input", null, HtmlElements.Input);
        input.Kind = UiNodeKind.Control;
        input.Input = factory.CreateInput(UiInputKind.Text, "value");
        input.Input.Value = Text("initial");
        input.Accessibility = new UiAccessibility { Label = "Value" };
        UiNode output = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "output", "initial");
        input.Bindings = new UiBinding[]
        {
            factory.CreateBinding("input-binding", "value", string.Empty, input.Identity.StableId, "value", UiBindingMode.TwoWay, UiBindingTrigger.Input),
            factory.CreateBinding("output-binding", "value", string.Empty, output.Identity.StableId, "text", UiBindingMode.OneWay, UiBindingTrigger.Immediate)
        };
        root.Children = new UiNode[] { input, output };
        root.Form = new UiForm
        {
            Key = "profile",
            Fields = new UiField[]
            {
                new UiField { Key = "value", Name = "value", Label = "Value", Value = Text("initial"), Input = factory.CreateInput(UiInputKind.Text, "value") }
            }
        };
        view.ContentRoot = root;
        page.Title = "Recovery";
        page.RouteId = "next";
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        window.BridgeOptions = HostOptions();
        window.FocusScopes = new UiFocusScope[]
        {
            new UiFocusScope { Id = "main", OwnerNodeId = root.Identity.StableId, TargetIds = new string[] { input.Identity.StableId } }
        };
        window.FocusTargets = new UiFocusTarget[]
        {
            new UiFocusTarget { Id = input.Identity.StableId, ScopeId = "main" }
        };
        application.Windows = new UiWindow[] { window };
        application.StateEntries = new UiStateEntry[] { new UiStateEntry { Id = "value", Value = Text("initial"), Origin = "model" } };
        application.Routes = new UiRoute[] { new UiRoute { Id = "next", Template = "/next", View = view } };
        application.RouteIds = new string[] { "next" };
        if (unknownCommand)
        {
            application.Commands = new UiCommand[]
            {
                new UiCommand { Id = "missing", Label = "Missing", HandlerId = "missing", Invocation = new UiCommandInvocation { CommandId = "missing" } }
            };
            application.CommandIds = new string[] { "missing" };
        }

        return application;
    }

    private static string InputId(UiApplication application)
    {
        return application.Windows[0].Pages[0].ContentView!.ContentRoot!.Children[0].Identity.StableId;
    }

    private static UiEventBinding EventBinding()
    {
        return new UiEventBinding { Id = "input-event", HandlerId = "input-handler", Kind = UiEventKind.Input, Phase = UiEventPhase.Target };
    }

    private static UiEventData InputEvent(string inputId, string value)
    {
        return new UiEventData
        {
            Kind = UiEventKind.Input,
            TargetId = inputId,
            Path = new string[] { inputId },
            Input = new UiInputData { Value = Text(value) },
            IsTrusted = true
        };
    }

    private static BridgeSession BrowserSession()
    {
        UiBridgeOptions options = HostOptions();
        options.Origin = BridgeProtocolNames.HostOrigin;
        return new BridgeSession(options, Capabilities());
    }

    private static UiBridgeOptions HostOptions()
    {
        return new UiBridgeOptions
        {
            Version = BridgeProtocolVersions.Current,
            Origin = BridgeProtocolNames.BrowserOrigin,
            MaximumMessageLength = 1048576,
            MaximumPendingRequests = 16,
            TimeoutMilliseconds = 1000
        };
    }

    private static UiTransportCapabilities Capabilities()
    {
        return new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current,
            SupportsBinary = true,
            Capabilities = new UiCapability[] { new UiCapability { Name = "binary", IsSupported = true, Version = "1" } }
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}

public class RecoveryFailingRenderer : IUiComponentRenderer
{
    public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
    {
        return Task.FromResult(new UiComponentRenderResult
        {
            Success = false,
            Diagnostics = new UiDiagnostic[]
            {
                new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = "component.renderer-rejected", Message = "Rejected", Location = "/component" }
            }
        });
    }
}

public class RecoveryLifecycleHandler : IUiLifecycleHandler
{
    public Task<UiLifecycleHandlerResult> HandleUiComponentLifecycle(UiLifecycleNotification notification)
    {
        return Task.FromResult(new UiLifecycleHandlerResult { Success = true });
    }
}
