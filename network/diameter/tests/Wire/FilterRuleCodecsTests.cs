using System.Text;

namespace diameter.Tests;

public class FilterRuleCodecsTests
{
    [Test]
    public void EnumeratedCodecUsesSignedInteger32WireRepresentation()
    {
        byte[] minimumBytes = new byte[4];
        byte[] maximumBytes = new byte[4];

        DiameterValueResult minimumEncoding = ValueCodecs.EncodeEnumerated(
            minimumBytes,
            int.MinValue);
        DiameterValueResult maximumEncoding = ValueCodecs.EncodeEnumerated(
            maximumBytes,
            int.MaxValue);
        DiameterValueResult minimumDecoding = ValueCodecs.DecodeEnumerated(minimumBytes);
        DiameterValueResult maximumDecoding = ValueCodecs.DecodeEnumerated(maximumBytes);

        Assert.Multiple(() =>
        {
            Assert.That(minimumEncoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(maximumEncoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(minimumBytes, Is.EqualTo(new byte[] { 128, 0, 0, 0 }));
            Assert.That(maximumBytes, Is.EqualTo(new byte[] { 127, 255, 255, 255 }));
            Assert.That(minimumDecoding.Integer32, Is.EqualTo(int.MinValue));
            Assert.That(maximumDecoding.Integer32, Is.EqualTo(int.MaxValue));
            Assert.That(minimumDecoding.ConsumedCount, Is.EqualTo(4));
            Assert.That(maximumDecoding.ConsumedCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void EnumeratedCodecReportsBufferFailures()
    {
        byte[] destination = new byte[] { 170, 170, 170 };

        DiameterValueResult encoding = ValueCodecs.EncodeEnumerated(destination, 1);
        DiameterValueResult decoding = ValueCodecs.DecodeEnumerated(new byte[3]);

        Assert.Multiple(() =>
        {
            Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.DestinationTooSmall));
            Assert.That(encoding.RequiredCount, Is.EqualTo(4));
            Assert.That(destination, Is.All.EqualTo(170));
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
            Assert.That(decoding.RequiredCount, Is.EqualTo(4));
        });
    }

    [TestCase("permit out ip from any to assigned")]
    [TestCase("deny in 6 from 192.0.2.0/24 80,443 to !198.51.100.0/24 1024-65535 established tcpflags syn,!ack")]
    [TestCase("permit in 17 from 2001:db8::/32 53 to any")]
    [TestCase("permit in 1 from any to 192.0.2.1 icmptypes 0,3-5,echo")]
    [TestCase("permit in 1 from any to any icmptypes echo reply,destination unreachable")]
    [TestCase("permit out ip from any to any ipoptions ssrr,!rr")]
    public void IpFilterRuleCodecRoundTripsValidStandardRules(string rule)
    {
        byte[] destination = new byte[Encoding.ASCII.GetByteCount(rule)];

        DiameterValueResult encoding = ValueCodecs.EncodeIpFilterRule(destination, rule);
        DiameterValueResult decoding = ValueCodecs.DecodeIpFilterRule(destination);

        Assert.Multiple(() =>
        {
            Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encoding.WrittenCount, Is.EqualTo(destination.Length));
            Assert.That(encoding.Text, Is.EqualTo(rule));
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoding.ConsumedCount, Is.EqualTo(destination.Length));
            Assert.That(decoding.Text, Is.EqualTo(rule));
        });
    }

    [TestCase("allow out ip from any to any")]
    [TestCase("permit sideways ip from any to any")]
    [TestCase("permit out tcp from any to any")]
    [TestCase("permit out ip from 192.0.2.1/24 to any")]
    [TestCase("permit out ip from 192.0.2.0/33 to any")]
    [TestCase("permit out ip from any 80 to any")]
    [TestCase("permit out 6 from any 100-50 to any")]
    [TestCase("permit out 6 from any 80 to any frag")]
    [TestCase("permit out 17 from any to any tcpflags syn")]
    [TestCase("permit out 6 from any to any icmptypes 8")]
    [TestCase("permit out ip from any to any mystery")]
    public void IpFilterRuleCodecRejectsMalformedOrContradictoryRules(string rule)
    {
        DiameterValueResult result = ValueCodecs.EncodeIpFilterRule(new byte[256], rule);

        AssertInvalidFilter(result, "invalid-ip-filter-rule");
    }

    [Test]
    public void IpFilterRuleCodecRejectsMissingNonAsciiAndShortDestinationValues()
    {
        string validRule = "permit out ip from any to any";
        byte[] invalidAscii = Encoding.UTF8.GetBytes("permit out ip from any to café");
        byte[] destination = new byte[] { 170, 170, 170 };

        DiameterValueResult missing = ValueCodecs.EncodeIpFilterRule(new byte[0], null);
        DiameterValueResult nonAscii = ValueCodecs.DecodeIpFilterRule(invalidAscii);
        DiameterValueResult shortDestination = ValueCodecs.EncodeIpFilterRule(
            destination,
            validRule);

        Assert.Multiple(() =>
        {
            AssertInvalidFilter(missing, "invalid-ip-filter-rule");
            AssertInvalidFilter(nonAscii, "invalid-ip-filter-rule");
            Assert.That(shortDestination.Status, Is.EqualTo(DiameterCodecStatus.DestinationTooSmall));
            Assert.That(shortDestination.RequiredCount, Is.EqualTo(validRule.Length));
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    [TestCase("tag out 17 from 192.0.2.0/24 53 to any DSCP AF21")]
    [TestCase("tag in ip from any to assigned DSCP 46")]
    [TestCase("meter in ip from any to assigned metering 1000000 AF11 drop")]
    [TestCase("meter out 6 from any to 2001:db8::1 443 metering 250000 CS1 EF")]
    public void QosFilterRuleCodecRoundTripsActionSpecificRules(string rule)
    {
        byte[] destination = new byte[rule.Length];

        DiameterValueResult encoding = ValueCodecs.EncodeQosFilterRule(destination, rule);
        DiameterValueResult decoding = ValueCodecs.DecodeQosFilterRule(destination);

        Assert.Multiple(() =>
        {
            Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encoding.WrittenCount, Is.EqualTo(rule.Length));
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoding.ConsumedCount, Is.EqualTo(rule.Length));
            Assert.That(decoding.Text, Is.EqualTo(rule));
        });
    }

    [TestCase("tag out ip from any to any")]
    [TestCase("tag out ip from any to any metering 100 AF11 AF12")]
    [TestCase("tag out ip from any to any DSCP AF49")]
    [TestCase("tag out ip from any to any DSCP drop")]
    [TestCase("meter out ip from any to any DSCP EF")]
    [TestCase("meter out ip from any to any metering 0 AF11 AF12")]
    [TestCase("meter out ip from any to any metering 100 drop AF12")]
    [TestCase("meter out ip from any to any metering 100 AF11 BAD")]
    public void QosFilterRuleCodecRejectsWrongOrMalformedActionOptions(string rule)
    {
        DiameterValueResult result = ValueCodecs.DecodeQosFilterRule(
            Encoding.ASCII.GetBytes(rule));

        AssertInvalidFilter(result, "invalid-qos-filter-rule");
    }

    private static void AssertInvalidFilter(DiameterValueResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(code));
            Assert.That(result.WrittenCount, Is.EqualTo(0));
            Assert.That(result.ConsumedCount, Is.EqualTo(0));
        });
    }
}
