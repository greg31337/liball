namespace svg;

/// <summary>Loads nested SVG documents through explicit caller callbacks without implicit I/O.</summary>
public class SvgNestedDocumentLoader : ISvgNestedDocumentLoader
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgNestedDocumentLoader()
    {
    }

    /// <summary>Acquires, parses, rebases, and selects one nested SVG document fragment.</summary>
    /// <param name="request">The resolved nested resource request.</param>
    /// <param name="options">The inherited load options and shared resource context.</param>
    /// <returns>The nested document, selected target, provenance, and diagnostics.</returns>
    public SvgNestedDocumentResult Load(SvgNestedDocumentRequest? request, SvgNestedDocumentOptions options)
    {
        SvgNestedDocumentResult result = new SvgNestedDocumentResult();
        if (!new SvgNestedDocumentLoader().Valid(request, options))
        {
            new SvgNestedDocumentLoader().AddError(result, SvgDiagnosticCode.InvalidArgument, "Nested document loading arguments are invalid.", request == null ? null : request.Source);
            return result;
        }
        if ((request ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ReferenceDepth > options.LoadOptions.Limits.References.MaxDepth)
        {
            new SvgNestedDocumentLoader().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "Nested document loading exceeds the reference MaxDepth.", request.Source);
            return result;
        }
        SvgResourcePolicyRequest initialPolicy = new SvgNestedDocumentLoader().PolicyRequest(request, request.ResolvedUri, SvgMimeTypes.Svg, 0, false, options.LoadOptions.Limits.Resources.MaxRedirects);
        result.InitialPolicyDecision = options.LoadOptions.ResourcePolicy.Evaluate(initialPolicy);
        if (!result.InitialPolicyDecision.Allowed)
        {
            result.Diagnostics.Add((result.InitialPolicyDecision.Diagnostic ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            return result;
        }
        SvgResourceRequest acquisitionRequest = new SvgResourceRequest(request.AuthoredUri, SvgMimeTypes.Svg, options.LoadOptions.CancellationToken);
        acquisitionRequest.NormalizedUri = request.ResolvedUri;
        acquisitionRequest.BaseUri = request.BaseUri;
        acquisitionRequest.ResourceKind = SvgResourceKind.Document;
        acquisitionRequest.Source = request.Source;
        acquisitionRequest.ReferenceDepth = request.ReferenceDepth;
        acquisitionRequest.ResourceIndex = request.ResourceIndex;
        SvgResourceAcquisitionOptions acquisitionOptions = new SvgNestedDocumentLoader().AcquisitionOptions(request, options);
        result.Acquisition = new SvgResourceResolver().Resolve(acquisitionRequest, acquisitionOptions);
        options.Context.Add(result.Acquisition);
        new SvgNestedDocumentLoader().Append(result.Diagnostics, result.Acquisition.Diagnostics);
        if (!result.Acquisition.Success) return result;
        bool redirected = result.Acquisition.RedirectCount > 0;
        SvgResourcePolicyRequest finalPolicy = new SvgNestedDocumentLoader().PolicyRequest(request, result.Acquisition.FinalUri, result.Acquisition.EffectiveMediaType, result.Acquisition.RedirectCount, redirected, options.LoadOptions.Limits.Resources.MaxRedirects);
        finalPolicy.BaseUri = result.Acquisition.NormalizedUri;
        result.FinalPolicyDecision = options.LoadOptions.ResourcePolicy.Evaluate(finalPolicy);
        if (!result.FinalPolicyDecision.Allowed)
        {
            result.Diagnostics.Add((result.FinalPolicyDecision.Diagnostic ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            return result;
        }
        SvgLoadOptions nestedOptions = new SvgNestedDocumentLoader().CopyLoadOptions(options.LoadOptions, result.Acquisition.FinalUri, result.Acquisition.EffectiveMediaType);
        result.LoadResult = new SvgLoader().LoadWithByteArrayAndSvgLoadOptions(result.Acquisition.Bytes, nestedOptions);
        new SvgNestedDocumentLoader().Append(result.Diagnostics, result.LoadResult.Diagnostics);
        new SvgNestedDocumentLoader().Rebase(result.LoadResult, result.Acquisition.ResourceSource);
        if (!result.LoadResult.Success || result.LoadResult.Document == null) return result;
        result.Document = result.LoadResult.Document;
        result.Target = request.Fragment.Length == 0 ? result.Document.DocumentElement : new SvgReferenceResolver().FindFragment(result.Document, "#" + request.Fragment);
        if (result.Target == null)
        {
            new SvgNestedDocumentLoader().AddError(result, SvgDiagnosticCode.InvalidReference, "The nested SVG fragment does not identify an element.", request.Source);
            if (result.Document.SourceSpan != null) result.Diagnostics[result.Diagnostics.Count - 1].AddRelatedSpan(result.Document.SourceSpan);
            return result;
        }
        result.Success = true;
        return result;
    }
SvgResourceAcquisitionOptions AcquisitionOptions(SvgNestedDocumentRequest request, SvgNestedDocumentOptions options)
    {
        SvgResourceAcquisitionOptions result = new SvgResourceAcquisitionOptions();
        result.ResourceProvider = options.LoadOptions.ResourceProvider;
        result.Limits = options.LoadOptions.Limits.Resources;
        result.CancellationToken = options.LoadOptions.CancellationToken;
        result.CopyInputData = options.LoadOptions.CopyInputData;
        result.MaxBytes = options.LoadOptions.Limits.Input.MaxInputBytes;
        result.AlreadyLoadedBytes = options.Context.InheritedEncodedBytes + options.Context.Resources.EncodedBytes;
        result.DecodeDataUrls = true;
        result.Cache = options.Context.Cache;
        result.ReferenceStack = new SvgNestedDocumentLoader().ReferenceStack(request);
        result.AlreadyLoadedCount = options.Context.AccountedCount;
        result.CacheFailures = true;
        return result;
    }
System.Collections.Generic.List<string> ReferenceStack(SvgNestedDocumentRequest request)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        SvgSourceUri? source = request.Source == null ? null : request.Source.Source;
        while (source != null)
        {
            if (source.Uri.Length != 0) result.Add(source.Uri);
            source = source.Parent;
        }
        if (result.Count == 0 && request.BaseUri.Length != 0) result.Add(request.BaseUri);
        return result;
    }
SvgResourcePolicyRequest PolicyRequest(SvgNestedDocumentRequest request, string resolvedUri, string mediaType, int redirectCount, bool redirected, int maxRedirects)
    {
        SvgResourcePolicyRequest result = new SvgResourcePolicyRequest(request.AuthoredUri, request.BaseUri, resolvedUri, SvgResourceKind.Document);
        result.MediaType = mediaType;
        result.IsRedirect = redirected;
        result.RedirectCount = redirectCount;
        result.MaxRedirects = maxRedirects;
        result.Source = request.Source;
        return result;
    }
SvgLoadOptions CopyLoadOptions(SvgLoadOptions source, string baseUri, string contentType)
    {
        SvgLoadOptions result = new SvgLoadOptions();
        result.Mode = source.Mode;
        result.BaseUri = baseUri;
        result.ContentType = contentType;
        result.Limits = source.Limits;
        result.ResourcePolicy = source.ResourcePolicy;
        result.ResourceProvider = source.ResourceProvider;
        result.CancellationToken = source.CancellationToken;
        result.DiagnosticBehavior = source.DiagnosticBehavior;
        result.PreserveSource = source.PreserveSource;
        result.PreserveUnknown = source.PreserveUnknown;
        result.CopyInputData = source.CopyInputData;
        result.DtdPolicy = source.DtdPolicy;
        return result;
    }
void Rebase(SvgLoadResult? loadResult, SvgSourceUri? nestedSource)
    {
        if (loadResult == null || nestedSource == null) return;
        SvgDocument? document = loadResult.Document;
        SvgSourceUri? original = document == null ? new SvgNestedDocumentLoader().FirstDiagnosticSource(loadResult.Diagnostics) : document.Source;
        new SvgNestedDocumentLoader().RebaseDiagnostics(loadResult.Diagnostics, original, nestedSource);
        if (document == null) return;
        document.Source = nestedSource;
        document.SourceSpan = new SvgNestedDocumentLoader().Replace(document.SourceSpan, original, nestedSource);
        System.Collections.Generic.List<SvgNode> stack = new System.Collections.Generic.List<SvgNode>();
        stack.Add(document.DocumentNode);
        while (stack.Count != 0)
        {
            int last = stack.Count - 1;
            SvgNode node = stack[last];
            stack.RemoveAt(last);
            node.SourceSpan = new SvgNestedDocumentLoader().Replace(node.SourceSpan, original, nestedSource);
            int attributeIndex = 0;
            while (attributeIndex < node.Attributes.Count)
            {
                node.Attributes[attributeIndex].SourceSpan = new SvgNestedDocumentLoader().Replace(node.Attributes[attributeIndex].SourceSpan, original, nestedSource);
                attributeIndex = attributeIndex + 1;
            }
            int childIndex = node.Children.Count - 1;
            while (childIndex >= 0)
            {
                stack.Add(node.Children[childIndex]);
                childIndex = childIndex - 1;
            }
        }
        new SvgNestedDocumentLoader().RebaseDiagnostics(document.Diagnostics, original, nestedSource);
    }
SvgSourceUri? FirstDiagnosticSource(System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index] != null && diagnostics[index].Source != null) return (diagnostics[index].Source ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Source;
            index = index + 1;
        }
        return null;
    }
void RebaseDiagnostics(System.Collections.Generic.List<SvgDiagnostic> diagnostics, SvgSourceUri? original, SvgSourceUri nestedSource)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            SvgDiagnostic diagnostic = diagnostics[index];
            if (diagnostic != null)
            {
                diagnostic.Source = new SvgNestedDocumentLoader().Replace(diagnostic.Source, original, nestedSource);
                diagnostic.ResourcePath = diagnostic.Source == null ? "" : diagnostic.Source.Source.ResourcePath;
                int relatedIndex = 0;
                while (relatedIndex < diagnostic.RelatedSpans.Count)
                {
                    diagnostic.RelatedSpans[relatedIndex] = (new SvgNestedDocumentLoader().Replace(diagnostic.RelatedSpans[relatedIndex], original, nestedSource) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                    relatedIndex = relatedIndex + 1;
                }
            }
            index = index + 1;
        }
    }
SvgSourceSpan? Replace(SvgSourceSpan? span, SvgSourceUri? original, SvgSourceUri nestedSource)
    {
        if (span == null || original == null || !object.ReferenceEquals(span.Source, original)) return span;
        return new SvgSourceSpan(nestedSource, span.StartOffset, span.StartLine, span.StartColumn, span.EndOffset, span.EndLine, span.EndColumn);
    }
bool Valid(SvgNestedDocumentRequest? request, SvgNestedDocumentOptions? options)
    {
        if (request == null || options == null || options.LoadOptions == null || options.Context == null) return false;
        if (request.AuthoredUri == null || request.AuthoredUri.Length == 0 || request.BaseUri == null || request.ResolvedUri == null || request.ResolvedUri.Length == 0 || request.Fragment == null || request.ResourceIndex < 0 || request.ReferenceDepth < 1) return false;
        SvgLoadOptions load = options.LoadOptions;
        if (load.Limits == null || load.ResourcePolicy == null || load.BaseUri == null || load.ContentType == null) return false;
        return load.Limits.Validate().Success;
    }
void Append(System.Collections.Generic.List<SvgDiagnostic> destination, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
void AddError(SvgNestedDocumentResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
