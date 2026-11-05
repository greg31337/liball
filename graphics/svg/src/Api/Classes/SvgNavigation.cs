namespace svg;

/// <summary>Computes focus order and plans link navigation without performing navigation.</summary>
public class SvgNavigation : ISvgNavigation
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgNavigation()
    {
    }

    /// <summary>Creates empty caller-controlled interaction state for an editable document.</summary>
    /// <param name="document">The source SVG document.</param>
    /// <returns>New interaction state backed by dynamic selector callbacks.</returns>
    public SvgInteractionState CreateInteractionStateWithDocument(SvgDocument document)
    {
        return new SvgInteractionState(document);
    }

    /// <summary>Computes focusable entries and sequential tab order with secure defaults.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <returns>The bounded focus-order result.</returns>
    public SvgFocusOrderResult FocusOrderWithDocument(SvgResolvedDocument document)
    {
        return new SvgNavigation().FocusOrderWithDocumentAndOptions(document, new SvgNavigationOptions());
    }

    /// <summary>Computes focusable entries and sequential tab order.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="options">The traversal and URI bounds.</param>
    /// <returns>The bounded focus-order result.</returns>
    public SvgFocusOrderResult FocusOrderWithDocumentAndOptions(SvgResolvedDocument document, SvgNavigationOptions options)
    {
        SvgFocusOrderResult result = new SvgFocusOrderResult();
        if (!new SvgNavigation().Valid(document, options)) return new SvgNavigation().FocusFail(result, SvgNavigationError.InvalidArgument);
        System.Collections.Generic.List<SvgFocusEntry> positive = new System.Collections.Generic.List<SvgFocusEntry>();
        System.Collections.Generic.List<SvgFocusEntry> ordinary = new System.Collections.Generic.List<SvgFocusEntry>();
        int index = 0;
        while (index < document.Nodes.Count)
        {
            if (result.VisitedNodeCount >= options.MaximumNodes) return new SvgNavigation().FocusFail(result, SvgNavigationError.LimitExceeded);
            SvgResolvedNode node = document.Nodes[index];
            result.VisitedNodeCount = result.VisitedNodeCount + 1;
            SvgFocusEntry? entry = new SvgNavigation().FocusEntry(node, result.Diagnostics);
            if (entry != null)
            {
                result.Entries.Add(entry);
                if (entry.Sequential)
                {
                    if (entry.HasTabIndex && entry.TabIndex > 0) new SvgNavigation().InsertPositive(positive, entry);
                    else ordinary.Add(entry);
                }
            }
            index = index + 1;
        }
        new SvgNavigation().CopyEntries(positive, result.TabOrder);
        new SvgNavigation().CopyEntries(ordinary, result.TabOrder);
        result.Success = true;
        return result;
    }

    /// <summary>Plans activation for a resolved link or one of its descendants.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="node">The activated resolved node.</param>
    /// <returns>A fragment activation or host navigation request.</returns>
    public SvgActivationRecord ActivateWithDocumentAndNode(SvgResolvedDocument document, SvgResolvedNode? node)
    {
        return new SvgNavigation().ActivateWithDocumentAndNodeAndOptions(document, node, new SvgNavigationOptions());
    }

    /// <summary>Plans bounded activation for a resolved link or descendant.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="node">The activated resolved node.</param>
    /// <param name="options">The traversal and URI bounds.</param>
    /// <returns>A fragment activation or host navigation request.</returns>
    public SvgActivationRecord ActivateWithDocumentAndNodeAndOptions(SvgResolvedDocument document, SvgResolvedNode? node, SvgNavigationOptions options)
    {
        SvgActivationRecord result = new SvgActivationRecord(node);
        if (!new SvgNavigation().Valid(document, options) || node == null || !object.ReferenceEquals(node.Document, document)) return new SvgNavigation().ActivationFail(result, SvgNavigationError.InvalidArgument, "Activation arguments are invalid.", node == null ? null : node.SourceNode);
        int[] traversal = new int[1];
        SvgResolvedNode? link = new SvgNavigation().LinkAncestor(node, options.MaximumNodes, traversal);
        if (link == null && traversal[0] != 0) return new SvgNavigation().ActivationFail(result, SvgNavigationError.LimitExceeded, "Link ancestor traversal exceeded MaximumNodes.", node.SourceNode);
        if (link == null) return new SvgNavigation().ActivationFail(result, SvgNavigationError.NotActivatable, "The activated node has no usable link ancestor.", node.SourceNode);
        result.LinkNode = link;
        SvgStructuralResult? structural = link.Structural;
        if (structural == null || !structural.Success) return new SvgNavigation().ActivationFail(result, SvgNavigationError.InvalidLink, "The resolved link metadata is invalid.", link.SourceNode);
        SvgIriReference? href = structural.Href != null ? structural.Href : structural.XLinkHref;
        if (href == null || !href.IsValid) return new SvgNavigation().ActivationFail(result, SvgNavigationError.NotActivatable, "The link has no usable href.", link.SourceNode);
        SvgUriResult uri = new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters((document.SourceDocument ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).BaseUri, href.Value, options.MaximumUriCharacters);
        if (!uri.Success) return new SvgNavigation().ActivationFail(result, SvgNavigationError.InvalidUri, "The link target URI is invalid.", link.SourceNode);
        SvgNavigationRequest request = new SvgNavigation().Request(link, structural, href, uri);
        bool sameDocument = new SvgNavigation().SameDocument(document.SourceDocument, href, uri, options.MaximumUriCharacters);
        if (sameDocument && uri.HasFragment)
        {
            SvgNode? fragment = new SvgReferenceResolver().FindFragment(document.SourceDocument, "#" + uri.Fragment);
            if (fragment == null) return new SvgNavigation().ActivationFail(result, SvgNavigationError.FragmentNotFound, "The same-document fragment target was not found.", link.SourceNode);
            request.Kind = SvgNavigationRequestKind.SameDocumentFragment;
            result.Kind = SvgActivationKind.Fragment;
            result.FragmentTarget = fragment;
        }
        else
        {
            request.Kind = SvgNavigationRequestKind.Navigate;
            result.Kind = SvgActivationKind.Link;
        }
        result.Request = request;
        result.Success = true;
        return result;
    }

    /// <summary>Plans activation for the target of a successful hit-test match.</summary>
    /// <param name="document">The current resolved document.</param>
    /// <param name="match">The hit-test match to activate.</param>
    /// <returns>A fragment activation or host navigation request.</returns>
    public SvgActivationRecord ActivateWithDocumentAndMatch(SvgResolvedDocument document, SvgHitTestMatch? match)
    {
        if (match == null) return new SvgNavigation().ActivateWithDocumentAndNodeAndOptions(document, null, new SvgNavigationOptions());
        return new SvgNavigation().ActivateWithDocumentAndNodeAndOptions(document, match.Node, new SvgNavigationOptions());
    }
SvgFocusEntry? FocusEntry(SvgResolvedNode? node, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        if (node == null || !node.Active || !new SvgNavigation().Displayed(node) || node.SourceNode == null || node.SourceNode.Kind != SvgNodeKind.Element) return null;
        SvgAttribute? attribute = new SvgNavigation().FindAttribute(node.SourceNode, SvgAttributeNames.TabIndex);
        bool hasTabIndex = false;
        int tabIndex = 0;
        if (attribute != null)
        {
            SvgIntegerParseResult parsed = SvgNumberParser.ParseInteger(attribute.Value, 64);
            if (parsed.Success)
            {
                hasTabIndex = true;
                tabIndex = parsed.Value;
            }
            else diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.InvalidValue, "The tabindex value is not a valid integer and was ignored.", attribute.SourceSpan));
        }
        bool implicitLink = new SvgNavigation().UsableLink(node);
        if (!hasTabIndex && !implicitLink) return null;
        bool sequential = hasTabIndex ? tabIndex >= 0 : implicitLink;
        return new SvgFocusEntry(node, hasTabIndex, tabIndex, sequential, implicitLink);
    }
bool UsableLink(SvgResolvedNode node)
    {
        return node.Kind == SvgResolvedNodeKind.Link && node.Structural != null && node.Structural.Success && (node.Structural.Href != null && node.Structural.Href.IsValid || node.Structural.XLinkHref != null && node.Structural.XLinkHref.IsValid);
    }
bool Displayed(SvgResolvedNode node)
    {
        SvgResolvedNode? current = node;
        System.Collections.Generic.HashSet<SvgResolvedNode> visited = new System.Collections.Generic.HashSet<SvgResolvedNode>();
        while (current != null && visited.Add(current))
        {
            if (current.ComputedStyle != null && current.ComputedStyle.GetKeyword(SvgCssPropertyNames.Display) == "none") return false;
            current = current.Parent;
        }
        return true;
    }
SvgResolvedNode? LinkAncestor(SvgResolvedNode node, int maximum, int[] traversal)
    {
        int count = 0;
        SvgResolvedNode? current = node;
        System.Collections.Generic.HashSet<SvgResolvedNode> visited = new System.Collections.Generic.HashSet<SvgResolvedNode>();
        while (current != null && count < maximum && visited.Add(current))
        {
            if (new SvgNavigation().UsableLink(current)) return current;
            current = current.Parent;
            count = count + 1;
        }
        if (current != null && count >= maximum) traversal[0] = 1;
        return null;
    }
SvgNavigationRequest Request(SvgResolvedNode link, SvgStructuralResult structural, SvgIriReference href, SvgUriResult uri)
    {
        SvgNavigationRequest result = new SvgNavigationRequest(link);
        result.Href = href.Value;
        result.NormalizedIri = uri.NormalizedIri;
        result.NormalizedUri = uri.NormalizedUri;
        result.ResourceIri = uri.ResourceIri;
        result.Fragment = new SvgUri().DecodeFragment(uri.Fragment);
        result.HasFragment = uri.HasFragment;
        result.Target = structural.Target;
        result.HasDownload = structural.HasDownload;
        result.Download = structural.Download;
        result.HrefLanguage = structural.HrefLanguage;
        result.MediaType = structural.MediaType;
        int index = 0;
        while (index < structural.Relations.Count)
        {
            result.Relations.Add(structural.Relations[index].Value);
            index = index + 1;
        }
        return result;
    }
bool SameDocument(SvgDocument document, SvgIriReference href, SvgUriResult uri, int maximum)
    {
        if (href.IsFragmentOnly) return true;
        if (!uri.HasFragment || document.BaseUri.Length == 0) return false;
        SvgUriResult current = new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(document.BaseUri, "", maximum);
        return current.Success && current.ResourceIri == uri.ResourceIri;
    }
void InsertPositive(System.Collections.Generic.List<SvgFocusEntry> entries, SvgFocusEntry entry)
    {
        int index = 0;
        while (index < entries.Count && entries[index].TabIndex <= entry.TabIndex) index = index + 1;
        entries.Insert(index, entry);
    }
void CopyEntries(System.Collections.Generic.List<SvgFocusEntry> source, System.Collections.Generic.List<SvgFocusEntry> target)
    {
        int index = 0;
        while (index < source.Count)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }
SvgAttribute? FindAttribute(SvgNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == name) return attribute;
            index = index + 1;
        }
        return null;
    }
bool Valid(SvgResolvedDocument document, SvgNavigationOptions options)
    {
        return document != null && document.SourceDocument != null && document.Nodes != null && options != null && options.MaximumNodes > 0 && options.MaximumUriCharacters > 0;
    }
SvgFocusOrderResult FocusFail(SvgFocusOrderResult result, SvgNavigationError error)
    {
        result.Error = error;
        result.Success = false;
        return result;
    }
SvgActivationRecord ActivationFail(SvgActivationRecord result, SvgNavigationError error, string message, SvgNode? node)
    {
        result.Error = error;
        result.Success = false;
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidReference, message, node == null ? null : node.SourceSpan));
        return result;
    }
}
