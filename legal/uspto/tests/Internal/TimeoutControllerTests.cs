using System.Net;
using System.Text;
using uspto;

namespace uspto.Tests;

public class TimeoutControllerTests
{
    [Test]
    public void ConnectTimeoutIsAppliedToSocketsHandler()
    {
        UsptoClientOptions options = CreateOptions(25, 1000, 1000, 1000);
        HttpTimeoutScope scope = new HttpTimeoutScope(options, new RequestContext(), false);
        using SocketsHttpHandler handler = new SocketsHttpHandler();

        TimeoutController.ApplyConnectTimeout(handler, scope);

        Assert.That(handler.ConnectTimeout, Is.EqualTo(TimeSpan.FromMilliseconds(25)));
    }

    [Test]
    public void ConnectPhaseHasItsOwnNonRetryableTimeout()
    {
        UsptoClientOptions options = CreateOptions(20, 1000, 1000, 1000);
        HttpTimeoutScope scope = new HttpTimeoutScope(options, new RequestContext { Service = ApiService.Odp }, false);

        ApiException exception = Assert.Throws<ApiException>(() => TimeoutController.RunConnectPhase(
            token => Task.Delay(Timeout.Infinite, token),
            scope));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("CONNECT_TIMEOUT"));
            Assert.That(exception.IsRetryable, Is.False);
            Assert.That(exception.HasStatusCode, Is.False);
        });
    }

    [Test]
    public void HeaderPhaseTimesOutWhileWaitingForResponseHeaders()
    {
        UsptoClientOptions options = CreateOptions(1000, 20, 1000, 1000);
        HttpTimeoutScope scope = new HttpTimeoutScope(options, new RequestContext { Service = ApiService.PatentsView }, false);
        using HttpClient client = new HttpClient(new DelayedHttpMessageHandler(Timeout.Infinite));
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        ApiException exception = Assert.Throws<ApiException>(() => TimeoutController.SendHeaderPhase(client, request, scope));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("HEADER_TIMEOUT"));
            Assert.That(exception.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(exception.IsRetryable, Is.False);
        });
    }

    [Test]
    public void HeaderPhaseReturnsAsSoonAsHeadersAreAvailable()
    {
        UsptoClientOptions options = CreateOptions(1000, 1000, 1000, 1000);
        HttpTimeoutScope scope = new HttpTimeoutScope(options, new RequestContext(), false);
        using HttpClient client = new HttpClient(new DelayedHttpMessageHandler(1));
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        using HttpResponseMessage response = TimeoutController.SendHeaderPhase(client, request, scope);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public void CallerCancellationInterruptsTheHeaderPhaseAndRetainsItsToken()
    {
        using CancellationTokenSource caller = new CancellationTokenSource();
        RequestContext context = new RequestContext
        {
            Service = ApiService.Odp,
            CancellationToken = caller.Token
        };
        HttpTimeoutScope scope = new HttpTimeoutScope(CreateOptions(1000, 1000, 1000, 1000), context, false);
        using HttpClient client = new HttpClient(new DelayedHttpMessageHandler(Timeout.Infinite));
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
        caller.CancelAfter(20);

        OperationCanceledException failure = Assert.Throws<OperationCanceledException>(() => TimeoutController.SendHeaderPhase(client, request, scope));

        Assert.That(failure.CancellationToken, Is.EqualTo(caller.Token));
    }

    [Test]
    public void BodyPhaseTimesOutIndependentlyOfHeaderCompletion()
    {
        UsptoClientOptions options = CreateOptions(1000, 1000, 20, 1000);
        RequestContext context = new RequestContext { Service = ApiService.Tsdr };
        HttpTimeoutScope scope = new HttpTimeoutScope(options, context, false);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new DelayedBodyContent(Timeout.Infinite, UsptoMediaTypes.Json);

        ApiException exception = Assert.Throws<ApiException>(() => TimeoutController.ReadTextBodyPhase(
            response,
            DocumentFormat.Json,
            context,
            new UsptoLimits(),
            null,
            scope));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("BODY_TIMEOUT"));
            Assert.That(exception.Service, Is.EqualTo(ApiService.Tsdr));
            Assert.That(exception.IsRetryable, Is.False);
        });
    }

    [Test]
    public void CallerCancellationInterruptsTheBodyPhaseAndRetainsItsToken()
    {
        using CancellationTokenSource caller = new CancellationTokenSource();
        RequestContext context = new RequestContext
        {
            Service = ApiService.Tsdr,
            CancellationToken = caller.Token
        };
        HttpTimeoutScope scope = new HttpTimeoutScope(CreateOptions(1000, 1000, 1000, 1000), context, false);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new DelayedBodyContent(Timeout.Infinite, UsptoMediaTypes.Json);
        caller.CancelAfter(20);

        OperationCanceledException failure = Assert.Throws<OperationCanceledException>(() => TimeoutController.ReadTextBodyPhase(
            response,
            DocumentFormat.Json,
            context,
            new UsptoLimits(),
            null,
            scope));

        Assert.That(failure.CancellationToken, Is.EqualTo(caller.Token));
    }

    [Test]
    public void OverallDeadlineWinsWhenItExpiresBeforeThePhaseTimeout()
    {
        UsptoClientOptions options = CreateOptions(1000, 1000, 1000, 15);
        HttpTimeoutScope scope = new HttpTimeoutScope(options, new RequestContext { Service = ApiService.OfficeAction }, false);

        ApiException exception = Assert.Throws<ApiException>(() => TimeoutController.RunConnectPhase(
            token => Task.Delay(Timeout.Infinite, token),
            scope));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("OVERALL_TIMEOUT"));
            Assert.That(exception.Service, Is.EqualTo(ApiService.OfficeAction));
            Assert.That(exception.IsRetryable, Is.False);
        });
    }

    [Test]
    public void CallerCancellationWinsAndRetainsTheCallerToken()
    {
        using CancellationTokenSource caller = new CancellationTokenSource();
        caller.Cancel();
        RequestContext context = new RequestContext
        {
            Service = ApiService.Odp,
            CancellationToken = caller.Token
        };
        HttpTimeoutScope scope = new HttpTimeoutScope(CreateOptions(1000, 1000, 1000, 1000), context, false);
        bool invoked = false;

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() => TimeoutController.RunConnectPhase(
            token =>
            {
                invoked = true;
                return Task.CompletedTask;
            },
            scope));

        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(caller.Token));
            Assert.That(invoked, Is.False);
        });
    }

    [Test]
    public void CallerCancellationInterruptsAnActivePhaseAndRetainsTheCallerToken()
    {
        using CancellationTokenSource caller = new CancellationTokenSource();
        RequestContext context = new RequestContext
        {
            Service = ApiService.Odp,
            CancellationToken = caller.Token
        };
        HttpTimeoutScope scope = new HttpTimeoutScope(CreateOptions(1000, 1000, 1000, 1000), context, false);
        caller.CancelAfter(20);

        OperationCanceledException exception = Assert.Throws<OperationCanceledException>(() => TimeoutController.RunConnectPhase(
            token => Task.Delay(Timeout.Infinite, token),
            scope));

        Assert.That(exception.CancellationToken, Is.EqualTo(caller.Token));
    }

    [Test]
    public void ExplicitRequestDeadlineOverridesTheConfiguredOverallTimeout()
    {
        RequestContext context = new RequestContext
        {
            Service = ApiService.Odp,
            HasDeadline = true,
            Deadline = DateTimeOffset.UtcNow.AddMilliseconds(-1)
        };
        HttpTimeoutScope scope = new HttpTimeoutScope(CreateOptions(1000, 1000, 1000, 1000), context, false);

        ApiException exception = Assert.Throws<ApiException>(() => TimeoutController.RunConnectPhase(
            token => Task.CompletedTask,
            scope));

        Assert.That(exception.ErrorCode, Is.EqualTo("OVERALL_TIMEOUT"));
    }

    [Test]
    public void BinaryBodyTimeoutCancelsTheReadWithoutClosingTheDestination()
    {
        UsptoClientOptions options = CreateOptions(1000, 1000, 20, 1000);
        options.OverallTimeoutMilliseconds = 5;
        RequestContext context = new RequestContext { Service = ApiService.TsdrPdfAndZip };
        HttpTimeoutScope scope = new HttpTimeoutScope(options, context, true);
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new DelayedBodyContent(Timeout.Infinite, UsptoMediaTypes.Zip);
        using MemoryStream destination = new MemoryStream();

        ApiException exception = Assert.Throws<ApiException>(() => TimeoutController.CopyBinaryBodyPhase(
            response,
            destination,
            16,
            context,
            null,
            scope));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("BODY_TIMEOUT"));
            Assert.That(destination.CanWrite, Is.True);
        });
    }

    [TestCase("connect")]
    [TestCase("headers")]
    [TestCase("body")]
    [TestCase("overall")]
    public void InvalidTimeoutsAreRejected(string invalidField)
    {
        UsptoClientOptions options = CreateOptions(100, 100, 100, 100);
        if (invalidField == "connect")
        {
            options.ConnectTimeoutMilliseconds = 0;
        }
        else if (invalidField == "headers")
        {
            options.HeaderTimeoutMilliseconds = 0;
        }
        else if (invalidField == "body")
        {
            options.BodyTimeoutMilliseconds = 0;
        }
        else
        {
            options.OverallTimeoutMilliseconds = 0;
        }

        ValidationException exception = Assert.Throws<ValidationException>(() => new HttpTimeoutScope(options, new RequestContext(), false));

        Assert.That(exception.ErrorCode, Is.EqualTo("INVALID_TIMEOUT"));
    }

    [Test]
    public void InvalidDownloadOverallTimeoutIsRejectedForDownloads()
    {
        UsptoClientOptions options = CreateOptions(100, 100, 100, 100);
        options.Download.TimeoutMilliseconds = 0;

        ValidationException exception = Assert.Throws<ValidationException>(() => new HttpTimeoutScope(options, new RequestContext(), true));

        Assert.Multiple(() =>
        {
            Assert.That(exception.ErrorCode, Is.EqualTo("INVALID_TIMEOUT"));
            Assert.That(exception.FieldName, Is.EqualTo("Download.TimeoutMilliseconds"));
        });
    }

    private static UsptoClientOptions CreateOptions(int connect, int headers, int body, int overall)
    {
        return new UsptoClientOptions
        {
            ConnectTimeoutMilliseconds = connect,
            HeaderTimeoutMilliseconds = headers,
            BodyTimeoutMilliseconds = body,
            OverallTimeoutMilliseconds = overall,
            Download = new DownloadPolicy { TimeoutMilliseconds = overall }
        };
    }
}

internal class DelayedHttpMessageHandler : HttpMessageHandler
{
    internal int DelayMilliseconds;

    internal DelayedHttpMessageHandler(int delayMilliseconds)
    {
        DelayMilliseconds = delayMilliseconds;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await Task.Delay(DelayMilliseconds, cancellationToken);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, UsptoMediaTypes.Json),
            RequestMessage = request
        };
    }
}

internal class DelayedBodyContent : HttpContent
{
    internal int DelayMilliseconds;

    internal DelayedBodyContent(int delayMilliseconds, string mediaType)
    {
        DelayMilliseconds = delayMilliseconds;
        Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
    }

    protected override Task<Stream> CreateContentReadStreamAsync()
    {
        Stream stream = new DelayedReadStream(DelayMilliseconds);
        return Task.FromResult(stream);
    }

    protected override Stream CreateContentReadStream(CancellationToken cancellationToken)
    {
        return new DelayedReadStream(DelayMilliseconds);
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return Task.CompletedTask;
    }

    protected override bool TryComputeLength(out long length)
    {
        length = 0L;
        return false;
    }
}

internal class DelayedReadStream : Stream
{
    internal int DelayMilliseconds;

    internal DelayedReadStream(int delayMilliseconds)
    {
        DelayMilliseconds = delayMilliseconds;
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return 0;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await Task.Delay(DelayMilliseconds, cancellationToken);
        return 0;
    }
}
