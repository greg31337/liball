namespace htmlgui;

/// <summary>Identifies the stored representation of a portable UI value.</summary>
public enum UiValueKind
{
    /// <summary>A null value.</summary>
    Null = 0,
    /// <summary>A Boolean value.</summary>
    Boolean = 1,
    /// <summary>A signed integer value.</summary>
    Integer = 2,
    /// <summary>A binary floating-point value.</summary>
    FloatingPoint = 3,
    /// <summary>A decimal value.</summary>
    Decimal = 4,
    /// <summary>A text value.</summary>
    String = 5,
    /// <summary>An invariant date and time text value.</summary>
    DateTime = 6,
    /// <summary>A binary value.</summary>
    Binary = 7,
    /// <summary>An ordered array value.</summary>
    Array = 8,
    /// <summary>A string-keyed object value.</summary>
    Object = 9
}

/// <summary>Identifies a portable UI model node.</summary>
public enum UiNodeKind
{
    /// <summary>An ordinary element node.</summary>
    Element = 0,
    /// <summary>A text node.</summary>
    Text = 1,
    /// <summary>A comment node.</summary>
    Comment = 2,
    /// <summary>A logical fragment.</summary>
    Fragment = 3,
    /// <summary>A reusable component.</summary>
    Component = 4,
    /// <summary>A high-level control.</summary>
    Control = 5,
    /// <summary>A layout container.</summary>
    Layout = 6,
    /// <summary>A conditional node.</summary>
    Conditional = 7,
    /// <summary>A keyed repeated node.</summary>
    Repeat = 8,
    /// <summary>A projected content slot.</summary>
    Slot = 9,
    /// <summary>A named portal.</summary>
    Portal = 10,
    /// <summary>Explicitly trusted raw content.</summary>
    RawContent = 11
}

/// <summary>Identifies a renderer-neutral DOM node.</summary>
public enum DomNodeKind
{
    /// <summary>An element node.</summary>
    Element = 0,
    /// <summary>A text node.</summary>
    Text = 1,
    /// <summary>A comment node.</summary>
    Comment = 2,
    /// <summary>A logical fragment node.</summary>
    Fragment = 3,
    /// <summary>A trusted raw-content node.</summary>
    RawContent = 4
}

/// <summary>Identifies the layout algorithm used by a container.</summary>
public enum UiLayoutKind
{
    /// <summary>Normal block flow.</summary>
    Flow = 0,
    /// <summary>Normal inline flow.</summary>
    Inline = 1,
    /// <summary>A vertical stack.</summary>
    VerticalStack = 2,
    /// <summary>A horizontal stack.</summary>
    HorizontalStack = 3,
    /// <summary>A flexible row.</summary>
    FlexRow = 4,
    /// <summary>A flexible column.</summary>
    FlexColumn = 5,
    /// <summary>A wrapping flow.</summary>
    Wrap = 6,
    /// <summary>A CSS grid.</summary>
    Grid = 7,
    /// <summary>A uniform grid.</summary>
    UniformGrid = 8,
    /// <summary>A dock or border layout.</summary>
    Dock = 9,
    /// <summary>An overlay stack.</summary>
    Overlay = 10,
    /// <summary>An absolute-positioning surface.</summary>
    Absolute = 11,
    /// <summary>A split-pane layout.</summary>
    Split = 12,
    /// <summary>A scrolling container.</summary>
    Scroll = 13,
    /// <summary>A responsive application shell.</summary>
    ResponsiveShell = 14
}

/// <summary>Identifies a horizontal or vertical direction.</summary>
public enum UiOrientation
{
    /// <summary>Horizontal orientation.</summary>
    Horizontal = 0,
    /// <summary>Vertical orientation.</summary>
    Vertical = 1
}

/// <summary>Identifies cross-axis alignment.</summary>
public enum UiAlignment
{
    /// <summary>Align at the logical start.</summary>
    Start = 0,
    /// <summary>Align at the center.</summary>
    Center = 1,
    /// <summary>Align at the logical end.</summary>
    End = 2,
    /// <summary>Stretch to the available size.</summary>
    Stretch = 3,
    /// <summary>Align text baselines.</summary>
    Baseline = 4
}

/// <summary>Identifies main-axis content distribution.</summary>
public enum UiJustification
{
    /// <summary>Place content at the logical start.</summary>
    Start = 0,
    /// <summary>Center content.</summary>
    Center = 1,
    /// <summary>Place content at the logical end.</summary>
    End = 2,
    /// <summary>Distribute space between items.</summary>
    SpaceBetween = 3,
    /// <summary>Distribute space around items.</summary>
    SpaceAround = 4,
    /// <summary>Distribute space evenly around items.</summary>
    SpaceEvenly = 5
}

/// <summary>Identifies overflow behavior.</summary>
public enum UiOverflowMode
{
    /// <summary>Overflow remains visible.</summary>
    Visible = 0,
    /// <summary>Overflow is hidden.</summary>
    Hidden = 1,
    /// <summary>Overflow is clipped without scrolling.</summary>
    Clip = 2,
    /// <summary>A scroll container is always available.</summary>
    Scroll = 3,
    /// <summary>A scroll container is available when needed.</summary>
    Auto = 4
}

/// <summary>Identifies the unit of a UI length.</summary>
public enum UiLengthUnit
{
    /// <summary>The browser determines the size.</summary>
    Auto = 0,
    /// <summary>CSS pixels.</summary>
    Pixel = 1,
    /// <summary>A percentage of the containing size.</summary>
    Percent = 2,
    /// <summary>Font-relative em units.</summary>
    Em = 3,
    /// <summary>Root-font-relative rem units.</summary>
    Rem = 4,
    /// <summary>Viewport width units.</summary>
    ViewportWidth = 5,
    /// <summary>Viewport height units.</summary>
    ViewportHeight = 6,
    /// <summary>Grid fractional units.</summary>
    Fraction = 7,
    /// <summary>Character width units.</summary>
    Character = 8,
    /// <summary>Centimeters.</summary>
    Centimeter = 9,
    /// <summary>Millimeters.</summary>
    Millimeter = 10,
    /// <summary>Inches.</summary>
    Inch = 11,
    /// <summary>Points.</summary>
    Point = 12,
    /// <summary>Picas.</summary>
    Pica = 13
}

/// <summary>Identifies a semantic text or content presentation.</summary>
public enum UiTextKind
{
    /// <summary>Plain inline text.</summary>
    Plain = 0,
    /// <summary>A heading.</summary>
    Heading = 1,
    /// <summary>A paragraph.</summary>
    Paragraph = 2,
    /// <summary>A form label.</summary>
    Label = 3,
    /// <summary>Inline code.</summary>
    Code = 4,
    /// <summary>Preformatted text.</summary>
    Preformatted = 5,
    /// <summary>A quotation.</summary>
    Quote = 6,
    /// <summary>A hyperlink.</summary>
    Link = 7,
    /// <summary>An icon.</summary>
    Icon = 8,
    /// <summary>An image.</summary>
    Image = 9,
    /// <summary>A figure with optional caption.</summary>
    Figure = 10,
    /// <summary>A badge.</summary>
    Badge = 11,
    /// <summary>A chip or tag.</summary>
    Chip = 12,
    /// <summary>Status text.</summary>
    Status = 13,
    /// <summary>A semantic landmark.</summary>
    Landmark = 14,
    /// <summary>A localized text key.</summary>
    Localized = 15
}

/// <summary>Identifies button behavior.</summary>
public enum UiButtonKind
{
    /// <summary>An ordinary push button.</summary>
    Push = 0,
    /// <summary>A form submit button.</summary>
    Submit = 1,
    /// <summary>A form reset button.</summary>
    Reset = 2,
    /// <summary>A pressed-state toggle button.</summary>
    Toggle = 3,
    /// <summary>A split action and menu button.</summary>
    Split = 4,
    /// <summary>An icon-only button.</summary>
    Icon = 5,
    /// <summary>A command-backed button.</summary>
    Command = 6
}

/// <summary>Identifies an input control.</summary>
public enum UiInputKind
{
    /// <summary>A single-line text input.</summary>
    Text = 0,
    /// <summary>A password input.</summary>
    Password = 1,
    /// <summary>A search input.</summary>
    Search = 2,
    /// <summary>An email-address input.</summary>
    Email = 3,
    /// <summary>A URL input.</summary>
    Url = 4,
    /// <summary>A telephone-number input.</summary>
    Telephone = 5,
    /// <summary>A multiline text input.</summary>
    Multiline = 6,
    /// <summary>A numeric input.</summary>
    Number = 7,
    /// <summary>A range input.</summary>
    Range = 8,
    /// <summary>A date input.</summary>
    Date = 9,
    /// <summary>A time input.</summary>
    Time = 10,
    /// <summary>A month input.</summary>
    Month = 11,
    /// <summary>A week input.</summary>
    Week = 12,
    /// <summary>A local date-time input.</summary>
    LocalDateTime = 13,
    /// <summary>A color input.</summary>
    Color = 14,
    /// <summary>A file input.</summary>
    File = 15,
    /// <summary>A hidden input.</summary>
    Hidden = 16,
    /// <summary>A read-only value display.</summary>
    ReadOnly = 17
}

/// <summary>Identifies supported selection behavior.</summary>
public enum UiSelectionMode
{
    /// <summary>Selection is disabled.</summary>
    None = 0,
    /// <summary>One item can be selected.</summary>
    Single = 1,
    /// <summary>Multiple independent items can be selected.</summary>
    Multiple = 2,
    /// <summary>Contiguous range selection is supported.</summary>
    Range = 3
}

/// <summary>Identifies sort direction.</summary>
public enum UiSortDirection
{
    /// <summary>No sort is applied.</summary>
    None = 0,
    /// <summary>Values sort in ascending order.</summary>
    Ascending = 1,
    /// <summary>Values sort in descending order.</summary>
    Descending = 2
}

/// <summary>Identifies menu-item behavior.</summary>
public enum UiMenuItemKind
{
    /// <summary>An ordinary command item.</summary>
    Command = 0,
    /// <summary>A checkable item.</summary>
    Checkbox = 1,
    /// <summary>An item in a radio group.</summary>
    Radio = 2,
    /// <summary>An item that opens a submenu.</summary>
    Submenu = 3,
    /// <summary>A visual separator.</summary>
    Separator = 4
}

/// <summary>Identifies dialog presentation and behavior.</summary>
public enum UiDialogKind
{
    /// <summary>A modal dialog.</summary>
    Modal = 0,
    /// <summary>A non-modal dialog.</summary>
    NonModal = 1,
    /// <summary>An urgent alert dialog.</summary>
    Alert = 2,
    /// <summary>A confirmation dialog.</summary>
    Confirm = 3,
    /// <summary>A prompt dialog.</summary>
    Prompt = 4,
    /// <summary>A sheet.</summary>
    Sheet = 5,
    /// <summary>A drawer.</summary>
    Drawer = 6
}

/// <summary>Identifies validation message severity.</summary>
public enum UiValidationSeverity
{
    /// <summary>An informational result.</summary>
    Information = 0,
    /// <summary>A warning result.</summary>
    Warning = 1,
    /// <summary>An error result.</summary>
    Error = 2
}

/// <summary>Identifies when validation runs.</summary>
public enum UiValidationTrigger
{
    /// <summary>Validate for every input update.</summary>
    Input = 0,
    /// <summary>Validate when the committed value changes.</summary>
    Change = 1,
    /// <summary>Validate when the field loses focus.</summary>
    Blur = 2,
    /// <summary>Validate when the form is submitted.</summary>
    Submit = 3,
    /// <summary>Validate only when explicitly requested.</summary>
    Manual = 4
}

/// <summary>Identifies a normalized UI event.</summary>
public enum UiEventKind
{
    /// <summary>A click event.</summary>
    Click = 0,
    /// <summary>A double-click event.</summary>
    DoubleClick = 1,
    /// <summary>A context-menu event.</summary>
    ContextMenu = 2,
    /// <summary>A pointer-down event.</summary>
    PointerDown = 3,
    /// <summary>A pointer-up event.</summary>
    PointerUp = 4,
    /// <summary>A pointer-move event.</summary>
    PointerMove = 5,
    /// <summary>A pointer-enter event.</summary>
    PointerEnter = 6,
    /// <summary>A pointer-leave event.</summary>
    PointerLeave = 7,
    /// <summary>A pointer-cancel event.</summary>
    PointerCancel = 8,
    /// <summary>A mouse-down event.</summary>
    MouseDown = 9,
    /// <summary>A mouse-up event.</summary>
    MouseUp = 10,
    /// <summary>A mouse-move event.</summary>
    MouseMove = 11,
    /// <summary>A mouse-enter event.</summary>
    MouseEnter = 12,
    /// <summary>A mouse-leave event.</summary>
    MouseLeave = 13,
    /// <summary>A wheel event.</summary>
    Wheel = 14,
    /// <summary>A key-down event.</summary>
    KeyDown = 15,
    /// <summary>A key-up event.</summary>
    KeyUp = 16,
    /// <summary>A focus event.</summary>
    Focus = 17,
    /// <summary>A blur event.</summary>
    Blur = 18,
    /// <summary>A bubbling focus-in event.</summary>
    FocusIn = 19,
    /// <summary>A bubbling focus-out event.</summary>
    FocusOut = 20,
    /// <summary>A before-input event.</summary>
    BeforeInput = 21,
    /// <summary>An input event.</summary>
    Input = 22,
    /// <summary>A change event.</summary>
    Change = 23,
    /// <summary>A composition-start event.</summary>
    CompositionStart = 24,
    /// <summary>A composition-update event.</summary>
    CompositionUpdate = 25,
    /// <summary>A composition-end event.</summary>
    CompositionEnd = 26,
    /// <summary>A selection-change event.</summary>
    SelectionChange = 27,
    /// <summary>A form-submit event.</summary>
    Submit = 28,
    /// <summary>A form-reset event.</summary>
    Reset = 29,
    /// <summary>A drag-start event.</summary>
    DragStart = 30,
    /// <summary>A drag event.</summary>
    Drag = 31,
    /// <summary>A drag-enter event.</summary>
    DragEnter = 32,
    /// <summary>A drag-over event.</summary>
    DragOver = 33,
    /// <summary>A drag-leave event.</summary>
    DragLeave = 34,
    /// <summary>A drop event.</summary>
    Drop = 35,
    /// <summary>A drag-end event.</summary>
    DragEnd = 36,
    /// <summary>A clipboard-copy event.</summary>
    Copy = 37,
    /// <summary>A clipboard-cut event.</summary>
    Cut = 38,
    /// <summary>A clipboard-paste event.</summary>
    Paste = 39,
    /// <summary>A scroll event.</summary>
    Scroll = 40,
    /// <summary>A resize event.</summary>
    Resize = 41,
    /// <summary>A visibility-change event.</summary>
    VisibilityChange = 42,
    /// <summary>A toggle event.</summary>
    Toggle = 43
}

/// <summary>Identifies the propagation phase of an event handler.</summary>
public enum UiEventPhase
{
    /// <summary>The capture phase.</summary>
    Capture = 0,
    /// <summary>The target phase.</summary>
    Target = 1,
    /// <summary>The bubble phase.</summary>
    Bubble = 2
}

/// <summary>Identifies a command's checked state.</summary>
public enum UiCommandCheckState
{
    /// <summary>The command is not checked.</summary>
    Unchecked = 0,
    /// <summary>The command is checked.</summary>
    Checked = 1,
    /// <summary>The command has a mixed state.</summary>
    Mixed = 2
}

/// <summary>Identifies data-binding direction.</summary>
public enum UiBindingMode
{
    /// <summary>Read the source once.</summary>
    OneTime = 0,
    /// <summary>Propagate source changes to the target.</summary>
    OneWay = 1,
    /// <summary>Propagate changes in both directions.</summary>
    TwoWay = 2,
    /// <summary>Propagate target changes to the source.</summary>
    OneWayToSource = 3
}

/// <summary>Identifies when a binding updates its source.</summary>
public enum UiBindingTrigger
{
    /// <summary>Update immediately.</summary>
    Immediate = 0,
    /// <summary>Update for input events.</summary>
    Input = 1,
    /// <summary>Update for committed change events.</summary>
    Change = 2,
    /// <summary>Update when focus leaves the target.</summary>
    Blur = 3,
    /// <summary>Update only when explicitly requested.</summary>
    Explicit = 4
}

/// <summary>Identifies a component lifecycle stage.</summary>
public enum UiLifecycleStage
{
    /// <summary>The component is created.</summary>
    Create = 0,
    /// <summary>The component is attached.</summary>
    Attach = 1,
    /// <summary>The component parameters changed.</summary>
    ParametersChanged = 2,
    /// <summary>The component is about to render.</summary>
    BeforeRender = 3,
    /// <summary>The component finished rendering.</summary>
    AfterRender = 4,
    /// <summary>The component became active.</summary>
    Activate = 5,
    /// <summary>The component became inactive.</summary>
    Deactivate = 6,
    /// <summary>The component is detached.</summary>
    Detach = 7,
    /// <summary>The component is disposed.</summary>
    Dispose = 8,
    /// <summary>Hydration started.</summary>
    HydrationStart = 9,
    /// <summary>Hydration completed.</summary>
    HydrationComplete = 10,
    /// <summary>A component error was reported.</summary>
    Error = 11
}

/// <summary>Identifies a navigation operation.</summary>
public enum UiNavigationKind
{
    /// <summary>Push a new history entry.</summary>
    Push = 0,
    /// <summary>Replace the current history entry.</summary>
    Replace = 1,
    /// <summary>Move backward in history.</summary>
    Back = 2,
    /// <summary>Move forward in history.</summary>
    Forward = 3,
    /// <summary>Reload the current location.</summary>
    Reload = 4,
    /// <summary>Navigate from an intercepted link.</summary>
    Link = 5,
    /// <summary>Navigate within the document to a fragment.</summary>
    Fragment = 6,
    /// <summary>Navigate to an external location.</summary>
    External = 7
}

/// <summary>Identifies the outcome of route resolution or navigation.</summary>
public enum UiNavigationStatus
{
    /// <summary>Navigation has not completed.</summary>
    Pending = 0,
    /// <summary>The destination resolved successfully.</summary>
    Resolved = 1,
    /// <summary>The destination redirects elsewhere.</summary>
    Redirected = 2,
    /// <summary>Navigation was cancelled.</summary>
    Cancelled = 3,
    /// <summary>No route matched.</summary>
    NotFound = 4,
    /// <summary>The destination is handled externally.</summary>
    External = 5,
    /// <summary>Navigation failed.</summary>
    Failed = 6
}

/// <summary>Identifies a logical focus movement.</summary>
public enum UiFocusDirection
{
    /// <summary>No directional movement.</summary>
    None = 0,
    /// <summary>Move to the first focus target.</summary>
    First = 1,
    /// <summary>Move to the last focus target.</summary>
    Last = 2,
    /// <summary>Move to the next focus target.</summary>
    Next = 3,
    /// <summary>Move to the previous focus target.</summary>
    Previous = 4,
    /// <summary>Move upward.</summary>
    Up = 5,
    /// <summary>Move downward.</summary>
    Down = 6,
    /// <summary>Move left.</summary>
    Left = 7,
    /// <summary>Move right.</summary>
    Right = 8
}

/// <summary>Identifies a programmatic focus operation.</summary>
public enum UiFocusOperation
{
    /// <summary>Focus an explicit, initial, active, or first target.</summary>
    Focus = 0,
    /// <summary>Clear the current focused and active-descendant targets.</summary>
    Blur = 1,
    /// <summary>Move through the active scope's tab order.</summary>
    Move = 2,
    /// <summary>Restore focus to the scope's return target.</summary>
    Restore = 3,
    /// <summary>Change the active descendant while retaining focus on the scope owner.</summary>
    SetActiveDescendant = 4
}

/// <summary>Identifies ARIA live-region announcement priority.</summary>
public enum UiAriaLiveMode
{
    /// <summary>Do not announce updates.</summary>
    Off = 0,
    /// <summary>Announce updates when convenient.</summary>
    Polite = 1,
    /// <summary>Announce updates immediately.</summary>
    Assertive = 2
}

/// <summary>Identifies the current item represented by <c>aria-current</c>.</summary>
public enum UiAriaCurrentKind
{
    /// <summary>The item is not current.</summary>
    None = 0,
    /// <summary>The current page.</summary>
    Page = 1,
    /// <summary>The current step.</summary>
    Step = 2,
    /// <summary>The current location.</summary>
    Location = 3,
    /// <summary>The current date.</summary>
    Date = 4,
    /// <summary>The current time.</summary>
    Time = 5,
    /// <summary>A current item without a more specific token.</summary>
    True = 6
}

/// <summary>Identifies an opt-in accessibility presentation or environment hook.</summary>
public enum UiAccessibilityFeature
{
    /// <summary>Expose a hook for forced-color and high-contrast presentation.</summary>
    ForcedColors = 0,
    /// <summary>Expose a hook for layouts that remain usable under browser zoom.</summary>
    Zoom = 1,
    /// <summary>Expose a hook for narrow-viewport content reflow.</summary>
    Reflow = 2,
    /// <summary>Expose a hook for keyboard-visible focus treatment.</summary>
    FocusVisible = 3,
    /// <summary>Expose a minimum pointer-target size.</summary>
    TargetSize = 4,
    /// <summary>Expose a hook for reduced-motion presentation.</summary>
    ReducedMotion = 5,
    /// <summary>Expose a hook for reduced-transparency presentation.</summary>
    ReducedTransparency = 6,
    /// <summary>Present the node through screen-reader-only styling.</summary>
    ScreenReaderOnly = 7,
    /// <summary>Expose the node's busy state to accessibility APIs.</summary>
    Busy = 8,
    /// <summary>Add a textual status indicator that does not depend on color.</summary>
    NonColorStatus = 9
}

/// <summary>Identifies a theme variant selection.</summary>
public enum UiThemeMode
{
    /// <summary>Use host or system preference.</summary>
    Auto = 0,
    /// <summary>Use a light theme.</summary>
    Light = 1,
    /// <summary>Use a dark theme.</summary>
    Dark = 2,
    /// <summary>Use a high-contrast theme.</summary>
    HighContrast = 3
}

/// <summary>Identifies a responsive condition.</summary>
public enum UiResponsiveOperator
{
    /// <summary>A minimum viewport or container width.</summary>
    MinWidth = 0,
    /// <summary>A maximum viewport or container width.</summary>
    MaxWidth = 1,
    /// <summary>A minimum viewport or container height.</summary>
    MinHeight = 2,
    /// <summary>A maximum viewport or container height.</summary>
    MaxHeight = 3,
    /// <summary>An orientation condition.</summary>
    Orientation = 4,
    /// <summary>A pointing-device accuracy condition.</summary>
    Pointer = 5,
    /// <summary>A hover capability condition.</summary>
    Hover = 6,
    /// <summary>A preferred color-scheme condition.</summary>
    ColorScheme = 7,
    /// <summary>A preferred contrast condition.</summary>
    Contrast = 8,
    /// <summary>A preferred motion condition.</summary>
    Motion = 9,
    /// <summary>A forced-colors mode condition.</summary>
    ForcedColors = 10
}

/// <summary>Identifies the stage of a UI animation.</summary>
public enum UiAnimationStage
{
    /// <summary>No animation is active.</summary>
    Idle = 0,
    /// <summary>An enter animation.</summary>
    Enter = 1,
    /// <summary>A leave animation.</summary>
    Leave = 2,
    /// <summary>A move animation.</summary>
    Move = 3,
    /// <summary>The animation completed.</summary>
    Completed = 4,
    /// <summary>The animation was cancelled.</summary>
    Cancelled = 5
}

/// <summary>Identifies a DOM mutation operation.</summary>
public enum UiMutationKind
{
    /// <summary>Insert a node.</summary>
    Insert = 0,
    /// <summary>Remove a node.</summary>
    Remove = 1,
    /// <summary>Move a keyed node.</summary>
    Move = 2,
    /// <summary>Replace a node or subtree.</summary>
    Replace = 3,
    /// <summary>Set text content.</summary>
    SetText = 4,
    /// <summary>Set an attribute.</summary>
    SetAttribute = 5,
    /// <summary>Remove an attribute.</summary>
    RemoveAttribute = 6,
    /// <summary>Set a DOM property.</summary>
    SetProperty = 7,
    /// <summary>Remove a DOM property.</summary>
    RemoveProperty = 8,
    /// <summary>Add a class token.</summary>
    AddClass = 9,
    /// <summary>Remove a class token.</summary>
    RemoveClass = 10,
    /// <summary>Set an inline style declaration.</summary>
    SetStyle = 11,
    /// <summary>Remove an inline style declaration.</summary>
    RemoveStyle = 12,
    /// <summary>Subscribe to an event.</summary>
    SubscribeEvent = 13,
    /// <summary>Unsubscribe from an event.</summary>
    UnsubscribeEvent = 14
}

/// <summary>Identifies the strategy used when hydration differs from server output.</summary>
public enum UiHydrationMode
{
    /// <summary>Require an exact match.</summary>
    Exact = 0,
    /// <summary>Repair mismatched nodes when possible.</summary>
    Repair = 1,
    /// <summary>Replace mismatched content.</summary>
    Replace = 2
}

/// <summary>Identifies a hydration result.</summary>
public enum UiHydrationStatus
{
    /// <summary>The DOM matched exactly.</summary>
    Matched = 0,
    /// <summary>The DOM was repaired.</summary>
    Repaired = 1,
    /// <summary>The DOM was replaced.</summary>
    Replaced = 2,
    /// <summary>A mismatch was detected but not changed.</summary>
    Mismatch = 3,
    /// <summary>Hydration failed.</summary>
    Failed = 4
}

/// <summary>Identifies update scheduling priority.</summary>
public enum UiUpdatePriority
{
    /// <summary>Process synchronously at the next safe boundary.</summary>
    Immediate = 0,
    /// <summary>Prioritize a user interaction.</summary>
    UserInteraction = 1,
    /// <summary>Use normal update priority.</summary>
    Normal = 2,
    /// <summary>Defer background work.</summary>
    Background = 3
}

/// <summary>Identifies a WebView bridge message.</summary>
public enum UiBridgeMessageKind
{
    /// <summary>Initialize a bridge session.</summary>
    Initialize = 0,
    /// <summary>Report that an endpoint is ready.</summary>
    Ready = 1,
    /// <summary>Transfer a complete render snapshot.</summary>
    RenderSnapshot = 2,
    /// <summary>Transfer an incremental DOM patch.</summary>
    DomPatch = 3,
    /// <summary>Transfer a UI event.</summary>
    Event = 4,
    /// <summary>Invoke or update a command.</summary>
    Command = 5,
    /// <summary>Transfer a state mutation.</summary>
    StateMutation = 6,
    /// <summary>Transfer validation data.</summary>
    Validation = 7,
    /// <summary>Transfer navigation data.</summary>
    Navigation = 8,
    /// <summary>Transfer a focus operation.</summary>
    Focus = 9,
    /// <summary>Transfer a measurement.</summary>
    Measurement = 10,
    /// <summary>Request a virtualized range.</summary>
    VirtualizationRange = 11,
    /// <summary>Return a virtualized range.</summary>
    VirtualizationResult = 12,
    /// <summary>Transfer a lifecycle notification.</summary>
    Lifecycle = 13,
    /// <summary>Transfer a log entry.</summary>
    Log = 14,
    /// <summary>Transfer a diagnostic.</summary>
    Diagnostic = 15,
    /// <summary>Invoke a host operation.</summary>
    HostCall = 16,
    /// <summary>Return a host-operation result.</summary>
    HostResult = 17,
    /// <summary>Begin a correlated request.</summary>
    Request = 18,
    /// <summary>Complete a correlated request.</summary>
    Response = 19,
    /// <summary>Report a correlated error.</summary>
    Error = 20,
    /// <summary>Cancel a correlated request.</summary>
    Cancel = 21,
    /// <summary>Acknowledge a message or revision.</summary>
    Acknowledgement = 22,
    /// <summary>Maintain session liveness.</summary>
    Heartbeat = 23,
    /// <summary>Shut down a bridge session.</summary>
    Shutdown = 24,
    /// <summary>Transfer one chunk of a larger message.</summary>
    Chunk = 25
}

/// <summary>Identifies the current transport lifecycle state.</summary>
public enum UiTransportState
{
    /// <summary>The transport is stopped.</summary>
    Stopped = 0,
    /// <summary>The transport is starting.</summary>
    Starting = 1,
    /// <summary>The transport is negotiating a session.</summary>
    Handshaking = 2,
    /// <summary>The transport is ready.</summary>
    Ready = 3,
    /// <summary>The transport is reconnecting.</summary>
    Reconnecting = 4,
    /// <summary>The transport is closing.</summary>
    Closing = 5,
    /// <summary>The transport has faulted.</summary>
    Faulted = 6
}

/// <summary>Identifies diagnostic severity exposed by the public API.</summary>
public enum UiDiagnosticSeverity
{
    /// <summary>An informational diagnostic.</summary>
    Information = 0,
    /// <summary>A warning diagnostic.</summary>
    Warning = 1,
    /// <summary>An error diagnostic.</summary>
    Error = 2
}
