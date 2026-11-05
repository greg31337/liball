namespace sdp.Tests.Unit.Validation;

public class SdpCoreValidationTests
{
    [Test]
    public void ValidCoreDescription_IsAccepted()
    {
        SdpSessionDescription description = ValidDescription();

        SdpValidationResult result = Validate(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void RequiredFieldsAndProtocolVersion_AreValidated()
    {
        SdpSessionDescription description = new SdpSessionDescription
        {
            HasProtocolVersion = true,
            ProtocolVersion = 1
        };

        SdpValidationResult result = Validate(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationProtocolVersion), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ParseMissingRequiredField && issue.FieldName == 'o'), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ParseMissingRequiredField && issue.FieldName == 's'), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ParseMissingRequiredField && issue.FieldName == 't'), Is.True);
        });
    }

    [Test]
    public void OriginIdentifiersAndUnicastAddress_AreValidated()
    {
        SdpSessionDescription description = ValidDescription();
        description.Origin!.SessionId = "not-a-number";
        description.Origin.SessionVersion = "18446744073709551616";
        description.Origin.UnicastAddress = "224.0.0.1";
        description.Origin.AddressKind = SdpAddressKind.Ipv4Multicast;

        SdpValidationResult result = Validate(description);

        Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationOrigin), Is.EqualTo(3));
    }

    [Test]
    public void ConnectionsRequireValidAddressStateAndCoverActiveMedia()
    {
        SdpSessionDescription active = ValidDescription();
        active.Connection = null;
        SdpSessionDescription rejected = ValidDescription();
        rejected.Connection = null;
        rejected.MediaDescriptions[0].Port = 0;
        SdpSessionDescription invalidConnection = ValidDescription();
        invalidConnection.Connection!.HasTimeToLive = true;
        invalidConnection.Connection.TimeToLive = 10;

        SdpValidationResult activeResult = Validate(active);
        SdpValidationResult rejectedResult = Validate(rejected);
        SdpValidationResult connectionResult = Validate(invalidConnection);

        Assert.Multiple(() =>
        {
            Assert.That(activeResult.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationMissingConnection), Is.True);
            Assert.That(rejectedResult.Success, Is.True);
            Assert.That(rejectedResult.Issues, Is.Empty);
            Assert.That(connectionResult.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationConnection), Is.True);
        });
    }

    [Test]
    public void TimingPeriodsRepeatsAndZoneAdjustments_AreValidated()
    {
        SdpSessionDescription description = ValidDescription();
        description.Timings[0].StartTime = "1234567890";
        description.Timings[0].StopTime = "1234567889";
        description.Timings[0].RepeatTimes.Add(new SdpRepeatTime
        {
            RepeatInterval = new SdpTypedTime { Value = 0 },
            ActiveDuration = new SdpTypedTime { Value = 60 }
        });
        description.TimeZoneAdjustments.Add(new SdpTimeZoneAdjustment
        {
            AdjustmentTime = "1",
            Offset = new SdpTypedTime { Value = 1, Unit = "weeks" }
        });

        SdpValidationResult result = Validate(description);

        Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationTiming), Is.EqualTo(3));
    }

    [Test]
    public void MediaPortsProtocolsAndFormats_AreValidated()
    {
        SdpSessionDescription description = ValidDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        media.Port = 65535;
        media.HasPortCount = true;
        media.PortCount = 2;
        media.Protocol = "RTP//AVP";
        media.Formats.Add("0");

        SdpValidationResult result = Validate(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationPort), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationMedia), Is.True);
        });
    }

    [Test]
    public void BandwidthAndAttributeScopeCardinality_AreValidated()
    {
        SdpSessionDescription description = ValidDescription();
        description.Bandwidths.Add(new SdpBandwidth { Type = "AS", Value = -1 });
        description.Attributes.Add(ValueAttribute("ptime", "20"));
        description.Attributes.Add(PropertyAttribute("sendonly"));
        description.Attributes.Add(PropertyAttribute("recvonly"));
        description.Attributes.Add(ValueAttribute("tool", "one"));
        description.Attributes.Add(ValueAttribute("tool", "two"));

        SdpValidationResult result = Validate(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationBandwidth), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope && issue.AttributeName == "ptime"), Is.True);
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(2));
        });
    }

    static SdpSessionDescription ValidDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription
        {
            HasProtocolVersion = true,
            ProtocolVersion = 0,
            Origin = new SdpOrigin
            {
                Username = "-",
                SessionId = "2890844526",
                SessionVersion = "2890842807",
                NetworkType = "IN",
                AddressType = "IP4",
                UnicastAddress = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            },
            HasSessionName = true,
            SessionName = "Core validation",
            Connection = new SdpConnectionData
            {
                NetworkType = "IN",
                AddressType = "IP4",
                Address = "192.0.2.1",
                AddressKind = SdpAddressKind.Ipv4
            }
        };
        description.Timings.Add(new SdpTiming { StartTime = "0", StopTime = "0" });
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = "audio",
            MediaKind = SdpMediaKind.Audio,
            Port = 49170,
            Protocol = "RTP/AVP"
        };
        media.Formats.Add("0");
        description.MediaDescriptions.Add(media);
        return description;
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Value, Name = name, Value = value, RawText = name + ":" + value };
    }

    static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute { Kind = SdpAttributeKind.Property, Name = name, RawText = name };
    }

    static SdpValidationResult Validate(SdpSessionDescription description)
    {
        SdpValidationOptions options = new SdpValidationOptions { Profile = SdpValidationProfile.Rfc8866 };
        return new SdpValidator().ValidateDescription(description, options);
    }
}
