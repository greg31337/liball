using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class IncrementalUpdateEndToEndTests
{
    [Test]
    public void KeyedComponentCollectionThemeResponsiveAndAnimationChangesStayIncremental()
    {
        IUiRenderer renderer = new UiRenderer();
        UiApplication beforeApplication = StyledCollectionApplication(
            new string[] { "alpha", "beta" },
            "#123456",
            40,
            1);
        UiApplication afterApplication = StyledCollectionApplication(
            new string[] { "beta", "gamma", "alpha" },
            "#654321",
            60,
            2);

        UiRenderResult beforeRender = renderer.RenderDocument(beforeApplication);
        UiRenderResult afterRender = renderer.RenderDocument(afterApplication);
        DomNode beforeComponent = FindNode(beforeRender.Snapshot!.Roots[0], "catalog-component");
        DomNode afterComponent = FindNode(afterRender.Snapshot!.Roots[0], "catalog-component");
        DomNode beforeAlpha = FindNode(beforeRender.Snapshot.Roots[0], "alpha");
        DomNode afterAlpha = FindNode(afterRender.Snapshot.Roots[0], "alpha");
        IUiUpdateEngine updates = new UiUpdateEngine();
        UiUpdateBatch batch = updates.ComputeDomUpdates(
            beforeRender.Snapshot,
            afterRender.Snapshot,
            UiUpdatePriority.UserInteraction,
            new UiPreservationState { FocusedNodeId = beforeAlpha.Id });
        UiUpdateResult accepted = updates.AcceptDomUpdates(batch.Id, batch.Revision);

        CssMotionCompiler motion = new CssMotionCompiler();
        CssAnimationPlan entering = motion.StartAnimation(
            Animation("catalog-motion", "catalog-enter", UiAnimationStage.Enter),
            FadeFrames(),
            false);
        CssAnimationPlan moving = motion.StartAnimation(
            Animation("catalog-motion", "catalog-move", UiAnimationStage.Move),
            FadeFrames(),
            false);
        AnimationLifecycleMessage completed = motion.CompleteAnimation("catalog-motion", "catalog-move");

        Assert.Multiple(() =>
        {
            Assert.That(beforeRender.Diagnostics, Is.Empty);
            Assert.That(afterRender.Diagnostics, Is.Empty);
            Assert.That(beforeRender.Css, Does.Contain("#123456"));
            Assert.That(afterRender.Css, Does.Contain("#654321"));
            Assert.That(beforeRender.Css, Does.Contain("min-width: 40rem"));
            Assert.That(afterRender.Css, Does.Contain("min-width: 60rem"));
            Assert.That(afterComponent.Id, Is.EqualTo(beforeComponent.Id));
            Assert.That(afterAlpha.Id, Is.EqualTo(beforeAlpha.Id));
            Assert.That(batch.Mutations.Select(item => item.Kind), Does.Contain(UiMutationKind.Move));
            Assert.That(batch.Mutations.Select(item => item.Kind), Does.Contain(UiMutationKind.Insert));
            Assert.That(batch.Mutations.Select(item => item.Kind), Does.Contain(UiMutationKind.SetText));
            Assert.That(batch.Preservation.FocusedNodeId, Is.EqualTo(beforeAlpha.Id));
            Assert.That(accepted.Accepted, Is.True);
            Assert.That(accepted.Revision, Is.EqualTo(1));
            Assert.That(entering.Messages.Single().Stage, Is.EqualTo(UiAnimationStage.Enter));
            Assert.That(moving.Messages.Select(item => item.Stage), Is.EqualTo(new UiAnimationStage[] { UiAnimationStage.Cancelled, UiAnimationStage.Move }));
            Assert.That(completed.Stage, Is.EqualTo(UiAnimationStage.Completed));
            Assert.That(motion.GetActiveAnimationCount(), Is.Zero);
        });
    }

    [Test]
    public async Task ProviderDrivenVirtualRangeChangesPreserveOverlappingItemIdentity()
    {
        IUiVirtualizationEngine virtualization = new UiVirtualizationEngine();
        virtualization.RegisterUiRangeProvider("catalog", new IncrementalRangeProvider());

        UiRangeResult firstRange = await virtualization.RequestUiRange("catalog", 0, 2, null, null, null, null);
        UiRangeResult secondRange = await virtualization.RequestUiRange("catalog", 1, 2, null, null, null, null);
        IUiRenderer renderer = new UiRenderer();
        UiRenderResult beforeRender = renderer.RenderFragment(VirtualCollectionApplication(firstRange));
        UiRenderResult afterRender = renderer.RenderFragment(VirtualCollectionApplication(secondRange));
        DomNode beforeOverlap = FindNode(beforeRender.Snapshot!.Roots[0], "item-1");
        DomNode afterOverlap = FindNode(afterRender.Snapshot!.Roots[0], "item-1");
        IUiUpdateEngine updates = new UiUpdateEngine();
        UiUpdateBatch batch = updates.ComputeDomUpdates(beforeRender.Snapshot, afterRender.Snapshot, UiUpdatePriority.Background, null);
        UiUpdateResult accepted = updates.AcceptDomUpdates(batch.Id, batch.Revision);

        Assert.Multiple(() =>
        {
            Assert.That(firstRange.Generation, Is.EqualTo(1));
            Assert.That(secondRange.Generation, Is.EqualTo(2));
            Assert.That(firstRange.Items.Select(item => item.Key), Is.EqualTo(new string[] { "item-0", "item-1" }));
            Assert.That(secondRange.Items.Select(item => item.Key), Is.EqualTo(new string[] { "item-1", "item-2" }));
            Assert.That(beforeRender.Diagnostics, Is.Empty);
            Assert.That(afterRender.Diagnostics, Is.Empty);
            Assert.That(afterOverlap.Id, Is.EqualTo(beforeOverlap.Id));
            Assert.That(batch.Priority, Is.EqualTo(UiUpdatePriority.Background));
            Assert.That(batch.Mutations.Select(item => item.Kind), Does.Contain(UiMutationKind.Remove));
            Assert.That(batch.Mutations.Select(item => item.Kind), Does.Contain(UiMutationKind.Insert));
            Assert.That(accepted.Accepted, Is.True);
        });
    }

    [Test]
    public void ServerSnapshotRepairsChangedClientContentWithoutReplacingTheDocument()
    {
        UiApplication expectedApplication = TextApplication("hydration-seed", "Server value", false);
        UiApplication actualApplication = TextApplication("hydration-seed", "Client value", false);
        IUiRenderer renderer = new UiRenderer();
        UiRenderResult expected = renderer.RenderDocument(expectedApplication);
        UiRenderResult actual = renderer.RenderDocument(actualApplication);
        IUiHydrator hydrator = new UiHydrator();
        UiHydrationManifest manifest = hydrator.CreateHydrationManifest(expectedApplication, expected.Snapshot);

        UiHydrationResult repaired = hydrator.PlanHydration(
            expected.Snapshot,
            actual.Snapshot,
            manifest,
            UiHydrationMode.Repair);

        Assert.Multiple(() =>
        {
            Assert.That(expected.Diagnostics, Is.Empty);
            Assert.That(actual.Diagnostics, Is.Empty);
            Assert.That(repaired.Status, Is.EqualTo(UiHydrationStatus.Repaired));
            Assert.That(repaired.ParityMatched, Is.False);
            Assert.That(repaired.Mutations, Has.Length.EqualTo(1));
            Assert.That(repaired.Mutations[0].Kind, Is.EqualTo(UiMutationKind.SetText));
            Assert.That(repaired.Mutations[0].Value, Is.EqualTo("Server value"));
            Assert.That(repaired.Mutations.Select(item => item.Kind), Does.Not.Contain(UiMutationKind.Replace));
        });
    }

    [Test]
    public void AcceptedIncrementReconnectAndRejectedIncrementConvergeThroughFullSnapshots()
    {
        HostRecordingTransport transport = new HostRecordingTransport();
        IUiBindingEngine bindings = new UiBindingEngine();
        IUiEventDispatcher events = new UiEventDispatcher();
        UiApplication application = BoundApplication();
        string inputId = application.Windows[0].Pages[0].ContentView!.ContentRoot!.Children[0].Identity.StableId;
        events.RegisterEventHandler(inputId, InputBinding(), new RecordingUiEventHandler());
        IUiHost host = new UiHost(
            new UiModelValidator(),
            new UiRenderer(),
            new UiUpdateEngine(),
            events,
            bindings,
            new UiValidationEngine(),
            new UiCommandDispatcher(),
            new UiComponentRuntime(),
            new UiRouter(),
            new UiFocusController(),
            transport);
        Assert.That(host.InitializeUiHost(application).Success, Is.True);
        Assert.That(host.MountUiHost("session-1", "nonce-1", 100).Snapshot, Is.Not.Null);
        BridgeSession browser = CompleteReady(host, transport, 101, 102);

        UiEventDispatchResult first = host.DispatchUiHostEvent(InputEvent(inputId, "accepted"));
        UiBridgeEnvelope firstPatch = transport.Messages.Last();
        BridgeMessageFlowResult acknowledgement = new BridgeMessageFlow(browser).ReceiveBridgeMessage(firstPatch, true, 103);
        Assert.That(host.ReceiveUiHostMessage(acknowledgement.Envelope, true, 104).Success, Is.True);

        Assert.That(host.RecoverUiHostTransport("session-2", "nonce-2", 200).Success, Is.True);
        UiBridgeEnvelope initialize = transport.Messages.Last();
        BridgeSession replacementBrowser = BrowserSession();
        BridgeSessionOperation ready = replacementBrowser.AcceptBridgeInitialize(initialize, true, 201);
        Assert.That(host.ReceiveUiHostMessage(ready.Envelope, true, 202).Success, Is.True);
        UiBridgeEnvelope reconnectSnapshot = transport.Messages.Last();

        UiEventDispatchResult second = host.DispatchUiHostEvent(InputEvent(inputId, "rejected"));
        UiUpdateBatch rejectedBatch = second.RenderResult!.UpdateBatch!;
        UiUpdateResult rejected = host.RejectUiHostUpdate(rejectedBatch.Id, rejectedBatch.Revision, 203);
        UiBridgeEnvelope resyncSnapshot = transport.Messages.Last();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(firstPatch.Kind, Is.EqualTo(UiBridgeMessageKind.DomPatch));
            Assert.That(host.GetUiHostSnapshot()!.Revision, Is.EqualTo(1));
            Assert.That(initialize.Kind, Is.EqualTo(UiBridgeMessageKind.Initialize));
            Assert.That(reconnectSnapshot.Kind, Is.EqualTo(UiBridgeMessageKind.RenderSnapshot));
            Assert.That(EnvelopeInteger(reconnectSnapshot, "revision"), Is.EqualTo(1));
            Assert.That(second.Success, Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RequiresResync, Is.True);
            Assert.That(rejected.Snapshot!.Revision, Is.EqualTo(1));
            Assert.That(resyncSnapshot.Kind, Is.EqualTo(UiBridgeMessageKind.RenderSnapshot));
            Assert.That(EnvelopeInteger(resyncSnapshot, "revision"), Is.EqualTo(1));
            Assert.That(resyncSnapshot.Payload!.ObjectValue!.Single(item => item.Key == "roots").Value.ArrayValue, Is.Not.Empty);
        });
    }

    private static UiApplication StyledCollectionApplication(string[] keys, string accent, int minimumWidth, int version)
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication("incremental-seed", "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        UiNode component = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "catalog-component", null, HtmlElements.Division);
        component.Kind = UiNodeKind.Component;
        component.Name = "catalog";
        component.State.Version = version;
        UiNode repeat = factory.CreateElementNode(application.RootSeed, component.Identity.StableId, "catalog-items", null, HtmlElements.Division);
        repeat.Kind = UiNodeKind.Repeat;
        repeat.Name = string.Empty;
        repeat.Repeat = new UiRepeat
        {
            Items = keys.Select(key => new UiCollectionItem
            {
                Key = key,
                Value = Text(key),
                Content = factory.CreateTextNode(application.RootSeed, repeat.Identity.StableId, key + "-content", key.ToUpperInvariant())
            }).ToArray()
        };
        component.Children = new UiNode[] { repeat };
        component.Animation = Animation("catalog-motion", version == 1 ? "catalog-enter" : "catalog-move", version == 1 ? UiAnimationStage.Enter : UiAnimationStage.Move);
        root.Children = new UiNode[] { component };
        view.ContentRoot = root;
        page.ContentView = view;
        page.Title = "Incremental catalog";
        page.Styles = new UiStyle[]
        {
            new UiStyle
            {
                Key = "catalog-style",
                Declarations = new UiCssDeclaration[]
                {
                    new UiCssDeclaration { Property = CssProperties.Color, Value = "var(--accent)", IsTrusted = true }
                },
                ResponsiveRules = new UiResponsiveRule[]
                {
                    new UiResponsiveRule
                    {
                        Operator = UiResponsiveOperator.MinWidth,
                        Length = new UiLength { HasValue = true, Value = minimumWidth, Unit = UiLengthUnit.Rem },
                        Declarations = new UiCssDeclaration[]
                        {
                            new UiCssDeclaration { Property = CssProperties.Gap, Value = "2rem", IsTrusted = true }
                        }
                    }
                }
            }
        };
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        window.IsActive = true;
        application.Windows = new UiWindow[] { window };
        application.Theme = new UiTheme
        {
            Key = "catalog-theme",
            Mode = UiThemeMode.Light,
            Tokens = new UiValueEntry[] { new UiValueEntry { Key = "accent", Value = Text(accent) } }
        };
        application.RenderOptions.IncludeHydration = false;
        application.RenderOptions.IncludeBootstrap = false;
        return application;
    }

    private static UiApplication VirtualCollectionApplication(UiRangeResult range)
    {
        UiApplication application = TextApplication("virtual-seed", "Virtual catalog", false);
        IUiFactory factory = new UiFactory();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        UiNode list = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "virtual-list", null, HtmlElements.Division);
        list.Kind = UiNodeKind.Control;
        list.Name = "list";
        list.Accessibility = new UiAccessibility { Label = "Virtual catalog" };
        list.Collection = new UiCollection
        {
            Key = "catalog",
            Items = range.Items,
            PageRange = new UiPageRange
            {
                Start = range.Start,
                Count = range.Items.Length,
                HasTotalCount = range.HasTotalCount,
                TotalCount = range.TotalCount,
                IsEnd = range.IsEnd
            }
        };
        list.Virtualization = new UiVirtualization
        {
            IsEnabled = true,
            Orientation = UiOrientation.Vertical,
            HasItemExtent = true,
            ItemExtent = 24,
            Overscan = 1,
            HasTotalCount = range.HasTotalCount,
            TotalCount = range.TotalCount,
            Result = range
        };
        root.Children = new UiNode[] { list };
        return application;
    }

    private static UiApplication TextApplication(string seed, string text, bool bridge)
    {
        IUiFactory factory = new UiFactory();
        UiApplication application = factory.CreateApplication(seed, "application");
        UiWindow window = factory.CreateWindow(application.RootSeed, application.Id, "window");
        UiPage page = factory.CreatePage(application.RootSeed, window.Id, "page");
        UiView view = factory.CreateView(application.RootSeed, page.Id, "view");
        UiNode root = factory.CreateElementNode(application.RootSeed, view.Id, "root", null, HtmlElements.Main);
        root.Children = new UiNode[] { factory.CreateTextNode(application.RootSeed, root.Identity.StableId, "content", text) };
        view.ContentRoot = root;
        page.Title = "Incremental";
        page.ContentView = view;
        window.Pages = new UiPage[] { page };
        window.CurrentPageKey = page.Key;
        window.IsActive = true;
        if (bridge)
        {
            window.BridgeOptions = HostOptions();
        }

        application.Windows = new UiWindow[] { window };
        application.RenderOptions.IncludeHydration = false;
        application.RenderOptions.IncludeBootstrap = false;
        return application;
    }

    private static UiApplication BoundApplication()
    {
        UiApplication application = TextApplication("recovery-seed", "initial", true);
        IUiFactory factory = new UiFactory();
        UiNode root = application.Windows[0].Pages[0].ContentView!.ContentRoot!;
        UiNode input = factory.CreateElementNode(application.RootSeed, root.Identity.StableId, "input", null, HtmlElements.Input);
        input.Kind = UiNodeKind.Control;
        input.Name = "input";
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
        application.StateEntries = new UiStateEntry[]
        {
            new UiStateEntry { Id = "value", Value = Text("initial"), Origin = "model" }
        };
        return application;
    }

    private static DomNode FindNode(DomNode node, string key)
    {
        if (node.Key == key)
        {
            return node;
        }

        foreach (DomNode child in node.Children)
        {
            DomNode? found = FindOptionalNode(child, key);
            if (found != null)
            {
                return found;
            }
        }

        throw new AssertionException("No DOM node with key '" + key + "' was found.");
    }

    private static DomNode? FindOptionalNode(DomNode node, string key)
    {
        if (node.Key == key)
        {
            return node;
        }

        foreach (DomNode child in node.Children)
        {
            DomNode? found = FindOptionalNode(child, key);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static UiAnimation Animation(string key, string name, UiAnimationStage stage)
    {
        return new UiAnimation
        {
            Key = key,
            Name = name,
            Stage = stage,
            DurationMilliseconds = 150,
            DisableForReducedMotion = true
        };
    }

    private static CssKeyframeValue[] FadeFrames()
    {
        return new CssKeyframeValue[]
        {
            new CssKeyframeValue
            {
                OffsetPercent = 0,
                Declarations = new UiCssDeclaration[]
                {
                    new UiCssDeclaration { Property = CssProperties.Opacity, Value = "0", IsTrusted = true }
                }
            },
            new CssKeyframeValue
            {
                OffsetPercent = 100,
                Declarations = new UiCssDeclaration[]
                {
                    new UiCssDeclaration { Property = CssProperties.Opacity, Value = "1", IsTrusted = true }
                }
            }
        };
    }

    private static BridgeSession CompleteReady(IUiHost host, HostRecordingTransport transport, long browserMilliseconds, long hostMilliseconds)
    {
        BridgeSession browser = BrowserSession();
        BridgeSessionOperation ready = browser.AcceptBridgeInitialize(transport.Messages.Last(), true, browserMilliseconds);
        Assert.That(host.ReceiveUiHostMessage(ready.Envelope, true, hostMilliseconds).Success, Is.True);
        return browser;
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
            Capabilities = new UiCapability[]
            {
                new UiCapability { Name = "binary", IsSupported = true, Version = "1" }
            }
        };
    }

    private static UiEventBinding InputBinding()
    {
        return new UiEventBinding
        {
            Id = "input-event",
            HandlerId = "input-handler",
            Kind = UiEventKind.Input,
            Phase = UiEventPhase.Target
        };
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

    private static long EnvelopeInteger(UiBridgeEnvelope envelope, string key)
    {
        return envelope.Payload!.ObjectValue!.Single(item => item.Key == key).Value.IntegerValue;
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private sealed class IncrementalRangeProvider : IUiRangeProvider
    {
        public Task<UiRangeResult> ProvideUiRange(UiRangeRequest request)
        {
            UiCollectionItem[] items = new UiCollectionItem[request.Count];
            int index = 0;
            while (index < request.Count)
            {
                string key = "item-" + (request.Start + index);
                items[index] = new UiCollectionItem { Key = key, Value = Text("Item " + (request.Start + index)) };
                index++;
            }

            return Task.FromResult(new UiRangeResult
            {
                Generation = request.Generation,
                Start = request.Start,
                Items = items,
                HasTotalCount = true,
                TotalCount = 10,
                IsEnd = request.Start + request.Count >= 10
            });
        }
    }
}
