using whois;

namespace whois.Tests;

public class ApiDefaultsTests
{
    [Test]
    public void ProtocolEndpointsUseAssignedDefaults()
    {
        Assert.That(WhoisDefaults.WhoisPort, Is.EqualTo(43));
        Assert.That(WhoisDefaults.RWhoisPort, Is.EqualTo(4321));
        Assert.That(WhoisDefaults.IanaHost, Is.EqualTo("whois.iana.org"));
    }

    [Test]
    public void ResourceLimitsAreBoundedAndPositive()
    {
        Assert.That(WhoisDefaults.MaximumEncodedQueryBytes, Is.EqualTo(4096));
        Assert.That(WhoisDefaults.MaximumResponseBytes, Is.EqualTo(4 * 1024 * 1024));
        Assert.That(WhoisDefaults.MaximumReferralHops, Is.EqualTo(5));
        Assert.That(WhoisDefaults.MaximumLines, Is.EqualTo(65536));
        Assert.That(WhoisDefaults.MaximumFields, Is.EqualTo(32768));
    }

    [Test]
    public void StageTimeoutsFitWithinOverallTimeout()
    {
        Assert.That(WhoisDefaults.ResolveTimeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(WhoisDefaults.ConnectTimeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(WhoisDefaults.WriteTimeout, Is.EqualTo(TimeSpan.FromSeconds(10)));
        Assert.That(WhoisDefaults.ReadIdleTimeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
        Assert.That(WhoisDefaults.OverallTimeout, Is.EqualTo(TimeSpan.FromSeconds(60)));
        Assert.That(WhoisDefaults.ResolveTimeout, Is.LessThanOrEqualTo(WhoisDefaults.OverallTimeout));
        Assert.That(WhoisDefaults.ConnectTimeout, Is.LessThanOrEqualTo(WhoisDefaults.OverallTimeout));
        Assert.That(WhoisDefaults.WriteTimeout, Is.LessThanOrEqualTo(WhoisDefaults.OverallTimeout));
        Assert.That(WhoisDefaults.ReadIdleTimeout, Is.LessThanOrEqualTo(WhoisDefaults.OverallTimeout));
    }

    [Test]
    public void ReferralFieldNamesPreserveWireLabels()
    {
        Assert.That(WhoisFieldNames.Refer, Is.EqualTo("refer"));
        Assert.That(WhoisFieldNames.Whois, Is.EqualTo("whois"));
        Assert.That(WhoisFieldNames.WhoisServer, Is.EqualTo("Whois Server"));
        Assert.That(WhoisFieldNames.RegistrarWhoisServer, Is.EqualTo("Registrar WHOIS Server"));
        Assert.That(WhoisFieldNames.ReferralServer, Is.EqualTo("ReferralServer"));
        Assert.That(WhoisFieldNames.ReferralServerWithSpace, Is.EqualTo("Referral Server"));
        Assert.That(WhoisFieldNames.ReferralDirective, Is.EqualTo("%referral"));
    }
}
