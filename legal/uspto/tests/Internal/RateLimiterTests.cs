using System.Collections.Concurrent;
using System.Net;
using uspto;

namespace uspto.Tests;

public class RateLimiterTests
{
    [Test]
    public void DefaultOptionsEnforceTheDocumentedIndependentTsdrMinuteClasses()
    {
        UsptoClientOptions options = new UsptoClientOptions();
        RateLimiter limiter = new RateLimiter(options.RateLimits);
        for (int index = 0; index < UsptoDefaults.TsdrGeneralRequestsPerMinute; index++)
        {
            limiter.Acquire(ApiService.Tsdr, CancellationToken.None);
        }

        for (int index = 0; index < UsptoDefaults.TsdrPdfAndZipRequestsPerMinute; index++)
        {
            limiter.Acquire(ApiService.TsdrPdfAndZip, CancellationToken.None);
        }

        using CancellationTokenSource cancellation = new CancellationTokenSource();
        Task general = Task.Run(() => limiter.Acquire(ApiService.Tsdr, cancellation.Token));
        Task download = Task.Run(() => limiter.Acquire(ApiService.TsdrPdfAndZip, cancellation.Token));
        try
        {
            Assert.That(SpinWait.SpinUntil(() =>
                limiter.GetObservation(ApiService.Tsdr).WaitingCount == 1 &&
                limiter.GetObservation(ApiService.TsdrPdfAndZip).WaitingCount == 1, 1000), Is.True);

            RateLimitObservation generalObservation = limiter.GetObservation(ApiService.Tsdr);
            RateLimitObservation downloadObservation = limiter.GetObservation(ApiService.TsdrPdfAndZip);
            Assert.Multiple(() =>
            {
                Assert.That(generalObservation.ConfiguredLimit, Is.EqualTo(60));
                Assert.That(generalObservation.Used, Is.EqualTo(60));
                Assert.That(downloadObservation.ConfiguredLimit, Is.EqualTo(4));
                Assert.That(downloadObservation.Used, Is.EqualTo(4));
                Assert.That(general.IsCompleted, Is.False);
                Assert.That(download.IsCompleted, Is.False);
            });
        }
        finally
        {
            cancellation.Cancel();
            Assert.Throws<AggregateException>(() => Task.WaitAll(new[] { general, download }, 1000));
        }
    }

    [Test]
    public void TsdrGeneralAndPdfDownloadsUseIndependentBuckets()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[]
        {
            CreatePolicy(ApiService.Tsdr, 1, 1000),
            CreatePolicy(ApiService.TsdrPdfAndZip, 1, 1000)
        });
        limiter.Acquire(ApiService.Tsdr, CancellationToken.None);
        using CancellationTokenSource waitingCancellation = new CancellationTokenSource();
        Task waiting = Task.Run(() => limiter.Acquire(ApiService.Tsdr, waitingCancellation.Token));
        Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.Tsdr).WaitingCount == 1, 1000), Is.True);

        limiter.Acquire(ApiService.TsdrPdfAndZip, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(limiter.GetObservation(ApiService.Tsdr).Used, Is.EqualTo(1));
            Assert.That(limiter.GetObservation(ApiService.TsdrPdfAndZip).Used, Is.EqualTo(1));
            Assert.That(waiting.IsCompleted, Is.False);
        });
        waitingCancellation.Cancel();
        Assert.Throws<AggregateException>(() => waiting.Wait(1000));
    }

    [Test]
    public void QueuedCallersReceivePermitsInArrivalOrder()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.Tsdr, 1, 60000) });
        ConcurrentQueue<int> completionOrder = new ConcurrentQueue<int>();
        limiter.Acquire(ApiService.Tsdr, CancellationToken.None);

        Task first = Task.Run(() =>
        {
            limiter.Acquire(ApiService.Tsdr, CancellationToken.None);
            completionOrder.Enqueue(1);
        });
        Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.Tsdr).WaitingCount == 1, 1000), Is.True);
        Task second = Task.Run(() =>
        {
            limiter.Acquire(ApiService.Tsdr, CancellationToken.None);
            completionOrder.Enqueue(2);
        });
        Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.Tsdr).WaitingCount == 2, 1000), Is.True);
        Task third = Task.Run(() =>
        {
            limiter.Acquire(ApiService.Tsdr, CancellationToken.None);
            completionOrder.Enqueue(3);
        });
        Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.Tsdr).WaitingCount == 3, 1000), Is.True);

        ReleaseOneObservedPermit(limiter, ApiService.Tsdr);
        Assert.That(SpinWait.SpinUntil(() => completionOrder.Count == 1, 1000), Is.True);
        ReleaseOneObservedPermit(limiter, ApiService.Tsdr);
        Assert.That(SpinWait.SpinUntil(() => completionOrder.Count == 2, 1000), Is.True);
        ReleaseOneObservedPermit(limiter, ApiService.Tsdr);
        bool completed = Task.WaitAll(new Task[] { first, second, third }, 3000);

        Assert.Multiple(() =>
        {
            Assert.That(completed, Is.True);
            Assert.That(completionOrder.ToArray(), Is.EqualTo(new int[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void CancelingTheHeadCallerDoesNotStallTheNextCaller()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.Tsdr, 1, 60000) });
        limiter.Acquire(ApiService.Tsdr, CancellationToken.None);
        using CancellationTokenSource firstCancellation = new CancellationTokenSource();
        Task first = Task.Run(() => limiter.Acquire(ApiService.Tsdr, firstCancellation.Token));
        Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.Tsdr).WaitingCount == 1, 1000), Is.True);
        Task second = Task.Run(() => limiter.Acquire(ApiService.Tsdr, CancellationToken.None));
        Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.Tsdr).WaitingCount == 2, 1000), Is.True);

        firstCancellation.Cancel();
        Assert.Throws<AggregateException>(() => first.Wait(1000));
        ReleaseOneObservedPermit(limiter, ApiService.Tsdr);

        Assert.That(second.Wait(2000), Is.True);
    }

    [Test]
    public void QueueCancellationRetainsTheCallerToken()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.Tsdr, 1, 1000) });
        limiter.Acquire(ApiService.Tsdr, CancellationToken.None);
        using CancellationTokenSource caller = new CancellationTokenSource();
        Task<OperationCanceledException?> waiting = Task.Run(() =>
        {
            try
            {
                limiter.Acquire(ApiService.Tsdr, caller.Token);
            }
            catch (OperationCanceledException exception)
            {
                return exception;
            }

            return null;
        });
        Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.Tsdr).WaitingCount == 1, 1000), Is.True);

        caller.Cancel();
        OperationCanceledException? result = waiting.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.CancellationToken, Is.EqualTo(caller.Token));
            Assert.That(limiter.GetObservation(ApiService.Tsdr).WaitingCount, Is.Zero);
        });
    }

    [Test]
    public void FullQueueRejectsAdditionalCallersWithoutAnHttpStatus()
    {
        RateLimitPolicy policy = CreatePolicy(ApiService.PatentsView, 1, 1000);
        policy.HasQueueCapacity = true;
        policy.QueueCapacity = 1;
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { policy });
        limiter.Acquire(ApiService.PatentsView, CancellationToken.None);
        using CancellationTokenSource queuedCancellation = new CancellationTokenSource();
        Task queued = Task.Run(() => limiter.Acquire(ApiService.PatentsView, queuedCancellation.Token));
        Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.PatentsView).WaitingCount == 1, 1000), Is.True);

        RateLimitException exception = Assert.Throws<RateLimitException>(() => limiter.Acquire(ApiService.PatentsView, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(exception.Service, Is.EqualTo(ApiService.PatentsView));
            Assert.That(exception.HasStatusCode, Is.False);
        });
        queuedCancellation.Cancel();
        Assert.Throws<AggregateException>(() => queued.Wait(1000));
    }

    [Test]
    public void StandardServerHeadersAreObservedAndTightenTheLocalLimit()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.PatentsView, 10, 1000) });
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("RateLimit-Limit", "3;w=60");
        response.Headers.TryAddWithoutValidation("RateLimit-Remaining", "1");
        response.Headers.TryAddWithoutValidation("RateLimit-Reset", "2");
        DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(1);

        limiter.ObserveResponse(ApiService.PatentsView, response);
        RateLimitObservation observation = limiter.GetObservation(ApiService.PatentsView);

        Assert.Multiple(() =>
        {
            Assert.That(observation.ConfiguredLimit, Is.EqualTo(10));
            Assert.That(observation.EffectiveLimit, Is.EqualTo(3));
            Assert.That(observation.HasObservedLimit, Is.True);
            Assert.That(observation.ObservedLimit, Is.EqualTo(3));
            Assert.That(observation.HasObservedRemaining, Is.True);
            Assert.That(observation.ObservedRemaining, Is.EqualTo(1));
            Assert.That(observation.HasObservedResetAt, Is.True);
            Assert.That(observation.ObservedResetAt, Is.GreaterThan(before));
        });
    }

    [Test]
    public void LegacyRateHeadersOnContentAreObservedWithoutAffectingOtherBuckets()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[]
        {
            CreatePolicy(ApiService.Tsdr, 10, 60000),
            CreatePolicy(ApiService.TsdrPdfAndZip, 4, 60000)
        });
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(string.Empty);
        response.Content.Headers.TryAddWithoutValidation("X-RateLimit-Limit", "2");
        response.Content.Headers.TryAddWithoutValidation("X-RateLimit-Remaining", "1");

        limiter.ObserveResponse(ApiService.Tsdr, response);
        RateLimitObservation general = limiter.GetObservation(ApiService.Tsdr);
        RateLimitObservation download = limiter.GetObservation(ApiService.TsdrPdfAndZip);

        Assert.Multiple(() =>
        {
            Assert.That(general.EffectiveLimit, Is.EqualTo(2));
            Assert.That(general.ObservedLimit, Is.EqualTo(2));
            Assert.That(general.ObservedRemaining, Is.EqualTo(1));
            Assert.That(download.EffectiveLimit, Is.EqualTo(4));
            Assert.That(download.HasObservedLimit, Is.False);
            Assert.That(download.HasObservedRemaining, Is.False);
        });
    }

    [Test]
    public void LegacyResetHeaderAcceptsUnixTime()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.Tsdr, 5, 1000) });
        long reset = DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds();
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("X-RateLimit-Reset", reset.ToString(System.Globalization.CultureInfo.InvariantCulture));

        limiter.ObserveResponse(ApiService.Tsdr, response);
        RateLimitObservation observation = limiter.GetObservation(ApiService.Tsdr);

        Assert.Multiple(() =>
        {
            Assert.That(observation.HasObservedResetAt, Is.True);
            Assert.That(observation.ObservedResetAt.ToUnixTimeSeconds(), Is.EqualTo(reset));
        });
    }

    [Test]
    public void Tsdr429ExhaustsOnlyTheClassifiedBucketUntilRetryAfter()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[]
        {
            CreatePolicy(ApiService.Tsdr, 60, 60000),
            CreatePolicy(ApiService.TsdrPdfAndZip, 4, 60000)
        });
        using HttpResponseMessage response = new HttpResponseMessage((HttpStatusCode)429);
        response.Headers.TryAddWithoutValidation(UsptoHeaders.RetryAfter, "5");
        DateTimeOffset beforeReset = DateTimeOffset.UtcNow.AddMilliseconds(4500);

        limiter.ObserveResponse(TsdrPolicy.ClassifyRateService(DocumentFormat.Pdf), response);
        limiter.Acquire(TsdrPolicy.ClassifyRateService(DocumentFormat.Xml), CancellationToken.None);
        RateLimitObservation download = limiter.GetObservation(ApiService.TsdrPdfAndZip);
        RateLimitObservation general = limiter.GetObservation(ApiService.Tsdr);

        Assert.Multiple(() =>
        {
            Assert.That(download.Used, Is.EqualTo(4));
            Assert.That(download.HasObservedResetAt, Is.True);
            Assert.That(download.ObservedResetAt, Is.GreaterThan(beforeReset));
            Assert.That(general.Used, Is.EqualTo(1));
            Assert.That(general.HasObservedResetAt, Is.False);
        });
    }

    [Test]
    public void LaterServerLimitsCanRelaxAnObservationOnlyToTheConfiguredCeiling()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.PatentsView, 10, 1000) });
        using HttpResponseMessage restrictive = new HttpResponseMessage(HttpStatusCode.OK);
        restrictive.Headers.TryAddWithoutValidation("RateLimit-Limit", "3");
        limiter.ObserveResponse(ApiService.PatentsView, restrictive);
        using HttpResponseMessage relaxed = new HttpResponseMessage(HttpStatusCode.OK);
        relaxed.Headers.TryAddWithoutValidation("RateLimit-Limit", "50");

        limiter.ObserveResponse(ApiService.PatentsView, relaxed);
        RateLimitObservation observation = limiter.GetObservation(ApiService.PatentsView);

        Assert.Multiple(() =>
        {
            Assert.That(observation.ObservedLimit, Is.EqualTo(50));
            Assert.That(observation.EffectiveLimit, Is.EqualTo(10));
        });
    }

    [Test]
    public void ObservedExhaustionBlocksUntilTheCurrentWindowResets()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.PatentsView, 5, 60000) });
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("RateLimit-Remaining", "0");
        limiter.ObserveResponse(ApiService.PatentsView, response);

        Task waiting = Task.Run(() => limiter.Acquire(ApiService.PatentsView, CancellationToken.None));

        Assert.That(waiting.Wait(10), Is.False);
        ReleaseOneObservedPermit(limiter, ApiService.PatentsView);
        Assert.That(waiting.Wait(2000), Is.True);
    }

    [Test]
    public void ServerHeadersAreIgnoredWhenObservationIsDisabled()
    {
        RateLimitPolicy policy = CreatePolicy(ApiService.PatentsView, 5, 1000);
        policy.ObserveServerHeaders = false;
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { policy });
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("RateLimit-Limit", "1");
        response.Headers.TryAddWithoutValidation("RateLimit-Remaining", "0");

        limiter.ObserveResponse(ApiService.PatentsView, response);
        limiter.Acquire(ApiService.PatentsView, CancellationToken.None);
        RateLimitObservation observation = limiter.GetObservation(ApiService.PatentsView);

        Assert.Multiple(() =>
        {
            Assert.That(observation.EffectiveLimit, Is.EqualTo(5));
            Assert.That(observation.HasObservedLimit, Is.False);
            Assert.That(observation.Used, Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedOrUnboundedServerHeadersAreIgnored()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.PatentsView, 5, 1000) });
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("RateLimit-Limit", "invalid");
        response.Headers.TryAddWithoutValidation("RateLimit-Remaining", "-1");
        response.Headers.TryAddWithoutValidation("X-RateLimit-Reset", long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));

        limiter.ObserveResponse(ApiService.PatentsView, response);
        RateLimitObservation observation = limiter.GetObservation(ApiService.PatentsView);

        Assert.Multiple(() =>
        {
            Assert.That(observation.HasObservedLimit, Is.False);
            Assert.That(observation.HasObservedRemaining, Is.False);
            Assert.That(observation.HasObservedResetAt, Is.False);
            Assert.That(observation.EffectiveLimit, Is.EqualTo(5));
        });
    }

    [Test]
    public void ServicesWithoutPoliciesAreNotThrottled()
    {
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.Tsdr, 1, 1000) });

        for (int index = 0; index < 100; index += 1)
        {
            limiter.Acquire(ApiService.Odp, CancellationToken.None);
        }

        Assert.Throws<ValidationException>(() => limiter.GetObservation(ApiService.Odp));
    }

    [Test]
    public void ConcurrentBurstNeverReceivesMoreThanOneWindowOfPermits()
    {
        const int limit = 4;
        const int callerCount = 20;
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.PatentsView, limit, 60000) });
        for (int index = 0; index < limit; index++)
        {
            limiter.Acquire(ApiService.PatentsView, CancellationToken.None);
        }

        using CancellationTokenSource cancellation = new CancellationTokenSource();
        Task[] callers = new Task[callerCount];
        for (int index = 0; index < callers.Length; index++)
        {
            callers[index] = Task.Factory.StartNew(
                () => limiter.Acquire(ApiService.PatentsView, cancellation.Token),
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        try
        {
            Assert.That(SpinWait.SpinUntil(() => limiter.GetObservation(ApiService.PatentsView).WaitingCount == callerCount, 2000), Is.True);
            ReleaseOneObservedPermit(limiter, ApiService.PatentsView);
            Assert.That(SpinWait.SpinUntil(() =>
                callers.Count(task => task.IsCompletedSuccessfully) == limit &&
                limiter.GetObservation(ApiService.PatentsView).WaitingCount == callerCount - limit, 2000), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(callers.Count(task => task.IsCompletedSuccessfully), Is.EqualTo(limit));
                Assert.That(limiter.GetObservation(ApiService.PatentsView).Used, Is.EqualTo(limit));
                Assert.That(limiter.GetObservation(ApiService.PatentsView).WaitingCount, Is.EqualTo(callerCount - limit));
            });
        }
        finally
        {
            cancellation.Cancel();
            Assert.Throws<AggregateException>(() => Task.WaitAll(callers, 2000));
        }
    }

    [Test]
    public void ConfiguredTimingHooksAdvanceAFullWindowWithoutWallClockDelay()
    {
        DateTimeOffset start = new DateTimeOffset(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);
        FakeTestClock clock = new FakeTestClock(start);
        AdvancingRateLimitWait waiter = new AdvancingRateLimitWait(clock);
        RateLimiter limiter = new RateLimiter(new RateLimitPolicy[] { CreatePolicy(ApiService.PatentsView, 1, 1000) });
        limiter.ConfigureTimingHooks(clock, waiter);

        limiter.Acquire(ApiService.PatentsView, CancellationToken.None);
        limiter.Acquire(ApiService.PatentsView, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(waiter.Waits.Sum(), Is.EqualTo(1000));
            Assert.That(clock.Current, Is.EqualTo(start.AddMilliseconds(1000)));
            Assert.That(limiter.GetObservation(ApiService.PatentsView).Used, Is.EqualTo(1));
        });
    }

    [TestCase(0, 100, false, 0)]
    [TestCase(1, 0, false, 0)]
    [TestCase(1, 100, true, -1)]
    public void InvalidPoliciesAreRejected(int requests, int window, bool hasQueueCapacity, int queueCapacity)
    {
        RateLimitPolicy policy = CreatePolicy(ApiService.Tsdr, requests, window);
        policy.HasQueueCapacity = hasQueueCapacity;
        policy.QueueCapacity = queueCapacity;

        Assert.Throws<ValidationException>(() => new RateLimiter(new RateLimitPolicy[] { policy }));
    }

    private static RateLimitPolicy CreatePolicy(ApiService service, int requests, int windowMilliseconds)
    {
        return new RateLimitPolicy
        {
            Service = service,
            RequestsPerWindow = requests,
            WindowMilliseconds = windowMilliseconds
        };
    }

    private static void ReleaseOneObservedPermit(RateLimiter limiter, ApiService service)
    {
        using HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Headers.TryAddWithoutValidation("RateLimit-Remaining", "1");
        response.Headers.TryAddWithoutValidation("RateLimit-Reset", "0");
        limiter.ObserveResponse(service, response);
    }
}
