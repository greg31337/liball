using whois;

namespace whois.Tests;

public class EndpointPolicyTests
{
    [TestCase(WhoisEndpointSource.Direct)]
    [TestCase(WhoisEndpointSource.InitialSelection)]
    [TestCase(WhoisEndpointSource.Referral)]
    public void AllowAllPolicyPermitsTrustedPrivateAndNonstandardDestinations(WhoisEndpointSource source)
    {
        IWhoisEndpointPolicy policy = new WhoisAllowAllEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Port = 1043;

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Loopback, source);

        Assert.That(allowed, Is.True);
    }

    [TestCase("8.8.8.8", WhoisDefaults.WhoisPort, WhoisEndpointSource.InitialSelection)]
    [TestCase("8.8.8.8", WhoisDefaults.RWhoisPort, WhoisEndpointSource.InitialSelection)]
    [TestCase("1.1.1.1", WhoisDefaults.WhoisPort, WhoisEndpointSource.Referral)]
    [TestCase("1.1.1.1", WhoisDefaults.RWhoisPort, WhoisEndpointSource.Referral)]
    [TestCase("2606:4700:4700::1111", WhoisDefaults.WhoisPort, WhoisEndpointSource.Referral)]
    public void PublicPolicyAllowsAutomaticPublicWhoisDestinations(string addressText, int port, WhoisEndpointSource source)
    {
        IWhoisEndpointPolicy policy = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Port = port;

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Parse(addressText), source);

        Assert.That(allowed, Is.True);
    }

    [TestCase("127.0.0.1")]
    [TestCase("10.0.0.1")]
    [TestCase("192.0.2.1")]
    [TestCase("::1")]
    [TestCase("fc00::1")]
    [TestCase("2001:db8::1")]
    public void PublicPolicyRejectsAutomaticNonPublicDestinations(string addressText)
    {
        IWhoisEndpointPolicy policy = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Parse(addressText), WhoisEndpointSource.Referral);

        Assert.That(allowed, Is.False);
    }

    [TestCase("9.255.255.255", true)]
    [TestCase("10.0.0.0", false)]
    [TestCase("10.255.255.255", false)]
    [TestCase("11.0.0.0", true)]
    [TestCase("100.63.255.255", true)]
    [TestCase("100.64.0.0", false)]
    [TestCase("100.127.255.255", false)]
    [TestCase("100.128.0.0", true)]
    [TestCase("172.15.255.255", true)]
    [TestCase("172.16.0.0", false)]
    [TestCase("172.31.255.255", false)]
    [TestCase("172.32.0.0", true)]
    [TestCase("192.0.0.255", false)]
    [TestCase("192.0.1.0", true)]
    [TestCase("198.17.255.255", true)]
    [TestCase("198.18.0.0", false)]
    [TestCase("198.19.255.255", false)]
    [TestCase("198.20.0.0", true)]
    [TestCase("223.255.255.255", true)]
    [TestCase("224.0.0.0", false)]
    public void PublicPolicyAppliesIpv4SpecialUseBoundaries(string addressText, bool expected)
    {
        IWhoisEndpointPolicy policy = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Parse(addressText), WhoisEndpointSource.Referral);

        Assert.That(allowed, Is.EqualTo(expected));
    }

    [TestCase("2001:1ff:ffff:ffff:ffff:ffff:ffff:ffff", false)]
    [TestCase("2001:200::", true)]
    [TestCase("2001:db7:ffff:ffff:ffff:ffff:ffff:ffff", true)]
    [TestCase("2001:db8::", false)]
    [TestCase("2001:db8:ffff:ffff:ffff:ffff:ffff:ffff", false)]
    [TestCase("2001:db9::", true)]
    [TestCase("2002::", false)]
    [TestCase("2002:ffff:ffff:ffff:ffff:ffff:ffff:ffff", false)]
    [TestCase("2003::", true)]
    [TestCase("2620:4f:7fff:ffff:ffff:ffff:ffff:ffff", true)]
    [TestCase("2620:4f:8000::", false)]
    [TestCase("2620:4f:8000:ffff:ffff:ffff:ffff:ffff", false)]
    [TestCase("2620:4f:8001::", true)]
    [TestCase("3fff:fff:ffff:ffff:ffff:ffff:ffff:ffff", false)]
    [TestCase("3fff:1000::", true)]
    public void PublicPolicyAppliesIpv6SpecialUseBoundaries(string addressText, bool expected)
    {
        IWhoisEndpointPolicy policy = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Parse(addressText), WhoisEndpointSource.InitialSelection);

        Assert.That(allowed, Is.EqualTo(expected));
    }

    [TestCase(1)]
    [TestCase(42)]
    [TestCase(44)]
    [TestCase(1043)]
    [TestCase(4320)]
    [TestCase(4322)]
    [TestCase(65535)]
    public void PublicPolicyRejectsNonstandardReferralPorts(int port)
    {
        IWhoisEndpointPolicy policy = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Port = port;

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Parse("8.8.8.8"), WhoisEndpointSource.Referral);

        Assert.That(allowed, Is.False);
    }

    [Test]
    public void PublicPolicyRejectsNonstandardInitialSelectionPort()
    {
        IWhoisEndpointPolicy policy = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Port = 1043;

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Parse("8.8.8.8"), WhoisEndpointSource.InitialSelection);

        Assert.That(allowed, Is.False);
    }

    [TestCase(1)]
    [TestCase(1043)]
    [TestCase(65535)]
    public void PublicPolicyAllowsValidNonstandardPortForPublicDirectEndpoint(int port)
    {
        IWhoisEndpointPolicy policy = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Port = port;

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Parse("8.8.8.8"), WhoisEndpointSource.Direct);

        Assert.That(allowed, Is.True);
    }

    [TestCase("10.0.0.1", 43, WhoisEndpointSource.Direct)]
    [TestCase("8.8.8.8", 0, WhoisEndpointSource.Direct)]
    [TestCase("8.8.8.8", 65536, WhoisEndpointSource.Direct)]
    [TestCase("8.8.8.8", 43, (WhoisEndpointSource)99)]
    public void PublicPolicyRejectsIneligibleDirectOrUnknownSource(string addressText, int port, WhoisEndpointSource source)
    {
        IWhoisEndpointPolicy policy = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        endpoint.Port = port;

        bool allowed = policy.AllowEndpoint(endpoint, System.Net.IPAddress.Parse(addressText), source);

        Assert.That(allowed, Is.False);
    }

    [Test]
    public void PoliciesRejectMissingInputs()
    {
        IWhoisEndpointPolicy allowAll = new WhoisAllowAllEndpointPolicy();
        IWhoisEndpointPolicy publicOnly = new WhoisPublicEndpointPolicy();
        WhoisEndpoint endpoint = new WhoisEndpoint();
        System.Net.IPAddress address = System.Net.IPAddress.Loopback;

        Assert.Throws<ArgumentNullException>(() => allowAll.AllowEndpoint(null!, address, WhoisEndpointSource.Direct));
        Assert.Throws<ArgumentNullException>(() => allowAll.AllowEndpoint(endpoint, null!, WhoisEndpointSource.Direct));
        Assert.Throws<ArgumentNullException>(() => publicOnly.AllowEndpoint(null!, address, WhoisEndpointSource.Direct));
        Assert.Throws<ArgumentNullException>(() => publicOnly.AllowEndpoint(endpoint, null!, WhoisEndpointSource.Direct));
    }
}
