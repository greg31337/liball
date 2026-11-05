namespace sdp.Tests.Unit.Validation;

public class SdpEffectiveValueValidationTests
{
    [Test]
    public void SourceFilters_ValidateApplicableDestinationsRangesAndUniqueCoverage()
    {
        SdpSessionDescription description = DescriptionWithMediaCount(1);
        SdpConnectionData connection = Connection("232.3.4.5");
        connection.AddressKind = SdpAddressKind.Ipv4Multicast;
        connection.HasAddressCount = true;
        connection.AddressCount = 3;
        description.Connection = connection;
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "incl IN IP4 232.3.4.6 192.0.2.10"));

        SdpValidationResult valid = ValidateRtp(description);

        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "excl IN IP4 232.3.4.6 192.0.2.11"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "incl IN IP4 232.3.4.9 192.0.2.12"));
        SdpValidationResult invalid = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Issues.Where(issue => issue.AttributeName == SdpAttributeNames.SourceFilter), Is.Empty);
            Assert.That(invalid.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality && issue.AttributeName == SdpAttributeNames.SourceFilter), Is.EqualTo(1));
            Assert.That(invalid.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationConnection && issue.AttributeName == SdpAttributeNames.SourceFilter), Is.EqualTo(1));
        });
    }

    [Test]
    public void MediaSourceFilters_CompletelyOverrideSessionFilters()
    {
        SdpSessionDescription description = DescriptionWithMediaCount(1);
        description.Connection = Connection("192.0.2.20");
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "incl IN IP4 192.0.2.20 198.51.100.1"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "excl IN IP4 192.0.2.20 198.51.100.2"));

        SdpValidationResult result = ValidateRtp(description);
        List<SdpSourceFilterAttribute> filters = new SdpQueryService().ResolveEffectiveSourceFilters(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Where(issue => issue.AttributeName == SdpAttributeNames.SourceFilter), Is.Empty);
            Assert.That(filters.Count, Is.EqualTo(1));
            Assert.That(filters[0].FilterMode, Is.EqualTo("excl"));
        });
    }

    [Test]
    public void ReferenceClocks_RejectMixedTraceabilityAndIncompleteEffectiveCoverage()
    {
        SdpSessionDescription mixed = DescriptionWithMediaCount(1);
        mixed.Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "local"));
        mixed.Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "gps"));
        SdpSessionDescription incomplete = DescriptionWithMediaCount(2);
        incomplete.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "local"));

        SdpValidationResult mixedResult = ValidateRtp(mixed);
        SdpValidationResult incompleteResult = ValidateRtp(incomplete);

        Assert.Multiple(() =>
        {
            Assert.That(mixedResult.Issues.Count(issue => issue.AttributeName == SdpAttributeNames.TimestampReferenceClock && issue.Detail.Contains("must not be mixed")), Is.EqualTo(1));
            Assert.That(incompleteResult.Issues.Count(issue => issue.AttributeName == SdpAttributeNames.TimestampReferenceClock && issue.MediaIndex == 1), Is.EqualTo(1));
        });
    }

    [Test]
    public void DirectMediaClock_RequiresAnEffectiveReferenceClock()
    {
        SdpSessionDescription invalid = DescriptionWithMediaCount(1);
        invalid.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaClock, "direct=42"));
        SdpSessionDescription valid = DescriptionWithMediaCount(1);
        valid.Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "local"));
        valid.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaClock, "direct=42"));

        SdpValidationResult invalidResult = ValidateRtp(invalid);
        SdpValidationResult validResult = ValidateRtp(valid);

        Assert.Multiple(() =>
        {
            Assert.That(invalidResult.Issues.Count(issue => issue.AttributeName == SdpAttributeNames.MediaClock && issue.Detail.Contains("requires an effective")), Is.EqualTo(1));
            Assert.That(validResult.Issues.Where(issue => issue.AttributeName == SdpAttributeNames.MediaClock), Is.Empty);
        });
    }

    [Test]
    public void ClockAttributes_RequireRtpMediaWhenMediaScoped()
    {
        SdpSessionDescription description = DescriptionWithMediaCount(1);
        description.MediaDescriptions[0].Protocol = SdpTransportProtocols.UdpDtlsSctp;
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "local"));
        description.MediaDescriptions[0].Attributes.Add(ValueAttribute(SdpAttributeNames.MediaClock, "sender"));

        SdpValidationResult result = ValidateRtp(description);

        Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope && (issue.AttributeName == SdpAttributeNames.TimestampReferenceClock || issue.AttributeName == SdpAttributeNames.MediaClock)), Is.EqualTo(2));
    }

    private static SdpSessionDescription DescriptionWithMediaCount(int count)
    {
        SdpSessionDescription description = new SdpSessionDescription();
        int index = 0;
        while (index < count)
        {
            SdpMediaDescription media = new SdpMediaDescription();
            media.MediaType = SdpMediaTypes.Audio;
            media.MediaKind = SdpMediaKind.Audio;
            media.Port = 5000 + index * 2;
            media.Protocol = SdpTransportProtocols.RtpAvp;
            media.Formats.Add("0");
            description.MediaDescriptions.Add(media);
            index = index + 1;
        }
        return description;
    }

    private static SdpConnectionData Connection(string address)
    {
        SdpConnectionData connection = new SdpConnectionData();
        connection.NetworkType = SdpNetworkTypes.Internet;
        connection.AddressType = SdpAddressTypes.Ip4;
        connection.Address = address;
        connection.RawAddress = address;
        connection.RawText = "IN IP4 " + address;
        connection.AddressKind = SdpAddressKind.Ipv4;
        return connection;
    }

    private static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    private static SdpValidationResult ValidateRtp(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rtp });
    }
}
