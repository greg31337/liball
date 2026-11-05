namespace svg;

/// <summary>Resolves SVG href attributes, base URIs, and fragment targets without implicit external I/O.</summary>
public class SvgReferenceResolver : ISvgReferenceResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgReferenceResolver()
    {
    }

    /// <summary>Resolves one element's optional unnamespaced or legacy XLink href.</summary>
    /// <param name="node">The source SVG element.</param>
    /// <returns>The selected reference and available target.</returns>
    public SvgReferenceResult ResolveWithNode(SvgNode node)
    {
        return new SvgReferenceResolver().ResolveWithNodeAndOptions(node, new SvgReferenceResolveOptions());
    }

    /// <summary>Resolves one element's href with explicit requiredness, external document, and URI bound.</summary>
    /// <param name="node">The source SVG element.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The selected reference and available target.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an element.</exception>
    public SvgReferenceResult ResolveWithNodeAndOptions(SvgNode node, SvgReferenceResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        if (node.Kind != SvgNodeKind.Element) throw new System.ArgumentOutOfRangeException("node");
        SvgReferenceResult result = new SvgReferenceResult(node);
        if (options.MaxUriCharacters < 1 || options.MaxBaseDepth < 1)
        {
            new SvgReferenceResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Reference resolution options are invalid.", node.SourceSpan);
            result.Status = SvgReferenceStatus.Invalid;
            return result;
        }

        new SvgReferenceResolver().SelectAttribute(node, result);
        if (result.SelectedAttribute == null)
        {
            result.Status = SvgReferenceStatus.Missing;
            if (options.RequireReference) new SvgReferenceResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "A required href or xlink:href reference is missing.", node.SourceSpan);
            result.Success = !new SvgReferenceResolver().HasError(result.Diagnostics);
            return result;
        }
        new SvgReferenceResolver().SetReferenceProvenance(result);
        return new SvgReferenceResolver().ResolveText(node, result, result.SelectedAttribute.Value, result.ReferenceSource, options);
    }

    /// <summary>Resolves a computed URL reference without adding a synthetic DOM attribute.</summary>
    /// <param name="node">The element whose base URI and document provide reference context.</param>
    /// <param name="value">The unwrapped computed IRI text.</param>
    /// <param name="source">The computed declaration source span, or <c>null</c>.</param>
    /// <param name="options">The resolution options.</param>
    /// <returns>The resolved reference with URI and target provenance.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not an element.</exception>
    public SvgReferenceResult ResolveValue(SvgNode node, string value, SvgSourceSpan? source, SvgReferenceResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (value == null) throw new System.ArgumentNullException("value");
        if (options == null) throw new System.ArgumentNullException("options");
        if (node.Kind != SvgNodeKind.Element) throw new System.ArgumentOutOfRangeException("node");
        SvgReferenceResult result = new SvgReferenceResult(node);
        if (options.MaxUriCharacters < 1 || options.MaxBaseDepth < 1)
        {
            new SvgReferenceResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Reference resolution options are invalid.", source == null ? node.SourceSpan : source);
            result.Status = SvgReferenceStatus.Invalid;
            return result;
        }
        result.ReferenceSource = source;
        if (source != null) new SvgReferenceResolver().CopySpans(new SvgSourceMapping().GetProvenance(source), result.ReferenceProvenance);
        return new SvgReferenceResolver().ResolveText(node, result, value, source, options);
    }
SvgReferenceResult ResolveText(SvgNode node, SvgReferenceResult result, string value, SvgSourceSpan? source, SvgReferenceResolveOptions options)
    {
        SvgIriParseResult parsed = new SvgIriParser().ParseIri(value, options.MaxUriCharacters);
        if (!parsed.Success)
        {
            result.Status = SvgReferenceStatus.Invalid;
            new SvgReferenceResolver().AddError(result, parsed.Error == SvgIriParseError.TokenTooLong ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidReference, "The selected SVG reference is empty, invalid, or exceeds MaxUriCharacters.", source);
            return result;
        }
        result.Reference = parsed.Reference;
        result.AuthoredUri = (parsed.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value;
        result.HasFragment = parsed.Reference.HasFragment;
        result.Fragment = parsed.Reference.Fragment;
        result.FragmentIdentifier = new SvgReferenceResolver().DecodeFragment(parsed.Reference.Fragment);

        if (!new SvgReferenceResolver().ResolveBase(node, options, result))
        {
            result.Status = SvgReferenceStatus.Invalid;
            return result;
        }
        result.ResolvedUri = SvgReferenceUri.Resolve(result.BaseUri, result.AuthoredUri);
        if (result.ResolvedUri.Length > options.MaxUriCharacters)
        {
            result.Status = SvgReferenceStatus.Invalid;
            new SvgReferenceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The resolved SVG reference exceeds MaxUriCharacters.", result.ReferenceSource);
            return result;
        }
        if (!SvgReferenceUri.Valid(result.ResolvedUri))
        {
            result.Status = SvgReferenceStatus.Invalid;
            new SvgReferenceResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "The resolved SVG reference URI is invalid.", result.ReferenceSource);
            return result;
        }
        result.ResourceUri = SvgReferenceUri.RemoveFragment(result.ResolvedUri);
        result.IsSameDocument = new SvgReferenceResolver().SameDocument(result);
        if (result.IsSameDocument) result.TargetDocument = result.SourceDocument;
        else result.TargetDocument = options.ExternalDocument;
        if (result.TargetDocument == null && options.NestedDocumentOptions != null)
        {
            SvgNestedDocumentRequest nestedRequest = new SvgNestedDocumentRequest(result.AuthoredUri, result.BaseUri, result.ResourceUri, result.Fragment);
            nestedRequest.Source = result.ReferenceSource;
            nestedRequest.ResourceIndex = new SvgReferenceResolver().ResourceIndex(node);
            nestedRequest.ReferenceDepth = result.ReferenceSource == null || result.ReferenceSource.Source == null ? 1 : result.ReferenceSource.Source.Depth + 1;
            result.NestedDocument = new SvgNestedDocumentLoader().Load(nestedRequest, options.NestedDocumentOptions);
            new SvgReferenceResolver().AppendDiagnostics(result.Diagnostics, result.NestedDocument.Diagnostics);
            if (result.NestedDocument.Success)
            {
                result.TargetDocument = result.NestedDocument.Document;
                result.Target = result.NestedDocument.Target;
            }
            else
            {
                result.Status = result.NestedDocument.Document == null ? SvgReferenceStatus.Invalid : SvgReferenceStatus.TargetNotFound;
                return result;
            }
        }
        if (result.TargetDocument == null)
        {
            result.Status = SvgReferenceStatus.External;
            result.Success = true;
            return result;
        }

        if (result.Target == null) result.Target = new SvgReferenceResolver().ResolveTarget(result.TargetDocument, result.HasFragment, result.FragmentIdentifier);
        if (result.Target == null)
        {
            result.Status = SvgReferenceStatus.TargetNotFound;
            new SvgReferenceResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "The referenced SVG fragment does not identify an element in the available target document.", result.ReferenceSource);
            SvgDiagnostic diagnostic = result.Diagnostics[result.Diagnostics.Count - 1];
            new SvgReferenceResolver().AddBaseRelated(diagnostic, result);
            if (result.TargetDocument.SourceSpan != null && !new SvgReferenceResolver().ContainsSpan(diagnostic.RelatedSpans, result.TargetDocument.SourceSpan) && !object.ReferenceEquals(diagnostic.Source, result.TargetDocument.SourceSpan)) diagnostic.AddRelatedSpan(result.TargetDocument.SourceSpan);
            return result;
        }
        result.TargetSource = result.Target.SourceSpan;
        if (result.TargetSource != null) new SvgReferenceResolver().CopySpans(new SvgSourceMapping().GetProvenance(result.TargetSource), result.TargetProvenance);
        result.Status = SvgReferenceStatus.Resolved;
        result.Success = true;
        return result;
    }

    /// <summary>Finds a percent-encoded fragment identifier in a document's current ID index.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="fragment">The fragment text with or without a leading number sign.</param>
    /// <returns>The first exact ID match in document order, the document root for an empty fragment, or <c>null</c>.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    public SvgNode? FindFragment(SvgDocument document, string fragment)
    {
        if (document == null) throw new System.ArgumentNullException("document");
        if (fragment == null) throw new System.ArgumentNullException("fragment");
        string value = fragment.Length > 0 && fragment[0] == '#' ? fragment.Substring(1) : fragment;
        if (value.Length == 0) return document.DocumentElement;
        SvgIriParseResult parsed = new SvgIriParser().ParseIri("#" + value, 8192);
        if (!parsed.Success) return null;
        return document.Index.GetById(new SvgReferenceResolver().DecodeFragment((parsed.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Fragment));
    }
void SelectAttribute(SvgNode node, SvgReferenceResult result)
    {
        result.HrefAttribute = new SvgReferenceResolver().FindAttribute(node, "", SvgAttributeNames.Href);
        result.XLinkHrefAttribute = new SvgReferenceResolver().FindAttribute(node, SvgNamespaces.XLink, SvgAttributeNames.Href);
        if (result.HrefAttribute != null)
        {
            result.SelectedAttribute = result.HrefAttribute;
            result.AttributeKind = SvgReferenceAttributeKind.Href;
            result.XLinkHrefIgnored = result.XLinkHrefAttribute != null;
        }
        else if (result.XLinkHrefAttribute != null)
        {
            result.SelectedAttribute = result.XLinkHrefAttribute;
            result.AttributeKind = SvgReferenceAttributeKind.XLinkHref;
        }
    }
int ResourceIndex(SvgNode target)
    {
        SvgDocument? document = target.OwnerDocument;
        if (document == null || document.DocumentNode == null) return 0;
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(document.DocumentNode);
        int index = 0;
        while (stack.Count != 0)
        {
            int last = stack.Count - 1;
            SvgNode node = stack[last];
            stack.RemoveAt(last);
            if (object.ReferenceEquals(node, target)) return index;
            index = index + 1;
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                stack.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        return 0;
    }
void AppendDiagnostics(System.Collections.Generic.List<SvgDiagnostic> destination, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
void SetReferenceProvenance(SvgReferenceResult result)
    {
        result.ReferenceSource = (result.SelectedAttribute ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).SourceSpan;
        if (result.ReferenceSource != null) new SvgReferenceResolver().CopySpans(new SvgSourceMapping().GetProvenance(result.ReferenceSource), result.ReferenceProvenance);
    }
bool ResolveBase(SvgNode node, SvgReferenceResolveOptions options, SvgReferenceResult result)
    {
        string effective = "";
        SvgDocument? document = node.OwnerDocument;
        if (document != null)
        {
            effective = document.BaseUri;
            if (effective.Length == 0 && document.Source != null) effective = document.Source.Uri;
            result.BaseSource = document.SourceSpan;
        }
        if (!new SvgReferenceResolver().ValidBase(effective, options.MaxUriCharacters))
        {
            new SvgReferenceResolver().AddError(result, effective.Length > options.MaxUriCharacters ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidReference, "The document base URI is invalid or exceeds MaxUriCharacters.", result.BaseSource == null ? node.SourceSpan : result.BaseSource);
            return false;
        }
        if (effective.Length != 0) effective = (new SvgIriParser().ParseIri(effective, options.MaxUriCharacters).Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value;

        System.Collections.Generic.List<SvgNode> ancestors = new System.Collections.Generic.List<SvgNode>();
        SvgNode? current = node;
        while (current != null)
        {
            if (current.Kind == SvgNodeKind.Element)
            {
                if (ancestors.Count >= options.MaxBaseDepth)
                {
                    new SvgReferenceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The xml:base ancestry exceeds MaxBaseDepth.", result.ReferenceSource == null ? node.SourceSpan : result.ReferenceSource);
                    return false;
                }
                ancestors.Add(current);
            }
            current = current.Parent;
        }
        int index = ancestors.Count - 1;
        while (index >= 0)
        {
            SvgAttribute? attribute = new SvgReferenceResolver().FindAttribute(ancestors[index], SvgNamespaces.Xml, SvgAttributeNames.Base);
            if (attribute != null)
            {
                result.BaseSource = attribute.SourceSpan == null ? ancestors[index].SourceSpan : attribute.SourceSpan;
                if (attribute.SourceSpan != null) result.BaseProvenance.Add(attribute.SourceSpan);
                if (!new SvgReferenceResolver().ValidBase(attribute.Value, options.MaxUriCharacters))
                {
                    new SvgReferenceResolver().AddError(result, attribute.Value.Length > options.MaxUriCharacters ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidReference, "An xml:base URI is invalid or exceeds MaxUriCharacters.", result.BaseSource);
                    new SvgReferenceResolver().AddReferenceRelated(result.Diagnostics[result.Diagnostics.Count - 1], result);
                    return false;
                }
                string baseValue = attribute.Value.Length == 0 ? "" : (new SvgIriParser().ParseIri(attribute.Value, options.MaxUriCharacters).Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Value;
                effective = SvgReferenceUri.Resolve(effective, baseValue);
                if (effective.Length > options.MaxUriCharacters)
                {
                    new SvgReferenceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The effective xml:base URI exceeds MaxUriCharacters.", result.BaseSource);
                    new SvgReferenceResolver().AddReferenceRelated(result.Diagnostics[result.Diagnostics.Count - 1], result);
                    return false;
                }
            }
            index = index - 1;
        }
        result.BaseUri = effective;
        return true;
    }
bool ValidBase(string? value, int maximum)
    {
        if (value == null || value.Length > maximum) return false;
        if (value.Length == 0) return true;
        return new SvgIriParser().ParseIri(value, maximum).Success;
    }
bool SameDocument(SvgReferenceResult result)
    {
        if (result.SourceDocument == null) return false;
        string documentUri = result.SourceDocument.BaseUri;
        if (documentUri.Length == 0 && result.SourceDocument.Source != null) documentUri = result.SourceDocument.Source.Uri;
        if (documentUri.Length != 0) documentUri = SvgReferenceUri.RemoveFragment(SvgReferenceUri.Resolve("", documentUri));
        if (result.ResourceUri.Length == 0) return true;
        return string.Equals(result.ResourceUri, documentUri, System.StringComparison.Ordinal);
    }
SvgNode? ResolveTarget(SvgDocument document, bool hasFragment, string fragment)
    {
        if (!hasFragment || fragment.Length == 0) return document.DocumentElement;
        return document.Index.GetById(fragment);
    }
string DecodeFragment(string fragment)
    {
        return new SvgUri().DecodeFragment(fragment);
    }
SvgAttribute? FindAttribute(SvgNode node, string namespaceUri, string localName)
    {
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri == namespaceUri && attribute.LocalName == localName) return attribute;
            index = index + 1;
        }
        return null;
    }
void AddReferenceRelated(SvgDiagnostic diagnostic, SvgReferenceResult result)
    {
        if (result.ReferenceSource != null && !object.ReferenceEquals(diagnostic.Source, result.ReferenceSource)) diagnostic.AddRelatedSpan(result.ReferenceSource);
    }
void AddBaseRelated(SvgDiagnostic diagnostic, SvgReferenceResult result)
    {
        if (result.BaseSource != null && !object.ReferenceEquals(diagnostic.Source, result.BaseSource)) diagnostic.AddRelatedSpan(result.BaseSource);
    }
void CopySpans(System.Collections.Generic.List<SvgSourceSpan> source, System.Collections.Generic.List<SvgSourceSpan> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
bool ContainsSpan(System.Collections.Generic.List<SvgSourceSpan> spans, SvgSourceSpan span)
    {
        int index = 0;
        while (index < spans.Count)
        {
            if (object.ReferenceEquals(spans[index], span)) return true;
            index = index + 1;
        }
        return false;
    }
bool HasError(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Severity == SvgDiagnosticSeverity.Error) return true;
            index = index + 1;
        }
        return false;
    }
void AddError(SvgReferenceResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
