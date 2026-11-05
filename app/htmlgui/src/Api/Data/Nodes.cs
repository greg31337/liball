using System;

namespace htmlgui;

/// <summary>Describes a portable node shared by hierarchy, semantic, and low-level content.</summary>
public class UiNode
{
    /// <summary>The node category.</summary>
    public UiNodeKind Kind;
    /// <summary>The node's deterministic and author-supplied identity.</summary>
    public UiNodeIdentity Identity = new UiNodeIdentity();
    /// <summary>The interactive and presentation state of the node.</summary>
    public UiNodeState State = new UiNodeState();
    /// <summary>The optional namespace URI for a low-level element.</summary>
    public string? NamespaceUri;
    /// <summary>The element, component, control, or layout name.</summary>
    public string Name = string.Empty;
    /// <summary>The optional text or trusted-content payload.</summary>
    public string? Text;
    /// <summary>The node's logical children.</summary>
    public UiNode[] Children = Array.Empty<UiNode>();
    /// <summary>The named content slots owned by the node.</summary>
    public UiSlot[] Slots = Array.Empty<UiSlot>();
    /// <summary>The optional conditional branch description.</summary>
    public UiConditional? Conditional;
    /// <summary>The optional keyed repetition description.</summary>
    public UiRepeat? Repeat;
    /// <summary>The optional portal associated with the node.</summary>
    public UiPortal? Portal;
    /// <summary>Additional portable values associated with the node.</summary>
    public UiValueEntry[] Values = Array.Empty<UiValueEntry>();
    /// <summary>The low-level attributes applied to an element node.</summary>
    public DomAttribute[] Attributes = Array.Empty<DomAttribute>();
    /// <summary>The named token-list attributes applied to an element node.</summary>
    public UiTokenList[] TokenLists = Array.Empty<UiTokenList>();
    /// <summary>The custom data attributes applied to an element node, without the <c>data-</c> prefix.</summary>
    public UiValueEntry[] DataAttributes = Array.Empty<UiValueEntry>();
    /// <summary>The live properties applied to an element node.</summary>
    public DomProperty[] Properties = Array.Empty<DomProperty>();
    /// <summary>Whether an element name is explicitly permitted to identify a custom element.</summary>
    public bool IsCustomElement;
    /// <summary>The explicit trust description required by a raw-content node.</summary>
    public UiRawContentPolicy? RawContentPolicy;
    /// <summary>The optional layout payload.</summary>
    public UiLayout? Layout;
    /// <summary>The optional semantic text payload.</summary>
    public UiText? TextContent;
    /// <summary>The optional button payload.</summary>
    public UiButton? Button;
    /// <summary>The optional input or choice-control payload.</summary>
    public UiInput? Input;
    /// <summary>The optional media payload.</summary>
    public UiMedia? Media;
    /// <summary>The optional feedback payload.</summary>
    public UiFeedback? Feedback;
    /// <summary>The optional disclosure payload.</summary>
    public UiDisclosure? Disclosure;
    /// <summary>The optional collection payload.</summary>
    public UiCollection? Collection;
    /// <summary>The optional table or data-grid payload.</summary>
    public UiTable? Table;
    /// <summary>The optional tree or tree-grid payload.</summary>
    public UiTree? Tree;
    /// <summary>The optional menu payload.</summary>
    public UiMenu? Menu;
    /// <summary>The optional tabs payload.</summary>
    public UiTabs? Tabs;
    /// <summary>The optional toolbar payload.</summary>
    public UiToolbar? Toolbar;
    /// <summary>The optional breadcrumb payload.</summary>
    public UiBreadcrumb? Breadcrumb;
    /// <summary>The optional pagination payload.</summary>
    public UiPagination? Pagination;
    /// <summary>The optional dialog payload.</summary>
    public UiDialog? Dialog;
    /// <summary>The optional popover payload.</summary>
    public UiPopover? Popover;
    /// <summary>The optional tooltip payload.</summary>
    public UiTooltip? Tooltip;
    /// <summary>The optional toast payload.</summary>
    public UiToast? Toast;
    /// <summary>The optional form payload.</summary>
    public UiForm? Form;
    /// <summary>The event bindings.</summary>
    public UiEventBinding[] EventBindings = Array.Empty<UiEventBinding>();
    /// <summary>The optional most recent normalized event.</summary>
    public UiEventData? EventData;
    /// <summary>The data bindings.</summary>
    public UiBinding[] Bindings = Array.Empty<UiBinding>();
    /// <summary>The optional accessibility description.</summary>
    public UiAccessibility? Accessibility;
    /// <summary>The queued live announcements.</summary>
    public UiLiveAnnouncement[] Announcements = Array.Empty<UiLiveAnnouncement>();
    /// <summary>The optional accessibility report.</summary>
    public UiAccessibilityReport? AccessibilityReport;
    /// <summary>The optional style description.</summary>
    public UiStyle? Style;
    /// <summary>The optional active animation.</summary>
    public UiAnimation? Animation;
    /// <summary>The optional virtualization description.</summary>
    public UiVirtualization? Virtualization;
}

/// <summary>Describes one named, unordered set of element attribute tokens.</summary>
public class UiTokenList
{
    /// <summary>The attribute name that receives the canonical token list.</summary>
    public string Name = string.Empty;
    /// <summary>The tokens, which are deduplicated and ordered ordinally during expansion.</summary>
    public string[] Tokens = Array.Empty<string>();
}

/// <summary>Describes explicit application approval for trusted raw markup.</summary>
public class UiRawContentPolicy
{
    /// <summary>Whether trusted raw markup is approved for this node.</summary>
    public bool IsTrusted;
    /// <summary>The non-empty application-defined policy identifier that granted approval.</summary>
    public string PolicyId = string.Empty;
}

/// <summary>Describes the two branches of a logical conditional node.</summary>
public class UiConditional
{
    /// <summary>Whether the true branch is selected.</summary>
    public bool Condition;
    /// <summary>The content selected when <see cref="UiConditional.Condition"/> is true.</summary>
    public UiNode[] WhenTrue = Array.Empty<UiNode>();
    /// <summary>The content selected when <see cref="UiConditional.Condition"/> is false.</summary>
    public UiNode[] WhenFalse = Array.Empty<UiNode>();
}

/// <summary>Describes materialized keyed items under a repeated node.</summary>
public class UiRepeat
{
    /// <summary>The keyed items in presentation order.</summary>
    public UiCollectionItem[] Items = Array.Empty<UiCollectionItem>();
}

/// <summary>Describes stable node identity independent of browser-native objects.</summary>
public class UiNodeIdentity
{
    /// <summary>The author-supplied key that preserves identity among siblings.</summary>
    public string AuthorKey = string.Empty;
    /// <summary>The deterministic identifier emitted for the node.</summary>
    public string StableId = string.Empty;
    /// <summary>The optional deterministic identifier of the parent node.</summary>
    public string? ParentStableId;
    /// <summary>The canonical sibling identity used when deriving <see cref="UiNodeIdentity.StableId"/>.</summary>
    public string SiblingIdentity = string.Empty;
}

/// <summary>Describes common visibility, interaction, accessibility, and version state for a node.</summary>
public class UiNodeState
{
    /// <summary>Whether the node participates in presentation.</summary>
    public bool IsVisible = true;
    /// <summary>Whether the node permits interaction.</summary>
    public bool IsEnabled = true;
    /// <summary>Whether the node is processing an operation.</summary>
    public bool IsBusy;
    /// <summary>Whether the node can receive focus.</summary>
    public bool IsFocusable;
    /// <summary>Whether <see cref="UiNodeState.TabIndex"/> is explicitly supplied.</summary>
    public bool HasTabIndex;
    /// <summary>The explicit tab index when <see cref="UiNodeState.HasTabIndex"/> is true.</summary>
    public int TabIndex;
    /// <summary>Whether <see cref="UiNodeState.IsPressed"/> is explicitly supplied.</summary>
    public bool HasPressed;
    /// <summary>The pressed state when <see cref="UiNodeState.HasPressed"/> is true.</summary>
    public bool IsPressed;
    /// <summary>Whether <see cref="UiNodeState.IsSelected"/> is explicitly supplied.</summary>
    public bool HasSelected;
    /// <summary>The selected state when <see cref="UiNodeState.HasSelected"/> is true.</summary>
    public bool IsSelected;
    /// <summary>Whether <see cref="UiNodeState.IsExpanded"/> is explicitly supplied.</summary>
    public bool HasExpanded;
    /// <summary>The expanded state when <see cref="UiNodeState.HasExpanded"/> is true.</summary>
    public bool IsExpanded;
    /// <summary>Whether <see cref="UiNodeState.CheckState"/> is explicitly supplied.</summary>
    public bool HasCheckState;
    /// <summary>The checked state when <see cref="UiNodeState.HasCheckState"/> is true.</summary>
    public UiCommandCheckState CheckState;
    /// <summary>Whether <see cref="UiNodeState.CurrentKind"/> is explicitly supplied.</summary>
    public bool HasCurrentKind;
    /// <summary>The current-item kind when <see cref="UiNodeState.HasCurrentKind"/> is true.</summary>
    public UiAriaCurrentKind CurrentKind;
    /// <summary>The monotonically increasing model version used for subtree comparisons.</summary>
    public int Version;
}
