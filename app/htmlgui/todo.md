# Universal Declarative HTML GUI — development plan

## Goal and acceptance criteria

Build a production-dependency-free .NET library that lets browser and WebView applications describe a GUI as a portable C# object model, render that model deterministically to standards-based HTML/CSS/ARIA, and keep it interactive through small incremental DOM updates and a host-neutral JSON message bridge.

The library is complete when it can:

- Represent an application → window → page → view → component/control hierarchy without storing browser-native objects in the public model.
- Offer both a safe low-level HTML/CSS escape hatch and high-level accessible GUI controls.
- Render the same normalized model and options to byte-identical HTML, CSS, hydration metadata, and patch messages.
- Escape untrusted text, attributes, CSS values, URLs, and JSON for the correct output context; raw markup is never the default path.
- Process state, bindings, commands, events, validation, routing, focus, lifecycle, responsive behavior, and virtualized collections without requiring a particular browser or WebView product.
- Render a useful non-JavaScript initial document, then attach behavior through hydration hooks or create it entirely on the client.
- Communicate over injected host transports for WebView2, WKWebView, Android WebView, or an application-specific browser transport without referencing their SDKs from `src/`.
- Expose only the public API categories and syntax allowed by `syntax.md`, use only built-in .NET libraries in `src/`, and have functional tests covering every public type, method, branch, failure mode, and supported control behavior.

## Design constraints and decisions

- Target `net10.0`; use `System`, `System.Collections.Generic`, `System.Globalization`, `System.Text`, `System.Text.Encodings.Web`, `System.Text.Json`, `System.Threading`, and `System.Threading.Tasks` only where needed.
- Declare every public type under `src/Api` in its required category directory; declare no public type elsewhere and no non-public type under `src/Api`.
- Public mutable descriptions are field-based data classes. Operations are instance methods on interfaces and their public implementation classes. No public factory is static.
- Give every public implementation class exactly one constructor and expose only the instance methods declared by its implemented interfaces; expose no public static method on any public type.
- Do not define project-owned generic types or methods, delegates, properties, events, overloads, inheritance hierarchies, records, structs, attributes, LINQ, or async syntax in `src/`.
- Model optional value-type data with explicit `HasX` flags or a tagged `UiValue`; never use nullable value types.
- Model callbacks with handler interfaces and stable handler IDs because delegates are not allowed. Methods returning `Task` may compose built-in task APIs without `await`.
- Give every instance member in an interface, and every combined interface set implemented by a class, a unique name. Prefix methods by subsystem where useful.
- Prefer arrays and built-in `List<T>`/`Dictionary<TKey,TValue>` types at API boundaries. Use `ReadOnlySpan<char>`, `Span<char>`, and `ReadOnlySpan<byte>` only in internal hot parsing/escaping paths where they simplify allocation-safe code.
- Keep the portable UI tree separate from the normalized render tree and the serialized DOM mutation protocol. Never treat existing DOM state as the source of truth.
- Use native HTML semantics first. Add ARIA only when native semantics are insufficient or when a composite widget requires explicit state/relationships.
- Use stable author keys for collection identity. Generated IDs are deterministic paths derived from root seed, node kind, and sibling identity.
- Use JSON envelopes with protocol version, session, monotonically increasing sequence, correlation ID, origin, message kind, and payload. Validate all inbound messages and bound all lengths/counts.
- Model expected outcomes such as invalid form values, unmatched routes, unsupported bridge capabilities, and hydration mismatch reports with result data and `Try...` operations; reserve exceptions for malformed models, protocol violations, and unrecoverable runtime failures.
- Own a small plain-JavaScript browser runtime as an embedded source asset. It may use web-platform APIs but no third-party JavaScript package.

## Research synthesis

- **Lit:** adopt declarative templates, explicit property/attribute distinction, batched reactive updates, scoped styles, controller-like lifecycle participation, SSR, and hydration concepts. Keep the portable model independent of custom elements or Shadow DOM.
- **Svelte:** adopt first-class state, derived values, effects with cleanup, two-way binding, keyed conditional/list rendering, context, transitions, and hydratable state. Implement these at runtime because this project has no compiler transform.
- **Blazor:** adopt a sequence-stable render tree, diff batches, component parameters, event-to-rerender flow, forms/edit context, validation messages, routing/navigation interception, SSR/hydration modes, and collection virtualization. Avoid ASP.NET hosting dependencies.
- **Vaadin Flow:** adopt a server/host-owned element tree, high-level components over a low-level element API, typed field binding and validation, lazy data ranges, command-like listeners, and a versioned client/server protocol. Keep transport and host lifetime injectable.
- **Dioxus:** adopt pure UI-as-state projection, signals with automatic dependency tracking, batching, keyed reconciliation, component context/effects, SSR consistency checks, typed routing ideas, and WebView portability.
- **Standards:** HTML and DOM define serialization and interaction; CSSOM plus Flexbox/Grid/Cascade/Values/Media Queries/Containment define layout and style generation; UI/Pointer/Input Events define event data; WAI-ARIA, Accessible Name, ARIA in HTML, APG, and WCAG 2.2 define semantics and keyboard behavior; URL defines routing normalization; RFC 8259/6901/6902 inform bridge and patch formats.
- Local snapshots and exact source URLs are catalogued in `specs/SOURCES.md`. They are design inputs, not runtime dependencies.

## Layered architecture

1. **Portable values and model:** tagged values, stable IDs, immutable-by-convention input graphs, application/window/page/view hierarchy, component/control descriptions, styles, bindings, routes, and bridge messages.
2. **Normalization and semantic expansion:** validate the graph; supply defaults; resolve themes, responsive variants, bindings, control templates, native semantics, ARIA, and deterministic IDs; reject cycles and conflicting definitions.
3. **Render tree:** produce a renderer-neutral element/text/comment tree with canonical attributes, DOM properties, event subscriptions, node keys, style scopes, and hydration markers.
4. **Outputs:** serialize safe HTML and generated CSS, construct a full document, or diff two snapshots into ordered DOM mutations.
5. **Reactive runtime:** store observable values, track dependencies, batch writes, evaluate derived values/effects, dispatch events/commands, validate forms, manage components/routes/focus, and schedule render batches.
6. **Browser runtime:** hydrate or create DOM, delegate events, apply mutations, preserve focus/selection/scroll, observe resize/intersection, enforce protocol sequencing, and exchange messages with the host.
7. **Host adapters:** applications implement the transport interface around WebView2, WKWebView, Android WebView, browser WebSocket/postMessage, or an in-memory test transport.

## Complete feature inventory

### Model primitives and hierarchy

- Tagged null, Boolean, signed integer, floating-point, decimal, string, date/time text, binary/base64, array, and string-keyed object values with deterministic equality and JSON conversion.
- Stable application, window, page, view, component, node, state, binding, command, route, form, field, and collection-item IDs.
- One application with shared theme/routes/commands/state; zero or more independently addressed windows; each window has document metadata, current page, overlays, and host capabilities.
- Pages with title, language, text direction, metadata, stylesheet references, route identity, shell/chrome view, content view, and not-found/error/loading views.
- Views with stable key, component identity, parameter values, local state scope, content root, lifecycle state, focus scope, and optional error boundary.
- Logical fragments, slots/content projection, conditional nodes, keyed repeated nodes, portals into named overlay regions, and safe low-level HTML element/text/comment nodes.

### Low-level HTML, DOM, and CSSOM surface

- Namespaced HTML/SVG elements, text and comments; canonical attributes; Boolean attributes; class/token lists; `data-*`; DOM-property writes; element references; and controlled custom elements.
- Void/raw-text/escapable-raw-text element rules, whitespace policy, Unicode validation, URL attributes, form-control live properties, and optional declarative Shadow DOM/custom-element hydration hooks.
- Stylesheets, layers, imports, media/supports/container conditions, selectors, declarations, custom properties, keyframes, pseudo classes/elements, and scoped component rules.
- Safe opt-in raw HTML/CSS tokens require explicit trust markers and policy approval; ordinary text and values always use contextual encoding.

### Layout containers

- Block/flow, inline, vertical/horizontal stack, flex row/column, wrapping flow, CSS grid, uniform grid, dock/border, overlay, absolute/canvas-like positioning, split pane, scroll container, and responsive shell layouts.
- Gap, padding, margin, border, width/height/min/max, aspect ratio, alignment, justification, order, grow/shrink/basis, grid tracks/areas/spans, sticky position, overflow, clipping, containment, z-index, and safe-area insets.
- Spacer, separator, card/group box, field set, collapsible region, resizable panels, and scroll-to/reveal behavior.

### Content and feedback controls

- Plain text, headings, paragraphs, labels, code/preformatted text, quotations, links, icons, images, figures/captions, badges, chips/tags, status text, and semantic landmarks.
- Progress bar, indeterminate progress, meter, skeleton/busy placeholder, spinner, empty state, error boundary view, inline alert, toast/status notification, and live-region announcements.
- Audio/video/embed are safe opt-in media nodes with fallback content and capability policy; inline SVG uses namespaced low-level nodes.

### Buttons and input controls

- Push, submit, reset, toggle, split, icon, and command-backed buttons with enabled/visible/busy/pressed state.
- Single-line text, password, search, email, URL, telephone, multiline text, numeric, range, date, time, month, week, local date-time, color, file, hidden, and read-only display fields.
- Checkbox, radio group, switch, select, multi-select, list box, combo box, autocomplete, editable combo, and segmented choice controls.
- Placeholder, helper text, prefix/suffix, clear/reveal buttons, input mode, autocomplete tokens, min/max/step, length constraints, accepted file types, multiple files, spellcheck, enter-key hint, and change-vs-input update policy.

### Lists, tables, and trees

- Ordered/unordered/description lists and interactive list/listbox views with stable item keys, item templates, empty/loading/error rows, single/multiple/range selection, active item, grouping, filtering, sorting, paging, drag reorder, and incremental item changes.
- Semantic tables and interactive data grids with captions, row/column headers, column groups, fixed/resizable/reorderable/hideable columns, sort indicators, filter rows, grouped rows, summaries, frozen regions, cell/row selection, inline editing, keyboard navigation, and row details.
- Trees and tree grids with parent/child identity, depth/position metadata, lazy children, expand/collapse, single/multiple selection, check state, type-ahead, keyboard navigation, drag/drop, and virtualized flattened ranges.

### Menus, navigation controls, and overlays

- Menu bar, pull-down menu, context menu, submenu, menu button, command palette, check/radio menu items, separators, icons, accelerators, mnemonics, roving focus, type-ahead, and dismissal rules.
- Tabs with automatic/manual activation, orientation, close/reorder/overflow behavior, lazy panels, and correct tab/tablist/tabpanel relationships.
- Toolbars with groups, separators, overflow menu, toggle/radio tools, embedded fields, roving focus, orientation, and command status projection.
- Breadcrumbs, pagination, navigation rail/sidebar, stepper, and link state.
- Modal/non-modal dialogs, alert dialogs, confirm/prompt workflows, sheets/drawers, popovers, tooltips, anchored positioning, backdrop, focus trap, focus restoration, escape/outside-click dismissal, stacking, and inert background handling.

### Forms and validation

- Form/edit context, field registration, dirty/touched/visited/pending/valid state, initial/reset values, submit/reset/cancel, and validation summary.
- Required, length, numeric range, step, regular expression, comparison, selection count, and custom handler validation at field, cross-field, form, and remote-result levels.
- Validation triggers on input/change/blur/submit/manual request; severity levels; multiple messages; message IDs wired with `aria-describedby`; first-invalid focus; native constraint attributes where equivalent.
- Synchronous and task-returning validation handlers, cancellation/version tokens for stale remote results, exceptionless validation result APIs, and server/host error merging.

### Events and commands

- Click/double-click/context, pointer, mouse, wheel, keyboard, focus, input/change/beforeinput, composition, selection, submit/reset, drag/drop, clipboard, scroll, resize, visibility, toggle, and semantic control events.
- Capture/target/bubble phase, delegation, prevent-default, stop-propagation, once/passive flags, debounce/throttle policy, trusted-origin metadata, normalized modifiers/buttons/coordinates, and event-to-node path validation.
- Commands with stable ID, label/description/icon, execute handler, status query, enabled/visible/checked/busy state, parameter value, shortcut(s), scope, command group, and change notification.
- Keyboard shortcut normalization, platform modifier aliases, chords/sequences, conflict resolution by active scope, repeat policy, and command projection into buttons/menu items/toolbars.

### Observable state and data binding

- Scoped observable state store with create/read/write/remove, compare-and-set, snapshots, restore, transactions/batches, versioning, subscriptions, and deterministic notification order.
- Primitive/object/list state paths expressed with RFC 6901-compatible JSON pointers and mutation operations aligned with RFC 6902 where practical.
- Derived state with explicit dependency IDs, memoized value/version, cycle detection, dirty propagation, and deterministic reevaluation.
- Effects scheduled after commit with dependency IDs, cleanup handler, cancellation, error routing, and prevention of render-time writes.
- One-time, one-way, two-way, and one-way-to-source bindings for text, attributes, properties, classes, styles, visibility, enabled state, selection, form values, items, and command state.
- Converters and validators represented by interfaces/handler IDs; null/fallback values; update triggers; loop prevention; origin tracking; and binding error diagnostics.

### Components and lifecycle

- Reusable component definition with stable type ID, parameter values, slots, local state scope, context provision/consumption, render handler, and optional lifecycle/effect/error handlers.
- Create, attach, parameters-changed, before-render, after-render, activate, deactivate, detach, dispose, hydration-start, hydration-complete, and error notifications with parent-before-child/child-before-parent ordering defined per stage.
- Keyed component identity, conditional preservation, explicit remount key, deterministic cleanup, nested error boundaries, and scheduler protection from reentrant/infinite updates.
- Suspense-like pending content and task completion notifications without making rendering itself asynchronous.

### Routing and navigation

- Route templates with literals, named parameters, optional segments, catch-all segments, constraints, defaults, precedence, query strings, fragments, base path, URL decoding, and ambiguity detection.
- Nested routes/layouts/outlets, index and not-found routes, route data inheritance, redirect result, and route-linked titles/metadata.
- Programmatic push/replace/back/forward/reload, link interception, external-navigation policy, same-document fragments, navigation state, scroll restoration, and focus-on-navigation.
- Before/after/cancel/error observers, cancellable guards, pending view, stale-navigation suppression, browser history synchronization, and initial deep-link hydration.

### Focus, keyboard, pointer, clipboard, and drag/drop

- Programmatic focus/blur, focus scopes, tab order, roving `tabindex`, `aria-activedescendant`, focus trap, initial focus, focus return, focus-visible state, and resilient focus preservation across patches.
- APG-compatible key maps for buttons, radio groups, listboxes, combo boxes, menus, menu bars, tabs, toolbars, dialogs, grids, trees, tree grids, sliders, and disclosure widgets.
- Logical direction handling for RTL, type-ahead buffers, composition/IME safety, key repeat, modifier normalization, pointer capture, touch/pen/mouse normalization, and context menu behavior.
- Clipboard copy/cut/paste policy and drag source/drop target data with allowed effect, keyboard alternative, drag image hook, and untrusted payload validation.

### Accessibility and internationalization

- Native-role selection, explicit ARIA role/state/property/relationship model, landmarks, heading levels, labels/descriptions, accessible-name inputs, live regions, busy/invalid/current/expanded/selected/checked/pressed states, set position/size, and ownership/control relationships.
- Validation for prohibited/redundant ARIA, required role children/parents/properties, duplicate IDs, broken ID references, missing accessible names, keyboard-inaccessible actions, hidden focus targets, and color-independent status text.
- High-contrast/forced-colors, reduced motion, reduced transparency where available, zoom/reflow, target sizing, visible focus, screen-reader-only content, and non-color status indicators.
- Language and text direction inheritance, locale-neutral model values, culture-aware display/parse handlers, localized text catalogs/plural keys, bidirectional isolation, and invariant deterministic serialization.

### Themes, styling, CSS generation, responsive behavior, and motion

- Theme tokens for color, typography, spacing, sizing, radius, borders, shadows, z-layers, motion, focus rings, and control states; named light/dark/high-contrast variants and host overrides through CSS custom properties.
- Global, window, page, component-scope, control-variant, state, and inline style precedence with canonical selector/declaration order and collision-resistant deterministic scope names.
- CSS identifier/string/URL escaping, property allowlists for untrusted values, unit-aware dimensions, color normalization, shorthand avoidance where determinism benefits, deduplication, rule interning, and stable stylesheet hashes.
- Viewport/container breakpoints, min/max width/height/orientation/pointer/hover/color-scheme/contrast/motion conditions, responsive value overrides, responsive visibility/order/layout, and resize observation.
- CSS transitions/keyframes and enter/leave/move animations with cancellation, lifecycle notifications, reduced-motion fallback, and no animation requirement for correctness.

### Virtualization and incremental data

- Fixed- and variable-extent vertical/horizontal virtualization; overscan; viewport/range requests; stable keys; placeholders; total count; anchor preservation; measured extents; and scroll-to-index/key/alignment.
- Provider requests with start/count/sort/filter/group/tree context, request generation, cancellation/stale-result rejection, partial/end-of-data results, loading/error/retry rows, and unknown total counts.
- Virtual list, grid row/column, tree flattening, and tabular accessibility metadata (`aria-rowcount`, indexes, positions) independent of materialized DOM count.

### Deterministic rendering, updates, hydration, and security

- Canonical model validation and normalization, stable sibling ordering, deterministic IDs/style scopes, ordinal comparisons, invariant number formatting, normalized line endings, explicit culture, and no dependence on dictionary enumeration or wall-clock/random values.
- Contextual HTML text/attribute escaping, Unicode scalar validation/replacement policy, URL scheme allowlist, CSS token encoding, JSON `<`/script-safe encoding, safe comment handling, and trust-policy gates for raw content.
- Full document, fragment, head, body, CSS, bootstrap script, and hydration manifest rendering; doctype, charset, viewport, title, base URL, CSP nonce field, language/direction, metadata, and resource hints.
- Keyed tree diff for insert/remove/move/replace, text/attribute/property/class/style/event updates, deterministic mutation ordering, duplicate-key detection, subtree bailout by version/hash, and full-replace fallback.
- Patch batching with revision/base revision, transaction boundaries, acknowledgement, rejection/recovery, idempotency, resync snapshots, and preservation directives for focus, selection, input composition, scroll, media state, and open overlays.
- Hydration markers/manifests, exact/repair/replace modes, node/key/tag/attribute/text checks, mismatch diagnostics, event/binding attachment, state handoff, component lifecycle hooks, and deterministic server/client parity requirements.

### WebView bridge and message transport

- Abstract send/start/stop/capabilities transport plus inbound message handler; implementations supplied by host applications or tests.
- Version/capability handshake, ready state, origin and main-frame checks, session nonce, message size/depth/count limits, schema validation, ordered sequence, duplicate detection, correlation IDs, request/response/error, notification, cancellation, timeout, heartbeat, and reconnect/resync.
- Message kinds for initialize, render snapshot, DOM patch, event, command, state mutation, validation, navigation, focus, measurement, virtualization range/result, lifecycle, log/diagnostic, host call/result, and shutdown.
- Chunking/reassembly for large messages, base64 binary payload option, bounded pending requests, backpressure, acknowledgement windows, deterministic JSON, protocol-error isolation, and sensitive-value redaction in diagnostics.
- Browser adapters for `window.chrome.webview`, `window.webkit.messageHandlers`, Android injected message listener/reply proxy, `MessagePort`/`postMessage`, and loopback test transport selected by feature detection rather than compile-time SDK references.

## Proposed public API and planned source files

All public types use the `HtmlGui` namespace. Data classes below are field-only public models (with internal helpers only), and every listed enum is consumed by one of those models or an interface.

### `src/Api/Data`

- `Values.cs`: `UiValue`, `UiValueEntry`, `UiDiagnostic`, `UiCapability`, and `UiResult` tagged/field-based transport values.
- `Hierarchy.cs`: `UiApplication`, `UiWindow`, `UiPage`, `UiView`, `UiDocumentMetadata`, `UiResource`, `UiSlot`, and `UiPortal`.
- `Nodes.cs`: `UiNode`, `UiNodeIdentity`, `UiNodeState`, `UiConditional`, `UiRepeat`, and `UiRawContentPolicy`.
- `Dom.cs`: `DomNode`, `DomAttribute`, `DomProperty`, `DomEventSubscription`, `DomSnapshot`, and `DomMutation`.
- `Layout.cs`: `UiLayout`, `UiLength`, `UiSpacing`, `UiGridTrack`, `UiGridPlacement`, `UiPosition`, and `UiScrollOptions`.
- `Controls.cs`: `UiText`, `UiButton`, `UiInput`, `UiChoice`, `UiMedia`, `UiFeedback`, and `UiDisclosure`.
- `Collections.cs`: `UiCollection`, `UiCollectionItem`, `UiSelection`, `UiSort`, `UiFilter`, `UiGroup`, and `UiPageRange`.
- `Tables.cs`: `UiTable`, `UiTableColumn`, `UiTableRow`, `UiTableCell`, `UiTableSummary`, and `UiEditState`.
- `Trees.cs`: `UiTree`, `UiTreeItem`, `UiTreeRelationship`, and `UiTreeRange`.
- `Menus.cs`: `UiMenu`, `UiMenuItem`, `UiTabs`, `UiTab`, `UiToolbar`, `UiTool`, `UiBreadcrumb`, and `UiPagination`.
- `Overlays.cs`: `UiDialog`, `UiPopover`, `UiTooltip`, `UiToast`, `UiAnchor`, and `UiOverlayState`.
- `Forms.cs`: `UiForm`, `UiField`, `UiValidationRule`, `UiValidationIssue`, `UiValidationReport`, and `UiFormState`.
- `Events.cs`: `UiEventBinding`, `UiEventData`, `UiPointerData`, `UiKeyboardData`, `UiInputData`, `UiClipboardData`, and `UiDragData`.
- `Commands.cs`: `UiCommand`, `UiCommandInvocation`, `UiCommandStatus`, `UiShortcut`, and `UiShortcutChord`.
- `State.cs`: `UiStateEntry`, `UiStateChange`, `UiStatePatch`, `UiDerivedState`, `UiEffect`, and `UiSubscription`.
- `Bindings.cs`: `UiBinding`, `UiBindingContext`, `UiBindingUpdate`, and `UiConversionResult`.
- `Components.cs`: `UiComponentDefinition`, `UiComponentContext`, `UiComponentRenderRequest`, `UiLifecycleNotification`, and `UiErrorBoundaryState`.
- `Routing.cs`: `UiRoute`, `UiRouteSegment`, `UiRouteData`, `UiNavigationRequest`, `UiNavigationResult`, and `UiHistoryEntry`.
- `Focus.cs`: `UiFocusRequest`, `UiFocusState`, `UiKeyboardMap`, and `UiTypeAheadState`.
- `Accessibility.cs`: `UiAccessibility`, `UiAriaRelationship`, `UiLiveAnnouncement`, and `UiAccessibilityReport`.
- `Styling.cs`: `UiTheme`, `UiThemeVariant`, `UiStyle`, `UiCssDeclaration`, `UiCssRule`, `UiCssCondition`, `UiResponsiveRule`, and `UiAnimation`.
- `Virtualization.cs`: `UiVirtualization`, `UiRangeRequest`, `UiRangeResult`, `UiMeasuredItem`, and `UiScrollTarget`.
- `Rendering.cs`: `UiRenderOptions`, `UiRenderResult`, `UiUpdateBatch`, `UiUpdateResult`, `UiHydrationManifest`, `UiHydrationResult`, and `UiPreservationState`.
- `Bridge.cs`: `UiBridgeOptions`, `UiBridgeEnvelope`, `UiBridgeRequest`, `UiBridgeResponse`, `UiBridgeError`, and `UiTransportCapabilities`.

### `src/Api/Contracts`

- `IUiFactory.cs`: uniquely named creation methods for hierarchy nodes, low-level elements, layouts, every high-level control family, bindings, styles, routes, and bridge options.
- `IUiModelValidator.cs`: `ValidateModel` and `TryValidateModel` operations returning diagnostics without mutation.
- `IUiRenderer.cs`: `RenderDocument`, `RenderFragment`, `RenderStyles`, and `RenderBootstrap`.
- `IUiUpdateEngine.cs`: `CreateDomSnapshot`, `ComputeDomUpdates`, `AcceptDomUpdates`, and `RejectDomUpdates`.
- `IUiHydrator.cs`: `CreateHydrationManifest` and `PlanHydration`.
- `IUiStateStore.cs` and `IUiStateObserver.cs`: state CRUD, snapshots, batching, compare-and-set, derived/effect registration, subscribe/unsubscribe, and change callback.
- `IUiBindingEngine.cs` and `IUiValueConverter.cs`: attach/detach/evaluate/push binding operations and conversion callbacks.
- `IUiEventDispatcher.cs` and `IUiEventHandler.cs`: handler registration, normalized event dispatch, cancellation/default result, and event callback.
- `IUiCommandDispatcher.cs` and `IUiCommandHandler.cs`: command registration, status query, execution, shortcut dispatch, and handler callbacks.
- `IUiValidationEngine.cs` and `IUiValidationHandler.cs`: field/form validation, issue merge/clear, submit eligibility, and custom validation callbacks.
- `IUiComponentRuntime.cs`, `IUiComponentRenderer.cs`, and `IUiLifecycleHandler.cs`: component registration/render/invalidate/context/lifecycle/dispose operations and callbacks.
- `IUiRouter.cs` and `IUiNavigationObserver.cs`: route registration/resolve, navigation/history operations, guards, and navigation callbacks.
- `IUiFocusController.cs`: focus scope registration, focus/move/restore/trap operations, and patch-preservation decisions.
- `IUiKeyboardController.cs`: key-map registration, shortcut/type-ahead/composite-widget key processing, and reset.
- `IUiVirtualizationEngine.cs` and `IUiRangeProvider.cs`: viewport/range calculation, provider request, measurement update, result acceptance, and scroll target calculation.
- `IUiAccessibilityEngine.cs`: native/ARIA semantic expansion, accessible-name inputs, live announcements, and accessibility validation.
- `IUiCssCompiler.cs`: theme resolution, scoped rule generation, responsive rule generation, and canonical CSS serialization.
- `IUiMessageTransport.cs` and `IUiBridgeMessageHandler.cs`: start/send/stop/capability methods and inbound-message callback.
- `IUiWebViewBridge.cs`: handshake, request, notification, receive, acknowledgement, timeout sweep, reconnect, and shutdown operations.
- `IUiHost.cs`: high-level initialize, mount, update, dispatch, navigate, receive-message, snapshot, and shutdown operations.

### `src/Api/Enums`

- `UiEnums.cs`: `UiValueKind`, `UiNodeKind`, `DomNodeKind`, `UiLayoutKind`, `UiOrientation`, `UiAlignment`, `UiJustification`, `UiOverflowMode`, `UiLengthUnit`, `UiTextKind`, `UiButtonKind`, `UiInputKind`, `UiSelectionMode`, `UiSortDirection`, `UiMenuItemKind`, `UiDialogKind`, `UiValidationSeverity`, `UiValidationTrigger`, `UiEventKind`, `UiEventPhase`, `UiCommandCheckState`, `UiBindingMode`, `UiBindingTrigger`, `UiLifecycleStage`, `UiNavigationKind`, `UiNavigationStatus`, `UiFocusDirection`, `UiAriaLiveMode`, `UiAriaCurrentKind`, `UiThemeMode`, `UiResponsiveOperator`, `UiAnimationStage`, `UiMutationKind`, `UiHydrationMode`, `UiHydrationStatus`, `UiUpdatePriority`, `UiBridgeMessageKind`, `UiTransportState`, and `UiDiagnosticSeverity`.

### `src/Api/Static`

- `HtmlNames.cs`: `HtmlElements`, `HtmlAttributes`, and `HtmlInputTypes` constant fields.
- `AriaNames.cs`: `AriaRoles`, `AriaAttributes`, and `AriaKeyNames` constant fields.
- `CssNames.cs`: `CssProperties`, `CssUnits`, and `CssMediaFeatures` constant fields.
- `ProtocolNames.cs`: `DomEventNames`, `BridgeProtocolNames`, and `BridgeProtocolVersions` constant fields.

### `src/Api/Exceptions`

- `UiModelException.cs`, `UiRenderException.cs`, `UiStateException.cs`, `UiBindingException.cs`, `UiComponentException.cs`, `UiHydrationException.cs`, and `UiBridgeProtocolException.cs`, each deriving directly from `System.Exception`.

### `src/Api/Classes`

- `UiFactory.cs` implements `IUiFactory`.
- `UiModelValidator.cs` implements `IUiModelValidator`.
- `UiRenderer.cs` implements `IUiRenderer`.
- `UiUpdateEngine.cs` implements `IUiUpdateEngine`.
- `UiHydrator.cs` implements `IUiHydrator`.
- `UiStateStore.cs` implements `IUiStateStore`.
- `UiBindingEngine.cs` implements `IUiBindingEngine`.
- `UiEventDispatcher.cs` implements `IUiEventDispatcher`.
- `UiCommandDispatcher.cs` implements `IUiCommandDispatcher`.
- `UiValidationEngine.cs` implements `IUiValidationEngine`.
- `UiComponentRuntime.cs` implements `IUiComponentRuntime`.
- `UiRouter.cs` implements `IUiRouter`.
- `UiFocusController.cs` implements `IUiFocusController`.
- `UiKeyboardController.cs` implements `IUiKeyboardController`.
- `UiVirtualizationEngine.cs` implements `IUiVirtualizationEngine`.
- `UiAccessibilityEngine.cs` implements `IUiAccessibilityEngine`.
- `UiCssCompiler.cs` implements `IUiCssCompiler`.
- `UiWebViewBridge.cs` implements `IUiWebViewBridge`.
- `UiHost.cs` implements `IUiHost`; its single constructor receives the required public service interfaces and transport.

### Internal and browser-runtime source files

- `src/Core/`: `Guard.cs`, `ValueComparer.cs`, `ValueJsonCodec.cs`, `StableId.cs`, `StableHash.cs`, `ModelWalker.cs`, `ModelNormalizer.cs`, `DependencyGraph.cs`, `UpdateScheduler.cs`, and `DiagnosticSink.cs`.
- `src/Rendering/`: `SemanticExpander.cs`, `RenderTreeBuilder.cs`, `HtmlEscaper.cs`, `HtmlSerializer.cs`, `CssEscaper.cs`, `CssSerializer.cs`, `DocumentComposer.cs`, `DomDiffer.cs`, `MutationSerializer.cs`, `HydrationPlanner.cs`, and `PreservationPlanner.cs`.
- `src/Runtime/`: `StateStoreCore.cs`, `BindingCore.cs`, `EventCore.cs`, `CommandCore.cs`, `ValidationCore.cs`, `ComponentCore.cs`, `RouterCore.cs`, `FocusCore.cs`, `KeyboardCore.cs`, `VirtualizationCore.cs`, and `AccessibilityCore.cs`.
- `src/Interop/`: `BridgeCodec.cs`, `BridgeValidator.cs`, `BridgeSession.cs`, `PendingRequestTable.cs`, `ChunkAssembler.cs`, and `CapabilityNegotiator.cs`.
- `src/Browser/htmlgui.runtime.js`: dependency-free DOM creation/hydration/patch/event/focus/observer/transport runtime, embedded by `src/htmlgui.csproj`.

## Test layout and coverage rule

- Tests may use NUnit, Roslyn, browser simulators, property/fuzzing helpers, or other test-only packages; production code may not reference them.
- Each public data type gets construction/default, populated, invalid, JSON round-trip where applicable, and deterministic comparison/serialization coverage.
- Each public interface method is invoked through its interface and public implementation, including success, boundary, invalid-input, repeated-call/idempotency, ordering, and exception/result behavior.
- Golden outputs cover exact HTML, CSS, JSON, hydration manifests, and mutation batches; semantic assertions cover DOM meaning so snapshots cannot conceal invalid behavior.
- Cross-feature scenarios cover controls inside forms/components/routes/virtualized collections, theme and responsive changes, hydration followed by events, and bridge reconnect/resync.
- Coverage completion means no behaviorally reachable branch in `src/Api` is untested and no public member exists without at least one direct contract test. This is functional API coverage, not a syntax-policy/source-checking test.

## Implementation task graph

### Foundation and portable model

- [DONE] T001: Create the planned source/test directory structure and configure the owned browser runtime as an embedded resource without adding a production dependency.
- [DONE] T002: Implement internal argument guards, ordinal comparers, invariant formatting, stable hashing, deterministic ID generation, and diagnostic collection.
- [DONE] T003: Implement all public enums and constant-only static name/protocol classes and consume every enum from a model or contract.
- [DONE] T004: Implement `UiValue`, value entries, tagged optional-value conventions, deep deterministic comparison, cloning, and bounded JSON conversion.
- [DONE] T005: Implement application, window, page, view, document metadata, resource, slot, portal, node identity, and node-state data models.
- [DONE] T006: Implement low-level DOM node, attribute, property, event-subscription, snapshot, and mutation data models.
- [DONE] T007: Implement layout, control, collection, table, tree, menu, overlay, and form data models.
- [DONE] T008: Implement event, command, state, binding, component, routing, focus, accessibility, styling, virtualization, rendering, and bridge data models.
- [DONE] T009: Implement the public exception classes with contextual error fields/messages and direct `System.Exception` bases.
- [DONE] T010: Implement `IUiFactory` and `UiFactory` creation operations with safe defaults and stable identity requirements for every model/control family.
- [DONE] T011: Implement graph walking, cycle detection, duplicate identity/key detection, parent/child legality, and non-mutating model validation.
- [DONE] T012: Implement model normalization for defaults, canonical ordering, hierarchy inheritance, culture independence, and immutable-by-convention normalized copies.
- [DONE] T013: Implement `IUiModelValidator`/`UiModelValidator` exception and exceptionless validation paths with location-aware diagnostics.
- [DONE] T014: Add data-model tests covering every public data field/default, populated graph, tagged value kind, enum use, and invalid optional-value combination.
- [DONE] T015: Add factory tests covering every creation method, safe defaults, distinct instances, supplied identity, and all control families.
- [DONE] T016: Add model validator/normalizer tests for cycles, duplicates, illegal nesting, missing references, deterministic order, non-mutation, and diagnostic locations.

### Semantic hierarchy, elements, and layouts

- [DONE] T017: Implement semantic expansion from application/window/page/view/component/control nodes into a renderer-neutral keyed tree.
- [DONE] T018: Implement page head/body composition including doctype, charset, viewport, title, language, direction, base, metadata, resource references, shell, content, and overlays.
- [DONE] T019: Implement low-level HTML/SVG element, text, comment, Boolean attribute, token-list, `data-*`, property, custom-element, and trusted raw-content handling.
- [DONE] T020: Implement fragment, conditional, repeated/keyed, slot projection, and named portal expansion with stable identity across branch changes.
- [DONE] T021: Implement flow/inline, stack, flex, wrapping, grid/uniform-grid, dock, overlay, absolute, split, scroll, and responsive-shell layout expansion.
- [DONE] T022: Implement sizing, spacing, alignment, grid placement, positioning, overflow, containment, sticky, safe-area, z-order, resize, and scroll/reveal options.
- [DONE] T023: Implement spacer, separator, card, group box, field set, disclosure, collapsible region, and resizable-panel semantic/layout templates.
- [DONE] T024: Add hierarchy/composition tests for multiple windows/pages/views, slots, fragments, conditions, repeats, portals, metadata inheritance, and stable IDs.
- [DONE] T025: Add low-level element tests for namespaces, void/raw-text rules, attributes/properties/tokens, custom elements, comments, and raw-content policy rejection.
- [DONE] T026: Add layout tests for every container and option combination, logical RTL behavior, CSS mapping, invalid tracks/spans, overflow, sticky, and safe-area output.

### Content, controls, collections, and overlays

- [DONE] T027: Implement text, heading, paragraph, label, code/pre, quote, link, icon, image, figure, badge, chip, landmark, and localized text expansion.
- [DONE] T028: Implement progress, meter, spinner, skeleton, empty/error/loading state, alert, toast, status, and live-announcement expansion.
- [DONE] T029: Implement safe opt-in audio/video/embed and inline SVG nodes with fallback content and resource capability policy.
- [DONE] T030: Implement push/submit/reset/toggle/split/icon/command button semantics and interactive state projection.
- [DONE] T031: Implement text-like, multiline, password, search, email, URL, telephone, numeric, range, temporal, color, file, hidden, and read-only input mappings.
- [DONE] T032: Implement checkbox, radio, switch, select, multi-select, listbox, combo box, autocomplete, editable combo, and segmented choice mappings.
- [DONE] T033: Implement input adornments, helper/error text, clear/reveal actions, autocomplete/input-mode hints, constraints, file acceptance, spellcheck, and update triggers.
- [DONE] T034: Implement static and interactive lists with templates, keyed items, loading/empty/error states, grouping, filtering, sorting, paging, selection, active item, and reorder intent.
- [DONE] T035: Implement semantic table/data-grid structure, columns/rows/cells/headers/captions, sorting/filtering/grouping/summaries, frozen regions, selection, editing, resizing, reordering, and details.
- [DONE] T036: Implement tree/tree-grid flattening, lazy children, expansion, selection/check state, relationship metadata, type-ahead data, drag intent, and range materialization.
- [DONE] T037: Implement menu bar, popup/context/menu-button/submenus, menu item kinds, separators, accelerators, mnemonics, type-ahead, and dismissal state.
- [DONE] T038: Implement tabs, tab panels, close/reorder/overflow/lazy behavior, toolbar groups/tools/overflow, breadcrumbs, pagination, rail/sidebar, and stepper.
- [DONE] T039: Implement modal/non-modal/alert dialogs, prompt/confirm model, sheets, drawers, popovers, tooltips, anchor positioning, backdrop, stacking, and dismissal policy.
- [DONE] T040: Add content/feedback/media tests covering every semantic variant, fallback, live-region mode, locale/direction, and unsafe resource rejection.
- [DONE] T041: Add button/input/choice tests covering every kind, constraint, state, value/property mapping, command projection, helper relationship, and event trigger.
- [DONE] T042: Add list/table/tree tests covering all selection modes, key stability, sort/filter/group/page metadata, editing, lazy states, invalid relationships, and accessible indices.
- [DONE] T043: Add menu/tab/toolbar/navigation tests covering structure, active state, overflow, accelerator presentation, lazy panels, and command changes.
- [DONE] T044: Add dialog/popover/tooltip/toast tests covering modality, inert background intent, stacking, anchor loss, each dismissal route, and focus restore target.

### CSS, themes, responsiveness, and motion

- [DONE] T045: Implement CSS identifier, string, URL, selector, and declaration-value escaping plus trusted/untrusted property policies.
- [DONE] T046: Implement theme token resolution, variant inheritance, host overrides, missing/cyclic token diagnostics, and light/dark/high-contrast selection.
- [DONE] T047: Implement style precedence across global/window/page/component/control/state/inline scopes and deterministic collision-resistant scope IDs.
- [DONE] T048: Implement canonical CSS rule/declaration serialization, conditions, layers, custom properties, rule interning, deduplication, stable ordering, and stylesheet hashes.
- [DONE] T049: Implement unit-aware lengths, colors, typography, spacing, borders, radius, shadows, focus rings, z-layers, and control-state selector generation.
- [DONE] T050: Implement viewport/container responsive rules for size/orientation/input/color/contrast/motion features and responsive value/layout/visibility/order overrides.
- [DONE] T051: Implement transitions/keyframes and enter/leave/move animation states, cancellation, lifecycle messages, and reduced-motion fallback.
- [DONE] T052: Implement `IUiCssCompiler` and `UiCssCompiler` operations over the theme/style/responsive model.
- [DONE] T053: Add CSS safety tests for every escaping context, malicious tokens/URLs, allowed properties, invalid units/colors/selectors, and trust policy.
- [DONE] T054: Add theme/style tests for precedence, variants, overrides, cycles, scoping, interning, hashing, and byte-identical reordered-input output.
- [DONE] T055: Add responsive/motion tests for each condition/operator, container versus viewport output, RTL, forced colors, reduced motion, and cancellation.

### Accessibility, focus, and keyboard interaction

- [DONE] T056: Implement native semantic selection and ARIA role/state/property/relationship projection for every high-level control.
- [DONE] T057: Implement accessible-name input resolution, label/description/error relationships, landmark/heading metadata, and live announcements.
- [DONE] T058: Implement accessibility validation for roles, required context/owned children/properties, redundant/prohibited ARIA, IDs/IDREFs, names, hidden focus, and keyboard access.
- [DONE] T059: Implement high-contrast, zoom/reflow, focus-visible, target-size, reduced-motion, screen-reader-only, busy, and non-color status support hooks.
- [DONE] T060: Implement `IUiAccessibilityEngine` and `UiAccessibilityEngine` expansion/report operations.
- [DONE] T061: Implement focus scopes, programmatic focus/blur, tab order, roving tabindex, active descendant, traps, initial/return focus, and reveal behavior.
- [DONE] T062: Implement focus/selection/composition/scroll preservation decisions for node insert/move/remove/replace patches.
- [DONE] T063: Implement APG keyboard maps for button, radio, listbox, combo, menu/menu bar, tabs, toolbar, dialog, grid, tree/tree-grid, slider, and disclosure.
- [DONE] T064: Implement logical RTL navigation, type-ahead buffers, key repeat/modifier handling, platform aliases, IME composition safety, and reset rules.
- [DONE] T065: Implement `IUiFocusController`/`UiFocusController` and `IUiKeyboardController`/`UiKeyboardController`.
- [DONE] T066: Add accessibility mapping tests for every control role/state/name/relationship and native-semantics-first behavior.
- [DONE] T067: Add accessibility validator tests for every report category, multi-error accumulation, locations, and valid APG composite examples.
- [DONE] T068: Add focus tests for scopes, traps, roving/active-descendant strategies, initial/return focus, removal fallback, and patch preservation.
- [DONE] T069: Add keyboard tests for every APG map, orientation, RTL, modifier alias, sequence conflict, type-ahead timeout, repeat, and IME suppression.

### Events, commands, clipboard, and drag/drop

- [DONE] T070: Implement normalized event data conversion for pointer/mouse/wheel/key/focus/input/composition/selection/form/drag/clipboard/scroll/resize/visibility/toggle events.
- [DONE] T071: Implement handler registration and event dispatch across capture/target/bubble with delegation, once/passive, prevent-default, stop-propagation, and path validation.
- [DONE] T072: Implement deterministic debounce/throttle scheduling, trusted-origin/main-frame metadata, stale-node rejection, and semantic control-event synthesis.
- [DONE] T073: Implement `IUiEventDispatcher`/`UiEventDispatcher` and handler callback flow without public delegates or C# events.
- [DONE] T074: Implement command registration, scoped status query, execution, checked/busy state, parameter passing, change notification, and unregister cleanup.
- [DONE] T075: Implement shortcut normalization, chords/sequences, active-scope precedence, conflict diagnostics, repeat policy, and semantic command invocation.
- [DONE] T076: Implement `IUiCommandDispatcher`/`UiCommandDispatcher` and command projection updates for buttons, menus, and toolbars.
- [DONE] T077: Implement clipboard policy and copy/cut/paste payload normalization plus drag source/drop target state, effects, keyboard alternative, and payload validation.
- [DONE] T078: Add event tests for every event kind/payload, phase/order, delegation, propagation flags, once/passive behavior, stale path, debounce, and throttle.
- [DONE] T079: Add command tests for registration/status/execute/unregister, handler failures, scope changes, projection, shortcut aliases/chords/conflicts/repeats.
- [DONE] T080: Add clipboard/drag tests for allowed and rejected data, effects, cross-origin/untrusted payloads, keyboard equivalent, and cancellation.

### Observable state and bindings

- [DONE] T081: Implement scoped state entries, deterministic create/read/write/remove, compare-and-set, monotonic versions, and origin metadata.
- [DONE] T082: Implement atomic batches/transactions, nested-batch rules, snapshots/restores, rollback on validation failure, and ordered deduplicated notifications.
- [DONE] T083: Implement RFC 6901-compatible state paths and bounded add/remove/replace/move/copy/test patch operations with exceptionless results.
- [DONE] T084: Implement derived-state dependency graph, automatic dirty propagation, memoization, cycle detection, and deterministic topological reevaluation.
- [DONE] T085: Implement post-commit effects, explicit dependencies, cleanup handler calls, cancellation/versioning, error routing, and render-write prevention.
- [DONE] T086: Implement subscription scope, weak-by-lifetime registration semantics without weak public types, unsubscribe/dispose cleanup, and reentrancy handling.
- [DONE] T087: Implement `IUiStateStore`/`UiStateStore` and state observer callbacks for all operations.
- [DONE] T088: Implement binding attachment for one-time/one-way/two-way/one-way-to-source modes and text/attribute/property/class/style/visibility/enabled/selection/value/items/command targets.
- [DONE] T089: Implement converter callback, fallback/null behavior, input/change/blur/explicit triggers, origin-based loop prevention, and binding diagnostics.
- [DONE] T090: Implement `IUiBindingEngine`/`UiBindingEngine` lifecycle and propagation ordering across batched state and DOM events.
- [DONE] T091: Add state tests for every value kind/path/operation, CAS, nested batches, rollback, snapshots, versions, observer order, unsubscribe, and reentrancy.
- [DONE] T092: Add derived/effect tests for dynamic dependencies, memoization, diamond graphs, cycles, cleanup order, cancellation, failures, and forbidden writes.
- [DONE] T093: Add binding tests for every mode/target/trigger, conversion success/failure, fallback, null, validation interaction, batching, loop prevention, and detach cleanup.

### Forms and validation

- [DONE] T094: Implement form/edit context field registration and dirty/touched/visited/pending/valid state transitions, initial values, reset, cancel, and submit state.
- [DONE] T095: Implement built-in required/length/range/step/pattern/comparison/selection-count validators and equivalent native constraint emission.
- [DONE] T096: Implement custom field/cross-field/form/task-returning validation callbacks, result versions, cancellation, stale-result suppression, and exception routing.
- [DONE] T097: Implement input/change/blur/submit/manual triggers, issue merge/replace/clear, severity ordering, validation summary, first-invalid focus, and ARIA wiring.
- [DONE] T098: Implement host/server validation issue ingestion keyed by form/field/version and preserve user edits made after the submitted version.
- [DONE] T099: Implement `IUiValidationEngine`/`UiValidationEngine` and exceptionless validity/submission APIs.
- [DONE] T100: Add form-state tests for registration, every state transition, initial/reset/cancel/submit, dynamic fields, unmount, and nested form rejection.
- [DONE] T101: Add built-in validation tests for boundary values, invariant/culture-aware parsing, Unicode length policy, patterns, cross-field comparison, and selection counts.
- [DONE] T102: Add custom/remote validation tests for ordering, cancellation, stale results, server merging, exceptions, trigger policies, first-invalid focus, summary, and ARIA IDs.

### Components and lifecycle

- [DONE] T103: Implement component registry, stable type/key identity, parameters, local state scopes, slots, context provide/consume, and render callback dispatch.
- [DONE] T104: Implement component create/attach/parameter/before-render/after-render/activate/deactivate/detach/dispose and hydration lifecycle ordering.
- [DONE] T105: Implement keyed preservation/remount behavior, conditional deactivation, deterministic child cleanup, and context/subscription/effect teardown.
- [DONE] T106: Implement nested component error boundaries, fallback rendering, reset keys, diagnostic propagation, and unrecoverable root failure behavior.
- [DONE] T107: Implement invalidation/update scheduler with batching, priorities, parent/child ordering, reentrancy guards, maximum update depth, and pending-content completion.
- [DONE] T108: Implement `IUiComponentRuntime`/`UiComponentRuntime` and renderer/lifecycle handler callbacks without public delegates.
- [DONE] T109: Add component tests for registration, parameters, slots, contexts, local state, keyed identity, remount, conditionals, and nested composition.
- [DONE] T110: Add lifecycle tests for exact ordering at every stage, repeat attach/detach, hydration, cleanup, invalidation batching, and task completion.
- [DONE] T111: Add component error/scheduler tests for boundary selection, reset, handler failures, reentrant updates, depth limit, and root failure.

### Routing and navigation

- [DONE] T112: Implement URL/base-path canonicalization, percent decoding, query/fragment parsing, safe scheme handling, and invariant route values.
- [DONE] T113: Implement route template parsing for literals/parameters/optional/catch-all/constraints/defaults plus precedence and ambiguity validation.
- [DONE] T114: Implement nested/index/not-found routes, layouts/outlets, inherited route data, redirects, and page title/metadata selection.
- [DONE] T115: Implement push/replace/back/forward/reload/link navigation, external policy, fragment targets, history state, and scroll/focus restoration intents.
- [DONE] T116: Implement cancellable guards and before/pending/after/cancel/error observers with generation IDs and stale-navigation suppression.
- [DONE] T117: Implement browser history bridge messages, initial deep-link resolution, route hydration handoff, and reconnect reconciliation.
- [DONE] T118: Implement `IUiRouter`/`UiRouter` and navigation observer callbacks with an exceptionless route-resolution path.
- [DONE] T119: Add route parser/matcher tests for every segment kind/constraint/default, encoding, base path, precedence, ambiguity, invalid input, nested outlet, and not-found path.
- [DONE] T120: Add navigation tests for every navigation kind, history/scroll/focus state, link interception, external policy, redirect loops, guards, cancellation, stale completion, and hydration deep link.

### Deterministic HTML rendering and CSS output

- [DONE] T121: Implement Unicode-aware contextual HTML text/attribute/comment/raw-text escaping with safe replacement/rejection policy.
- [DONE] T122: Implement URL scheme validation/normalization, safe resource attributes, script/JSON closing-token protection, CSP nonce propagation, and raw-content trust gates.
- [DONE] T123: Implement canonical HTML serialization for namespaces, attributes, Boolean attributes, void/raw-text elements, text, comments, fragments, and line endings.
- [DONE] T124: Implement full document/head/body, fragment, CSS, bootstrap asset, hydration manifest, and resource rendering with deterministic ordering.
- [DONE] T125: Implement render-tree sequence/key generation, duplicate-key diagnostics, subtree version/hash annotations, and independence from input dictionary order/culture/time/randomness.
- [DONE] T126: Implement `IUiRenderer`/`UiRenderer` public operations and complete `UiRenderResult` diagnostics/resources/hash fields.
- [DONE] T127: Add escaping/security tests with malformed Unicode, metacharacters, script/style terminators, entity lookalikes, malicious URLs/CSS/JSON/comments, and raw-policy denial.
- [DONE] T128: Add exact HTML tests for every node/control/form/ARIA/layout construct, namespace, void/raw-text rule, metadata/resource option, and fragment/document mode.
- [DONE] T129: Add determinism tests across cultures, time zones, randomized insertion order, repeated renders, equivalent models, line endings, and stable IDs/hashes.
- [DONE] T130: Add renderer failure/result tests for invalid models, cycles, duplicate keys/IDs, invalid raw content, unsupported namespaces, and diagnostic locations.

### Incremental DOM updates and hydration

- [DONE] T131: Implement keyed tree diff matching and insert/remove/move/replace/text mutation generation with deterministic parent-safe ordering.
- [DONE] T132: Implement attribute/property/class/style/event-subscription diffs and property-specific handling for input value/checked/selection/open/media state.
- [DONE] T133: Implement subtree hash/version bailout, duplicate/unstable-key detection, mutation coalescing, and full-subtree/full-document fallback.
- [DONE] T134: Implement update batches with base/new revision, transaction boundaries, acknowledgement, rejection, idempotent replay, resync snapshot, and bounded history.
- [DONE] T135: Implement preservation plans for focus, selection ranges, IME composition, scroll anchors, media playback, open overlays, and user-edited controlled fields.
- [DONE] T136: Implement `IUiUpdateEngine`/`UiUpdateEngine` snapshot/diff/accept/reject operations.
- [DONE] T137: Implement hydration manifest markers for nodes/components/events/bindings/state/styles and exact/repair/replace planning modes.
- [DONE] T138: Implement hydration comparison, mismatch diagnostics, state handoff, listener/binding attachment plan, component lifecycle notifications, and client/server parity hash.
- [DONE] T139: Implement `IUiHydrator`/`UiHydrator` manifest and plan operations.
- [DONE] T140: Add diff tests for every mutation kind, keyed reorder/insert/delete, unkeyed fallback, nested moves, attributes/properties/events, ordering, coalescing, and bailout.
- [DONE] T141: Add revision tests for accept/reject/replay, stale base, bounded history, recovery snapshot, idempotency, and preservation metadata.
- [DONE] T142: Add hydration tests for exact success and every mismatch class under exact/repair/replace modes, state handoff, lifecycle order, listener attachment, and parity hash.

### Virtualization

- [DONE] T143: Implement fixed-extent viewport-to-range calculation, overscan, clamping, orientation, scroll target alignment, and anchor preservation.
- [DONE] T144: Implement variable-extent measurement cache, estimated extents, correction deltas, resize updates, unknown totals, and stable-key anchoring.
- [DONE] T145: Implement range-provider request generations, sort/filter/group/tree context, cancellation, stale-result rejection, partial/end results, and loading/error/retry rows.
- [DONE] T146: Implement virtual list, grid row/column, and flattened tree materialization with accessible total/index/position metadata.
- [DONE] T147: Implement `IUiVirtualizationEngine`/`UiVirtualizationEngine` and range-provider callbacks.
- [DONE] T148: Add fixed/variable virtualization tests for empty/small/huge logical counts, boundaries, overscan, horizontal mode, resize corrections, anchor preservation, and scroll targets.
- [DONE] T149: Add provider/integration tests for cancellation, stale/out-of-order results, unknown totals, sort/filter changes, tree expansion, errors/retry, selection, and accessible indices.

### WebView bridge and browser runtime

- [DONE] T150: Implement deterministic bridge JSON codec and strict envelope/payload validation with protocol/session/origin/frame/sequence/correlation checks and bounded input limits.
- [DONE] T151: Implement capability/version handshake, ready/closing states, nonce/session negotiation, feature fallback, heartbeat, timeout sweep, and orderly shutdown.
- [DONE] T152: Implement ordered notifications and request/response/error/cancel flow with bounded pending requests, duplicate detection, acknowledgement windows, and backpressure.
- [DONE] T153: Implement chunk split/reassembly, base64 binary values, checksum/length validation, expiry, resource bounds, and sensitive-field diagnostic redaction.
- [DONE] T154: Implement reconnect and resync using last acknowledged revisions, state/route/focus handoff, full snapshot fallback, and stale-session isolation.
- [DONE] T155: Implement `IUiWebViewBridge`/`UiWebViewBridge` against injected `IUiMessageTransport` and `IUiBridgeMessageHandler` callbacks.
- [DONE] T156: Implement the browser runtime bootstrap, capability detection, handshake, transport abstraction, message validation, sequencing, acknowledgements, and resync.
- [DONE] T157: Implement browser runtime DOM snapshot creation, hydration, deterministic mutation application, revision transactions, rollback-to-resync, and preservation restore.
- [DONE] T158: Implement browser runtime delegated events, semantic payload extraction, default/propagation decisions, controlled input synchronization, and stale-node suppression.
- [DONE] T159: Implement browser runtime focus/selection/scroll/IME/overlay behavior, resize/intersection observers, virtual range messages, and lifecycle callbacks.
- [DONE] T160: Implement browser adapters for WebView2, WKWebView, Android message listener/reply proxy, `MessagePort`/`postMessage`, and loopback feature-detected transports.
- [DONE] T161: Add bridge codec/validation tests for every message kind, malformed JSON/schema, version/capability mismatch, origin/frame/session/sequence errors, and resource limits.
- [DONE] T162: Add bridge session tests for handshake, request/response/error/cancel, timeout, heartbeat, duplicates, acknowledgements, backpressure, chunking, reconnect, resync, shutdown, and redaction.
- [DONE] T163: Add browser-runtime tests in a test-only DOM environment for bootstrap, each adapter, hydration, every mutation, events, focus/selection/scroll/IME preservation, observers, and resync.
- [DONE] T164: Add host-adapter contract fixtures that simulate WebView2, WKWebView, Android WebView, MessagePort, delayed/out-of-order delivery, disconnect, and hostile inbound pages.

### High-level host integration and completion coverage

- [DONE] T165: Implement `IUiHost`/`UiHost` orchestration for validation, normalization, initial mount, full render, state-driven update, event/command dispatch, navigation, bridge receive, snapshot, and shutdown.
- [DONE] T166: Integrate scheduler transactions so one event can update state, bindings, validation, commands, components, route/focus state, render diff, and bridge batch in deterministic order.
- [DONE] T167: Implement host failure isolation, diagnostic routing, component boundary recovery, rejected-batch resync, transport loss recovery, and deterministic cleanup of all handlers/scopes.
- [DONE] T168: Add end-to-end model-to-document tests for representative desktop WebView, browser SPA, server-prerendered/hydrated, multi-window, RTL, high-contrast, and no-JavaScript initial views.
- [DONE] T169: Add end-to-end interaction tests for forms/validation, command menus/toolbars, tabs/dialogs, list/grid/tree selection/editing, routing, focus, and state binding through bridge round trips.
- [DONE] T170: Add end-to-end incremental-update tests for keyed component/collection changes, responsive/theme changes, animation lifecycle, virtual ranges, hydration repair, reconnect, and full resync.
- [DONE] T171: Add exhaustive public data/static/enum/exception API behavior tests, including construction, all meaningful values, invalid values, serialization, and exception context.
- [DONE] T172: Add exhaustive interface/implementation contract tests that call every public method through its interface and cover success, boundary, repeated, invalid, and failure behavior.
- [DONE] T173: Add a public API coverage inventory test that maps every public member to functional test cases and fails only when an exposed behavior has no direct coverage.
- [DONE] T174: Add randomized model/state/event/patch/bridge property tests for deterministic output, round-trip invariants, no crashes on bounded hostile input, and convergence after updates.
- [DONE] T175: Run the complete test suite with production warnings treated as errors, resolve all functional failures, and verify that `src/` has no package dependency beyond the .NET framework reference.
