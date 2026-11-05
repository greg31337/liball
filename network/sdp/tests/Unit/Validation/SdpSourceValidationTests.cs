namespace sdp.Tests.Unit.Validation;

public class SdpSourceValidationTests
{
    [Test]
    public void ValidSourcesAndGroups_AcceptUnsignedBoundsAndExtensionSemantics()
    {
        SdpSessionDescription description = RtpDescription();
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "0 cname:participant@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "0 msid:stream track");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "4294967295 cname:participant@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "4294967295 msid:stream track");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, "FID 0 4294967295");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, "X-CUSTOM 0");

        SdpValidationResult result = ValidateSources(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void SourceAndGroupIdentifiers_ReportInvalidAndOverflowRanges()
    {
        SdpSessionDescription description = RtpDescription();
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "-1 cname:participant@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "4294967296 cname:participant@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, "FID 0 4294967296");

        SdpValidationResult result = ValidateSources(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.Select(issue => issue.Code), Is.EqualTo(new[]
            {
                SdpDiagnosticCodes.ParseInvalidNumber,
                SdpDiagnosticCodes.ParseNumberOverflow,
                SdpDiagnosticCodes.ParseNumberOverflow
            }));
            Assert.That(result.Issues.Select(issue => issue.Path.OccurrenceIndex), Is.EqualTo(new[] { 0, 1, 0 }));
        });
    }

    [Test]
    public void EverySourceRequiresCname_AndSingletonAttributesRejectDuplicatesAndConflicts()
    {
        SdpSessionDescription description = RtpDescription();
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "10 msid:stream track");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "10 msid:stream track");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "20 cname:first@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "20 cname:second@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "30 cname:third@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "30 previous-ssrc:1");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "30 previous-ssrc:1");

        SdpValidationResult result = ValidateSources(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(4));
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationSource), Is.EqualTo(2));
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(2));
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("must have a cname")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("conflicting values")), Is.True);
        });
    }

    [Test]
    public void RegisteredGroups_RequireDistinctMembersAndLocalSourceReferences()
    {
        SdpSessionDescription description = RtpDescription();
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "100 cname:participant@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, "FID 100");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, "FEC-FR 100 100");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, "DUP 100 200");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, SdpGroupSemantics.Simulcast + " 100");

        SdpValidationResult result = ValidateSources(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(5));
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(3));
            Assert.That(result.Issues.Count(issue => issue.Detail.Contains("repeated within one source group")), Is.EqualTo(1));
            Assert.That(result.Issues.Count(issue => issue.Detail.Contains("same media section")), Is.EqualTo(1));
        });
    }

    [Test]
    public void PreviousSourceIdentifiers_RequireUnsignedUniqueValues()
    {
        SdpSessionDescription description = RtpDescription();
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "10 cname:participant@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "10 previous-ssrc:1 1");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "20 cname:participant@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "20 previous-ssrc:4294967296");

        SdpValidationResult result = ValidateSources(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationSource));
            Assert.That(result.Issues[0].Detail, Does.Contain("must not be repeated"));
            Assert.That(result.Issues[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
        });
    }

    [Test]
    public void DuplicateGroupsAndInconsistentGroupedAssociations_AreRejected()
    {
        SdpSessionDescription description = RtpDescription();
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "1 cname:first@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "1 msid:stream track-one");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "2 cname:second@example.test");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "2 msid:stream track-two");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, "DUP 1 2");
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSourceGroup, "DUP 1 2");

        SdpValidationResult result = ValidateSources(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(5));
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(1));
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationSource), Is.EqualTo(4));
            Assert.That(result.Issues.Count(issue => issue.Detail.Contains("consistent cname and msid")), Is.EqualTo(4));
        });
    }

    [Test]
    public void SourceAttributes_RequireMediaScopeAndRtpTransport()
    {
        SdpSessionDescription description = RtpDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.SynchronizationSource, "1 cname:session@example.test"));
        SdpMediaDescription nonRtp = new SdpMediaDescription { MediaType = "application", Protocol = "UDP/DTLS/SCTP", Port = 5002 };
        nonRtp.Attributes.Add(ValueAttribute(SdpAttributeNames.SynchronizationSourceGroup, "FID 1 2"));
        description.MediaDescriptions.Add(nonRtp);

        SdpValidationResult result = ValidateSources(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.True);
            Assert.That(result.Issues.Select(issue => issue.Scope), Is.EqualTo(new[] { SdpScope.Session, SdpScope.Media }));
            Assert.That(result.Issues[1].MediaIndex, Is.EqualTo(1));
        });
    }

    [TestCase(SdpValidationProfile.Rtp, true)]
    [TestCase(SdpValidationProfile.WebRtc, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true)]
    [TestCase(SdpValidationProfile.SyntaxOnly, false)]
    [TestCase(SdpValidationProfile.Bundle, false)]
    public void SourceRules_RunOnlyForProfilesThatIncludeRtp(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = RtpDescription();
        AddValueAttribute(description.MediaDescriptions[0], SdpAttributeNames.SynchronizationSource, "1 label:camera");

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationSource), Is.EqualTo(expectedIssue));
    }

    static SdpValidationResult ValidateSources(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rtp });
    }

    static SdpSessionDescription RtpDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = 5000,
            Protocol = "RTP/AVP",
            Location = new SdpSourceLocation { LineNumber = 4, ColumnNumber = 1 }
        };
        media.Formats.Add("0");
        description.MediaDescriptions.Add(media);
        return description;
    }

    static void AddValueAttribute(SdpMediaDescription media, string name, string value)
    {
        media.Attributes.Add(ValueAttribute(name, value));
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value,
            Location = new SdpSourceLocation { LineNumber = 5, ColumnNumber = 1 }
        };
    }
}
