using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class InteractionEndToEndTests
{
    [Test]
    public void FormValidationStateRoutingAndFocusCompleteOneBridgeRoundTrip()
    {
        UiApplication application = FormApplication();
        UiNode input = application.Windows[0].Pages[0].ContentView!.ContentRoot!.Children[0];
        InteractionHarness harness = Start(application, input.Identity.StableId, UiEventKind.Input, new UiCommandDispatcher());

        UiEventDispatchResult invalid = DispatchAndAcknowledge(harness, new UiEventData
        {
            Kind = UiEventKind.Input,
            TargetId = input.Identity.StableId,
            Path = new string[] { input.Identity.StableId },
            IsTrusted = true,
            Input = new UiInputData { Value = Text(string.Empty), InputType = "deleteContentBackward" }
        });
        UiEventDispatchResult result = DispatchAndAcknowledge(harness, new UiEventData
        {
            Kind = UiEventKind.Input,
            TargetId = input.Identity.StableId,
            Path = new string[] { input.Identity.StableId },
            IsTrusted = true,
            Input = new UiInputData { Value = Text("after"), InputType = "insertText" }
        });

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.True);
            Assert.That(invalid.ValidationResult!.IsValid, Is.False);
            Assert.That(invalid.ValidationResult.Report!.FirstInvalidFieldKey, Is.EqualTo("name"));
            Assert.That(invalid.ValidationResult.Report.Issues.Single().Message, Is.EqualTo("Name is required"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.BindingResult!.Applied, Is.True);
            Assert.That(result.ValidationResult!.Success, Is.True);
            Assert.That(result.ValidationResult.IsValid, Is.True);
            Assert.That(result.ValidationResult.CanSubmit, Is.True);
            Assert.That(harness.Bindings.GetBindingStateStore().ReadState(application.Id, "name").Entry!.Value.StringValue, Is.EqualTo("after"));
            Assert.That(invalid.NavigationResult!.Applied, Is.True);
            Assert.That(invalid.NavigationResult.CanonicalUrl, Is.EqualTo("/profile"));
            Assert.That(result.RenderResult!.Html, Does.Contain("after"));
            Assert.That(invalid.FocusResult!.State.FocusedNodeId, Is.EqualTo(FocusTargetId(application)));
            Assert.That(result.BridgeResult!.Success, Is.True);
            Assert.That(harness.LastPatch.Kind, Is.EqualTo(UiBridgeMessageKind.DomPatch));
            Assert.That(harness.LastPatch.Payload!.ObjectValue!.Single(item => item.Key == "mutations").Value.ArrayValue, Is.Not.Empty);
            Assert.That(harness.Host.GetUiHostSnapshot()!.Revision, Is.EqualTo(2));
        });
    }

    [Test]
    public void CommandMenuToolbarTabsAndDialogCompleteOneBridgeRoundTrip()
    {
        UiApplication application = CommandSurfaceApplication();
        UiNode commandButton = application.Windows[0].Pages[0].ContentView!.ContentRoot!.Children[4];
        RecordingCommandHandler handler = new RecordingCommandHandler();
        handler.Status = new UiCommandStatus { IsEnabled = true, IsVisible = true };
        handler.Result = new UiCommandHandlerResult { Success = true, HasCheckState = true, CheckState = UiCommandCheckState.Checked };
        IUiCommandDispatcher commands = new UiCommandDispatcher();
        commands.RegisterUiCommand(new UiCommand { Id = "save", Label = "Save", HandlerId = "save-handler" }, handler);
        InteractionHarness harness = Start(application, commandButton.Identity.StableId, UiEventKind.Click, commands);

        Assert.Multiple(() =>
        {
            Assert.That(harness.Mounted.Html, Does.Contain("role=\"menubar\""));
            Assert.That(harness.Mounted.Html, Does.Contain("role=\"toolbar\""));
            Assert.That(harness.Mounted.Html, Does.Contain("role=\"tablist\""));
            Assert.That(harness.Mounted.Html, Does.Contain("<dialog"));
            Assert.That(harness.Mounted.Html, Does.Contain("aria-modal=\"true\""));
            Assert.That(harness.Mounted.Html, Does.Contain("General settings"));
            Assert.That(harness.Mounted.Html, Does.Contain("Confirm save"));
        });

        UiEventDispatchResult result = DispatchAndAcknowledge(harness, new UiEventData
        {
            Kind = UiEventKind.Click,
            TargetId = commandButton.Identity.StableId,
            Path = new string[] { commandButton.Identity.StableId },
            IsTrusted = true,
            Pointer = new UiPointerData { PointerType = "mouse", PointerId = 1 }
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.CommandResult!.Success, Is.True);
            Assert.That(result.CommandResult.Invocation!.CommandId, Is.EqualTo("save"));
            Assert.That(result.CommandResult.HandlerResult!.CheckState, Is.EqualTo(UiCommandCheckState.Checked));
            Assert.That(handler.Executions, Has.Count.EqualTo(1));
            Assert.That(result.RenderResult!.Html, Does.Contain("aria-pressed=\"true\""));
            Assert.That(result.RenderResult.Html, Does.Contain("data-command-id=\"save\""));
            Assert.That(harness.LastPatch.Kind, Is.EqualTo(UiBridgeMessageKind.DomPatch));
            Assert.That(harness.Host.GetUiHostSnapshot()!.Revision, Is.EqualTo(1));
        });
    }

    [Test]
    public void ListGridAndTreeSelectionAndEditingSurviveBoundBridgeRoundTrip()
    {
        UiApplication application = CollectionApplication();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        UiNode input = root.Children[0];
        InteractionHarness harness = Start(application, input.Identity.StableId, UiEventKind.Input, new UiCommandDispatcher());

        AssertInteractionSurfaces(harness.Mounted.Html);
        UiEventDispatchResult result = DispatchAndAcknowledge(harness, new UiEventData
        {
            Kind = UiEventKind.Input,
            TargetId = input.Identity.StableId,
            Path = new string[] { input.Identity.StableId },
            IsTrusted = true,
            Input = new UiInputData { Value = Text("after") }
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BindingResult!.Applied, Is.True);
            Assert.That(harness.Bindings.GetBindingStateStore().ReadState(application.Id, "query").Entry!.Value.StringValue, Is.EqualTo("after"));
            Assert.That(result.RenderResult!.Html, Does.Contain("after"));
            Assert.That(result.RenderResult.UpdateBatch!.Mutations, Is.Not.Empty);
            Assert.That(harness.LastPatch.Kind, Is.EqualTo(UiBridgeMessageKind.DomPatch));
            Assert.That(harness.Host.GetUiHostSnapshot()!.Revision, Is.EqualTo(1));
        });
        AssertInteractionSurfaces(result.RenderResult!.Html);
    }

    private static InteractionHarness Start(UiApplication application, string targetId, UiEventKind kind, IUiCommandDispatcher commands)
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiEventDispatcher events = new UiEventDispatcher();
        IUiBindingEngine bindings = new UiBindingEngine();
        events.RegisterEventHandler(targetId, new UiEventBinding
        {
            Id = "interaction-event",
            HandlerId = "interaction-handler",
            Kind = kind,
            Phase = UiEventPhase.Target
        }, new RecordingUiEventHandler());
        IUiHost host = new UiHost(
            new UiModelValidator(),
            new UiRenderer(),
            new UiUpdateEngine(),
            events,
            bindings,
            new UiValidationEngine(),
            commands,
            new UiComponentRuntime(),
            new UiRouter(),
            new UiFocusController(),
            transport);
        UiResult initialized = host.InitializeUiHost(application);
        UiRenderResult mounted = host.MountUiHost("interaction-session", "interaction-nonce", 100);
        Assert.That(initialized.Success, Is.True, DiagnosticCodes(initialized.Diagnostics));
        Assert.That(mounted.Snapshot, Is.Not.Null, DiagnosticCodes(mounted.Diagnostics));
        BridgeSession browser = BrowserSession();
        BridgeSessionOperation ready = browser.AcceptBridgeInitialize(transport.Messages.Single(), true, 101);
        Assert.That(host.ReceiveUiHostMessage(ready.Envelope, true, 102).Success, Is.True);
        return new InteractionHarness(host, transport, browser, bindings, mounted);
    }

    private static UiEventDispatchResult DispatchAndAcknowledge(InteractionHarness harness, UiEventData eventData)
    {
        UiEventDispatchResult result = harness.Host.DispatchUiHostEvent(eventData);
        Assert.That(result.Success, Is.True, DispatchDiagnostics(result));
        Assert.That(result.BridgeResult, Is.Not.Null);
        Assert.That(result.BridgeResult!.Success, Is.True, DiagnosticCodes(result.BridgeResult.Diagnostics));
        UiBridgeEnvelope patch = harness.Transport.Messages.Last();
        BridgeMessageFlowResult browserResult = harness.BrowserFlow.ReceiveBridgeMessage(patch, true, 104 + harness.RoundTripCount * 10);
        Assert.That(browserResult.Success, Is.True, browserResult.Code);
        Assert.That(browserResult.Envelope, Is.Not.Null);
        UiResult acknowledged = harness.Host.ReceiveUiHostMessage(browserResult.Envelope, true, 105 + harness.RoundTripCount * 10);
        Assert.That(acknowledged.Success, Is.True, DiagnosticCodes(acknowledged.Diagnostics));
        harness.LastPatch = patch;
        harness.RoundTripCount++;
        return result;
    }

    private static UiApplication FormApplication()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("form-interaction", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage formPage = factory.CreatePage(application.RootSeed, window.Id, "form-page");
        UiView formView = factory.CreateView(application.RootSeed, formPage.Id, "form-view");
        UiNode formRoot = factory.CreateElementNode(application.RootSeed, formView.Id, "form-root", null, HtmlElements.Main);
        UiNode input = factory.CreateElementNode(application.RootSeed, formRoot.Identity.StableId, "name", null, HtmlElements.Input);
        input.Kind = UiNodeKind.Control;
        input.Input = factory.CreateInput(UiInputKind.Text, "name");
        input.Input.Value = Text("before");
        input.Accessibility = new UiAccessibility { Label = "Name" };
        UiNode output = factory.CreateTextNode(application.RootSeed, formRoot.Identity.StableId, "output", "before");
        input.Bindings = new UiBinding[]
        {
            factory.CreateBinding("name-input", "name", string.Empty, input.Identity.StableId, "value", UiBindingMode.TwoWay, UiBindingTrigger.Input),
            factory.CreateBinding("name-output", "name", string.Empty, output.Identity.StableId, "text", UiBindingMode.OneWay, UiBindingTrigger.Immediate)
        };
        formRoot.Children = new UiNode[] { input, output };
        formRoot.Form = new UiForm
        {
            Key = "profile",
            Fields = new UiField[]
            {
                new UiField
                {
                    Key = "name",
                    Name = "name",
                    Label = "Name",
                    Value = Text("before"),
                    Input = factory.CreateInput(UiInputKind.Text, "name"),
                    Rules = new UiValidationRule[]
                    {
                        new UiValidationRule { Key = "required", Kind = "required", Trigger = UiValidationTrigger.Input, Message = "Name is required" }
                    }
                }
            }
        };
        formView.ContentRoot = formRoot;
        formPage.Title = "Profile";
        formPage.ContentView = formView;
        formView.FocusRequest = new UiFocusRequest { Operation = UiFocusOperation.Focus, TargetId = input.Identity.StableId, ScopeId = "form-focus", Reveal = true };
        formPage.RouteId = "profile";
        window.Pages = new UiPage[] { formPage };
        window.CurrentPageKey = formPage.Key;
        window.NavigationRequest = new UiNavigationRequest { Kind = UiNavigationKind.Push, Url = "/profile" };
        window.FocusScopes = new UiFocusScope[]
        {
            new UiFocusScope { Id = "form-focus", OwnerNodeId = formRoot.Identity.StableId, TargetIds = new string[] { input.Identity.StableId }, InitialFocusId = input.Identity.StableId }
        };
        window.FocusTargets = new UiFocusTarget[]
        {
            new UiFocusTarget { Id = input.Identity.StableId, ScopeId = "form-focus", IsFocusable = true, IsEnabled = true, IsVisible = true }
        };
        window.BridgeOptions = HostOptions();
        application.Windows = new UiWindow[] { window };
        application.StateEntries = new UiStateEntry[] { new UiStateEntry { Id = "name", Value = Text("before"), Origin = "model" } };
        application.Routes = new UiRoute[] { new UiRoute { Id = "profile", Template = "/profile", View = formView } };
        application.RouteIds = new string[] { "profile" };
        return application;
    }

    private static string FocusTargetId(UiApplication application)
    {
        return application.Windows[0].Pages[0].ContentView!.ContentRoot!.Children[0].Identity.StableId;
    }

    private static UiApplication CommandSurfaceApplication()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("command-interaction", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        UiNode menu = Control(factory, application, root, "menu", "menu-bar");
        menu.Menu = new UiMenu
        {
            Key = "application-menu",
            Label = "Application",
            Orientation = UiOrientation.Horizontal,
            Items = new UiMenuItem[] { new UiMenuItem { Kind = UiMenuItemKind.Command, Key = "save", Label = "Save", CommandId = "save" } }
        };
        UiNode toolbar = Control(factory, application, root, "toolbar", "toolbar");
        toolbar.Toolbar = new UiToolbar
        {
            Key = "main-tools",
            Label = "Main tools",
            Tools = new UiTool[] { new UiTool { Key = "save", Kind = "command", Label = "Save", CommandId = "save" } }
        };
        UiNode tabs = Control(factory, application, root, "tabs", "tabs");
        tabs.Tabs = new UiTabs
        {
            Key = "settings",
            ActiveTabKey = "general",
            Tabs = new UiTab[]
            {
                new UiTab { Key = "general", Label = "General", IsSelected = true, Panel = factory.CreateTextNode(application.RootSeed, tabs.Identity.StableId, "general-panel", "General settings") },
                new UiTab { Key = "advanced", Label = "Advanced", Panel = factory.CreateTextNode(application.RootSeed, tabs.Identity.StableId, "advanced-panel", "Advanced settings") }
            }
        };
        UiNode dialog = Control(factory, application, root, "dialog", "dialog");
        dialog.Dialog = new UiDialog
        {
            Kind = UiDialogKind.Modal,
            Key = "confirmation",
            Title = "Confirm save",
            Content = new UiNode[] { factory.CreateTextNode(application.RootSeed, dialog.Identity.StableId, "dialog-content", "Save changes?") },
            State = new UiOverlayState { IsOpen = true, IsModal = true, HasBackdrop = true, TrapFocus = true }
        };
        UiNode commandButton = Control(factory, application, root, "command-button", "button");
        commandButton.Button = new UiButton { Kind = UiButtonKind.Toggle, Label = "Save", CommandId = "save" };
        root.Children = new UiNode[] { menu, toolbar, tabs, dialog, commandButton };
        view.ContentRoot = root;
        page.Title = "Commands";
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        window.BridgeOptions = HostOptions();
        application.Windows = new UiWindow[] { window };
        application.Commands = new UiCommand[] { new UiCommand { Id = "save", Label = "Save", HandlerId = "save-handler" } };
        application.CommandIds = new string[] { "save" };
        return application;
    }

    private static UiApplication CollectionApplication()
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("collection-interaction", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        UiNode input = Control(factory, application, root, "query", "input");
        input.Input = factory.CreateInput(UiInputKind.Text, "query");
        input.Input.Value = Text("before");
        input.Accessibility = new UiAccessibility { Label = "Query" };
        UiNode output = factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "query-output", "before");
        input.Bindings = new UiBinding[]
        {
            factory.CreateBinding("query-input", "query", string.Empty, input.Identity.StableId, "value", UiBindingMode.TwoWay, UiBindingTrigger.Input),
            factory.CreateBinding("query-output", "query", string.Empty, output.Identity.StableId, "text", UiBindingMode.OneWay, UiBindingTrigger.Immediate)
        };
        UiNode list = Control(factory, application, root, "list", "interactive-list");
        list.Collection = new UiCollection
        {
            Key = "items",
            Items = new UiCollectionItem[]
            {
                new UiCollectionItem { Key = "first", Value = Text("First") },
                new UiCollectionItem { Key = "second", Value = Text("Second") }
            },
            Selection = new UiSelection { Mode = UiSelectionMode.Single, SelectedKeys = new string[] { "second" }, ActiveKey = "second" }
        };
        list.Accessibility = new UiAccessibility { Label = "Items" };
        UiNode grid = Control(factory, application, root, "grid", "data-grid");
        UiTableCell firstName = new UiTableCell { ColumnKey = "name", Value = Text("Ada") };
        UiTableCell firstScore = new UiTableCell { ColumnKey = "score", Value = Text("20") };
        UiTableCell secondName = new UiTableCell { ColumnKey = "name", Value = Text("Grace") };
        UiTableCell secondScore = new UiTableCell { ColumnKey = "score", Value = Text("30"), IsEditable = true };
        grid.Table = new UiTable
        {
            Key = "people",
            IsInteractive = true,
            Columns = new UiTableColumn[]
            {
                new UiTableColumn { Key = "name", Header = "Name" },
                new UiTableColumn { Key = "score", Header = "Score" }
            },
            Rows = new UiTableRow[]
            {
                new UiTableRow { Key = "first", Cells = new UiTableCell[] { firstName, firstScore } },
                new UiTableRow { Key = "second", Cells = new UiTableCell[] { secondName, secondScore }, IsSelected = true }
            },
            Selection = new UiSelection { Mode = UiSelectionMode.Single, SelectedKeys = new string[] { "second" }, ActiveKey = "second" },
            EditState = new UiEditState { RowKey = "second", ColumnKey = "score", OriginalValue = Integer(30), CurrentValue = Integer(31), IsDirty = true, IsValid = true }
        };
        grid.Accessibility = new UiAccessibility { Label = "People" };
        UiNode tree = Control(factory, application, root, "tree", "tree-grid");
        tree.Tree = new UiTree
        {
            Key = "outline",
            IsTreeGrid = true,
            Items = new UiTreeItem[]
            {
                new UiTreeItem { Key = "root", Label = "Root", Value = Text("Root") },
                new UiTreeItem { Key = "selected", Label = "Selected node", Value = Text("Selected node"), IsSelected = true }
            },
            Selection = new UiSelection { Mode = UiSelectionMode.Single, SelectedKeys = new string[] { "selected" }, ActiveKey = "selected" }
        };
        tree.Accessibility = new UiAccessibility { Label = "Outline" };
        root.Children = new UiNode[] { input, output, list, grid, tree };
        view.ContentRoot = root;
        page.Title = "Collections";
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        window.BridgeOptions = HostOptions();
        application.Windows = new UiWindow[] { window };
        application.StateEntries = new UiStateEntry[] { new UiStateEntry { Id = "query", Value = Text("before"), Origin = "model" } };
        return application;
    }

    private static UiNode Control(IUiFactory factory, UiApplication application, UiNode parent, string key, string name)
    {
        UiNode node = factory.CreateElementNode(application.RootSeed, parent.Identity.StableId, key, null, name);
        node.Kind = UiNodeKind.Control;
        return node;
    }

    private static void AssertInteractionSurfaces(string html)
    {
        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("role=\"listbox\""));
            Assert.That(html, Does.Contain("data-selection-mode=\"single\""));
            Assert.That(html, Does.Contain("data-table-key=\"people\""));
            Assert.That(html, Does.Contain("data-edit-row=\"second\""));
            Assert.That(html, Does.Contain("data-edit-column=\"score\""));
            Assert.That(html, Does.Contain("data-editing=\"true\""));
            Assert.That(html, Does.Contain("role=\"treegrid\""));
            Assert.That(html, Does.Contain("Selected node"));
            Assert.That(html, Does.Contain("aria-selected=\"true\""));
        });
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
            MaximumMessageLength = 1048576,
            MaximumPendingRequests = 32,
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

    private static UiValue Integer(long value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    private static string DiagnosticCodes(UiDiagnostic[] diagnostics)
    {
        return string.Join(",", diagnostics.Select(item => item.Code));
    }

    private static string DispatchDiagnostics(UiEventDispatchResult result)
    {
        return string.Join("/", result.TransactionStages)
            + ":event=" + DiagnosticCodes(result.Diagnostics)
            + ":binding=" + DiagnosticCodes(result.BindingResult == null ? System.Array.Empty<UiDiagnostic>() : result.BindingResult.Diagnostics)
            + ":validation=" + DiagnosticCodes(result.ValidationResult == null ? System.Array.Empty<UiDiagnostic>() : result.ValidationResult.Diagnostics)
            + ":navigation=" + DiagnosticCodes(result.NavigationResult == null ? System.Array.Empty<UiDiagnostic>() : result.NavigationResult.Diagnostics)
            + ":focus=" + DiagnosticCodes(result.FocusResult == null ? System.Array.Empty<UiDiagnostic>() : result.FocusResult.Diagnostics)
            + ":render=" + DiagnosticCodes(result.RenderResult == null ? System.Array.Empty<UiDiagnostic>() : result.RenderResult.Diagnostics);
    }
}

internal class InteractionHarness
{
    internal IUiHost Host;
    internal HostRecordingTransport Transport;
    internal BridgeMessageFlow BrowserFlow;
    internal IUiBindingEngine Bindings;
    internal UiRenderResult Mounted;
    internal UiBridgeEnvelope LastPatch = new UiBridgeEnvelope();
    internal int RoundTripCount;

    internal InteractionHarness(IUiHost host, HostRecordingTransport transport, BridgeSession browser, IUiBindingEngine bindings, UiRenderResult mounted)
    {
        Host = host;
        Transport = transport;
        BrowserFlow = new BridgeMessageFlow(browser);
        Bindings = bindings;
        Mounted = mounted;
    }
}
