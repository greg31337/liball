namespace http.Tests;

public class CacheAgeSemanticsTests
{
    [Test]
    public void CalculatesConservativeCurrentAgeFromEveryComponent()
    {
        DateTimeOffset responseTime = Time(2026, 1, 2, 12, 0, 5);
        HttpCacheResponseContext context = Context(responseTime);
        context.RequestTime = responseTime.AddSeconds(-5);
        context.HasDate = true;
        context.Date = responseTime.AddSeconds(-15);
        context.HasAge = true;
        context.Age = 7;
        context.CurrentTime = responseTime.AddSeconds(20);

        HttpCacheDecision result = CacheAgeSemantics.Calculate(context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Outcome, Is.EqualTo(HttpCacheOutcome.Store));
            Assert.That(result.HasCurrentAge, Is.True);
            Assert.That(result.CurrentAge, Is.EqualTo(35));
            Assert.That(result.ResponseContext, Is.SameAs(context));
            Assert.That(result.Response, Is.SameAs(context.Response));
        });
    }

    [Test]
    public void CorrectedAgeValueCanDominateApparentAge()
    {
        DateTimeOffset responseTime = Time(2026, 1, 2, 12, 0, 5);
        HttpCacheResponseContext context = Context(responseTime);
        context.RequestTime = responseTime.AddSeconds(-5);
        context.HasDate = true;
        context.Date = responseTime;
        context.HasAge = true;
        context.Age = 30;
        context.CurrentTime = responseTime.AddSeconds(10);

        HttpCacheDecision result = CacheAgeSemantics.Calculate(context);

        Assert.That(result.CurrentAge, Is.EqualTo(45));
    }

    [Test]
    public void MissingDateAndAgeStillIncludeTransportAndResidentTime()
    {
        DateTimeOffset responseTime = Time(2026, 1, 2, 12, 0, 5);
        HttpCacheResponseContext context = Context(responseTime);
        context.RequestTime = responseTime.AddSeconds(-5);
        context.CurrentTime = responseTime.AddSeconds(10);

        HttpCacheDecision result = CacheAgeSemantics.Calculate(context);

        Assert.That(result.CurrentAge, Is.EqualTo(15));
    }

    [Test]
    public void ClockSkewCannotProduceOrSubtractNegativeAge()
    {
        DateTimeOffset responseTime = Time(2026, 1, 2, 12, 0, 5);
        HttpCacheResponseContext context = Context(responseTime);
        context.RequestTime = responseTime.AddSeconds(20);
        context.HasDate = true;
        context.Date = responseTime.AddSeconds(30);
        context.HasAge = true;
        context.Age = 8;
        context.CurrentTime = responseTime.AddSeconds(-10);

        HttpCacheDecision result = CacheAgeSemantics.Calculate(context);

        Assert.That(result.CurrentAge, Is.EqualTo(8));
    }

    [Test]
    public void ElapsedComponentsUseWholeSeconds()
    {
        DateTimeOffset responseTime = Time(2026, 1, 2, 12, 0, 5)
            .AddMilliseconds(900);
        HttpCacheResponseContext context = Context(responseTime);
        context.RequestTime = responseTime.AddMilliseconds(-900);
        context.HasDate = true;
        context.Date = responseTime.AddMilliseconds(-1900);
        context.CurrentTime = responseTime.AddMilliseconds(1999);

        HttpCacheDecision result = CacheAgeSemantics.Calculate(context);

        Assert.That(result.CurrentAge, Is.EqualTo(2));
    }

    [Test]
    public void EquivalentInstantsWithDifferentOffsetsHaveZeroElapsedTime()
    {
        DateTimeOffset responseTime = new DateTimeOffset(
            2026,
            1,
            2,
            12,
            0,
            0,
            TimeSpan.Zero);
        HttpCacheResponseContext context = Context(responseTime);
        context.RequestTime = new DateTimeOffset(
            2026,
            1,
            2,
            7,
            0,
            0,
            TimeSpan.FromHours(-5));
        context.HasDate = true;
        context.Date = new DateTimeOffset(
            2026,
            1,
            2,
            13,
            0,
            0,
            TimeSpan.FromHours(1));
        context.CurrentTime = responseTime;

        HttpCacheDecision result = CacheAgeSemantics.Calculate(context);

        Assert.That(result.CurrentAge, Is.Zero);
    }

    [Test]
    public void CurrentAgeSaturatesInsteadOfOverflowing()
    {
        DateTimeOffset responseTime = Time(2026, 1, 2, 12, 0, 5);
        HttpCacheResponseContext context = Context(responseTime);
        context.RequestTime = responseTime.AddSeconds(-1);
        context.HasAge = true;
        context.Age = long.MaxValue;
        context.CurrentTime = responseTime.AddSeconds(1);

        HttpCacheDecision result = CacheAgeSemantics.Calculate(context);

        Assert.That(result.CurrentAge, Is.EqualTo(long.MaxValue));
    }

    [Test]
    public void InactiveAgeValueIsIgnoredButActiveNegativeAgeIsRejected()
    {
        DateTimeOffset responseTime = Time(2026, 1, 2, 12, 0, 5);
        HttpCacheResponseContext inactive = Context(responseTime);
        inactive.Age = -1;
        HttpCacheResponseContext active = Context(responseTime);
        active.HasAge = true;
        active.Age = -1;

        HttpCacheDecision inactiveResult = CacheAgeSemantics.Calculate(inactive);

        Assert.Multiple(() =>
        {
            Assert.That(inactiveResult.CurrentAge, Is.Zero);
            Assert.Throws<HttpProtocolException>(() =>
                CacheAgeSemantics.Calculate(active));
        });
    }

    [Test]
    public void MissingStoredResponseIsRejected()
    {
        HttpCacheResponseContext context = new HttpCacheResponseContext();

        Assert.Throws<HttpProtocolException>(() =>
            CacheAgeSemantics.Calculate(context));
    }

    static HttpCacheResponseContext Context(DateTimeOffset responseTime)
    {
        HttpCacheResponseContext context = new HttpCacheResponseContext();
        context.Response = new HttpResponse();
        context.RequestTime = responseTime;
        context.ResponseTime = responseTime;
        context.StoredTime = responseTime;
        context.CurrentTime = responseTime;
        return context;
    }

    static DateTimeOffset Time(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        int second)
    {
        return new DateTimeOffset(
            year,
            month,
            day,
            hour,
            minute,
            second,
            TimeSpan.Zero);
    }
}
