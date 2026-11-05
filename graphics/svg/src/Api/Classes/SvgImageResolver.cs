namespace svg;

/// <summary>Acquires image resources and resolves layout metadata without decoding pixels.</summary>
public class SvgImageResolver : ISvgImageResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgImageResolver()
    {
    }

    /// <summary>Resolves an already-normalized image reference through explicit caller mediation.</summary>
    /// <param name="reference">The selected and normalized image reference.</param>
    /// <param name="options">The inherited callback, policy, cache, and limits.</param>
    /// <returns>Encoded image bytes plus callback or PNG intrinsic metadata.</returns>
    public SvgImageResolveResult Resolve(SvgReferenceResult? reference, SvgImageResolveOptions options)
    {
        SvgReferenceResult stableReference = reference == null ? new SvgReferenceResult(null) : reference;
        SvgImageResolveResult result = new SvgImageResolveResult(stableReference);
        if (!new SvgImageResolver().Valid(reference, options))
        {
            new SvgImageResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Image resolution arguments are invalid.", reference == null ? null : reference.ReferenceSource);
            return result;
        }
        reference = stableReference;
        if (!reference.Success || reference.Reference == null)
        {
            new SvgImageResolver().AddError(result, SvgDiagnosticCode.InvalidReference, "The image reference is not resolvable.", reference.ReferenceSource);
            return result;
        }
        if (reference.IsSameDocument || reference.TargetDocument != null)
        {
            result.Success = true;
            return result;
        }

        SvgLoadOptions load = options.LoadOptions;
        string requestedMediaType = new SvgImageResolver().RequestedMediaType(reference.ResourceUri);
        SvgResourcePolicyRequest initial = new SvgImageResolver().PolicyRequest(reference, reference.ResourceUri, requestedMediaType, 0, false, load.Limits.Resources.MaxRedirects);
        result.InitialPolicyDecision = load.ResourcePolicy.Evaluate(initial);
        if (!result.InitialPolicyDecision.Allowed)
        {
            result.Diagnostics.Add((result.InitialPolicyDecision.Diagnostic ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            return result;
        }

        SvgResourceRequest request = new SvgResourceRequest(reference.AuthoredUri, requestedMediaType, load.CancellationToken);
        request.NormalizedUri = reference.ResourceUri;
        request.BaseUri = reference.BaseUri;
        request.ResourceKind = SvgResourceKind.Image;
        request.Source = reference.ReferenceSource;
        request.ReferenceDepth = reference.ReferenceSource == null || reference.ReferenceSource.Source == null ? 1 : reference.ReferenceSource.Source.Depth + 1;
        request.ResourceIndex = new SvgImageResolver().ResourceIndex(reference.Node);
        SvgResourceAcquisitionOptions acquisitionOptions = new SvgImageResolver().AcquisitionOptions(reference, options);
        result.Acquisition = new SvgResourceResolver().Resolve(request, acquisitionOptions);
        options.Context.Add(result.Acquisition);
        new SvgImageResolver().Append(result.Diagnostics, result.Acquisition.Diagnostics);
        if (!result.Acquisition.Success) return result;

        bool redirected = result.Acquisition.RedirectCount > 0;
        SvgResourcePolicyRequest final = new SvgImageResolver().PolicyRequest(reference, result.Acquisition.FinalUri, result.Acquisition.EffectiveMediaType, result.Acquisition.RedirectCount, redirected, load.Limits.Resources.MaxRedirects);
        final.BaseUri = result.Acquisition.NormalizedUri;
        result.FinalPolicyDecision = load.ResourcePolicy.Evaluate(final);
        if (!result.FinalPolicyDecision.Allowed)
        {
            result.Diagnostics.Add((result.FinalPolicyDecision.Diagnostic ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
            return result;
        }

        SvgImageMetadata? metadata = result.Acquisition.ImageMetadata;
        if (metadata != null && metadata.IntrinsicSize != null)
        {
            if (!new SvgImageResolver().WithinRasterLimits(metadata.IntrinsicSize, load.Limits.Raster))
            {
                new SvgImageResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The callback image metadata exceeds raster limits.", reference.ReferenceSource);
                return result;
            }
            result.IntrinsicSize = new SvgImageResolver().CopyIntrinsic(metadata.IntrinsicSize);
            result.UsedCallbackMetadata = true;
        }
        if (metadata != null) result.DecodedContentAvailable = metadata.DecodedContentAvailable;
        if (result.IntrinsicSize == null && new SvgImageResolver().IsPng(result.Acquisition))
        {
            SvgPngProbeResult probe = new SvgPngCodec().ProbeWithBytesAndLimits(result.Acquisition.Bytes, load.Limits.Raster);
            if (!probe.Success)
            {
                SvgDiagnosticCode code = probe.Error == SvgPngProbeError.DimensionLimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.ResourceFailed;
                new SvgImageResolver().AddError(result, code, "The image PNG header is invalid, truncated, or exceeds raster limits.", reference.ReferenceSource);
                return result;
            }
            result.IntrinsicSize = probe.IntrinsicSize;
            result.UsedPngHeader = true;
        }
        result.Success = true;
        return result;
    }
bool Valid(SvgReferenceResult? reference, SvgImageResolveOptions? options)
    {
        if (reference == null || options == null || options.LoadOptions == null || options.Context == null) return false;
        SvgLoadOptions load = options.LoadOptions;
        return load.Limits != null && load.Limits.Validate().Success && load.ResourcePolicy != null && load.BaseUri != null && load.ContentType != null;
    }
SvgResourceAcquisitionOptions AcquisitionOptions(SvgReferenceResult reference, SvgImageResolveOptions options)
    {
        SvgLoadOptions load = options.LoadOptions;
        SvgResourceAcquisitionOptions result = new SvgResourceAcquisitionOptions();
        result.ResourceProvider = load.ResourceProvider;
        result.Limits = load.Limits.Resources;
        result.CancellationToken = load.CancellationToken;
        result.CopyInputData = load.CopyInputData;
        result.MaxBytes = load.Limits.Input.MaxInputBytes;
        result.AlreadyLoadedBytes = options.Context.InheritedEncodedBytes + options.Context.Resources.EncodedBytes;
        result.DecodeDataUrls = true;
        result.Cache = options.Context.Cache;
        result.ReferenceStack = new SvgImageResolver().ReferenceStack(reference);
        result.AlreadyLoadedCount = options.Context.AccountedCount;
        result.CacheFailures = true;
        return result;
    }
System.Collections.Generic.List<string> ReferenceStack(SvgReferenceResult reference)
    {
        System.Collections.Generic.List<string> result = new System.Collections.Generic.List<string>();
        SvgSourceUri? source = reference.ReferenceSource == null ? null : reference.ReferenceSource.Source;
        while (source != null)
        {
            if (source.Uri.Length != 0) result.Add(source.Uri);
            source = source.Parent;
        }
        if (result.Count == 0 && reference.BaseUri.Length != 0) result.Add(reference.BaseUri);
        return result;
    }
SvgResourcePolicyRequest PolicyRequest(SvgReferenceResult reference, string resolvedUri, string mediaType, int redirectCount, bool redirected, int maxRedirects)
    {
        SvgResourcePolicyRequest result = new SvgResourcePolicyRequest(reference.AuthoredUri, reference.BaseUri, resolvedUri, SvgResourceKind.Image);
        result.MediaType = mediaType;
        result.IsRedirect = redirected;
        result.RedirectCount = redirectCount;
        result.MaxRedirects = maxRedirects;
        result.Source = reference.ReferenceSource;
        return result;
    }
bool IsPng(SvgResourceAcquisitionResult acquisition)
    {
        if (string.Equals(acquisition.EffectiveMediaType, SvgMimeTypes.Png, System.StringComparison.OrdinalIgnoreCase)) return true;
        byte[] bytes = acquisition.Bytes;
        return bytes.Length >= 8 && bytes[0] == 137 && bytes[1] == 80 && bytes[2] == 78 && bytes[3] == 71 && bytes[4] == 13 && bytes[5] == 10 && bytes[6] == 26 && bytes[7] == 10;
    }
string RequestedMediaType(string uri)
    {
        int query = uri.IndexOf('?');
        string value = query < 0 ? uri : uri.Substring(0, query);
        int fragment = value.IndexOf('#');
        if (fragment >= 0) value = value.Substring(0, fragment);
        string lower = value.ToLowerInvariant();
        if (lower.EndsWith(".svg", System.StringComparison.Ordinal) || lower.StartsWith("data:image/svg+xml", System.StringComparison.Ordinal)) return SvgMimeTypes.Svg;
        if (lower.EndsWith(".jpg", System.StringComparison.Ordinal) || lower.EndsWith(".jpeg", System.StringComparison.Ordinal) || lower.StartsWith("data:image/jpeg", System.StringComparison.Ordinal)) return SvgMimeTypes.Jpeg;
        if (lower.EndsWith(".gif", System.StringComparison.Ordinal) || lower.StartsWith("data:image/gif", System.StringComparison.Ordinal)) return SvgMimeTypes.Gif;
        if (lower.EndsWith(".webp", System.StringComparison.Ordinal) || lower.StartsWith("data:image/webp", System.StringComparison.Ordinal)) return SvgMimeTypes.WebP;
        if (lower.EndsWith(".avif", System.StringComparison.Ordinal) || lower.StartsWith("data:image/avif", System.StringComparison.Ordinal)) return SvgMimeTypes.Avif;
        return SvgMimeTypes.Png;
    }
SvgIntrinsicSize CopyIntrinsic(SvgIntrinsicSize source)
    {
        SvgIntrinsicSize result = new SvgIntrinsicSize();
        result.HasWidth = source.HasWidth;
        result.Width = source.Width;
        result.HasHeight = source.HasHeight;
        result.Height = source.Height;
        result.HasAspectRatio = source.HasAspectRatio;
        result.AspectRatio = source.AspectRatio;
        return result;
    }
bool WithinRasterLimits(SvgIntrinsicSize intrinsic, SvgRasterLimits limits)
    {
        if (intrinsic.HasWidth && intrinsic.Width > limits.MaxWidth) return false;
        if (intrinsic.HasHeight && intrinsic.Height > limits.MaxHeight) return false;
        if (intrinsic.HasWidth && intrinsic.HasHeight)
        {
            double pixels = intrinsic.Width * intrinsic.Height;
            if (!SvgFiniteMath.IsFinite(pixels) || pixels > limits.MaxSurfacePixels || pixels > limits.MaxPixelBytes / 4) return false;
        }
        return true;
    }
int ResourceIndex(SvgNode? target)
    {
        SvgDocument? document = target == null ? null : target.OwnerDocument;
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
void Append(System.Collections.Generic.List<SvgDiagnostic> destination, System.Collections.Generic.List<SvgDiagnostic> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }
void AddError(SvgImageResolveResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
