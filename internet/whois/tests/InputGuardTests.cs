using whois;

namespace whois.Tests;

public class InputGuardTests
{
    [Test]
    public void RequiredReferenceRejectsNullWithParameterName()
    {
        Assert.That(() => InputGuard.RequireReference(new object(), "dependency"), Throws.Nothing);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => InputGuard.RequireReference(null, "dependency"));

        Assert.That(exception.ParamName, Is.EqualTo("dependency"));
    }

    [Test]
    public void QueryTextAcceptsVisibleSingleAndMultiwordQueries()
    {
        Assert.That(() => InputGuard.ValidateQueryText("example.com", WhoisQueryKind.Domain), Throws.Nothing);
        Assert.That(() => InputGuard.ValidateQueryText("domain example.com", WhoisQueryKind.FreeForm), Throws.Nothing);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void QueryTextRejectsEmptyOrWhitespace(string value)
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateQueryText(value, WhoisQueryKind.FreeForm));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryKind, Is.EqualTo(WhoisQueryKind.FreeForm));
    }

    [TestCase("example.com\r")]
    [TestCase("example.com\nnext")]
    [TestCase("example.com\0")]
    [TestCase("example.com\t")]
    [TestCase("example.com\u007f")]
    public void QueryTextRejectsEveryControlCharacter(string value)
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateQueryText(value, WhoisQueryKind.Domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(value));
    }

    [TestCase("whois.example")]
    [TestCase("192.0.2.1")]
    [TestCase("2001:db8::1")]
    public void EndpointAcceptsDnsAndIpHosts(string host)
    {
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = host;

        Assert.That(() => InputGuard.ValidateEndpoint(endpoint, "example.com", WhoisQueryKind.Domain), Throws.Nothing);
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(" whois.example")]
    [TestCase("whois.example ")]
    [TestCase("whois.example/path")]
    [TestCase("user@whois.example")]
    [TestCase("bad host")]
    [TestCase("whois.example\n")]
    public void EndpointRejectsInvalidHosts(string host)
    {
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = host;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateEndpoint(endpoint, "example.com", WhoisQueryKind.Domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Endpoint.Host"));
        Assert.That(exception.Endpoint, Is.SameAs(endpoint));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(65536)]
    public void EndpointRejectsPortsOutsideTcpRange(int port)
    {
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Host = "whois.example";
        endpoint.Port = port;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateEndpoint(endpoint, "example.com", WhoisQueryKind.Domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Endpoint.Port"));
        Assert.That(exception.Endpoint, Is.SameAs(endpoint));
    }

    [Test]
    public void EndpointRejectsMissingObject()
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateEndpoint(null, "example.com", WhoisQueryKind.Domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Endpoint"));
        Assert.That(exception.Endpoint, Is.Null);
    }

    [TestCase("MaximumEncodedQueryBytes")]
    [TestCase("MaximumResponseBytes")]
    [TestCase("MaximumReferralHops")]
    [TestCase("MaximumLines")]
    [TestCase("MaximumFields")]
    public void LimitsRejectEveryNonpositiveField(string fieldName)
    {
        WhoisLimits limits = new WhoisLimits();
        if (fieldName == "MaximumEncodedQueryBytes")
        {
            limits.MaximumEncodedQueryBytes = 0;
        }
        else if (fieldName == "MaximumResponseBytes")
        {
            limits.MaximumResponseBytes = 0;
        }
        else if (fieldName == "MaximumReferralHops")
        {
            limits.MaximumReferralHops = 0;
        }
        else if (fieldName == "MaximumLines")
        {
            limits.MaximumLines = 0;
        }
        else
        {
            limits.MaximumFields = 0;
        }

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateLimits(limits, "example.com", WhoisQueryKind.Domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Limits." + fieldName));
    }

    [Test]
    public void LimitsAcceptDefaultsAndRejectMissingObject()
    {
        Assert.That(() => InputGuard.ValidateLimits(new WhoisLimits(), "example.com", WhoisQueryKind.Domain), Throws.Nothing);

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateLimits(null, "example.com", WhoisQueryKind.Domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Limits"));
    }

    [Test]
    public void TimeoutsAcceptDefaultsSubmillisecondStagesAndInfiniteOverall()
    {
        WhoisTimeoutOptions defaults = new WhoisTimeoutOptions();
        WhoisTimeoutOptions submillisecond = new WhoisTimeoutOptions();
        WhoisTimeoutOptions infiniteOverall = new WhoisTimeoutOptions();
        submillisecond.Resolve = TimeSpan.FromTicks(1);
        submillisecond.Connect = TimeSpan.FromTicks(1);
        submillisecond.Write = TimeSpan.FromTicks(1);
        submillisecond.ReadIdle = TimeSpan.FromTicks(1);
        infiniteOverall.Overall = Timeout.InfiniteTimeSpan;

        Assert.That(() => InputGuard.ValidateTimeouts(defaults, "example.com", WhoisQueryKind.Domain), Throws.Nothing);
        Assert.That(() => InputGuard.ValidateTimeouts(submillisecond, "example.com", WhoisQueryKind.Domain), Throws.Nothing);
        Assert.That(() => InputGuard.ValidateTimeouts(infiniteOverall, "example.com", WhoisQueryKind.Domain), Throws.Nothing);
    }

    [TestCase("Resolve")]
    [TestCase("Connect")]
    [TestCase("Write")]
    [TestCase("ReadIdle")]
    public void TimeoutsRejectNonpositiveStageValues(string fieldName)
    {
        WhoisTimeoutOptions timeouts = new WhoisTimeoutOptions();
        if (fieldName == "Resolve")
        {
            timeouts.Resolve = TimeSpan.Zero;
        }
        else if (fieldName == "Connect")
        {
            timeouts.Connect = TimeSpan.FromMilliseconds(-1);
        }
        else if (fieldName == "Write")
        {
            timeouts.Write = TimeSpan.Zero;
        }
        else
        {
            timeouts.ReadIdle = TimeSpan.Zero;
        }

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateTimeouts(timeouts, "example.com", WhoisQueryKind.Domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Timeouts." + fieldName));
    }

    [Test]
    public void TimeoutsRejectOverflowAndInconsistentOverallBudget()
    {
        WhoisTimeoutOptions overflow = new WhoisTimeoutOptions();
        WhoisTimeoutOptions inconsistent = new WhoisTimeoutOptions();
        overflow.Connect = TimeSpan.FromMilliseconds(2147483648d);
        inconsistent.Overall = TimeSpan.FromSeconds(5);

        WhoisQueryException overflowException = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateTimeouts(overflow, "example.com", WhoisQueryKind.Domain));
        WhoisQueryException inconsistentException = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateTimeouts(inconsistent, "example.com", WhoisQueryKind.Domain));

        Assert.That(overflowException.InvalidParameter, Is.EqualTo("Timeouts.Connect"));
        Assert.That(inconsistentException.InvalidParameter, Is.EqualTo("Timeouts.Overall"));
    }

    [Test]
    public void TimeoutsRejectMissingObject()
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => InputGuard.ValidateTimeouts(null, "example.com", WhoisQueryKind.Domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Timeouts"));
    }
}
