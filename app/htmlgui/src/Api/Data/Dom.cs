using System;

namespace htmlgui;

/// <summary>Describes a renderer-neutral DOM element, text, comment, fragment, or trusted-content node.</summary>
public class DomNode
{
    /// <summary>The deterministic preorder position within the render tree.</summary>
    public int Sequence;
    /// <summary>The DOM node category.</summary>
    public DomNodeKind Kind;
    /// <summary>The stable reconciliation key.</summary>
    public string Key = string.Empty;
    /// <summary>The deterministic DOM identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The optional namespace URI for an element.</summary>
    public string? NamespaceUri;
    /// <summary>The element local name.</summary>
    public string Name = string.Empty;
    /// <summary>The optional text, comment, or trusted-content payload.</summary>
    public string? Text;
    /// <summary>The trust-policy identifier carried by an approved raw-content node.</summary>
    public string? RawContentPolicyId;
    /// <summary>The canonical serialized attributes.</summary>
    public DomAttribute[] Attributes = Array.Empty<DomAttribute>();
    /// <summary>The live DOM property writes.</summary>
    public DomProperty[] Properties = Array.Empty<DomProperty>();
    /// <summary>The delegated or direct event subscriptions.</summary>
    public DomEventSubscription[] EventSubscriptions = Array.Empty<DomEventSubscription>();
    /// <summary>The child nodes in document order.</summary>
    public DomNode[] Children = Array.Empty<DomNode>();
    /// <summary>The optional deterministic component style-scope token.</summary>
    public string? StyleScope;
    /// <summary>The optional hydration marker.</summary>
    public string? HydrationMarker;
    /// <summary>The model version used for subtree bailout decisions.</summary>
    public int Version;
    /// <summary>The optional deterministic subtree content hash.</summary>
    public string? SubtreeHash;
}

/// <summary>Describes a canonical DOM attribute.</summary>
public class DomAttribute
{
    /// <summary>The optional attribute namespace URI.</summary>
    public string? NamespaceUri;
    /// <summary>The optional namespace prefix used during serialization.</summary>
    public string? Prefix;
    /// <summary>The attribute local name.</summary>
    public string Name = string.Empty;
    /// <summary>The encoded attribute value before contextual HTML escaping.</summary>
    public string Value = string.Empty;
    /// <summary>Whether the attribute uses Boolean presence semantics.</summary>
    public bool IsBoolean;
}

/// <summary>Describes a live DOM property write whose value is not represented only by markup.</summary>
public class DomProperty
{
    /// <summary>The DOM property name.</summary>
    public string Name = string.Empty;
    /// <summary>The portable property value.</summary>
    public UiValue Value = new UiValue();
}

/// <summary>Describes one browser event subscription and its dispatch options.</summary>
public class DomEventSubscription
{
    /// <summary>The stable subscription identifier.</summary>
    public string Id = string.Empty;
    /// <summary>The stable handler identifier invoked for the event.</summary>
    public string HandlerId = string.Empty;
    /// <summary>The normalized event kind.</summary>
    public UiEventKind Kind;
    /// <summary>The canonical DOM event name.</summary>
    public string EventName = DomEventNames.Click;
    /// <summary>The propagation phase in which the handler runs.</summary>
    public UiEventPhase Phase = UiEventPhase.Bubble;
    /// <summary>Whether the browser runtime uses delegated dispatch.</summary>
    public bool IsDelegated = true;
    /// <summary>Whether the browser default action is prevented.</summary>
    public bool PreventDefault;
    /// <summary>Whether event propagation stops after this handler.</summary>
    public bool StopPropagation;
    /// <summary>Whether the subscription is removed after its first dispatch.</summary>
    public bool Once;
    /// <summary>Whether the handler is passive.</summary>
    public bool Passive;
    /// <summary>Whether <see cref="DomEventSubscription.DebounceMilliseconds"/> is explicitly supplied.</summary>
    public bool HasDebounceMilliseconds;
    /// <summary>The debounce interval when <see cref="DomEventSubscription.HasDebounceMilliseconds"/> is true.</summary>
    public int DebounceMilliseconds;
    /// <summary>Whether <see cref="DomEventSubscription.ThrottleMilliseconds"/> is explicitly supplied.</summary>
    public bool HasThrottleMilliseconds;
    /// <summary>The throttle interval when <see cref="DomEventSubscription.HasThrottleMilliseconds"/> is true.</summary>
    public int ThrottleMilliseconds;
}

/// <summary>Captures a deterministic renderer-neutral DOM state at a revision boundary.</summary>
public class DomSnapshot
{
    /// <summary>The bridge or rendering session identifier.</summary>
    public string SessionId = string.Empty;
    /// <summary>The deterministic seed shared by the snapshot roots.</summary>
    public string RootSeed = string.Empty;
    /// <summary>The monotonically increasing snapshot revision.</summary>
    public int Revision;
    /// <summary>The snapshot roots in document or fragment order.</summary>
    public DomNode[] Roots = Array.Empty<DomNode>();
    /// <summary>The deterministic hash of the complete snapshot.</summary>
    public string ContentHash = string.Empty;
}

/// <summary>Describes one ordered mutation applied to a renderer-neutral or browser DOM tree.</summary>
public class DomMutation
{
    /// <summary>The mutation operation.</summary>
    public UiMutationKind Kind;
    /// <summary>The zero-based order of the mutation within its batch.</summary>
    public int Sequence;
    /// <summary>The stable identifier of the mutation target.</summary>
    public string TargetId = string.Empty;
    /// <summary>The optional stable identifier of the target parent.</summary>
    public string? ParentId;
    /// <summary>The optional stable identifier of a sibling used as an insertion reference.</summary>
    public string? ReferenceId;
    /// <summary>The optional node payload for insert or replace operations.</summary>
    public DomNode? Node;
    /// <summary>The optional attribute payload.</summary>
    public DomAttribute? Attribute;
    /// <summary>The optional property or style payload.</summary>
    public DomProperty? Property;
    /// <summary>The optional event-subscription payload.</summary>
    public DomEventSubscription? EventSubscription;
    /// <summary>The optional text, class-token, style-name, or event identifier payload.</summary>
    public string? Name;
    /// <summary>The optional text or serialized value payload.</summary>
    public string? Value;
    /// <summary>Whether <see cref="DomMutation.Index"/> is explicitly supplied.</summary>
    public bool HasIndex;
    /// <summary>The destination child index when <see cref="DomMutation.HasIndex"/> is true.</summary>
    public int Index;
    /// <summary>Whether <see cref="DomMutation.FromIndex"/> is explicitly supplied.</summary>
    public bool HasFromIndex;
    /// <summary>The original child index when <see cref="DomMutation.HasFromIndex"/> is true.</summary>
    public int FromIndex;
    /// <summary>Whether <see cref="DomMutation.ToIndex"/> is explicitly supplied.</summary>
    public bool HasToIndex;
    /// <summary>The new child index when <see cref="DomMutation.HasToIndex"/> is true.</summary>
    public int ToIndex;
}
