using System;
using htmlgui;

namespace htmlgui.Tests;

public class ExtendedDataModelsTests
{
    [Test]
    public void EventAndCommandModelsComposeTypedPayloads()
    {
        UiPointerData pointer = new UiPointerData { PointerType = "pen", PointerId = 2, ClientX = 1.5, ClientY = 2.5, Button = 1, Buttons = 3U, PrimaryModifier = true, Shift = true };
        UiKeyboardData keyboard = new UiKeyboardData { Key = "Enter", Code = "Enter", PrimaryModifier = true, Alt = true, Shift = true, Repeat = true, IsComposing = true };
        UiInputData input = new UiInputData { Value = Text("x"), InputType = "insertText", Data = "x", IsComposing = true, HasSelectionStart = true, SelectionStart = 0, HasSelectionEnd = true, SelectionEnd = 1 };
        UiClipboardData clipboard = new UiClipboardData { Operation = "paste", Data = new UiValueEntry[] { Entry("text/plain", Text("x")) }, IsAllowed = true };
        UiDragData drag = new UiDragData { SourceId = "a", TargetId = "b", Effect = "move", AllowedEffect = "copyMove", Data = clipboard.Data, IsKeyboard = true };
        UiEventData data = new UiEventData { Kind = UiEventKind.PointerDown, TargetId = "b", Path = new string[] { "b", "root" }, IsTrusted = true, Pointer = pointer, Keyboard = keyboard, Input = input, Clipboard = clipboard, Drag = drag };
        UiEventBinding binding = new UiEventBinding { Id = "event", HandlerId = "handler", Kind = UiEventKind.PointerDown, Phase = UiEventPhase.Capture, PreventDefault = true, StopPropagation = true, Once = true, Passive = true };
        UiShortcutChord chord = new UiShortcutChord { Key = "S", PrimaryModifier = true, Alt = true, Shift = true };
        UiShortcut shortcut = new UiShortcut { Id = "save-key", Chords = new UiShortcutChord[] { chord }, ScopeId = "page", AllowRepeat = true };
        UiCommandStatus status = new UiCommandStatus { IsEnabled = false, IsVisible = false, IsBusy = true, HasCheckState = true, CheckState = UiCommandCheckState.Mixed };
        UiCommandInvocation invocation = new UiCommandInvocation { CommandId = "save", ScopeId = "page", SourceNodeId = "button", Parameter = Text("now"), IsRepeat = true };
        UiCommand command = new UiCommand { Id = "save", Label = "Save", Description = "Save data", IconKey = "disk", HandlerId = "save-handler", StatusHandlerId = "status-handler", Shortcuts = new UiShortcut[] { shortcut }, Status = status, Invocation = invocation };

        Assert.Multiple(() =>
        {
            Assert.That(data.Pointer, Is.SameAs(pointer));
            Assert.That(data.Keyboard?.IsComposing, Is.True);
            Assert.That(data.Input?.SelectionStart, Is.Zero);
            Assert.That(data.Clipboard?.Data[0].Key, Is.EqualTo("text/plain"));
            Assert.That(data.Drag?.IsKeyboard, Is.True);
            Assert.That(binding.Phase, Is.EqualTo(UiEventPhase.Capture));
            Assert.That(command.Shortcuts[0].Chords[0], Is.SameAs(chord));
            Assert.That(command.Status, Is.SameAs(status));
            Assert.That(command.Invocation, Is.SameAs(invocation));
            Assert.That(status.CheckState, Is.EqualTo(UiCommandCheckState.Mixed));
        });
    }

    [Test]
    public void StateBindingComponentAndRoutingModelsCompose()
    {
        UiStatePatch patch = new UiStatePatch { Operation = "replace", Path = "/name", FromPath = "/old", Value = Text("Ada") };
        UiStateChange change = new UiStateChange { StateId = "user", Path = "/name", PreviousValue = Text("A"), CurrentValue = Text("Ada"), Version = 2, Origin = "input" };
        UiDerivedState derived = new UiDerivedState { Id = "display", DependencyIds = new string[] { "user" }, HandlerId = "derive", Value = Text("Ada"), Version = 2, IsDirty = false };
        UiEffect effect = new UiEffect { Id = "persist", DependencyIds = new string[] { "user" }, HandlerId = "effect", CleanupHandlerId = "cleanup", Generation = 2, IsCancelled = true };
        UiSubscription subscription = new UiSubscription { Id = "sub", StateId = "user", Path = "/name", HandlerId = "observer", ScopeId = "view", IsActive = false };
        UiStateEntry state = new UiStateEntry { Id = "user", Path = "/", Value = Text("Ada"), Version = 2, Origin = "input", Patches = new UiStatePatch[] { patch }, Changes = new UiStateChange[] { change }, Derived = new UiDerivedState[] { derived }, Effects = new UiEffect[] { effect }, Subscriptions = new UiSubscription[] { subscription } };
        UiBindingContext context = new UiBindingContext { BindingId = "binding", ScopeId = "view", Origin = "state", Version = 2, ToSource = true };
        UiBindingUpdate update = new UiBindingUpdate { BindingId = "binding", TargetNodeId = "name", Target = "value", Value = Text("Ada"), Version = 2, Origin = "state" };
        UiConversionResult conversion = new UiConversionResult { Success = true, Value = Text("Ada"), Diagnostic = new UiDiagnostic { Severity = UiDiagnosticSeverity.Information, Code = "ok", Message = "Converted" } };
        UiBinding binding = new UiBinding { Id = "binding", StateId = "user", Path = "/name", TargetNodeId = "name", Target = "value", Mode = UiBindingMode.TwoWay, Trigger = UiBindingTrigger.Input, ConverterId = "converter", FallbackValue = Text(""), Context = context, Update = update, Conversion = conversion };
        UiComponentDefinition definition = new UiComponentDefinition { TypeId = "profile", RenderHandlerId = "render", LifecycleHandlerId = "life", ErrorHandlerId = "error", DefaultParameters = new UiValueEntry[] { Entry("name", Text("")) } };
        UiComponentContext component = new UiComponentContext { ComponentId = "c1", TypeId = "profile", Key = "profile", Parameters = definition.DefaultParameters, StateScopeId = "state-c1", Slots = new UiSlot[] { new UiSlot { Name = "body" } }, ContextValues = new UiValueEntry[] { Entry("locale", Text("en")) } };
        UiComponentRenderRequest render = new UiComponentRenderRequest { Context = component, Generation = 2, Priority = UiUpdatePriority.Immediate, IsHydration = true };
        UiLifecycleNotification lifecycle = new UiLifecycleNotification { ComponentId = "c1", Stage = UiLifecycleStage.AfterRender, Generation = 2, Diagnostic = conversion.Diagnostic };
        UiErrorBoundaryState boundary = new UiErrorBoundaryState { BoundaryId = "c1", HasError = true, Diagnostic = conversion.Diagnostic, Fallback = new UiNode { Text = "Error" }, ResetKey = "r1" };
        UiRouteSegment segment = new UiRouteSegment { Kind = "parameter", Value = "id", Constraint = "int", DefaultValue = "1", IsOptional = true };
        UiRouteData routeData = new UiRouteData { RouteId = "user", Parameters = new UiValueEntry[] { Entry("id", Text("1")) }, Query = new UiValueEntry[] { Entry("tab", Text("info")) }, Fragment = "top" };
        UiRoute route = new UiRoute { Id = "user", Template = "/user/{id}", Segments = new UiRouteSegment[] { segment }, ParentId = "root", View = new UiView(), IsIndex = true, IsNotFound = true };
        UiNavigationRequest request = new UiNavigationRequest { Kind = UiNavigationKind.Replace, Url = "/user/1", State = Text("state"), Generation = 2, MoveFocus = false };
        UiNavigationResult result = new UiNavigationResult { Status = UiNavigationStatus.Resolved, RouteData = routeData, RedirectUrl = "/resolved", Diagnostic = conversion.Diagnostic, Generation = 2 };
        UiHistoryEntry history = new UiHistoryEntry { Id = "h1", Url = "/user/1", State = Text("state"), HasScrollOffset = true, ScrollX = 0, ScrollY = 20, FocusedNodeId = "title" };

        Assert.Multiple(() =>
        {
            Assert.That(state.Patches[0], Is.SameAs(patch));
            Assert.That(state.Derived[0].IsDirty, Is.False);
            Assert.That(binding.Context, Is.SameAs(context));
            Assert.That(binding.Conversion, Is.SameAs(conversion));
            Assert.That(render.Context, Is.SameAs(component));
            Assert.That(lifecycle.Stage, Is.EqualTo(UiLifecycleStage.AfterRender));
            Assert.That(boundary.Fallback?.Text, Is.EqualTo("Error"));
            Assert.That(route.Segments[0], Is.SameAs(segment));
            Assert.That(result.RouteData, Is.SameAs(routeData));
            Assert.That(request.MoveFocus, Is.False);
            Assert.That(history.HasScrollOffset, Is.True);
        });
    }

    [Test]
    public void FocusAccessibilityStyleAndVirtualizationModelsCompose()
    {
        UiFocusRequest focusRequest = new UiFocusRequest { TargetId = "item", ScopeId = "list", Direction = UiFocusDirection.Next, Reveal = true, Restore = true };
        UiFocusState focus = new UiFocusState { FocusedNodeId = "item", ScopeId = "list", ActiveDescendantId = "option", IsTrapped = true, IsFocusVisible = true };
        UiKeyboardMap map = new UiKeyboardMap { Id = "listbox", Widget = "listbox", Orientation = UiOrientation.Vertical, Mappings = new UiValueEntry[] { Entry("ArrowDown", Text("next")) }, IsRightToLeft = true };
        UiTypeAheadState typeAhead = new UiTypeAheadState { Buffer = "ad", MatchedKey = "ada", Generation = 2, IgnoreCase = false };
        UiAriaRelationship relationship = new UiAriaRelationship { Name = AriaAttributes.DescribedBy, TargetIds = new string[] { "help" } };
        UiLiveAnnouncement announcement = new UiLiveAnnouncement { Id = "live", Message = "Saved", Mode = UiAriaLiveMode.Assertive, Atomic = true };
        UiAccessibilityReport report = new UiAccessibilityReport { Diagnostics = new UiDiagnostic[] { new UiDiagnostic { Code = "a11y", Message = "Issue" } }, IsValid = false };
        UiAccessibility accessibility = new UiAccessibility { Role = AriaRoles.ListBox, Label = "People", Description = "Choose", HasHeadingLevel = true, HeadingLevel = 2, Relationships = new UiAriaRelationship[] { relationship }, LiveMode = UiAriaLiveMode.Polite, HasCurrentKind = true, CurrentKind = UiAriaCurrentKind.Page, IsHidden = true, Announcements = new UiLiveAnnouncement[] { announcement }, Report = report };
        UiCssDeclaration declaration = new UiCssDeclaration { Property = CssProperties.Color, Value = "red", Important = true, IsTrusted = true };
        UiCssCondition condition = new UiCssCondition { Kind = "media", ContainerName = "main", Expression = "(min-width: 40rem)" };
        UiCssRule rule = new UiCssRule { Selector = ".item", Declarations = new UiCssDeclaration[] { declaration }, Condition = condition, Layer = "components" };
        UiResponsiveRule responsive = new UiResponsiveRule { Operator = UiResponsiveOperator.MinWidth, Length = new UiLength { HasValue = true, Value = 40, Unit = UiLengthUnit.Rem }, Value = "wide", IsContainer = true, Declarations = new UiCssDeclaration[] { declaration } };
        UiStyle style = new UiStyle { Key = "item", Scope = "s1", Declarations = new UiCssDeclaration[] { declaration }, Rules = new UiCssRule[] { rule }, ResponsiveRules = new UiResponsiveRule[] { responsive } };
        UiThemeVariant variant = new UiThemeVariant { Key = "dark", ParentKey = "base", Mode = UiThemeMode.Dark, Tokens = new UiValueEntry[] { Entry("color", Text("black")) } };
        UiTheme theme = new UiTheme { Key = "theme", Mode = UiThemeMode.Dark, Tokens = variant.Tokens, Variants = new UiThemeVariant[] { variant } };
        UiAnimation animation = new UiAnimation { Key = "fade", Stage = UiAnimationStage.Enter, Name = "fade-in", DurationMilliseconds = 0, DisableForReducedMotion = false };
        UiRangeRequest rangeRequest = new UiRangeRequest { Generation = 2, Start = 0, Count = 20, Sorts = new UiSort[] { new UiSort() }, Filters = new UiFilter[] { new UiFilter() }, ParentKey = "root" };
        UiRangeResult rangeResult = new UiRangeResult { Generation = 2, Start = 0, Items = new UiCollectionItem[] { new UiCollectionItem { Key = "item" } }, HasTotalCount = true, TotalCount = 1, IsEnd = true, Diagnostic = report.Diagnostics[0] };
        UiMeasuredItem measured = new UiMeasuredItem { Key = "item", Index = 0, Extent = 42, Generation = 2 };
        UiScrollTarget target = new UiScrollTarget { HasIndex = true, Index = 0, Key = "item", Alignment = "center", Offset = 21 };
        UiVirtualization virtualization = new UiVirtualization { IsEnabled = true, Orientation = UiOrientation.Horizontal, HasItemExtent = true, ItemExtent = 42, Overscan = 2, HasTotalCount = true, TotalCount = 1, Request = rangeRequest, Result = rangeResult, Measurements = new UiMeasuredItem[] { measured }, ScrollTarget = target };

        Assert.Multiple(() =>
        {
            Assert.That(focusRequest.Direction, Is.EqualTo(UiFocusDirection.Next));
            Assert.That(focus.IsTrapped, Is.True);
            Assert.That(map.Mappings[0].Key, Is.EqualTo("ArrowDown"));
            Assert.That(typeAhead.IgnoreCase, Is.False);
            Assert.That(accessibility.Relationships[0], Is.SameAs(relationship));
            Assert.That(accessibility.HeadingLevel, Is.EqualTo(2));
            Assert.That(accessibility.Report, Is.SameAs(report));
            Assert.That(style.Rules[0].Condition, Is.SameAs(condition));
            Assert.That(theme.Variants[0], Is.SameAs(variant));
            Assert.That(animation.DurationMilliseconds, Is.Zero);
            Assert.That(virtualization.Request, Is.SameAs(rangeRequest));
            Assert.That(virtualization.Result, Is.SameAs(rangeResult));
            Assert.That(virtualization.ScrollTarget, Is.SameAs(target));
        });
    }

    [Test]
    public void RenderingBridgeAndSharedResultModelsCompose()
    {
        UiDiagnostic diagnostic = new UiDiagnostic { Severity = UiDiagnosticSeverity.Warning, Code = "warn", Message = "Warning", Location = "/root" };
        UiCapability capability = new UiCapability { Name = "binary", IsSupported = true, Version = "1" };
        UiResult portableResult = new UiResult { Success = true, Value = Text("ok"), Diagnostics = new UiDiagnostic[] { diagnostic } };
        UiPreservationState preservation = new UiPreservationState { FocusedNodeId = "input", HasSelection = true, SelectionStart = 0, SelectionEnd = 1, IsComposing = true, ScrollOffsets = new UiValueEntry[] { Entry("root", Text("10")) }, OpenOverlayIds = new string[] { "dialog" } };
        UiUpdateBatch batch = new UiUpdateBatch { Id = "batch", BaseRevision = 1, Revision = 2, Priority = UiUpdatePriority.Immediate, Mutations = new DomMutation[] { new DomMutation() }, Preservation = preservation };
        UiHydrationManifest manifest = new UiHydrationManifest { Version = 1, Hash = "hash", Nodes = new UiValueEntry[] { Entry("n", Text("m")) }, State = new UiValueEntry[] { Entry("s", Text("v")) } };
        UiHydrationResult hydration = new UiHydrationResult { Mode = UiHydrationMode.Repair, Status = UiHydrationStatus.Repaired, Mutations = batch.Mutations, Diagnostics = new UiDiagnostic[] { diagnostic } };
        UiUpdateResult update = new UiUpdateResult { Accepted = true, Revision = 2, RequiresResync = true, Snapshot = new DomSnapshot { Revision = 2 }, Diagnostics = new UiDiagnostic[] { diagnostic }, Result = portableResult };
        UiRenderOptions options = new UiRenderOptions { FullDocument = false, IncludeHydration = false, IncludeBootstrap = false, ContentSecurityPolicyNonce = "nonce", MaximumDiagnostics = 2 };
        UiRenderResult render = new UiRenderResult { Html = "<p>x</p>", Css = "p{}", Bootstrap = "", Snapshot = update.Snapshot, Hydration = manifest, Diagnostics = update.Diagnostics, Hash = "render", UpdateResult = update, UpdateBatch = batch, HydrationResult = hydration };
        UiBridgeError error = new UiBridgeError { Code = "timeout", Message = "Timed out", CorrelationId = "c1", IsRetryable = true };
        UiBridgeRequest request = new UiBridgeRequest { CorrelationId = "c1", Operation = "host.call", Arguments = new UiValueEntry[] { Entry("x", Text("y")) }, TimeoutMilliseconds = 10 };
        UiBridgeResponse response = new UiBridgeResponse { CorrelationId = "c1", Success = false, Value = Text("none"), Error = error };
        UiBridgeEnvelope envelope = new UiBridgeEnvelope { Version = 1, Session = "s1", Sequence = 1, CorrelationId = "c1", Origin = BridgeProtocolNames.HostOrigin, Kind = UiBridgeMessageKind.Request, Payload = Text("payload"), Request = request, Response = response, Error = error };
        UiBridgeOptions bridgeOptions = new UiBridgeOptions { Version = 1, Origin = "https://example.test", RequireMainFrame = false, MaximumMessageLength = 10, MaximumPendingRequests = 1, TimeoutMilliseconds = 10 };
        UiTransportCapabilities transport = new UiTransportCapabilities { State = UiTransportState.Ready, Version = 1, Capabilities = new UiCapability[] { capability }, SupportsBinary = true, SupportsChunking = true };

        Assert.Multiple(() =>
        {
            Assert.That(render.UpdateResult, Is.SameAs(update));
            Assert.That(render.UpdateBatch, Is.SameAs(batch));
            Assert.That(render.HydrationResult, Is.SameAs(hydration));
            Assert.That(batch.Preservation, Is.SameAs(preservation));
            Assert.That(update.Result, Is.SameAs(portableResult));
            Assert.That(options.FullDocument, Is.False);
            Assert.That(envelope.Request, Is.SameAs(request));
            Assert.That(envelope.Response, Is.SameAs(response));
            Assert.That(response.Error, Is.SameAs(error));
            Assert.That(bridgeOptions.RequireMainFrame, Is.False);
            Assert.That(transport.Capabilities[0], Is.SameAs(capability));
            Assert.That(transport.State, Is.EqualTo(UiTransportState.Ready));
        });
    }

    [Test]
    public void RootModelsCarryRuntimeDescriptions()
    {
        UiApplication application = new UiApplication { Commands = new UiCommand[] { new UiCommand() }, StateEntries = new UiStateEntry[] { new UiStateEntry() }, Routes = new UiRoute[] { new UiRoute() }, Theme = new UiTheme(), RenderOptions = new UiRenderOptions(), RenderResult = new UiRenderResult() };
        UiWindow window = new UiWindow { BridgeOptions = new UiBridgeOptions(), Transport = new UiTransportCapabilities(), Focus = new UiFocusState(), NavigationRequest = new UiNavigationRequest(), NavigationResult = new UiNavigationResult(), History = new UiHistoryEntry[] { new UiHistoryEntry() }, LastBridgeEnvelope = new UiBridgeEnvelope() };
        UiView view = new UiView { ComponentDefinition = new UiComponentDefinition(), ComponentContext = new UiComponentContext(), RenderRequest = new UiComponentRenderRequest(), LifecycleNotifications = new UiLifecycleNotification[] { new UiLifecycleNotification() }, ErrorBoundaryState = new UiErrorBoundaryState(), FocusRequest = new UiFocusRequest(), KeyboardMaps = new UiKeyboardMap[] { new UiKeyboardMap() }, TypeAhead = new UiTypeAheadState() };
        UiNode node = new UiNode { EventBindings = new UiEventBinding[] { new UiEventBinding() }, EventData = new UiEventData(), Bindings = new UiBinding[] { new UiBinding() }, Accessibility = new UiAccessibility(), Announcements = new UiLiveAnnouncement[] { new UiLiveAnnouncement() }, AccessibilityReport = new UiAccessibilityReport(), Style = new UiStyle(), Animation = new UiAnimation(), Virtualization = new UiVirtualization() };

        Assert.Multiple(() =>
        {
            Assert.That(application.Commands, Has.Length.EqualTo(1));
            Assert.That(application.RenderResult, Is.Not.Null);
            Assert.That(window.NavigationResult, Is.Not.Null);
            Assert.That(window.LastBridgeEnvelope, Is.Not.Null);
            Assert.That(view.ComponentContext, Is.Not.Null);
            Assert.That(view.KeyboardMaps, Has.Length.EqualTo(1));
            Assert.That(node.EventData, Is.Not.Null);
            Assert.That(node.Accessibility, Is.Not.Null);
            Assert.That(node.Virtualization, Is.Not.Null);
        });
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }
}
