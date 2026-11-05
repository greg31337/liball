namespace svg;

/// <summary>Resolves SVG embedded and host-owned elements into deterministic host-content records.</summary>
public class SvgEmbeddedContentResolver : ISvgEmbeddedContentResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgEmbeddedContentResolver()
    {
    }

    /// <summary>Resolves one supported embedded-content element using secure defaults.</summary>
    /// <param name="node">The supported embedded-content element.</param>
    /// <returns>The resolved geometry and host record.</returns>
    public SvgEmbeddedContentResult ResolveWithNode(SvgNode node)
    {
        return new SvgEmbeddedContentResolver().ResolveWithNodeAndOptions(node, new SvgEmbeddedContentResolveOptions());
    }

    /// <summary>Resolves one supported embedded-content element with explicit host and geometry context.</summary>
    /// <param name="node">The supported embedded-content element.</param>
    /// <param name="options">The computed style, intrinsic data, host availability, and bounds.</param>
    /// <returns>The resolved geometry and host record.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the node is not a supported embedded-content element.</exception>
    public SvgEmbeddedContentResult ResolveWithNodeAndOptions(SvgNode node, SvgEmbeddedContentResolveOptions options)
    {
        if (node == null) throw new System.ArgumentNullException("node");
        if (options == null) throw new System.ArgumentNullException("options");
        SvgEmbeddedContentKind kind = new SvgEmbeddedContentResolver().GetKind(node);
        if (node.Kind != SvgNodeKind.Element || kind == SvgEmbeddedContentKind.None) throw new System.ArgumentOutOfRangeException("node");
        SvgEmbeddedContentResult result = new SvgEmbeddedContentResult(node);
        result.Kind = kind;
        result.HostContent = new SvgHostContentRecord(node, kind);
        if (!new SvgEmbeddedContentResolver().ValidOptions(options))
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Embedded-content resolution options are invalid.", node.SourceSpan);
            return result;
        }
        result.InstancePath = options.InstancePath;
        result.HostContent.InstancePath = options.InstancePath;

        result.UsesComputedGeometry = options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node);
        result.Displayed = options.AncestorRendered && new SvgEmbeddedContentResolver().IsDisplayed(node, options);
        result.Visible = new SvgEmbeddedContentResolver().IsVisible(node, options);
        result.ConditionsSatisfied = options.ConditionsSatisfied;

        SvgEmbeddedLengthValue x = new SvgEmbeddedContentResolver().ReadLength(node, SvgAttributeNames.X, SvgLengthPercentageBasis.ViewportWidth, false, options, result);
        SvgEmbeddedLengthValue y = new SvgEmbeddedContentResolver().ReadLength(node, SvgAttributeNames.Y, SvgLengthPercentageBasis.ViewportHeight, false, options, result);
        SvgEmbeddedLengthValue width = new SvgEmbeddedContentResolver().ReadLength(node, SvgAttributeNames.Width, SvgLengthPercentageBasis.ViewportWidth, true, options, result);
        SvgEmbeddedLengthValue height = new SvgEmbeddedContentResolver().ReadLength(node, SvgAttributeNames.Height, SvgLengthPercentageBasis.ViewportHeight, true, options, result);
        if (!x.Success || !y.Success || !width.Success || !height.Success || new SvgEmbeddedContentResolver().HasError(result.Diagnostics)) return result;
        if (!width.Auto && width.Value < 0 || !height.Auto && height.Value < 0)
        {
            SvgSourceSpan? source = !width.Auto && width.Value < 0 ? width.Source : height.Source;
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "An embedded-content width or height cannot be negative.", source == null ? node.SourceSpan : source);
            return result;
        }

        SvgReferenceResult? imageReference = null;
        SvgImageResolveResult? imageResource = null;
        SvgIntrinsicSize? intrinsic = options.IntrinsicSize;
        bool imageContentAvailable = options.ImageContentAvailable;
        if (kind == SvgEmbeddedContentKind.Image)
        {
            imageReference = new SvgReferenceResolver().ResolveWithNodeAndOptions(node, options.ReferenceOptions);
            new SvgEmbeddedContentResolver().AppendReference(imageReference, result);
            if (new SvgEmbeddedContentResolver().CanAcquireImage(imageReference, options.ImageOptions))
            {
                imageResource = new SvgImageResolver().Resolve(imageReference, (options.ImageOptions ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                new SvgEmbeddedContentResolver().AppendImageResource(imageResource, result);
                if (imageResource.Success && imageResource.IntrinsicSize != null) intrinsic = imageResource.IntrinsicSize;
                if (imageResource.DecodedContentAvailable) imageContentAvailable = true;
            }
        }

        SvgEmbeddedUsedSize provisional = new SvgEmbeddedContentResolver().ResolveUsedSize(width, height, intrinsic);
        SvgRect provisionalBounds = SvgRect.Create(x.Value, y.Value, provisional.Width, provisional.Height, provisional.Width <= 0 || provisional.Height <= 0);
        new SvgEmbeddedContentResolver().SnapshotChildren(node, options, result);
        bool callbackEligible = result.Displayed && result.Visible && result.ConditionsSatisfied && provisionalBounds.Width > 0 && provisionalBounds.Height > 0;
        if (!new SvgEmbeddedContentResolver().HasError(result.Diagnostics) && callbackEligible && options.HostContentCallback != null)
        {
            SvgHostContentCallbackResult? callback = new SvgEmbeddedContentResolver().InvokeCallback(node, kind, provisionalBounds, imageReference, imageResource, options, result);
            if (callback != null && callback.Success)
            {
                result.CallbackResult = callback;
                if (callback.IntrinsicSize != null) intrinsic = callback.IntrinsicSize;
                else if (callback.RasterPixels != null) intrinsic = SvgIntrinsicSize.Create(callback.RasterPixels.Width, callback.RasterPixels.Height);
                if (kind == SvgEmbeddedContentKind.Image && callback.RasterPixels != null) imageContentAvailable = true;
            }
        }

        SvgEmbeddedUsedSize used = new SvgEmbeddedContentResolver().ResolveUsedSize(width, height, intrinsic);
        result.X = x.Value;
        result.Y = y.Value;
        result.Width = used.Width;
        result.Height = used.Height;
        result.WidthAuto = width.Auto;
        result.HeightAuto = height.Auto;
        result.UsedDefaultObjectSize = used.UsedDefault;
        result.SourceWidth = used.SourceWidth;
        result.SourceHeight = used.SourceHeight;
        result.HasIntrinsicAspectRatio = used.HasAspectRatio;
        result.IntrinsicAspectRatio = used.AspectRatio;
        result.HasGeometry = used.Width > 0 && used.Height > 0;
        result.Bounds = SvgRect.Create(result.X, result.Y, result.Width, result.Height, !result.HasGeometry);

        if (kind == SvgEmbeddedContentKind.Image) new SvgEmbeddedContentResolver().ResolveImage(node, options, used, imageReference, imageResource, imageContentAvailable, result);
        else new SvgEmbeddedContentResolver().ResolveHostContent(node, options, result);
        new SvgEmbeddedContentResolver().ApplyCallbackOutput(result);

        result.HostContent.Bounds = result.Bounds;
        result.HostContent.ContentBounds = result.ContentBounds;
        result.HostContent.ContentTransform = result.ContentTransform;
        result.HostContent.ClipsOverflow = result.ClipsOverflow;
        result.HostContent.ClipBounds = result.ClipBounds;
        result.HostContent.ContentAvailable = result.ContentAvailable;
        result.HostContent.ConditionsSatisfied = result.ConditionsSatisfied;
        result.HostContent.Href = result.Href;
        result.HostContent.XLinkHref = result.XLinkHref;
        result.HostContent.CrossOrigin = result.CrossOrigin;
        result.HostContent.Reference = result.Reference;
        result.HostContent.ImageResource = result.ImageResource;
        result.HostContent.CallbackResult = result.CallbackResult;
        result.Renderable = result.HasGeometry && result.HasContent && result.ContentAvailable && result.Displayed && result.Visible && result.ConditionsSatisfied;
        result.Success = !new SvgEmbeddedContentResolver().HasError(result.Diagnostics);
        return result;
    }
SvgEmbeddedContentKind GetKind(SvgNode? node)
    {
        if (node == null || node.Kind != SvgNodeKind.Element) return SvgEmbeddedContentKind.None;
        if (node.ElementKind == SvgElementKind.Image) return SvgEmbeddedContentKind.Image;
        if (node.ElementKind == SvgElementKind.ForeignObject) return SvgEmbeddedContentKind.ForeignObject;
        if (node.ElementKind == SvgElementKind.Audio) return SvgEmbeddedContentKind.Audio;
        if (node.ElementKind == SvgElementKind.Video) return SvgEmbeddedContentKind.Video;
        if (node.ElementKind == SvgElementKind.Iframe) return SvgEmbeddedContentKind.Iframe;
        if (node.ElementKind == SvgElementKind.Canvas) return SvgEmbeddedContentKind.Canvas;
        return SvgEmbeddedContentKind.None;
    }
bool CanAcquireImage(SvgReferenceResult? reference, SvgImageResolveOptions? options)
    {
        if (options == null || reference == null || !reference.Success || reference.Status != SvgReferenceStatus.External) return false;
        if (options.LoadOptions != null && options.LoadOptions.ResourceProvider != null) return true;
        return reference.ResourceUri.StartsWith("data:", System.StringComparison.OrdinalIgnoreCase);
    }
    bool ValidOptions(SvgEmbeddedContentResolveOptions options)
    {
        if (options.Attributes == null || options.InstancePath == null || options.LengthContext == null || options.ReferenceOptions == null || options.MaxHostContentNodes < 0) return false;
        if (options.MaxHostRasterWidth < 1 || options.MaxHostRasterHeight < 1 || options.MaxHostRasterPixels < 1 || options.MaxHostRasterBytes < 1 || options.MaxHostDiagnostics < 0 || options.MaxHostCallbackMilliseconds < 1) return false;
        if (options.ReferenceOptions.MaxUriCharacters < 1 || options.ReferenceOptions.MaxBaseDepth < 1) return false;
        if (!new SvgEmbeddedContentResolver().ValidLengthContext(options.LengthContext)) return false;
        SvgIntrinsicSize? intrinsic = options.IntrinsicSize;
        if (intrinsic == null) return true;
        if (intrinsic.HasWidth && (!SvgFiniteMath.IsFinite(intrinsic.Width) || intrinsic.Width <= 0)) return false;
        if (intrinsic.HasHeight && (!SvgFiniteMath.IsFinite(intrinsic.Height) || intrinsic.Height <= 0)) return false;
        if (intrinsic.HasAspectRatio && (!SvgFiniteMath.IsFinite(intrinsic.AspectRatio) || intrinsic.AspectRatio <= 0)) return false;
        return true;
    }
bool ValidLengthContext(SvgLengthContext context)
    {
        if (context.HasViewport && (!SvgFiniteMath.IsFinite(context.ViewportWidth) || !SvgFiniteMath.IsFinite(context.ViewportHeight) || context.ViewportWidth < 0 || context.ViewportHeight < 0)) return false;
        if (context.HasFontSize && (!SvgFiniteMath.IsFinite(context.FontSize) || context.FontSize < 0)) return false;
        if (context.HasXHeight && (!SvgFiniteMath.IsFinite(context.XHeight) || context.XHeight < 0)) return false;
        if (context.HasZeroAdvance && (!SvgFiniteMath.IsFinite(context.ZeroAdvance) || context.ZeroAdvance < 0)) return false;
        if (context.HasRootFontSize && (!SvgFiniteMath.IsFinite(context.RootFontSize) || context.RootFontSize < 0)) return false;
        return true;
    }
SvgEmbeddedUsedSize ResolveUsedSize(SvgEmbeddedLengthValue width, SvgEmbeddedLengthValue height, SvgIntrinsicSize? intrinsic)
    {
        SvgEmbeddedUsedSize result = new SvgEmbeddedUsedSize();
        bool hasWidth = intrinsic != null && intrinsic.HasWidth;
        bool hasHeight = intrinsic != null && intrinsic.HasHeight;
        bool hasRatio = intrinsic != null && intrinsic.HasAspectRatio;
        double intrinsicWidth = hasWidth ? (intrinsic ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Width : 0;
        double intrinsicHeight = hasHeight ? (intrinsic ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Height : 0;
        double ratio = hasRatio ? (intrinsic ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).AspectRatio : hasWidth && hasHeight ? intrinsicWidth / intrinsicHeight : 0;
        if (!hasRatio && hasWidth && hasHeight) hasRatio = true;

        if (!width.Auto) result.Width = width.Value;
        if (!height.Auto) result.Height = height.Value;
        if (width.Auto && height.Auto)
        {
            if (hasWidth && hasHeight)
            {
                result.Width = intrinsicWidth;
                result.Height = intrinsicHeight;
            }
            else if (hasWidth && hasRatio)
            {
                result.Width = intrinsicWidth;
                result.Height = intrinsicWidth / ratio;
            }
            else if (hasHeight && hasRatio)
            {
                result.Width = intrinsicHeight * ratio;
                result.Height = intrinsicHeight;
            }
            else if (hasWidth)
            {
                result.Width = intrinsicWidth;
                result.Height = 150;
                result.UsedDefault = true;
            }
            else if (hasHeight)
            {
                result.Width = 300;
                result.Height = intrinsicHeight;
                result.UsedDefault = true;
            }
            else if (hasRatio)
            {
                if (ratio >= 2)
                {
                    result.Width = 300;
                    result.Height = 300 / ratio;
                }
                else
                {
                    result.Width = 150 * ratio;
                    result.Height = 150;
                }
                result.UsedDefault = true;
            }
            else
            {
                result.Width = 300;
                result.Height = 150;
                result.UsedDefault = true;
            }
        }
        else if (width.Auto)
        {
            if (hasRatio) result.Width = result.Height * ratio;
            else if (hasWidth) result.Width = intrinsicWidth;
            else
            {
                result.Width = 300;
                result.UsedDefault = true;
            }
        }
        else if (height.Auto)
        {
            if (hasRatio) result.Height = result.Width / ratio;
            else if (hasHeight) result.Height = intrinsicHeight;
            else
            {
                result.Height = 150;
                result.UsedDefault = true;
            }
        }

        result.HasAspectRatio = hasRatio;
        result.AspectRatio = ratio;
        if (hasWidth && hasHeight)
        {
            result.SourceWidth = intrinsicWidth;
            result.SourceHeight = intrinsicHeight;
        }
        else if (hasWidth && hasRatio)
        {
            result.SourceWidth = intrinsicWidth;
            result.SourceHeight = intrinsicWidth / ratio;
        }
        else if (hasHeight && hasRatio)
        {
            result.SourceWidth = intrinsicHeight * ratio;
            result.SourceHeight = intrinsicHeight;
        }
        else if (hasRatio)
        {
            result.SourceWidth = ratio;
            result.SourceHeight = 1;
        }
        else
        {
            result.SourceWidth = result.Width;
            result.SourceHeight = result.Height;
        }
        return result;
    }
void ResolveImage(SvgNode node, SvgEmbeddedContentResolveOptions options, SvgEmbeddedUsedSize used, SvgReferenceResult? reference, SvgImageResolveResult? imageResource, bool contentAvailable, SvgEmbeddedContentResult result)
    {
        SvgAttribute? hrefAttribute = new SvgEmbeddedContentResolver().FindAttribute(node, "", SvgAttributeNames.Href);
        SvgAttribute? xlinkAttribute = new SvgEmbeddedContentResolver().FindAttribute(node, SvgNamespaces.XLink, SvgAttributeNames.Href);
        if (hrefAttribute != null) result.Href = new SvgEmbeddedContentResolver().ParseRetainedReference(hrefAttribute, options.ReferenceOptions.MaxUriCharacters);
        if (xlinkAttribute != null) result.XLinkHref = new SvgEmbeddedContentResolver().ParseRetainedReference(xlinkAttribute, options.ReferenceOptions.MaxUriCharacters);
        result.Reference = reference;
        result.ImageResource = imageResource;
        result.HasContent = (result.Reference ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Success && result.Reference.Reference != null;

        SvgTypedAttributeValue aspect = options.Attributes.Get(node, SvgAttributeNames.PreserveAspectRatio);
        new SvgEmbeddedContentResolver().AppendTyped(aspect, result);
        if (aspect.Success && aspect.HasValue && aspect.ValueKind == SvgTypedAttributeKind.AspectRatio) result.AspectRatio = (SvgAspectRatio?)aspect.Value;
        result.CrossOrigin = new SvgEmbeddedContentResolver().ReadCrossOrigin(node);
        result.ContentAvailable = contentAvailable;
        result.ClipsOverflow = new SvgEmbeddedContentResolver().ClipsOverflow(node, true, options, result);
        if (result.ClipsOverflow) result.ClipBounds = result.Bounds;

        if (!result.HasGeometry || used.SourceWidth <= 0 || used.SourceHeight <= 0)
        {
            result.ContentBounds = SvgRect.Create(result.X, result.Y, result.Width, result.Height, true);
            result.ContentTransform = new SvgMatrix(1, 0, 0, 1, result.X, result.Y);
            return;
        }
        SvgAspectRatio? fitAspect = result.AspectRatio;
        if (!used.HasAspectRatio)
        {
            fitAspect = new SvgAspectRatio(SvgAspectRatioAlignment.None, SvgAspectRatioScale.Meet, false);
        }
        result.AspectRatioApplied = used.HasAspectRatio;
        double fitWidth = used.HasAspectRatio ? used.AspectRatio : used.SourceWidth;
        double fitHeight = used.HasAspectRatio ? 1 : used.SourceHeight;
        SvgViewBox source = new SvgViewBox(0, 0, fitWidth, fitHeight);
        SvgViewBoxFitResult fit = (fitAspect ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Fit(source, result.X, result.Y, result.Width, result.Height);
        if (!fit.Success)
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The image intrinsic dimensions could not be fitted to its positioning rectangle.", node.SourceSpan);
            return;
        }
        double contentX = (fit.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).E;
        double contentY = fit.Matrix.F;
        double contentWidth = System.Math.Abs(fit.Matrix.A) * fitWidth;
        double contentHeight = System.Math.Abs(fit.Matrix.D) * fitHeight;
        result.ContentTransform = new SvgMatrix(contentWidth / used.SourceWidth, 0, 0, contentHeight / used.SourceHeight, contentX, contentY);
        result.ContentBounds = SvgRect.Create(contentX, contentY, contentWidth, contentHeight, false);
    }
void SnapshotChildren(SvgNode node, SvgEmbeddedContentResolveOptions options, SvgEmbeddedContentResult result)
    {
        int index = 0;
        while (index < node.Children.Count && index < options.MaxHostContentNodes)
        {
            (result.HostContent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children.Add(node.Children[index]);
            index = index + 1;
        }
        if (index < node.Children.Count)
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The host-content direct child-node limit was exceeded.", node.SourceSpan);
        }
    }
SvgHostContentCallbackResult? InvokeCallback(SvgNode node, SvgEmbeddedContentKind kind, SvgRect bounds, SvgReferenceResult? reference, SvgImageResolveResult? imageResource, SvgEmbeddedContentResolveOptions options, SvgEmbeddedContentResult result)
    {
        if (options.CancellationToken.IsCancellationRequested)
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.Cancelled, "Host-content processing was cancelled.", node.SourceSpan);
            return null;
        }
        SvgHostContentCallbackRequest request = new SvgHostContentCallbackRequest(node, kind, bounds);
        request.InstancePath = options.InstancePath;
        request.Reference = reference;
        request.ImageResource = imageResource;
        request.Children = new System.Collections.Generic.List<SvgNode>((result.HostContent ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Children);
        request.MaxRasterWidth = options.MaxHostRasterWidth;
        request.MaxRasterHeight = options.MaxHostRasterHeight;
        request.MaxRasterPixels = options.MaxHostRasterPixels;
        request.MaxRasterBytes = options.MaxHostRasterBytes;
        request.MaxDiagnostics = options.MaxHostDiagnostics;
        request.CancellationToken = options.CancellationToken;
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        SvgHostContentCallbackResult? callback = (options.HostContentCallback ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))(request);
        stopwatch.Stop();
        if (callback != null && new SvgEmbeddedContentResolver().ValidCallbackDiagnostics(callback.Diagnostics, options.MaxHostDiagnostics)) new SvgEmbeddedContentResolver().AppendCallbackDiagnostics(callback, result);
        else if (callback != null)
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The host-content callback diagnostics exceeded their configured bound or were invalid.", node.SourceSpan);
            return null;
        }
        if (options.CancellationToken.IsCancellationRequested)
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.Cancelled, "Host-content processing was cancelled.", node.SourceSpan);
            return null;
        }
        if (stopwatch.ElapsedMilliseconds > options.MaxHostCallbackMilliseconds)
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The host-content callback exceeded MaxHostCallbackMilliseconds.", node.SourceSpan);
            return null;
        }
        if (callback == null || callback.Diagnostics == null || !callback.Success)
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.RenderingFailed, "The host-content callback did not return a successful result.", node.SourceSpan);
            return null;
        }
        if (!new SvgEmbeddedContentResolver().ValidCallbackResult(callback, options))
        {
            SvgDiagnosticCode code = new SvgEmbeddedContentResolver().RasterExceedsLimits(callback.RasterPixels, options) ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidValue;
            new SvgEmbeddedContentResolver().AddError(result, code, "The host-content callback returned invalid or out-of-bounds output.", node.SourceSpan);
            return null;
        }
        return callback;
    }
void ResolveHostContent(SvgNode node, SvgEmbeddedContentResolveOptions options, SvgEmbeddedContentResult result)
    {
        SvgAttribute? hrefAttribute = new SvgEmbeddedContentResolver().FindAttribute(node, "", SvgAttributeNames.Href);
        SvgAttribute? xlinkAttribute = new SvgEmbeddedContentResolver().FindAttribute(node, SvgNamespaces.XLink, SvgAttributeNames.Href);
        if (hrefAttribute != null) result.Href = new SvgEmbeddedContentResolver().ParseRetainedReference(hrefAttribute, options.ReferenceOptions.MaxUriCharacters);
        if (xlinkAttribute != null) result.XLinkHref = new SvgEmbeddedContentResolver().ParseRetainedReference(xlinkAttribute, options.ReferenceOptions.MaxUriCharacters);
        bool authoredContent = node.Children.Count > 0 || result.Href != null || result.XLinkHref != null || result.Kind == SvgEmbeddedContentKind.Canvas;
        result.HasContent = authoredContent || result.CallbackResult != null && result.CallbackResult.Success;
        result.ContentAvailable = result.CallbackResult != null && result.CallbackResult.Success || result.Kind == SvgEmbeddedContentKind.ForeignObject && options.ForeignContentCallbackAvailable;
        result.ContentBounds = result.Bounds;
        result.ContentTransform = new SvgMatrix(1, 0, 0, 1, result.X, result.Y);
        result.ClipsOverflow = new SvgEmbeddedContentResolver().ClipsOverflow(node, false, options, result);
        if (result.ClipsOverflow) result.ClipBounds = result.Bounds;
    }
void ApplyCallbackOutput(SvgEmbeddedContentResult result)
    {
        SvgHostContentCallbackResult? callback = result.CallbackResult;
        if (callback == null || !callback.Success) return;
        if (callback.ContentBounds != null) result.ContentBounds = callback.ContentBounds;
        if (callback.ContentTransform != null) result.ContentTransform = callback.ContentTransform;
        else if (callback.RasterPixels != null && result.ContentBounds != null)
        {
            SvgRect bounds = result.ContentBounds;
            result.ContentTransform = new SvgMatrix(bounds.Width / callback.RasterPixels.Width, 0, 0, bounds.Height / callback.RasterPixels.Height, bounds.X, bounds.Y);
        }
        if (result.Kind != SvgEmbeddedContentKind.Image || callback.RasterPixels != null) result.ContentAvailable = true;
    }
bool ValidCallbackResult(SvgHostContentCallbackResult callback, SvgEmbeddedContentResolveOptions options)
    {
        if (!new SvgEmbeddedContentResolver().ValidCallbackDiagnostics(callback.Diagnostics, options.MaxHostDiagnostics)) return false;
        if (callback.IntrinsicSize != null && !new SvgEmbeddedContentResolver().ValidIntrinsic(callback.IntrinsicSize)) return false;
        if (callback.ContentBounds != null && !new SvgEmbeddedContentResolver().ValidRect(callback.ContentBounds)) return false;
        if (callback.ContentTransform != null && !new SvgEmbeddedContentResolver().ValidMatrix(callback.ContentTransform)) return false;
        if (callback.RasterPixels != null && (!SvgImageSampleMath.ValidBuffer(callback.RasterPixels) || new SvgEmbeddedContentResolver().RasterExceedsLimits(callback.RasterPixels, options))) return false;
        return true;
    }
bool RasterExceedsLimits(SvgPixelBuffer? pixels, SvgEmbeddedContentResolveOptions options)
    {
        if (pixels == null) return false;
        return pixels.Width > options.MaxHostRasterWidth || pixels.Height > options.MaxHostRasterHeight || (long)pixels.Width * pixels.Height > options.MaxHostRasterPixels || pixels.ByteLength > options.MaxHostRasterBytes;
    }
bool ValidCallbackDiagnostics(System.Collections.Generic.List<SvgDiagnostic>? diagnostics, int maximum)
    {
        if (diagnostics == null || diagnostics.Count > maximum) return false;
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index] == null) return false;
            index = index + 1;
        }
        return true;
    }
bool ValidIntrinsic(SvgIntrinsicSize intrinsic)
    {
        if (intrinsic.HasWidth && (!SvgFiniteMath.IsFinite(intrinsic.Width) || intrinsic.Width <= 0)) return false;
        if (intrinsic.HasHeight && (!SvgFiniteMath.IsFinite(intrinsic.Height) || intrinsic.Height <= 0)) return false;
        if (intrinsic.HasAspectRatio && (!SvgFiniteMath.IsFinite(intrinsic.AspectRatio) || intrinsic.AspectRatio <= 0)) return false;
        return true;
    }
bool ValidRect(SvgRect rectangle)
    {
        return SvgFiniteMath.IsFinite(rectangle.X) && SvgFiniteMath.IsFinite(rectangle.Y) && SvgFiniteMath.IsFinite(rectangle.Width) && SvgFiniteMath.IsFinite(rectangle.Height) && rectangle.Width >= 0 && rectangle.Height >= 0;
    }
bool ValidMatrix(SvgMatrix matrix)
    {
        return SvgFiniteMath.IsFinite(matrix.A) && SvgFiniteMath.IsFinite(matrix.B) && SvgFiniteMath.IsFinite(matrix.C) && SvgFiniteMath.IsFinite(matrix.D) && SvgFiniteMath.IsFinite(matrix.E) && SvgFiniteMath.IsFinite(matrix.F);
    }
SvgIriReference? ParseRetainedReference(SvgAttribute attribute, int maximum)
    {
        SvgIriParseResult parsed = new SvgIriParser().ParseIri(attribute.Value, maximum);
        return parsed.Success ? parsed.Reference : null;
    }
SvgCrossOriginMode ReadCrossOrigin(SvgNode node)
    {
        SvgAttribute? attribute = new SvgEmbeddedContentResolver().FindAttribute(node, "", SvgAttributeNames.CrossOrigin);
        if (attribute == null) return SvgCrossOriginMode.Unspecified;
        string value = attribute.Value.Trim();
        if (string.Equals(value, "use-credentials", System.StringComparison.OrdinalIgnoreCase)) return SvgCrossOriginMode.UseCredentials;
        return SvgCrossOriginMode.Anonymous;
    }
SvgEmbeddedLengthValue ReadLength(SvgNode node, string name, SvgLengthPercentageBasis basis, bool allowAuto, SvgEmbeddedContentResolveOptions options, SvgEmbeddedContentResult result)
    {
        SvgLength? length = null;
        SvgSourceSpan? source = node.SourceSpan;
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            SvgComputedValue? computed = options.ComputedStyle.Find(name);
            if (computed == null)
            {
                new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The computed geometry property '" + name + "' is missing.", node.SourceSpan);
                return new SvgEmbeddedLengthValue(false, false, 0, node.SourceSpan);
            }
            source = computed.Source == null ? node.SourceSpan : computed.Source;
            if (computed.Kind == SvgComputedValueKind.Keyword && allowAuto && string.Equals(computed.Keyword, "auto", System.StringComparison.OrdinalIgnoreCase)) return new SvgEmbeddedLengthValue(true, true, 0, source);
            if (computed.Kind != SvgComputedValueKind.Length || computed.Length == null)
            {
                new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The computed geometry property '" + name + "' is not a length.", source);
                return new SvgEmbeddedLengthValue(false, false, 0, source);
            }
            length = computed.Length;
        }
        else
        {
            SvgTypedAttributeValue typed = options.Attributes.Get(node, name);
            new SvgEmbeddedContentResolver().AppendTyped(typed, result);
            source = typed.SourceAttribute == null ? node.SourceSpan : typed.SourceAttribute.SourceSpan;
            if (!typed.Success || !typed.HasValue) return new SvgEmbeddedLengthValue(false, false, 0, source);
            if (typed.ValueKind == SvgTypedAttributeKind.Keyword && allowAuto && string.Equals((string?)typed.Value, "auto", System.StringComparison.OrdinalIgnoreCase)) return new SvgEmbeddedLengthValue(true, true, 0, source);
            if (typed.ValueKind != SvgTypedAttributeKind.Length || typed.Value == null)
            {
                new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The embedded-content geometry value '" + name + "' is not a length or auto.", source);
                return new SvgEmbeddedLengthValue(false, false, 0, source);
            }
            length = (SvgLength)typed.Value;
        }
        SvgLengthEvaluationResult evaluated = length.Evaluate(options.LengthContext, basis);
        if (!evaluated.Success)
        {
            new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The embedded-content length '" + name + "' could not be resolved in the supplied context.", source);
            return new SvgEmbeddedLengthValue(false, false, 0, source);
        }
        return new SvgEmbeddedLengthValue(true, false, evaluated.Value, source);
    }
bool IsDisplayed(SvgNode node, SvgEmbeddedContentResolveOptions options)
    {
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            string computed = options.ComputedStyle.GetKeyword(SvgCssPropertyNames.Display);
            if (computed.Length != 0) return !string.Equals(computed, "none", System.StringComparison.OrdinalIgnoreCase);
        }
        SvgAttribute? attribute = new SvgEmbeddedContentResolver().FindAttribute(node, "", SvgCssPropertyNames.Display);
        return attribute == null || !string.Equals(attribute.Value.Trim(), "none", System.StringComparison.OrdinalIgnoreCase);
    }
bool IsVisible(SvgNode node, SvgEmbeddedContentResolveOptions options)
    {
        string value = "visible";
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            string computed = options.ComputedStyle.GetKeyword(SvgCssPropertyNames.Visibility);
            if (computed.Length != 0) value = computed;
        }
        else
        {
            SvgAttribute? attribute = new SvgEmbeddedContentResolver().FindAttribute(node, "", SvgCssPropertyNames.Visibility);
            if (attribute != null) value = attribute.Value.Trim();
        }
        return !string.Equals(value, "hidden", System.StringComparison.OrdinalIgnoreCase) && !string.Equals(value, "collapse", System.StringComparison.OrdinalIgnoreCase);
    }
bool ClipsOverflow(SvgNode node, bool image, SvgEmbeddedContentResolveOptions options, SvgEmbeddedContentResult result)
    {
        if (options.ComputedStyle != null && object.ReferenceEquals(options.ComputedStyle.Node, node))
        {
            string x = options.ComputedStyle.GetKeyword(SvgCssPropertyNames.OverflowX);
            string y = options.ComputedStyle.GetKeyword(SvgCssPropertyNames.OverflowY);
            if (x.Length != 0 && y.Length != 0) return !new SvgEmbeddedContentResolver().VisibleOverflow(x) || !new SvgEmbeddedContentResolver().VisibleOverflow(y);
        }
        SvgAttribute? attribute = new SvgEmbeddedContentResolver().FindAttribute(node, "", SvgCssPropertyNames.Overflow);
        if (attribute == null) return image;
        string[]? values = attribute.Value.Split((char[]?)null, System.StringSplitOptions.RemoveEmptyEntries);
        if (values.Length == 1 && new SvgEmbeddedContentResolver().OverflowKeyword(values[0])) return !new SvgEmbeddedContentResolver().VisibleOverflow(values[0]);
        if (values.Length == 2 && new SvgEmbeddedContentResolver().OverflowKeyword(values[0]) && new SvgEmbeddedContentResolver().OverflowKeyword(values[1])) return !new SvgEmbeddedContentResolver().VisibleOverflow(values[0]) || !new SvgEmbeddedContentResolver().VisibleOverflow(values[1]);
        new SvgEmbeddedContentResolver().AddError(result, SvgDiagnosticCode.InvalidValue, "The embedded-content overflow value is invalid.", attribute.SourceSpan);
        return image;
    }
bool OverflowKeyword(string value)
    {
        return string.Equals(value, "visible", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "hidden", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "clip", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "scroll", System.StringComparison.OrdinalIgnoreCase) || string.Equals(value, "auto", System.StringComparison.OrdinalIgnoreCase);
    }
bool VisibleOverflow(string value)
    {
        return string.Equals(value, "visible", System.StringComparison.OrdinalIgnoreCase);
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
void AppendTyped(SvgTypedAttributeValue typed, SvgEmbeddedContentResult result)
    {
        int index = 0;
        while (index < typed.Diagnostics.Count)
        {
            result.Diagnostics.Add(typed.Diagnostics[index]);
            index = index + 1;
        }
    }
void AppendReference(SvgReferenceResult reference, SvgEmbeddedContentResult result)
    {
        int index = 0;
        while (index < reference.Diagnostics.Count)
        {
            result.Diagnostics.Add(reference.Diagnostics[index]);
            index = index + 1;
        }
    }
void AppendImageResource(SvgImageResolveResult image, SvgEmbeddedContentResult result)
    {
        int index = 0;
        while (index < image.Diagnostics.Count)
        {
            result.Diagnostics.Add(image.Diagnostics[index]);
            index = index + 1;
        }
    }
void AppendCallbackDiagnostics(SvgHostContentCallbackResult callback, SvgEmbeddedContentResult result)
    {
        int index = 0;
        while (index < callback.Diagnostics.Count)
        {
            SvgDiagnostic? diagnostic = callback.Diagnostics[index];
            if (diagnostic != null) result.Diagnostics.Add(diagnostic);
            index = index + 1;
        }
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
void AddError(SvgEmbeddedContentResult result, SvgDiagnosticCode code, string message, SvgSourceSpan? source)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, source));
    }
}
