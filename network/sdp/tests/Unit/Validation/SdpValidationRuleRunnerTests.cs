namespace sdp.Tests.Unit.Validation;

public class SdpValidationRuleRunnerTests
{
    [TestCase(SdpValidationProfile.Rtp, true, false)]
    [TestCase(SdpValidationProfile.Ice, false, true)]
    [TestCase(SdpValidationProfile.WebRtc, true, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true, true)]
    public void SelectedProfiles_RunOnlyTheirIncludedRuleSets(SdpValidationProfile profile, bool expectRtp, bool expectIce)
    {
        SdpSessionDescription description = DescriptionWithIndependentFailures();
        ISdpValidator validator = new SdpValidator();

        SdpValidationResult result = validator.ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap), Is.EqualTo(expectRtp));
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationIce), Is.EqualTo(expectIce));
        });
    }

    [Test]
    public void SyntaxOnlyProfile_DoesNotRunSemanticRuleSets()
    {
        SdpSessionDescription description = DescriptionWithIndependentFailures();
        ISdpValidator validator = new SdpValidator();

        SdpValidationResult result = validator.ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.SyntaxOnly });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void CombinedStrictProfile_IntegratesCoreAndSpecializedRuleSets()
    {
        SdpSessionDescription description = DescriptionWithIndependentFailures();
        SdpMediaDescription media = description.MediaDescriptions[0];
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Group, "BUNDLE missing"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Setup, "invalid-role"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMap, "0 urn:example:invalid"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.SctpPort, "invalid"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.FloorControl, "invalid"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.SynchronizationSource, "1 label:camera"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.SourceFilter, "incl IN IP4"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.TimestampReferenceClock, "invalid value"));
        media.Attributes.Add(PropertyAttribute(SdpAttributeNames.BundleOnly));

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.CombinedStrict });

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ParseMissingRequiredField), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDynamicPayloadMissingRtpMap), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationIce), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDtls), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationExtMap), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationGroup), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationBundle), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationSource), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationDataChannel), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationBfcp), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.SourceFilter), Is.True);
            Assert.That(result.Issues.Any(issue => issue.AttributeName == SdpAttributeNames.TimestampReferenceClock), Is.True);
        });
    }

    [Test]
    public void CombinedStrictProfile_DeduplicatesOneWrongScopeRootAcrossRuleSets()
    {
        SdpSessionDescription description = DescriptionWithIndependentFailures();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.PacketTime, "20"));

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.CombinedStrict });

        List<SdpValidationIssue> packetTimeIssues = result.Issues.Where(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope && issue.AttributeName == SdpAttributeNames.PacketTime).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(packetTimeIssues, Has.Count.EqualTo(1));
            Assert.That(packetTimeIssues[0].Detail, Does.Contain("media scope"));
        });
    }

    [Test]
    public void CombinedStrictProfile_StopsAfterFatalCoreStructureEvenAtDiagnosticLimit()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(null!);
        SdpValidationOptions options = new SdpValidationOptions
        {
            Profile = SdpValidationProfile.CombinedStrict,
            MaximumDiagnosticCount = 1
        };

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.DiagnosticLimitReached, Is.True);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
        });
    }

    private static SdpSessionDescription DescriptionWithIndependentFailures()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription();
        media.MediaType = SdpMediaTypes.Audio;
        media.MediaKind = SdpMediaKind.Audio;
        media.Port = 5000;
        media.Protocol = SdpTransportProtocols.RtpAvp;
        media.Formats.Add("96");
        media.Attributes.Add(new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = SdpAttributeNames.Candidate,
            Value = "malformed",
            RawText = SdpAttributeNames.Candidate + ":malformed"
        });
        description.MediaDescriptions.Add(media);
        return description;
    }

    private static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value
        };
    }

    private static SdpAttribute PropertyAttribute(string name)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Property,
            Name = name,
            RawText = name
        };
    }
}
