using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class UiHostSchedulerTests
{
    [Test]
    public void RunsOneEventTransactionThroughEveryStageAndSendsTheDomBatch()
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiEventDispatcher events = new UiEventDispatcher();
        IUiBindingEngine bindings = new UiBindingEngine();
        IUiValidationEngine validation = new UiValidationEngine();
        IUiCommandDispatcher commands = new UiCommandDispatcher();
        IUiComponentRuntime components = new UiComponentRuntime();
        IUiRouter router = new UiRouter();
        IUiFocusController focus = new UiFocusController();
        RecordingUiEventHandler eventHandler = new RecordingUiEventHandler();
        RecordingCommandHandler commandHandler = new RecordingCommandHandler();
        commands.RegisterUiCommand(new UiCommand { Id = "save", Label = "Save", HandlerId = "save-handler" }, commandHandler);
        SchedulerComponentRenderer componentRenderer = new SchedulerComponentRenderer();
        components.RegisterUiComponentRenderer("render-card", componentRenderer);
        components.RegisterUiComponentDefinition(new UiComponentDefinition { TypeId = "card", RenderHandlerId = "render-card" });
        UiComponentResult component = components.MountUiComponent("host-seed", null, "card", "card", System.Array.Empty<UiValueEntry>(), System.Array.Empty<UiSlot>(), System.Array.Empty<UiValueEntry>());
        components.InvalidateUiComponent(component.Context!.ComponentId, UiUpdatePriority.UserInteraction, false);
        UiApplication application = Application();
        string inputId = application.Windows[0].Pages[0].ContentView!.ContentRoot!.Children[0].Identity.StableId;
        events.RegisterEventHandler(inputId, new UiEventBinding
        {
            Id = "input-event",
            HandlerId = "input-handler",
            Kind = UiEventKind.Input,
            Phase = UiEventPhase.Target
        }, eventHandler);
        IUiHost host = new UiHost(new UiModelValidator(), new UiRenderer(), new UiUpdateEngine(), events, bindings, validation, commands, components, router, focus, transport);

        UiResult initialized = host.InitializeUiHost(application);
        UiRenderResult mounted = host.MountUiHost("scheduler-session", "scheduler-nonce", 100);
        Assert.That(initialized.Success, Is.True, initialized.Diagnostics.Length == 0 ? "initialization failed" : initialized.Diagnostics[0].Code);
        Assert.That(mounted.Snapshot, Is.Not.Null, mounted.Diagnostics.Length == 0 ? "mount failed" : mounted.Diagnostics[0].Code);
        BridgeSession browser = BrowserSession();
        BridgeSessionOperation browserReady = browser.AcceptBridgeInitialize(transport.Messages.Single(), true, 101);
        UiResult ready = host.ReceiveUiHostMessage(browserReady.Envelope, true, 102);
        UiEventDispatchResult result = host.DispatchUiHostEvent(new UiEventData
        {
            Kind = UiEventKind.Input,
            TargetId = inputId,
            Path = new string[] { inputId },
            IsTrusted = true,
            Input = new UiInputData { Value = Text("new name") }
        });
        Assert.That(result.BridgeResult, Is.Not.Null);
        Assert.That(result.BridgeResult!.Success, Is.True, result.BridgeResult.Diagnostics.Length == 0 ? "bridge failed" : result.BridgeResult.Diagnostics[0].Code);
        UiBridgeEnvelope patch = transport.Messages.Last();
        UiValueEntry[] patchFields = patch.Payload!.ObjectValue!;
        BridgeMessageFlow browserFlow = new BridgeMessageFlow(browser);
        BridgeMessageFlowResult browserPatch = browserFlow.ReceiveBridgeMessage(patch, true, 104);
        UiResult acknowledged = host.ReceiveUiHostMessage(browserPatch.Envelope, true, 105);

        Assert.Multiple(() =>
        {
            Assert.That(initialized.Success, Is.True);
            Assert.That(mounted.Html, Does.Contain("old name"));
            Assert.That(ready.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.TransactionStages, Is.EqualTo(new string[]
            {
                "event", "state", "bindings", "validation", "commands", "components", "route", "focus", "render", "bridge"
            }));
            Assert.That(result.InvokedHandlerIds, Is.EqualTo(new string[] { "input-handler" }));
            Assert.That(result.BindingResult!.Applied, Is.True);
            Assert.That(bindings.GetBindingStateStore().ReadState(application.Id, "name").Entry!.Value.StringValue, Is.EqualTo("new name"));
            Assert.That(result.ValidationResult!.Success, Is.True);
            Assert.That(result.ValidationResult.IsValid, Is.True);
            Assert.That(result.CommandResult!.Success, Is.True);
            Assert.That(commandHandler.Executions.Single().CommandId, Is.EqualTo("save"));
            Assert.That(result.ComponentResult!.Applied, Is.True);
            Assert.That(componentRenderer.RenderCount, Is.EqualTo(1));
            Assert.That(result.NavigationResult!.Applied, Is.True);
            Assert.That(result.NavigationResult.CanonicalUrl, Is.EqualTo("/next"));
            Assert.That(result.FocusResult!.Success, Is.True);
            Assert.That(result.FocusResult.State.FocusedNodeId, Is.EqualTo(inputId));
            Assert.That(result.RenderResult!.Html, Does.Contain("new name"));
            Assert.That(result.RenderResult.UpdateBatch, Is.Not.Null);
            Assert.That(result.RenderResult.UpdateBatch!.Mutations, Is.Not.Empty);
            Assert.That(result.BridgeResult!.Success, Is.True);
            Assert.That(acknowledged.Success, Is.True);
            Assert.That(patch.Kind, Is.EqualTo(UiBridgeMessageKind.DomPatch));
            Assert.That(patchFields.Single(item => item.Key == "baseRevision").Value.IntegerValue, Is.EqualTo(0));
            Assert.That(patchFields.Single(item => item.Key == "revision").Value.IntegerValue, Is.EqualTo(1));
            Assert.That(patchFields.Single(item => item.Key == "mutations").Value.ArrayValue, Is.Not.Empty);
            Assert.That(host.GetUiHostSnapshot()!.Revision, Is.EqualTo(1));
        });
    }

    private static UiApplication Application()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("host-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        UiNode input = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "input", null, HtmlElements.Input);
        input.Kind = UiNodeKind.Control;
        input.Input = factory.CreateInput(UiInputKind.Text, "name");
        input.Input.Value = Text("old name");
        input.Accessibility = new UiAccessibility { Label = "Name" };
        UiNode output = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "output", "old name");
        input.Bindings = new UiBinding[]
        {
            factory.CreateBinding("name-input", "name", string.Empty, input.Identity.StableId, "value", UiBindingMode.TwoWay, UiBindingTrigger.Input),
            factory.CreateBinding("name-output", "name", string.Empty, output.Identity.StableId, "text", UiBindingMode.OneWay, UiBindingTrigger.Immediate)
        };
        root.Children = new UiNode[] { input, output };
        root.Form = new UiForm
        {
            Key = "profile",
            Fields = new UiField[]
            {
                new UiField
                {
                    Key = "name",
                    Name = "name",
                    Label = "Name",
                    Value = Text("old name"),
                    Input = factory.CreateInput(UiInputKind.Text, "name"),
                    Rules = new UiValidationRule[]
                    {
                        new UiValidationRule { Key = "required", Kind = "required", Trigger = UiValidationTrigger.Input, Message = "Name is required" }
                    }
                }
            }
        };
        view.ContentRoot = root;
        view.FocusRequest = new UiFocusRequest { Operation = UiFocusOperation.Focus, TargetId = input.Identity.StableId, ScopeId = "main", Reveal = true };
        page.Title = "Scheduler";
        page.RouteId = "next";
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        window.BridgeOptions = HostOptions();
        window.NavigationRequest = new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = "/next" };
        window.FocusScopes = new UiFocusScope[]
        {
            new UiFocusScope { Id = "main", OwnerNodeId = root.Identity.StableId, TargetIds = new string[] { input.Identity.StableId }, InitialFocusId = input.Identity.StableId }
        };
        window.FocusTargets = new UiFocusTarget[]
        {
            new UiFocusTarget { Id = input.Identity.StableId, ScopeId = "main", IsFocusable = true, IsEnabled = true, IsVisible = true }
        };
        application.Windows = new UiWindow[] { window };
        application.StateEntries = new UiStateEntry[]
        {
            new UiStateEntry { Id = "name", Value = Text("old name"), Origin = "model" }
        };
        application.Commands = new UiCommand[]
        {
            new UiCommand { Id = "save", Label = "Save", HandlerId = "save-handler", Invocation = new UiCommandInvocation { CommandId = "save" } }
        };
        application.CommandIds = new string[] { "save" };
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
            MaximumMessageLength = 65536,
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

public class SchedulerComponentRenderer : IUiComponentRenderer
{
    public int RenderCount;

    public Task<UiComponentRenderResult> RenderUiComponent(UiComponentRenderRequest request)
    {
        RenderCount++;
        return Task.FromResult(new UiComponentRenderResult
        {
            Success = true,
            Content = new UiNode { Kind = UiNodeKind.Text, Text = request.Context.Key }
        });
    }
}
