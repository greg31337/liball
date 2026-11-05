using whois;

namespace whois.Tests;

public class AddressClassifierTests
{
    [TestCase("1.1.1.1")]
    [TestCase("8.8.8.8")]
    [TestCase("100.63.255.255")]
    [TestCase("100.128.0.0")]
    [TestCase("172.15.255.255")]
    [TestCase("172.32.0.0")]
    [TestCase("198.17.255.255")]
    [TestCase("198.20.0.0")]
    [TestCase("223.255.255.255")]
    public void AcceptsOrdinaryPublicIpv4Addresses(string text)
    {
        Assert.That(AddressClassifier.IsPublicUnicast(System.Net.IPAddress.Parse(text)), Is.True);
    }

    [TestCase("0.0.0.0")]
    [TestCase("0.255.255.255")]
    [TestCase("10.0.0.1")]
    [TestCase("100.64.0.0")]
    [TestCase("100.127.255.255")]
    [TestCase("127.0.0.1")]
    [TestCase("169.254.1.1")]
    [TestCase("172.16.0.0")]
    [TestCase("172.31.255.255")]
    [TestCase("192.168.1.1")]
    public void RejectsIpv4UnspecifiedPrivateLoopbackLinkLocalAndSharedAddresses(string text)
    {
        Assert.That(AddressClassifier.IsPublicUnicast(System.Net.IPAddress.Parse(text)), Is.False);
    }

    [TestCase("192.0.0.9")]
    [TestCase("192.0.2.1")]
    [TestCase("192.31.196.1")]
    [TestCase("192.52.193.1")]
    [TestCase("192.88.99.1")]
    [TestCase("192.175.48.1")]
    [TestCase("198.18.0.0")]
    [TestCase("198.19.255.255")]
    [TestCase("198.51.100.1")]
    [TestCase("203.0.113.1")]
    [TestCase("224.0.0.1")]
    [TestCase("239.255.255.255")]
    [TestCase("240.0.0.0")]
    [TestCase("255.255.255.255")]
    public void RejectsIpv4DocumentationBenchmarkingMulticastAndOtherSpecialUse(string text)
    {
        Assert.That(AddressClassifier.IsPublicUnicast(System.Net.IPAddress.Parse(text)), Is.False);
    }

    [TestCase("2001:200::1")]
    [TestCase("2001:db7:ffff::1")]
    [TestCase("2001:db9::1")]
    [TestCase("2003::1")]
    [TestCase("2606:4700:4700::1111")]
    [TestCase("2620:4f:7fff::1")]
    [TestCase("2620:4f:8001::1")]
    public void AcceptsOrdinaryPublicIpv6Addresses(string text)
    {
        Assert.That(AddressClassifier.IsPublicUnicast(System.Net.IPAddress.Parse(text)), Is.True);
    }

    [TestCase("::")]
    [TestCase("::1")]
    [TestCase("::ffff:8.8.8.8")]
    [TestCase("64:ff9b::1")]
    [TestCase("100::1")]
    [TestCase("fc00::1")]
    [TestCase("fdff:ffff::1")]
    [TestCase("fe80::1")]
    [TestCase("fec0::1")]
    [TestCase("ff02::1")]
    [TestCase("4000::1")]
    public void RejectsIpv6UnspecifiedMappedPrivateLinkLocalMulticastAndReservedAddresses(string text)
    {
        Assert.That(AddressClassifier.IsPublicUnicast(System.Net.IPAddress.Parse(text)), Is.False);
    }

    [TestCase("2001::1")]
    [TestCase("2001:1::1")]
    [TestCase("2001:2::1")]
    [TestCase("2001:20::1")]
    [TestCase("2001:db8::1")]
    [TestCase("2002::1")]
    [TestCase("2620:4f:8000::1")]
    [TestCase("3ffe::1")]
    [TestCase("3fff::1")]
    [TestCase("5f00::1")]
    public void RejectsIpv6DocumentationBenchmarkingTransitionAndOtherSpecialUse(string text)
    {
        Assert.That(AddressClassifier.IsPublicUnicast(System.Net.IPAddress.Parse(text)), Is.False);
    }

    [Test]
    public void RejectsMissingAddress()
    {
        Assert.Throws<ArgumentNullException>(() => AddressClassifier.IsPublicUnicast(null));
    }
}
