using System.Globalization;
using uspto;

namespace uspto.Tests;

public class RetryEngineTests
{
    [Test]
    public void ExponentialBackoffStopsAtDelayAndAttemptCaps()
    {
        RetryPolicy policy = CreatePolicy(4, 100, 250, 1000, false);
        RecordingRetryDelay delay = new RecordingRetryDelay();
        RetryEngine engine = new RetryEngine(policy, delay, new FixedRetryRandom(0.5));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
        HttpRequestException failure = new HttpRequestException("connection interrupted");

        bool first = engine.TryWaitForRetry(state, request, false, failure, CancellationToken.None);
        bool second = engine.TryWaitForRetry(state, request, false, failure, CancellationToken.None);
        bool third = engine.TryWaitForRetry(state, request, false, failure, CancellationToken.None);
        bool exhausted = engine.TryWaitForRetry(state, request, false, failure, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            Assert.That(third, Is.True);
            Assert.That(exhausted, Is.False);
            Assert.That(delay.Waits, Is.EqualTo(new long[] { 100L, 200L, 250L }));
            Assert.That(state.AttemptCount, Is.EqualTo(4));
            Assert.That(state.TotalDelayMilliseconds, Is.EqualTo(550L));
            Assert.That(state.LastDelayMilliseconds, Is.EqualTo(250L));
        });
    }

    [TestCase(0.0, 50L)]
    [TestCase(0.999999, 100L)]
    public void JitterStaysWithinTheUpperHalfOfTheExponentialDelay(double unit, long expected)
    {
        RetryEngine engine = new RetryEngine(CreatePolicy(2, 100, 100, 1000, true), new RecordingRetryDelay(), new FixedRetryRandom(unit));

        long result = engine.CalculateDelayMilliseconds(new RetryExecutionState(), new HttpRequestException("network"));

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ServerRetryAfterIsHonoredWithoutJitterAndCapped()
    {
        RetryEngine engine = new RetryEngine(CreatePolicy(2, 100, 300, 1000, true), new RecordingRetryDelay(), new FixedRetryRandom(0.0));
        RateLimitException failure = new RateLimitException("limited", ApiService.PatentsView, 429, true, null, 5000L, true, null);

        long result = engine.CalculateDelayMilliseconds(new RetryExecutionState(), failure);

        Assert.That(result, Is.EqualTo(300L));
    }

    [Test]
    public void ServerRetryAfterCanBeDisabled()
    {
        RetryPolicy policy = CreatePolicy(2, 100, 300, 1000, false);
        policy.HonorRetryAfter = false;
        RetryEngine engine = new RetryEngine(policy, new RecordingRetryDelay(), new FixedRetryRandom(0.5));
        ServiceUnavailableException failure = new ServiceUnavailableException("unavailable", ApiService.Odp, 503, true, null, 250L, true, null);

        long result = engine.CalculateDelayMilliseconds(new RetryExecutionState(), failure);

        Assert.That(result, Is.EqualTo(100L));
    }

    [Test]
    public void RetryThatExceedsTheRemainingBudgetIsNotStarted()
    {
        RecordingRetryDelay delay = new RecordingRetryDelay();
        RetryEngine engine = new RetryEngine(CreatePolicy(4, 100, 100, 99, false), delay, new FixedRetryRandom(0.5));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        bool result = engine.TryWaitForRetry(state, request, false, new HttpRequestException("network"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(delay.Waits, Is.Empty);
            Assert.That(state.AttemptCount, Is.EqualTo(1));
            Assert.That(state.TotalDelayMilliseconds, Is.Zero);
        });
    }

    [Test]
    public void CumulativeWaitsCannotExceedTheTotalBudget()
    {
        RecordingRetryDelay delay = new RecordingRetryDelay();
        RetryEngine engine = new RetryEngine(CreatePolicy(6, 60, 60, 120, false), delay, new FixedRetryRandom(0.5));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
        HttpRequestException failure = new HttpRequestException("network");

        bool first = engine.TryWaitForRetry(state, request, false, failure, CancellationToken.None);
        bool second = engine.TryWaitForRetry(state, request, false, failure, CancellationToken.None);
        bool third = engine.TryWaitForRetry(state, request, false, failure, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(second, Is.True);
            Assert.That(third, Is.False);
            Assert.That(delay.Waits, Is.EqualTo(new long[] { 60L, 60L }));
            Assert.That(state.AttemptCount, Is.EqualTo(3));
            Assert.That(state.TotalDelayMilliseconds, Is.EqualTo(120L));
        });
    }

    [Test]
    public void CallerCancellationBeforeTheWaitIsPropagatedWithoutChangingState()
    {
        RecordingRetryDelay delay = new RecordingRetryDelay();
        RetryEngine engine = new RetryEngine(CreatePolicy(2, 100, 100, 1000, false), delay, new FixedRetryRandom(0.5));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
        using CancellationTokenSource caller = new CancellationTokenSource();
        caller.Cancel();

        OperationCanceledException exception = Assert.Catch<OperationCanceledException>(() => engine.TryWaitForRetry(state, request, false, new HttpRequestException("network"), caller.Token));

        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(caller.Token));
            Assert.That(delay.Waits, Is.Empty);
            Assert.That(state.AttemptCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void CallerCancellationDuringTheWaitIsPropagatedWithoutConsumingBudget()
    {
        using CancellationTokenSource caller = new CancellationTokenSource();
        CancelingRetryDelay delay = new CancelingRetryDelay(caller);
        RetryEngine engine = new RetryEngine(CreatePolicy(2, 100, 100, 1000, false), delay, new FixedRetryRandom(0.5));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        OperationCanceledException exception = Assert.Catch<OperationCanceledException>(() => engine.TryWaitForRetry(state, request, false, new HttpRequestException("network"), caller.Token));

        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(caller.Token));
            Assert.That(state.AttemptCount, Is.EqualTo(1));
            Assert.That(state.TotalDelayMilliseconds, Is.Zero);
        });
    }

    [Test]
    public void DefaultDelayIsInterruptedByCallerCancellation()
    {
        RetryEngine engine = RetryEngine.CreateDefault(CreatePolicy(2, 1000, 1000, 2000, false));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");
        using CancellationTokenSource caller = new CancellationTokenSource();
        caller.CancelAfter(20);

        OperationCanceledException exception = Assert.Catch<OperationCanceledException>(() => engine.TryWaitForRetry(state, request, false, new HttpRequestException("network"), caller.Token));

        Assert.Multiple(() =>
        {
            Assert.That(exception.CancellationToken, Is.EqualTo(caller.Token));
            Assert.That(state.AttemptCount, Is.EqualTo(1));
            Assert.That(state.TotalDelayMilliseconds, Is.Zero);
        });
    }

    [Test]
    public void IneligibleFailureDoesNotWaitOrConsumeAnAttempt()
    {
        RecordingRetryDelay delay = new RecordingRetryDelay();
        RetryEngine engine = new RetryEngine(CreatePolicy(3, 100, 100, 1000, false), delay, new FixedRetryRandom(0.5));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/resource");

        bool result = engine.TryWaitForRetry(state, request, false, new InvalidOperationException("permanent"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(delay.Waits, Is.Empty);
            Assert.That(state.AttemptCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void SearchPostCanAdvanceToAnotherAttempt()
    {
        RecordingRetryDelay delay = new RecordingRetryDelay();
        RetryEngine engine = new RetryEngine(CreatePolicy(2, 10, 10, 100, false), delay, new FixedRetryRandom(0.5));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/search");

        bool result = engine.TryWaitForRetry(state, request, true, new HttpRequestException("network"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True);
            Assert.That(delay.Waits, Is.EqualTo(new long[] { 10L }));
            Assert.That(state.AttemptCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void UnsafePostDoesNotWaitOrConsumeAnAttempt()
    {
        RecordingRetryDelay delay = new RecordingRetryDelay();
        RetryEngine engine = new RetryEngine(CreatePolicy(3, 100, 100, 1000, false), delay, new FixedRetryRandom(0.5));
        RetryExecutionState state = new RetryExecutionState();
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://example.test/mutate");

        bool result = engine.TryWaitForRetry(state, request, false, new HttpRequestException("network"), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False);
            Assert.That(delay.Waits, Is.Empty);
            Assert.That(state.AttemptCount, Is.EqualTo(1));
            Assert.That(state.TotalDelayMilliseconds, Is.Zero);
        });
    }

    [Test]
    public void DeltaSecondsRetryAfterProducesAnExactDelay()
    {
        Dictionary<string, string[]> headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [UsptoHeaders.RetryAfter] = new string[] { "3" }
        };

        RetryAfterValue result = ErrorMapper.ParseRetryAfter(headers);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Milliseconds, Is.EqualTo(3000L));
        });
    }

    [Test]
    public void HttpDateRetryAfterProducesARelativeDelay()
    {
        DateTimeOffset retryAt = DateTimeOffset.UtcNow.AddSeconds(10);
        Dictionary<string, string[]> headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [UsptoHeaders.RetryAfter] = new string[] { retryAt.ToString("R", CultureInfo.InvariantCulture) }
        };

        RetryAfterValue result = ErrorMapper.ParseRetryAfter(headers);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Milliseconds, Is.GreaterThanOrEqualTo(8000L));
            Assert.That(result.Milliseconds, Is.LessThanOrEqualTo(10000L));
        });
    }

    [Test]
    public void PastHttpDateRetryAfterAllowsAnImmediateRetry()
    {
        Dictionary<string, string[]> headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [UsptoHeaders.RetryAfter] = new string[] { "Sun, 06 Nov 1994 08:49:37 GMT" }
        };

        RetryAfterValue result = ErrorMapper.ParseRetryAfter(headers);

        Assert.Multiple(() =>
        {
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Milliseconds, Is.Zero);
        });
    }

    [TestCase("not-a-date")]
    [TestCase("Sun, 31 Feb 2026 08:49:37 GMT")]
    [TestCase("Sun, 06 Nov 1994 25:49:37 GMT")]
    [TestCase("Mon, 06 Nov 1994 08:49:37 GMT")]
    public void InvalidHttpDateRetryAfterIsIgnored(string value)
    {
        Dictionary<string, string[]> headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [UsptoHeaders.RetryAfter] = new string[] { value }
        };

        RetryAfterValue result = ErrorMapper.ParseRetryAfter(headers);

        Assert.That(result.HasValue, Is.False);
    }

    [TestCase(0, 100, 100, 1000)]
    [TestCase(2, -1, 100, 1000)]
    [TestCase(2, 100, 99, 1000)]
    [TestCase(2, 100, 100, -1)]
    public void InvalidRetryPoliciesAreRejected(int attempts, int initial, int maximum, int budget)
    {
        RetryPolicy policy = CreatePolicy(attempts, initial, maximum, budget, false);

        Assert.Throws<ValidationException>(() => new RetryEngine(policy, new RecordingRetryDelay(), new FixedRetryRandom(0.5)));
    }

    private static RetryPolicy CreatePolicy(int attempts, int initial, int maximum, int budget, bool jitter)
    {
        return new RetryPolicy
        {
            AttemptCount = attempts,
            InitialDelayMilliseconds = initial,
            MaximumDelayMilliseconds = maximum,
            BudgetMilliseconds = budget,
            UseJitter = jitter
        };
    }
}

internal class CancelingRetryDelay : IRetryDelay
{
    internal CancellationTokenSource Source;

    internal CancelingRetryDelay(CancellationTokenSource source)
    {
        Source = source;
    }

    public void Wait(long milliseconds, CancellationToken cancellationToken)
    {
        Source.Cancel();
        cancellationToken.ThrowIfCancellationRequested();
    }
}
