namespace svg;

/// <summary>Stores one explicit visited or unvisited link-state override.</summary>
public class SvgInteractionLink
{
    /// <summary>The source link element.</summary>
    public SvgNode Node;
    /// <summary>The caller-provided link state.</summary>
    public SvgSelectorLinkState State;

    /// <summary>Creates one link-state override.</summary>
    /// <param name="node">The source link element.</param>
    /// <param name="state">The visited or unvisited state.</param>
    public SvgInteractionLink(SvgNode node, SvgSelectorLinkState state)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) throw new System.ArgumentOutOfRangeException("node");
        if (state != SvgSelectorLinkState.Unvisited && state != SvgSelectorLinkState.Visited) throw new System.ArgumentOutOfRangeException("state");
        Node = node;
        State = state;
    }
}

/// <summary>Owns synchronous caller-controlled SVG interaction and preference state.</summary>
public class SvgInteractionState
{
    /// <summary>The document whose source identities may receive interaction state.</summary>
    public SvgDocument Document;
    /// <summary>The monotonic interaction revision.</summary>
    public long Version;
    /// <summary>The element matching the current URL fragment target, or <c>null</c>.</summary>
    public SvgNode? Target;
    /// <summary>Explicit visited or unvisited overrides for recognized links.</summary>
    public System.Collections.Generic.List<SvgInteractionLink> Links;
    /// <summary>The deepest explicitly hovered element, or <c>null</c>.</summary>
    public SvgNode? Hovered;
    /// <summary>The explicitly active element, or <c>null</c>.</summary>
    public SvgNode? Active;
    /// <summary>The explicitly focused element, or <c>null</c>.</summary>
    public SvgNode? Focused;
    /// <summary>Whether caller-provided pointer coordinates are available.</summary>
    public bool HasPointer;
    /// <summary>The pointer horizontal coordinate in caller-defined viewport space.</summary>
    public double PointerX;
    /// <summary>The pointer vertical coordinate in caller-defined viewport space.</summary>
    public double PointerY;
    /// <summary>The source element currently under the pointer, or <c>null</c>.</summary>
    public SvgNode? PointerTarget;
    /// <summary>The caller-reported pressed pointer buttons.</summary>
    public SvgPointerButtons PointerButtons;
    /// <summary>The normalized host content language, or an empty string when unspecified.</summary>
    public string Language;
    /// <summary>Whether the caller requests reduced motion.</summary>
    public bool ReducedMotion;
    /// <summary>The dynamic selector callbacks backed by this state.</summary>
    public SvgDynamicSelectorState DynamicSelectors;

    /// <summary>Creates empty interaction state for one document.</summary>
    /// <param name="document">The source SVG document.</param>
    public SvgInteractionState(SvgDocument document)
    {
        if (document == null) throw new System.ArgumentNullException("document");
        Document = document;
        Version = 0;
        Target = null;
        Links = new System.Collections.Generic.List<SvgInteractionLink>();
        Hovered = null;
        Active = null;
        Focused = null;
        HasPointer = false;
        PointerX = 0;
        PointerY = 0;
        PointerTarget = null;
        PointerButtons = SvgPointerButtons.None;
        Language = "";
        ReducedMotion = false;
        DynamicSelectors = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        DynamicSelectors.LinkState = node => GetLinkState(node);
        DynamicSelectors.HoverState = node => IsHovered(node);
        DynamicSelectors.ActiveState = node => object.ReferenceEquals(Active, node);
        DynamicSelectors.FocusState = node => object.ReferenceEquals(Focused, node);
        DynamicSelectors.TargetState = node => object.ReferenceEquals(Target, node);
    }

    /// <summary>Sets or clears the current URL fragment target.</summary>
    /// <param name="node">The owned target element, or <c>null</c> to clear it.</param>
    /// <returns><c>true</c> when the supplied identity was valid.</returns>
    internal bool SetTarget(SvgNode? node)
    {
        if (!ValidNode(node, true)) return false;
        if (object.ReferenceEquals(Target, node)) return true;
        Target = node;
        Changed();
        return true;
    }

    /// <summary>Resolves and sets a nonempty percent-encoded fragment target.</summary>
    /// <param name="fragment">The fragment with or without a leading number sign, or an empty string to clear.</param>
    /// <returns><c>true</c> when the fragment was empty or resolved to an element.</returns>
    internal bool SetTargetFragment(string? fragment)
    {
        if (fragment == null) return false;
        string value = fragment.Length > 0 && fragment[0] == '#' ? fragment.Substring(1) : fragment;
        if (value.Length == 0) return SetTarget(null);
        SvgNode? node = new SvgReferenceResolver().FindFragment(Document, fragment);
        if (node == null) return false;
        return SetTarget(node);
    }

    /// <summary>Adds, replaces, or removes one recognized link-state override.</summary>
    /// <param name="node">The owned SVG link element.</param>
    /// <param name="state">Visited, unvisited, or none to remove an override.</param>
    /// <returns><c>true</c> when the node and state were valid.</returns>
    internal bool SetLinkState(SvgNode node, SvgSelectorLinkState state)
    {
        if (!ValidNode(node, false) || state < SvgSelectorLinkState.None || state > SvgSelectorLinkState.Visited) return false;
        int index = FindLink(node);
        if (state == SvgSelectorLinkState.None)
        {
            if (index >= 0) { Links.RemoveAt(index); Changed(); }
            return true;
        }
        if (!IsLink(node)) return false;
        if (index >= 0)
        {
            if (Links[index].State != state) { Links[index].State = state; Changed(); }
            return true;
        }
        Links.Add(new SvgInteractionLink(node, state));
        Changed();
        return true;
    }

    /// <summary>Returns none for non-links, unvisited by default, or an explicit override.</summary>
    /// <param name="node">The candidate source element.</param>
    /// <returns>The effective selector link state.</returns>
    internal SvgSelectorLinkState GetLinkState(SvgNode node)
    {
        if (!ValidNode(node, false) || !IsLink(node)) return SvgSelectorLinkState.None;
        int index = FindLink(node);
        return index < 0 ? SvgSelectorLinkState.Unvisited : Links[index].State;
    }

    /// <summary>Sets or clears the deepest hovered element.</summary>
    /// <param name="node">The owned hovered element, or <c>null</c>.</param>
    /// <returns><c>true</c> when the identity was valid.</returns>
    internal bool SetHover(SvgNode? node)
    {
        if (!ValidNode(node, true)) return false;
        if (object.ReferenceEquals(Hovered, node)) return true;
        Hovered = node;
        Changed();
        return true;
    }

    /// <summary>Tests whether a node is the hovered element or one of its element ancestors.</summary>
    /// <param name="node">The candidate source element.</param>
    /// <returns><c>true</c> when the candidate participates in hover state.</returns>
    internal bool IsHovered(SvgNode node)
    {
        if (!ValidNode(node, false)) return false;
        SvgNode? current = Hovered;
        while (current != null)
        {
            if (object.ReferenceEquals(current, node)) return true;
            current = current.Parent;
        }
        return false;
    }

    /// <summary>Sets or clears the active element.</summary>
    /// <param name="node">The owned active element, or <c>null</c>.</param>
    /// <returns><c>true</c> when the identity was valid.</returns>
    internal bool SetActive(SvgNode? node)
    {
        if (!ValidNode(node, true)) return false;
        if (object.ReferenceEquals(Active, node)) return true;
        Active = node;
        Changed();
        return true;
    }

    /// <summary>Sets or clears the focused element.</summary>
    /// <param name="node">The owned focused element, or <c>null</c>.</param>
    /// <returns><c>true</c> when the identity was valid.</returns>
    internal bool SetFocus(SvgNode? node)
    {
        if (!ValidNode(node, true)) return false;
        if (object.ReferenceEquals(Focused, node)) return true;
        Focused = node;
        Changed();
        return true;
    }

    /// <summary>Sets finite pointer coordinates, target, buttons, and corresponding hover state.</summary>
    /// <param name="target">The owned element under the pointer, or <c>null</c>.</param>
    /// <param name="x">The finite horizontal coordinate.</param>
    /// <param name="y">The finite vertical coordinate.</param>
    /// <param name="buttons">The valid combined button mask.</param>
    /// <returns><c>true</c> when every pointer input was valid.</returns>
    internal bool SetPointer(SvgNode? target, double x, double y, SvgPointerButtons buttons)
    {
        if (!ValidNode(target, true) || !SvgFiniteMath.IsFinite(x) || !SvgFiniteMath.IsFinite(y) || !ValidButtons(buttons)) return false;
        x = SvgFiniteMath.NormalizeZero(x);
        y = SvgFiniteMath.NormalizeZero(y);
        if (HasPointer && PointerX == x && PointerY == y && PointerButtons == buttons && object.ReferenceEquals(PointerTarget, target) && object.ReferenceEquals(Hovered, target)) return true;
        HasPointer = true;
        PointerX = x;
        PointerY = y;
        PointerTarget = target;
        PointerButtons = buttons;
        Hovered = target;
        Changed();
        return true;
    }

    /// <summary>Clears pointer availability, target, buttons, and pointer-derived hover state.</summary>
    internal void ClearPointer()
    {
        if (!HasPointer && PointerTarget == null && PointerButtons == SvgPointerButtons.None) return;
        SvgNode? oldTarget = PointerTarget;
        HasPointer = false;
        PointerX = 0;
        PointerY = 0;
        PointerTarget = null;
        PointerButtons = SvgPointerButtons.None;
        if (object.ReferenceEquals(Hovered, oldTarget)) Hovered = null;
        Changed();
    }

    /// <summary>Sets a normalized BCP 47 host content language or clears it.</summary>
    /// <param name="language">One language tag, or an empty string.</param>
    /// <returns><c>true</c> when the language was empty or valid.</returns>
    internal bool SetLanguage(string? language)
    {
        if (language == null) return false;
        string normalized = "";
        if (language.Length > 0)
        {
            SvgTextTokenListParseResult parsed = new SvgLanguageListParser().Parse(language, 1, 4096);
            if (!parsed.Success || parsed.Items.Count != 1) return false;
            normalized = parsed.Items[0].Value;
        }
        if (Language == normalized) return true;
        Language = normalized;
        DynamicSelectors.Language = normalized;
        Changed();
        return true;
    }

    /// <summary>Sets the caller reduced-motion preference.</summary>
    /// <param name="reduced">Whether motion should be reduced.</param>
    internal void SetReducedMotion(bool reduced)
    {
        if (ReducedMotion == reduced) return;
        ReducedMotion = reduced;
        Changed();
    }

    /// <summary>Connects this state to selector matching and style media evaluation.</summary>
    /// <param name="options">The style resolver options to update.</param>
    internal void Apply(SvgStyleResolverOptions options)
    {
        if (options == null || options.SelectorOptions == null || options.StyleEnvironment == null) throw new System.ArgumentOutOfRangeException("options");
        options.SelectorOptions.DynamicState = DynamicSelectors;
        if (options.StyleEnvironment.ReducedMotion != ReducedMotion)
        {
            options.StyleEnvironment.ReducedMotion = ReducedMotion;
            options.StyleEnvironment.MarkChanged();
        }
    }

    /// <summary>Connects selectors, language, and reduced motion to document resolution.</summary>
    /// <param name="options">The document resolution options to update.</param>
    internal void ApplyResolveOptions(SvgResolveOptions options)
    {
        if (options == null || options.StyleOptions == null || options.LanguagePreferences == null) throw new System.ArgumentOutOfRangeException("options");
        Apply(options.StyleOptions);
        options.LanguagePreferences.Clear();
        if (Language.Length > 0) options.LanguagePreferences.Add(Language);
    }

    /// <summary>Copies the current language into conditional-processing preferences.</summary>
    /// <param name="options">The conditional-processing options to update.</param>
    internal void ApplyConditionalProcessingOptions(SvgConditionalProcessingOptions options)
    {
        if (options == null || options.LanguagePreferences == null) throw new System.ArgumentOutOfRangeException("options");
        options.LanguagePreferences.Clear();
        if (Language.Length > 0) options.LanguagePreferences.Add(Language);
    }

    /// <summary>Copies the current reduced-motion preference into animation state.</summary>
    /// <param name="animation">The animation state to update.</param>
    internal void ApplyAnimationState(SvgAnimationState animation)
    {
        if (animation == null) throw new System.ArgumentNullException("animation");
        animation.ReducedMotion = ReducedMotion;
    }

    internal int FindLink(SvgNode node)
    {
        int index = 0;
        while (index < Links.Count)
        {
            if (object.ReferenceEquals(Links[index].Node, node)) return index;
            index = index + 1;
        }
        return -1;
    }

    internal bool IsLink(SvgNode node)
    {
        if (node.ElementKind != SvgElementKind.A) return false;
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.LocalName == SvgAttributeNames.Href && (attribute.NamespaceUri.Length == 0 || attribute.NamespaceUri == SvgNamespaces.XLink)) return true;
            index = index + 1;
        }
        return false;
    }

    internal bool ValidNode(SvgNode? node, bool allowNull)
    {
        if (node == null) return allowNull;
        return node.Kind == SvgNodeKind.Element && object.ReferenceEquals(node.OwnerDocument, Document);
    }

    internal static bool ValidButtons(SvgPointerButtons buttons)
    {
        int value = (int)buttons;
        return value >= 0 && (value & -32) == 0;
    }

    internal void Changed()
    {
        Version = DynamicSelectors.MarkChanged();
    }
}
