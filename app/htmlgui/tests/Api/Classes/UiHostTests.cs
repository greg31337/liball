using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class UiHostTests
{
    [Test]
    public void InitializesDetachedModelMountsAndCommitsStateDrivenUpdates()
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiHost host = Host(transport, new UiEventDispatcher(), new UiCommandDispatcher(), new UiRouter());
        UiApplication initial = Application("Before");
        string focusId = initial.Windows[0].Pages[0].ContentView!.ContentRoot!.Identity.StableId;

        UiResult initialized = host.InitializeUiHost(initial);
        initial.Windows[0].Pages[0].ContentView!.ContentRoot!.Children[0].Text = "Caller mutation";
        UiRenderResult mounted = host.MountUiHost("host-session", "host-nonce", 10);
        DomSnapshot first = host.GetUiHostSnapshot()!;
        UiApplication changed = Application("After");
        UiRenderResult updated = host.UpdateUiHost(changed, UiUpdatePriority.UserInteraction, new UiPreservationState { FocusedNodeId = focusId });
        DomSnapshot detached = host.GetUiHostSnapshot()!;
        detached.Roots[0].Name = "mutated";
        DomSnapshot current = host.GetUiHostSnapshot()!;
        UiRenderResult rerendered = host.RenderUiHost();

        Assert.Multiple(() =>
        {
            Assert.That(initialized.Success, Is.True);
            Assert.That(mounted.Diagnostics, Is.Empty);
            Assert.That(mounted.Html, Does.Contain("Before"));
            Assert.That(mounted.Html, Does.Not.Contain("Caller mutation"));
            Assert.That(transport.StartCount, Is.EqualTo(1));
            Assert.That(transport.Messages.Single().Kind, Is.EqualTo(UiBridgeMessageKind.Initialize));
            Assert.That(first.Revision, Is.EqualTo(0));
            Assert.That(updated.Html, Does.Contain("After"));
            Assert.That(updated.UpdateBatch, Is.Not.Null);
            Assert.That(updated.UpdateBatch!.BaseRevision, Is.EqualTo(0));
            Assert.That(updated.UpdateBatch.Revision, Is.EqualTo(1));
            Assert.That(updated.UpdateBatch.Priority, Is.EqualTo(UiUpdatePriority.UserInteraction));
            Assert.That(updated.UpdateBatch.Preservation.FocusedNodeId, Is.EqualTo(focusId));
            Assert.That(updated.UpdateBatch.Mutations, Is.Not.Empty);
            Assert.That(updated.UpdateResult!.Accepted, Is.True);
            Assert.That(current.Revision, Is.EqualTo(1));
            Assert.That(current.Roots[0].Name, Is.Not.EqualTo("mutated"));
            Assert.That(rerendered.Snapshot!.Revision, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task DispatchesEventsCommandsNavigationAndBridgeMessagesThenShutsDown()
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiEventDispatcher events = new UiEventDispatcher();
        IUiCommandDispatcher commands = new UiCommandDispatcher();
        IUiRouter router = new UiRouter();
        IUiHost host = Host(transport, events, commands, router);
        UiApplication application = Application("Interactive");
        string nodeId = application.Windows[0].Pages[0].ContentView!.ContentRoot!.Identity.StableId;
        RecordingUiEventHandler eventHandler = new RecordingUiEventHandler();
        events.RegisterEventHandler(nodeId, new UiEventBinding
        {
            Id = "activate-binding",
            HandlerId = "activate-handler",
            Kind = UiEventKind.Click,
            Phase = UiEventPhase.Target
        }, eventHandler);
        RecordingCommandHandler commandHandler = new RecordingCommandHandler();
        commands.RegisterUiCommand(new UiCommand { Id = "save", Label = "Save", HandlerId = "save-handler" }, commandHandler);

        UiResult initialized = host.InitializeUiHost(application);
        UiRenderResult mounted = host.MountUiHost("host-session", "host-nonce", 10);
        BridgeSession browser = BrowserSession();
        BridgeSessionOperation readyEnvelope = browser.AcceptBridgeInitialize(transport.Messages.Single(), true, 11);
        UiResult ready = host.ReceiveUiHostMessage(readyEnvelope.Envelope, true, 12);
        UiEventDispatchResult dispatched = host.DispatchUiHostEvent(new UiEventData
        {
            Kind = UiEventKind.Click,
            TargetId = nodeId,
            Path = new string[] { nodeId },
            IsTrusted = true
        });
        UiCommandResult commanded = host.DispatchUiHostCommand(new UiCommandInvocation { CommandId = "save" });
        UiNavigationResult navigated = await host.NavigateUiHost(new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = "/next" });
        BridgeMessageFlow browserFlow = new BridgeMessageFlow(browser);
        BridgeMessageFlowResult notification = browserFlow.SendBridgeNotification(UiBridgeMessageKind.Log, Text("browser log"), 13);
        UiResult received = host.ReceiveUiHostMessage(notification.Envelope, true, 14);
        UiResult shutdown = host.ShutdownUiHost(15);
        BridgeSessionOperation browserShutdown = browser.AcceptBridgeShutdown(transport.Messages[^1], true, 16);
        UiResult closed = host.ReceiveUiHostMessage(browserShutdown.Envelope, true, 17);
        UiResult repeatedShutdown = host.ShutdownUiHost(18);

        Assert.Multiple(() =>
        {
            Assert.That(initialized.Success, Is.True);
            Assert.That(mounted.Diagnostics, Is.Empty);
            Assert.That(ready.Success, Is.True);
            Assert.That(dispatched.Success, Is.True);
            Assert.That(dispatched.InvokedHandlerIds, Is.EqualTo(new string[] { "activate-handler" }));
            Assert.That(commanded.Success, Is.True);
            Assert.That(commanded.Invocation!.CommandId, Is.EqualTo("save"));
            Assert.That(navigated.Success, Is.True);
            Assert.That(navigated.Applied, Is.True);
            Assert.That(navigated.CanonicalUrl, Is.EqualTo("/next"));
            Assert.That(received.Success, Is.True);
            Assert.That(transport.Messages[^1].Kind, Is.EqualTo(UiBridgeMessageKind.Shutdown));
            Assert.That(shutdown.Success, Is.True);
            Assert.That(closed.Success, Is.True);
            Assert.That(repeatedShutdown.Success, Is.True);
            Assert.That(transport.StopCount, Is.EqualTo(1));
            Assert.That(host.GetUiHostSnapshot(), Is.Null);
        });
    }

    [Test]
    public void ReturnsLifecycleAndValidationDiagnosticsWithoutReplacingTheMountedState()
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiHost host = Host(transport, new UiEventDispatcher(), new UiCommandDispatcher(), new UiRouter());

        UiRenderResult beforeInitialization = host.RenderUiHost();
        UiResult invalid = host.InitializeUiHost(null);
        UiResult initialized = host.InitializeUiHost(Application("Stable"));
        UiRenderResult mounted = host.MountUiHost("host-session", "host-nonce", 10);
        UiApplication changedIdentity = Application("Different");
        changedIdentity.RootSeed = "different-seed";
        UiRenderResult rejected = host.UpdateUiHost(changedIdentity, UiUpdatePriority.Normal, null);
        UiRenderResult preserved = host.RenderUiHost();

        Assert.Multiple(() =>
        {
            Assert.That(beforeInitialization.Diagnostics.Single().Code, Is.EqualTo("host.not-initialized"));
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostics.Single().Code, Is.EqualTo("model.null-root"));
            Assert.That(initialized.Success, Is.True);
            Assert.That(mounted.Diagnostics, Is.Empty);
            Assert.That(rejected.Diagnostics.Single().Code, Is.EqualTo("host.update-identity-changed"));
            Assert.That(rejected.UpdateBatch, Is.Null);
            Assert.That(preserved.Html, Does.Contain("Stable"));
        });
    }

    private static IUiHost Host(HostRecordingTransport transport, IUiEventDispatcher events, IUiCommandDispatcher commands, IUiRouter router)
    {
        return new UiHost(new UiModelValidator(), new UiRenderer(), new UiUpdateEngine(), events, new UiBindingEngine(), new UiValidationEngine(), commands, new UiComponentRuntime(), router, new UiFocusController(), transport);
    }

    private static UiApplication Application(string text)
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("host-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        root.Children = new UiNode[] { factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "text", text) };
        view.ContentRoot = root;
        page.Title = "Host";
        page.RouteId = "next";
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        window.BridgeOptions = HostOptions();
        application.Windows = new UiWindow[] { window };
        application.Routes = new UiRoute[]
        {
            new UiRoute { Id = "next", Template = "/next", View = view }
        };
        application.RouteIds = new string[] { "next" };
        return application;
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
            RequireMainFrame = true,
            MaximumMessageLength = 4096,
            MaximumPendingRequests = 8,
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

    private static UiValue Text(string text)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = text };
    }
}

public class HostRecordingTransport : IUiMessageTransport
{
    public List<UiBridgeEnvelope> Messages = new List<UiBridgeEnvelope>();
    public int StartCount;
    public int StopCount;

    public UiResult StartUiMessageTransport()
    {
        StartCount++;
        return new UiResult { Success = true };
    }

    public UiResult SendUiMessage(UiBridgeEnvelope message)
    {
        Messages.Add(message);
        return new UiResult { Success = true };
    }

    public UiResult StopUiMessageTransport()
    {
        StopCount++;
        return new UiResult { Success = true };
    }

    public UiTransportCapabilities GetUiMessageTransportCapabilities()
    {
        return new UiTransportCapabilities
        {
            State = UiTransportState.Starting,
            Version = BridgeProtocolVersions.Current,
            SupportsBinary = true,
            Capabilities = new UiCapability[] { new UiCapability { Name = "binary", IsSupported = true, Version = "1" } }
        };
    }
}
