namespace svg;

/// <summary>Acquires bounded resources only from data URLs or an explicit synchronous caller provider.</summary>
public class SvgResourceResolver : ISvgResourceResolver
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgResourceResolver()
    {
    }

    /// <summary>Acquires one resource without implicit network or filesystem access.</summary>
    /// <param name="request">The caller-visible resource request.</param>
    /// <param name="options">The callback, cancellation, ownership, and byte limits.</param>
    /// <returns>A bounded byte snapshot with declared and actual media metadata.</returns>
    public SvgResourceAcquisitionResult Resolve(SvgResourceRequest request, SvgResourceAcquisitionOptions options)
    {
        SvgResourceAcquisitionResult result = new SvgResourceAcquisitionResult(request);
        if (!new SvgResourceResolver().Valid(request, options))
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "Resource acquisition arguments are invalid.");
            return result;
        }
        SvgUriResult uri = request.BaseUri.Length == 0 ? new SvgUri().NormalizeWithValueAndMaxCharacters(request.NormalizedUri, options.Limits.MaxUriCharacters) : new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(request.BaseUri, request.NormalizedUri, options.Limits.MaxUriCharacters);
        if (!uri.Success || uri.NormalizedIri.Length > options.Limits.MaxUriCharacters)
        {
            new SvgResourceResolver().AddError(result, uri.Error == SvgUriError.TooLong || uri.NormalizedIri.Length > options.Limits.MaxUriCharacters ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidReference, "The resource URI is invalid or exceeds MaxUriCharacters.");
            return result;
        }
        request.NormalizedUri = uri.NormalizedIri;
        result.NormalizedUri = uri.NormalizedIri;
        result.FinalUri = uri.NormalizedIri;
        result.CacheKey = uri.CacheKey;
        string requestedMediaType = new SvgResourceResolver().NormalizeMediaType(request.MediaKind);
        if (requestedMediaType.Length == 0)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.InvalidArgument, "The requested media kind is malformed.");
            return result;
        }
        if (request.RedirectCount > options.Limits.MaxRedirects)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The resource request exceeds MaxRedirects.");
            return result;
        }
        if (new SvgResourceResolver().Cancelled(request, options))
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.Cancelled, "Resource acquisition was cancelled.");
            return result;
        }
        if (new SvgResourceResolver().ReferenceContains(options.ReferenceStack, result.CacheKey, options.Limits.MaxUriCharacters))
        {
            new SvgResourceResolver().AddCycleError(result, result.FinalUri);
            return result;
        }
        if (options.Cache != null)
        {
            SvgResourceCacheLookup lookup = options.Cache.Get(result.CacheKey);
            if (lookup.Found) return new SvgResourceResolver().ResolveCached(requestedMediaType, options, result, lookup.Entry);
        }
        if (options.AlreadyLoadedCount >= options.Limits.MaxCount)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The resource count exceeds MaxCount.");
            return result;
        }
        result.BudgetCount = 1;
        if (new SvgResourceResolver().Scheme(uri.NormalizedIri) == "data" && options.DecodeDataUrls) return new SvgResourceResolver().ResolveDataUrl(request, options, result);
        if (options.ResourceProvider == null)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.CallbackRequired, "External resource acquisition requires a caller resource provider.");
            return result;
        }
        result.ProviderInvoked = true;
        result.RequestCount = 1;
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        SvgResourceResponse? response = options.ResourceProvider.GetResource(request);
        stopwatch.Stop();
        result.Response = response;
        if (response != null) new SvgResourceResolver().AppendDiagnostics(result, response.Diagnostics);
        if (new SvgResourceResolver().Cancelled(request, options))
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.Cancelled, "Resource acquisition was cancelled.");
            return result;
        }
        if (stopwatch.ElapsedMilliseconds > options.Limits.MaxCallbackMilliseconds)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.ResourceFailed, "The resource provider exceeded MaxCallbackMilliseconds.");
            new SvgResourceResolver().CacheFailure(options, result);
            return result;
        }
        if (response == null || !response.IsValid())
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.ResourceFailed, "The resource provider returned an invalid response.");
            new SvgResourceResolver().CacheFailure(options, result);
            return result;
        }
        if (response.RedirectCount > options.Limits.MaxRedirects)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The resource response exceeds MaxRedirects.");
            return result;
        }
        string finalText = response.FinalUri.Length == 0 ? result.NormalizedUri : response.FinalUri;
        SvgUriResult finalUri = new SvgUri().ResolveWithBaseUriAndReferenceAndMaxCharacters(result.NormalizedUri, finalText, options.Limits.MaxUriCharacters);
        if (!finalUri.Success || finalUri.NormalizedIri.Length > options.Limits.MaxUriCharacters)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.ResourceFailed, "The resource provider returned an invalid final URI.");
            new SvgResourceResolver().CacheFailure(options, result);
            return result;
        }
        result.FinalUri = finalUri.NormalizedIri;
        if (new SvgResourceResolver().ReferenceContains(options.ReferenceStack, finalUri.CacheKey, options.Limits.MaxUriCharacters))
        {
            new SvgResourceResolver().AddCycleError(result, result.FinalUri);
            return result;
        }
        result.RedirectCount = response.RedirectCount;
        result.DeclaredMediaType = new SvgResourceResolver().NormalizeMediaType(response.DeclaredMediaType);
        result.ActualMediaType = new SvgResourceResolver().NormalizeMediaType(response.ActualMediaType);
        if (response.DeclaredMediaType.Length != 0 && result.DeclaredMediaType.Length == 0 || response.ActualMediaType.Length != 0 && result.ActualMediaType.Length == 0)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.ResourceFailed, "The resource provider returned malformed media metadata.");
            new SvgResourceResolver().CacheFailure(options, result);
            return result;
        }
        result.EffectiveMediaType = result.ActualMediaType.Length != 0 ? result.ActualMediaType : result.DeclaredMediaType.Length != 0 ? result.DeclaredMediaType : requestedMediaType;
        byte[]? bytes = response.Bytes == null ? new SvgResourceResolver().ReadStream(response.Stream, request, options, result) : new SvgResourceResolver().ReadArray(response.Bytes, options, result);
        if (bytes == null) return result;
        if (new SvgResourceResolver().Cancelled(request, options))
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.Cancelled, "Resource acquisition was cancelled.");
            return result;
        }
        result.Bytes = bytes;
        result.ImageMetadata = new SvgResourceResolver().CopyImageMetadata(response.ImageMetadata);
        result.Cacheable = response.Cacheable;
        result.IsIndependentSnapshot = response.Bytes == null || options.CopyInputData;
        result.Success = true;
        result.LoadedCount = 1;
        result.ResourceSource = new SvgResourceResolver().CreateResourceSource(result.FinalUri, request);
        if (result.Cacheable && options.Cache != null) options.Cache.Add(result.CacheKey, result);
        return result;
    }
SvgResourceAcquisitionResult ResolveDataUrl(SvgResourceRequest request, SvgResourceAcquisitionOptions options, SvgResourceAcquisitionResult result)
    {
        long maximum = new SvgResourceResolver().EffectiveMaximum(options);
        if (maximum > options.Limits.MaxDataUrlBytes) maximum = options.Limits.MaxDataUrlBytes;
        SvgDataUrlResult parsed = new SvgDataUrl().ParseWithValueAndMaxDecodedBytes(result.NormalizedUri, maximum);
        if (!parsed.Success)
        {
            SvgDiagnosticCode code = parsed.Error == SvgDataUrlError.DecodedSizeLimitExceeded ? SvgDiagnosticCode.InputLimitExceeded : SvgDiagnosticCode.InvalidReference;
            new SvgResourceResolver().AddError(result, code, "The data URL is invalid or exceeds MaxDataUrlBytes.");
            if (code == SvgDiagnosticCode.InvalidReference) new SvgResourceResolver().CacheFailure(options, result);
            return result;
        }
        if (new SvgResourceResolver().Cancelled(request, options))
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.Cancelled, "Resource acquisition was cancelled.");
            return result;
        }
        result.Bytes = parsed.Bytes;
        result.DeclaredMediaType = (parsed.MediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText();
        result.EffectiveMediaType = result.DeclaredMediaType;
        result.Cacheable = true;
        result.IsDataUrl = true;
        result.IsIndependentSnapshot = true;
        result.Success = true;
        result.LoadedCount = 1;
        result.ResourceSource = new SvgResourceResolver().CreateResourceSource(result.FinalUri, request);
        if (options.Cache != null) options.Cache.Add(result.CacheKey, result);
        return result;
    }
byte[]? ReadArray(byte[] source, SvgResourceAcquisitionOptions options, SvgResourceAcquisitionResult result)
    {
        long maximum = new SvgResourceResolver().ResourceMaximum(options, result);
        if (source.LongLength > maximum)
        {
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The resource bytes exceed the configured limit.");
            return null;
        }
        if (!options.CopyInputData) return source;
        byte[] copy = new byte[source.Length];
        System.Array.Copy(source, 0, copy, 0, source.Length);
        return copy;
    }
SvgResourceAcquisitionResult ResolveCached(string requestedMediaType, SvgResourceAcquisitionOptions options, SvgResourceAcquisitionResult result, SvgResourceCacheEntry? entry)
    {
        result.CacheHit = true;
        result.FailureCacheHit = !(entry ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Success;
        result.FinalUri = entry.FinalUri;
        result.DeclaredMediaType = entry.DeclaredMediaType;
        result.ActualMediaType = entry.ActualMediaType;
        result.RedirectCount = entry.RedirectCount;
        result.Cacheable = entry.Cacheable;
        result.IsDataUrl = new SvgResourceResolver().Scheme(result.NormalizedUri) == "data";
        new SvgResourceResolver().AppendCachedDiagnostics(result, entry);
        if (!entry.Success) return result;
        if (new SvgResourceResolver().ReferenceContains(options.ReferenceStack, entry.FinalKey, options.Limits.MaxUriCharacters))
        {
            result.FailureCacheHit = false;
            new SvgResourceResolver().AddCycleError(result, result.FinalUri);
            return result;
        }
        long maximum = new SvgResourceResolver().CachedMaximum(options, result);
        if (entry.Bytes.LongLength > maximum)
        {
            result.FailureCacheHit = false;
            new SvgResourceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The cached resource bytes exceed the configured per-resource limit.");
            return result;
        }
        result.Bytes = SvgResourceCacheEntry.CopyBytes(entry.Bytes);
        result.EffectiveMediaType = result.ActualMediaType.Length != 0 ? result.ActualMediaType : result.DeclaredMediaType.Length != 0 ? result.DeclaredMediaType : requestedMediaType;
        result.IsIndependentSnapshot = true;
        result.ResourceSource = new SvgResourceResolver().CreateResourceSource(result.FinalUri, result.Request);
        result.ImageMetadata = new SvgResourceResolver().CopyImageMetadata(entry.ImageMetadata);
        result.Success = true;
        return result;
    }
SvgImageMetadata? CopyImageMetadata(SvgImageMetadata? source)
    {
        if (source == null) return null;
        SvgImageMetadata result = new SvgImageMetadata();
        result.DecodedContentAvailable = source.DecodedContentAvailable;
        if (source.IntrinsicSize != null)
        {
            result.IntrinsicSize = new SvgIntrinsicSize();
            result.IntrinsicSize.HasWidth = source.IntrinsicSize.HasWidth;
            result.IntrinsicSize.Width = source.IntrinsicSize.Width;
            result.IntrinsicSize.HasHeight = source.IntrinsicSize.HasHeight;
            result.IntrinsicSize.Height = source.IntrinsicSize.Height;
            result.IntrinsicSize.HasAspectRatio = source.IntrinsicSize.HasAspectRatio;
            result.IntrinsicSize.AspectRatio = source.IntrinsicSize.AspectRatio;
        }
        return result;
    }
long CachedMaximum(SvgResourceAcquisitionOptions options, SvgResourceAcquisitionResult result)
    {
        long maximum = options.MaxBytes;
        if (maximum > options.Limits.MaxBytesPerResource) maximum = options.Limits.MaxBytesPerResource;
        if (new SvgResourceResolver().Scheme(result.NormalizedUri) == "data" && maximum > options.Limits.MaxDataUrlBytes) maximum = options.Limits.MaxDataUrlBytes;
        return maximum;
    }
void CacheFailure(SvgResourceAcquisitionOptions options, SvgResourceAcquisitionResult result)
    {
        if (options.CacheFailures && options.Cache != null) options.Cache.Add(result.CacheKey, result);
    }
bool ReferenceContains(System.Collections.Generic.List<string> references, string cacheKey, int maxCharacters)
    {
        int index = 0;
        while (index < references.Count)
        {
            SvgUriResult reference = new SvgUri().NormalizeWithValueAndMaxCharacters(references[index], maxCharacters);
            if (reference.Success && reference.CacheKey == cacheKey) return true;
            index = index + 1;
        }
        return false;
    }
SvgSourceUri CreateResourceSource(string finalUri, SvgResourceRequest request)
    {
        if (request.Source == null || request.Source.Source == null) return new SvgSourceUri(finalUri);
        return SvgSourceUri.CreateNested(finalUri, request.Source.Source, request.Source, request.ResourceIndex);
    }
void AppendCachedDiagnostics(SvgResourceAcquisitionResult result, SvgResourceCacheEntry entry)
    {
        int index = 0;
        while (index < entry.Diagnostics.Count)
        {
            SvgDiagnostic diagnostic = entry.Diagnostics[index];
            SvgDiagnostic copied;
            if (diagnostic.Source == null || object.ReferenceEquals(diagnostic.Source, entry.RequestSource)) copied = new SvgResourceResolver().CopyDiagnostic(diagnostic, result.Request.Source);
            else copied = SvgResourceCacheEntry.CopyDiagnostic(diagnostic);
            result.Diagnostics.Add(copied);
            index = index + 1;
        }
    }
SvgDiagnostic CopyDiagnostic(SvgDiagnostic source, SvgSourceSpan? location)
    {
        SvgDiagnostic copy = new SvgDiagnostic(source.Severity, source.Code, source.Message, location);
        copy.InstancePath = source.InstancePath;
        copy.Sequence = source.Sequence;
        int index = 0;
        while (index < source.RelatedSpans.Count)
        {
            SvgSourceSpan? related = object.ReferenceEquals(source.RelatedSpans[index], source.Source) ? location : source.RelatedSpans[index];
            if (related != null) copy.AddRelatedSpan(related);
            index = index + 1;
        }
        return copy;
    }
void AddCycleError(SvgResourceAcquisitionResult result, string uri)
    {
        SvgDiagnostic diagnostic = new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.ReferenceCycle, "A resource reference cycle was detected for " + uri + ".", result.Request.Source);
        if (result.Request.Source != null) diagnostic.AddRelatedSpan(result.Request.Source);
        result.Diagnostics.Add(diagnostic);
    }
byte[]? ReadStream(System.IO.Stream? stream, SvgResourceRequest request, SvgResourceAcquisitionOptions options, SvgResourceAcquisitionResult result)
    {
        long maximum = new SvgResourceResolver().ResourceMaximum(options, result);
        if (maximum > 2147483647) maximum = 2147483647;
        using (System.IO.MemoryStream collected = new System.IO.MemoryStream())
        {
            byte[] buffer = new byte[8192];
            while (true)
            {
                if (new SvgResourceResolver().Cancelled(request, options))
                {
                    new SvgResourceResolver().AddError(result, SvgDiagnosticCode.Cancelled, "Resource acquisition was cancelled.");
                    return null;
                }
                int read = (stream ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Read(buffer, 0, buffer.Length);
                if (read == 0) break;
                if (collected.Length > maximum - read)
                {
                    new SvgResourceResolver().AddError(result, SvgDiagnosticCode.InputLimitExceeded, "The resource stream exceeds the configured limit.");
                    return null;
                }
                collected.Write(buffer, 0, read);
            }
            return collected.ToArray();
        }
    }
long EffectiveMaximum(SvgResourceAcquisitionOptions options)
    {
        long maximum = options.MaxBytes;
        if (maximum > options.Limits.MaxBytesPerResource) maximum = options.Limits.MaxBytesPerResource;
        long remaining = options.Limits.MaxTotalBytes - options.AlreadyLoadedBytes;
        if (maximum > remaining) maximum = remaining;
        return maximum;
    }
long ResourceMaximum(SvgResourceAcquisitionOptions options, SvgResourceAcquisitionResult result)
    {
        long maximum = new SvgResourceResolver().EffectiveMaximum(options);
        if (new SvgResourceResolver().Scheme(result.NormalizedUri) == "data" && maximum > options.Limits.MaxDataUrlBytes) maximum = options.Limits.MaxDataUrlBytes;
        return maximum;
    }
string NormalizeMediaType(string value)
    {
        if (value.Length == 0) return "";
        SvgMediaTypeParseResult parsed = new SvgMediaTypeParser().Parse(value, 64, 8192);
        return parsed.Success ? (parsed.MediaType ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).ToText() : "";
    }
string Scheme(string value)
    {
        int colon = value.IndexOf(':');
        if (colon <= 0) return "";
        return value.Substring(0, colon).ToLowerInvariant();
    }
bool Valid(SvgResourceRequest? request, SvgResourceAcquisitionOptions? options)
    {
        if (request == null || options == null || options.Limits == null) return false;
        if (request.SourceUri == null || request.SourceUri.Length == 0 || request.NormalizedUri == null || request.NormalizedUri.Length == 0 || request.MediaKind == null || request.MediaKind.Length == 0 || request.BaseUri == null || request.RedirectCount < 0 || request.ReferenceDepth < 0 || request.ResourceIndex < 0) return false;
        if ((int)request.ResourceKind < 0 || (int)request.ResourceKind > 5 || options.MaxBytes < 0 || options.AlreadyLoadedBytes < 0 || options.AlreadyLoadedBytes > options.Limits.MaxTotalBytes || options.ReferenceStack == null || options.AlreadyLoadedCount < 0 || options.AlreadyLoadedCount > options.Limits.MaxCount) return false;
        int referenceIndex = 0;
        while (referenceIndex < options.ReferenceStack.Count)
        {
            if (options.ReferenceStack[referenceIndex] == null) return false;
            referenceIndex = referenceIndex + 1;
        }
        if (options.Limits.MaxCount < 1 || options.Limits.MaxBytesPerResource < 1 || options.Limits.MaxTotalBytes < 1 || options.Limits.MaxDataUrlBytes < 1 || options.Limits.MaxRedirects < 1 || options.Limits.MaxUriCharacters < 1 || options.Limits.MaxCallbackMilliseconds < 1) return false;
        return true;
    }
bool Cancelled(SvgResourceRequest request, SvgResourceAcquisitionOptions options)
    {
        return request.CancellationToken.IsCancellationRequested || options.CancellationToken.IsCancellationRequested;
    }
void AppendDiagnostics(SvgResourceAcquisitionResult result, System.Collections.Generic.List<SvgDiagnostic>? diagnostics)
    {
        if (diagnostics == null) return;
        int index = 0;
        while (index < diagnostics.Count)
        {
            SvgDiagnostic diagnostic = diagnostics[index];
            if (diagnostic != null)
            {
                if (diagnostic.Source == null)
                {
                    SvgDiagnostic located = new SvgDiagnostic(diagnostic.Severity, diagnostic.Code, diagnostic.Message, result.Request.Source);
                    located.InstancePath = diagnostic.InstancePath;
                    result.Diagnostics.Add(located);
                }
                else result.Diagnostics.Add(diagnostic);
            }
            index = index + 1;
        }
    }
void AddError(SvgResourceAcquisitionResult result, SvgDiagnosticCode code, string message)
    {
        result.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, code, message, result.Request == null ? null : result.Request.Source));
    }
}
