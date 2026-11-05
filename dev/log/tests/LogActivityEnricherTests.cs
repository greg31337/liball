using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogActivityEnricherTests
{
    [Test]
    public void NormalizeTraceId_TreatsDefaultTraceIdAsEmpty()
    {
        Assert.That(LogActivityEnricher.NormalizeTraceId("00000000000000000000000000000000"), Is.EqualTo(""));
        Assert.That(LogActivityEnricher.NormalizeTraceId("4bf92f3577b34da6a3ce929d0e0e4736"), Is.EqualTo("4bf92f3577b34da6a3ce929d0e0e4736"));
    }

    [Test]
    public void NormalizeSpanId_TreatsDefaultSpanIdAsEmpty()
    {
        Assert.That(LogActivityEnricher.NormalizeSpanId("0000000000000000"), Is.EqualTo(""));
        Assert.That(LogActivityEnricher.NormalizeSpanId("00f067aa0ba902b7"), Is.EqualTo("00f067aa0ba902b7"));
    }
}
