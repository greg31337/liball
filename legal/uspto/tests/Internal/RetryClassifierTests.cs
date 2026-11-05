using System.Net;
using System.Net.Sockets;
using uspto;

namespace uspto.Tests;

public class RetryClassifierTests
{
    [Test]
    public void GetRequestsAreReplaySafe()
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        bool result = RetryClassifier.IsRequestReplaySafe(request, false);

        Assert.That(result, Is.True);
    }

    [Test]
    public void SearchPostRequestsAreReplaySafe()
    {
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/search");
        request.Content = new StringContent("{}");

        bool result = RetryClassifier.IsRequestReplaySafe(request, true);

        Assert.That(result, Is.True);
    }

    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("DELETE")]
    [TestCase("PATCH")]
    [TestCase("HEAD")]
    public void NonSearchOrMutatingRequestsAreNotReplaySafe(string method)
    {
        using HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(method), "https://example.test/resource");

        bool result = RetryClassifier.IsRequestReplaySafe(request, false);

        Assert.That(result, Is.False);
    }

    [TestCase(408, true)]
    [TestCase(429, true)]
    [TestCase(500, true)]
    [TestCase(502, true)]
    [TestCase(503, true)]
    [TestCase(504, true)]
    [TestCase(400, false)]
    [TestCase(401, false)]
    [TestCase(404, false)]
    [TestCase(409, false)]
    [TestCase(422, false)]
    [TestCase(501, false)]
    [TestCase(505, false)]
    public void HttpStatusClassificationSelectsOnlyTransientResponses(int statusCode, bool expected)
    {
        bool result = RetryClassifier.IsTransientStatusCode(statusCode);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(408)]
    [TestCase(429)]
    [TestCase(500)]
    [TestCase(502)]
    [TestCase(503)]
    [TestCase(504)]
    public void TransientApiStatusesAreRetryableWhenTheMapperMarksThemTransient(int statusCode)
    {
        ApiException failure = new ApiException("remote failure", ApiService.Odp, statusCode, true, "REMOTE_ERROR", null, null, true, null);

        bool result = RetryClassifier.IsTransientFailure(failure, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public void GenericApiExceptionRetryHintIsNotOverriddenByItsStatus()
    {
        ApiException failure = new ApiException("permanent failure", ApiService.Odp, 503, true, "PERMANENT", null, null, false, null);

        bool result = RetryClassifier.IsTransientFailure(failure, CancellationToken.None);

        Assert.That(result, Is.False);
    }

    [Test]
    public void RateLimitResponseIsRetryableButLocalQueueRejectionIsNot()
    {
        RateLimitException remote = new RateLimitException("remote limit", ApiService.Tsdr, 429, true, null, 0L, false, null);
        RateLimitException local = new RateLimitException("local queue", ApiService.Tsdr, 0, false, null, 0L, false, null);

        Assert.Multiple(() =>
        {
            Assert.That(RetryClassifier.IsTransientFailure(remote, CancellationToken.None), Is.True);
            Assert.That(RetryClassifier.IsTransientFailure(local, CancellationToken.None), Is.False);
        });
    }

    [TestCase(500, true)]
    [TestCase(502, true)]
    [TestCase(503, true)]
    [TestCase(504, true)]
    [TestCase(501, false)]
    public void ServiceUnavailableRequiresAnEligibleStatus(int statusCode, bool expected)
    {
        ServiceUnavailableException failure = new ServiceUnavailableException("unavailable", ApiService.PatentsView, statusCode, true, null, 0L, false, null);

        bool result = RetryClassifier.IsTransientFailure(failure, CancellationToken.None);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ExplicitApiRetryHintIsHonoredForOtherTransientResponses()
    {
        ApiException failure = new ApiException("too early", ApiService.Odp, 425, true, "TOO_EARLY", null, null, true, null);

        bool result = RetryClassifier.IsTransientFailure(failure, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [Test]
    public void NetworkTransportFailuresAreRetryable()
    {
        Exception[] failures = new Exception[]
        {
            new HttpRequestException("connection interrupted"),
            new IOException("response stream interrupted"),
            new SocketException((int)SocketError.ConnectionReset)
        };

        Assert.That(failures, Is.All.Matches<Exception>(failure => RetryClassifier.IsTransientFailure(failure, CancellationToken.None)));
    }

    [Test]
    public void HttpRequestExceptionWithPermanentStatusIsNotRetryable()
    {
        HttpRequestException failure = new HttpRequestException("bad request", null, HttpStatusCode.BadRequest);

        bool result = RetryClassifier.IsTransientFailure(failure, CancellationToken.None);

        Assert.That(result, Is.False);
    }

    [Test]
    public void HttpRequestExceptionWithTransientStatusIsRetryable()
    {
        HttpRequestException failure = new HttpRequestException("gateway unavailable", null, HttpStatusCode.BadGateway);

        bool result = RetryClassifier.IsTransientFailure(failure, CancellationToken.None);

        Assert.That(result, Is.True);
    }

    [TestCase("CONNECT_TIMEOUT")]
    [TestCase("HEADER_TIMEOUT")]
    [TestCase("BODY_TIMEOUT")]
    [TestCase("OVERALL_TIMEOUT")]
    public void ConfiguredTimeoutAndDeadlineFailuresAreNeverRetried(string errorCode)
    {
        ApiException failure = new ApiException("deadline", ApiService.Odp, 0, false, errorCode, null, null, true, null);

        bool result = RetryClassifier.IsTransientFailure(failure, CancellationToken.None);

        Assert.That(result, Is.False);
    }

    [Test]
    public void OperationCancellationIsNeverRetried()
    {
        using CancellationTokenSource caller = new CancellationTokenSource();
        caller.Cancel();
        OperationCanceledException failure = new OperationCanceledException(caller.Token);

        bool result = RetryClassifier.IsTransientFailure(failure, caller.Token);

        Assert.That(result, Is.False);
    }

    [Test]
    public void CallerCancellationOverridesAnOtherwiseTransientFailure()
    {
        using CancellationTokenSource caller = new CancellationTokenSource();
        caller.Cancel();

        bool result = RetryClassifier.IsTransientFailure(new HttpRequestException("connection interrupted"), caller.Token);

        Assert.That(result, Is.False);
    }

    [Test]
    public void SafeRequestAndTransientFailureMustBothBePresent()
    {
        using HttpRequestMessage safeGet = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
        using HttpRequestMessage unsafePost = new HttpRequestMessage(HttpMethod.Post, "https://example.test/mutate");
        HttpRequestException transient = new HttpRequestException("connection interrupted");
        InvalidOperationException permanent = new InvalidOperationException("invalid state");

        Assert.Multiple(() =>
        {
            Assert.That(RetryClassifier.ShouldRetry(safeGet, false, transient, CancellationToken.None), Is.True);
            Assert.That(RetryClassifier.ShouldRetry(safeGet, false, permanent, CancellationToken.None), Is.False);
            Assert.That(RetryClassifier.ShouldRetry(unsafePost, false, transient, CancellationToken.None), Is.False);
            Assert.That(RetryClassifier.ShouldRetry(unsafePost, true, transient, CancellationToken.None), Is.True);
        });
    }
}
