namespace svg.Tests;

public class SvgResourceResolverTests
{
    [Test]
    public void CallbackReceivesNormalizedProvenanceAndActualMediaMetadataWins()
    {
        byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes("<svg/>");
        SvgSourceSpan source = SvgSourceSpan.Unknown(new SvgSourceUri("memory:parent"));
        SvgResourceRequest request = new SvgResourceRequest("images/../root.svg", SvgMimeTypes.Svg, System.Threading.CancellationToken.None);
        request.BaseUri = "https://example.test/art/";
        request.ResourceKind = SvgResourceKind.Document;
        request.ReferenceDepth = 2;
        request.Source = source;
        SvgResourceRequest observed = null;
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.ResourceProvider = new SvgResourceProvider(value =>
        {
            observed = value;
            SvgResourceResponse response = new SvgResourceResponse();
            response.Bytes = responseBytes;
            response.DeclaredMediaType = "application/xml";
            response.ActualMediaType = "image/svg+xml; profile=full";
            response.FinalUri = "../cdn/root.svg";
            response.RedirectCount = 2;
            response.Cacheable = true;
            response.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Info, SvgDiagnosticCode.None, "caller metadata", null));
            return response;
        });

        SvgResourceAcquisitionResult result = new SvgResourceResolver().Resolve(request, options);
        responseBytes[0] = (byte)'!';

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(observed, Is.SameAs(request));
            Assert.That(observed.SourceUri, Is.EqualTo("images/../root.svg"));
            Assert.That(observed.NormalizedUri, Is.EqualTo("https://example.test/art/root.svg"));
            Assert.That(observed.BaseUri, Is.EqualTo("https://example.test/art/"));
            Assert.That(observed.ResourceKind, Is.EqualTo(SvgResourceKind.Document));
            Assert.That(observed.ReferenceDepth, Is.EqualTo(2));
            Assert.That(result.FinalUri, Is.EqualTo("https://example.test/cdn/root.svg"));
            Assert.That(result.DeclaredMediaType, Is.EqualTo("application/xml"));
            Assert.That(result.ActualMediaType, Is.EqualTo("image/svg+xml; profile=full"));
            Assert.That(result.EffectiveMediaType, Is.EqualTo(result.ActualMediaType));
            Assert.That(result.RedirectCount, Is.EqualTo(2));
            Assert.That(result.Cacheable, Is.True);
            Assert.That(result.Bytes[0], Is.EqualTo((byte)'<'));
            Assert.That(result.IsIndependentSnapshot, Is.True);
            Assert.That(result.RequestCount, Is.EqualTo(1));
            Assert.That(result.LoadedCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Single().Source, Is.SameAs(source));
        });
    }

    [Test]
    public void ExplicitDataUrlDecodingDoesNotInvokeTheCallerCallback()
    {
        bool invoked = false;
        SvgResourceRequest request = new SvgResourceRequest("data:text/plain;charset=utf-8,hello%20world", "text/plain", System.Threading.CancellationToken.None);
        request.ResourceKind = SvgResourceKind.Other;
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.DecodeDataUrls = true;
        options.ResourceProvider = new SvgResourceProvider(value =>
        {
            invoked = true;
            return null;
        });

        SvgResourceAcquisitionResult result = new SvgResourceResolver().Resolve(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(invoked, Is.False);
            Assert.That(result.ProviderInvoked, Is.False);
            Assert.That(result.IsDataUrl, Is.True);
            Assert.That(result.IsIndependentSnapshot, Is.True);
            Assert.That(result.DeclaredMediaType, Is.EqualTo("text/plain; charset=utf-8"));
            Assert.That(System.Text.Encoding.UTF8.GetString(result.Bytes), Is.EqualTo("hello world"));
            Assert.That(result.RequestCount, Is.Zero);
            Assert.That(result.LoadedCount, Is.EqualTo(1));
        });
    }

    [TestCase("file:///definitely/not/read/root.svg")]
    [TestCase("https://example.invalid/never-requested.svg")]
    public void UriResourcesWithoutCallbacksNeverPerformImplicitIo(string uri)
    {
        SvgResourceRequest request = new SvgResourceRequest(uri, SvgMimeTypes.Svg, System.Threading.CancellationToken.None);
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.DecodeDataUrls = false;

        SvgResourceAcquisitionResult result = new SvgResourceResolver().Resolve(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ProviderInvoked, Is.False);
            Assert.That(result.RequestCount, Is.Zero);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.CallbackRequired));
        });
    }

    [Test]
    public void CancellationIsObservedBeforeAndImmediatelyAfterTheCallback()
    {
        System.Threading.CancellationTokenSource beforeSource = new System.Threading.CancellationTokenSource();
        beforeSource.Cancel();
        bool invokedBefore = false;
        SvgResourceAcquisitionOptions beforeOptions = new SvgResourceAcquisitionOptions();
        beforeOptions.ResourceProvider = new SvgResourceProvider(value =>
        {
            invokedBefore = true;
            return ByteResponse("before");
        });
        SvgResourceAcquisitionResult before = new SvgResourceResolver().Resolve(new SvgResourceRequest("memory:before", "text/plain", beforeSource.Token), beforeOptions);

        System.Threading.CancellationTokenSource afterSource = new System.Threading.CancellationTokenSource();
        SvgResourceAcquisitionOptions afterOptions = new SvgResourceAcquisitionOptions();
        afterOptions.CancellationToken = afterSource.Token;
        afterOptions.ResourceProvider = new SvgResourceProvider(value =>
        {
            afterSource.Cancel();
            return ByteResponse("after");
        });
        SvgResourceAcquisitionResult after = new SvgResourceResolver().Resolve(new SvgResourceRequest("memory:after", "text/plain", System.Threading.CancellationToken.None), afterOptions);

        Assert.Multiple(() =>
        {
            Assert.That(before.Success, Is.False);
            Assert.That(invokedBefore, Is.False);
            Assert.That(before.RequestCount, Is.Zero);
            Assert.That(before.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
            Assert.That(after.Success, Is.False);
            Assert.That(after.ProviderInvoked, Is.True);
            Assert.That(after.RequestCount, Is.EqualTo(1));
            Assert.That(after.LoadedCount, Is.Zero);
            Assert.That(after.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
        });
    }

    [Test]
    public void StreamsRemainCallerOwnedAndAreCancelledDuringSynchronousReads()
    {
        System.Threading.CancellationTokenSource cancellation = new System.Threading.CancellationTokenSource();
        CancelAfterReadStream stream = new CancelAfterReadStream(System.Text.Encoding.UTF8.GetBytes("streamed data"), cancellation);
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.CancellationToken = cancellation.Token;
        options.ResourceProvider = new SvgResourceProvider(value =>
        {
            SvgResourceResponse response = new SvgResourceResponse();
            response.Stream = stream;
            return response;
        });

        SvgResourceAcquisitionResult result = new SvgResourceResolver().Resolve(new SvgResourceRequest("memory:stream", "text/plain", System.Threading.CancellationToken.None), options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.Cancelled));
            Assert.That(stream.CanRead, Is.True);
            Assert.That(result.Bytes, Is.Empty);
        });

        stream.Dispose();
        cancellation.Dispose();
    }

    [Test]
    public void ByteOwnershipAndConfiguredBoundsAreExplicit()
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("content");
        SvgResourceAcquisitionOptions retainedOptions = new SvgResourceAcquisitionOptions();
        retainedOptions.CopyInputData = false;
        retainedOptions.ResourceProvider = new SvgResourceProvider(value => ByteResponse(bytes));
        SvgResourceAcquisitionResult retained = new SvgResourceResolver().Resolve(new SvgResourceRequest("memory:retained", "text/plain", System.Threading.CancellationToken.None), retainedOptions);

        SvgResourceAcquisitionOptions boundedOptions = new SvgResourceAcquisitionOptions();
        boundedOptions.MaxBytes = bytes.Length - 1;
        boundedOptions.ResourceProvider = new SvgResourceProvider(value => ByteResponse(bytes));
        SvgResourceAcquisitionResult bounded = new SvgResourceResolver().Resolve(new SvgResourceRequest("memory:bounded", "text/plain", System.Threading.CancellationToken.None), boundedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(retained.Success, Is.True);
            Assert.That(retained.Bytes, Is.SameAs(bytes));
            Assert.That(retained.IsIndependentSnapshot, Is.False);
            Assert.That(bounded.Success, Is.False);
            Assert.That(bounded.Bytes, Is.Empty);
            Assert.That(bounded.Diagnostics.Single().Code, Is.EqualTo(SvgDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void MalformedResponseMetadataFailsAfterLocatedCallerDiagnostics()
    {
        SvgSourceSpan source = SvgSourceSpan.Unknown(new SvgSourceUri("memory:requester"));
        SvgResourceRequest request = new SvgResourceRequest("memory:malformed", SvgMimeTypes.Svg, System.Threading.CancellationToken.None);
        request.Source = source;
        SvgResourceAcquisitionOptions options = new SvgResourceAcquisitionOptions();
        options.ResourceProvider = new SvgResourceProvider(value =>
        {
            SvgResourceResponse response = ByteResponse("content");
            response.ActualMediaType = "not a media type";
            response.Diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Warning, SvgDiagnosticCode.CompatibilityRecovery, "caller warning", null));
            return response;
        });

        SvgResourceAcquisitionResult result = new SvgResourceResolver().Resolve(request, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(value => value.Code), Is.EqualTo(new[]
            {
                SvgDiagnosticCode.CompatibilityRecovery,
                SvgDiagnosticCode.ResourceFailed
            }));
            Assert.That(result.Diagnostics.All(value => value.Source == source), Is.True);
            Assert.That(result.LoadedCount, Is.Zero);
        });
    }

    static SvgResourceResponse ByteResponse(string value)
    {
        return ByteResponse(System.Text.Encoding.UTF8.GetBytes(value));
    }

    static SvgResourceResponse ByteResponse(byte[] value)
    {
        SvgResourceResponse response = new SvgResourceResponse();
        response.Bytes = value;
        return response;
    }
}

internal class CancelAfterReadStream : System.IO.MemoryStream
{
    readonly System.Threading.CancellationTokenSource Cancellation;

    internal CancelAfterReadStream(byte[] bytes, System.Threading.CancellationTokenSource cancellation)
    {
        Cancellation = cancellation;
        Write(bytes, 0, bytes.Length);
        Position = 0;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int read = base.Read(buffer, offset, count);
        if (read > 0) Cancellation.Cancel();
        return read;
    }
}
