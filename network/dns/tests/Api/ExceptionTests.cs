namespace dns.Tests.Api;

public class ExceptionTests
{
    [Test]
    public void FormatExceptionCanBeThrownWithItsDiagnosticMessage()
    {
        Assert.That(
            () => throw new DnsFormatException("Invalid compressed name."),
            Throws.TypeOf<DnsFormatException>()
                .With.Message.EqualTo("Invalid compressed name."));
    }

    [Test]
    public void TransportExceptionCanBeThrownWithItsDiagnosticMessage()
    {
        Assert.That(
            () => throw new DnsTransportException("The DNS server timed out."),
            Throws.TypeOf<DnsTransportException>()
                .With.Message.EqualTo("The DNS server timed out."));
    }

    [Test]
    public void ResolutionExceptionCanBeThrownWithItsDiagnosticMessage()
    {
        Assert.That(
            () => throw new DnsResolutionException("The alias depth was exceeded."),
            Throws.TypeOf<DnsResolutionException>()
                .With.Message.EqualTo("The alias depth was exceeded."));
    }

    [Test]
    public void ZoneExceptionCanBeThrownWithItsDiagnosticMessage()
    {
        Assert.That(
            () => throw new DnsZoneException("The apex SOA record is missing."),
            Throws.TypeOf<DnsZoneException>()
                .With.Message.EqualTo("The apex SOA record is missing."));
    }

    [Test]
    public void SecurityExceptionCanBeThrownWithItsDiagnosticMessage()
    {
        Assert.That(
            () => throw new DnsSecurityException("The DNSSEC signature is invalid."),
            Throws.TypeOf<DnsSecurityException>()
                .With.Message.EqualTo("The DNSSEC signature is invalid."));
    }
}
