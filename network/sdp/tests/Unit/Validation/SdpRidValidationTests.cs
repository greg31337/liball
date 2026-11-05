namespace sdp.Tests.Unit.Validation;

public class SdpRidValidationTests
{
    [Test]
    public void ValidRids_AcceptPayloadsDependenciesExtensionsAndNumericBoundaries()
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], "base send pt=31;max-width=0;max-bpp=0.0001;vendor-mode=compact");
        AddRid(description.MediaDescriptions[0], "high recv pt=31;max-height=2160;max-fps=60;max-fs=8294400;max-br=18446744073709551615;max-pps=497664000;max-bpp=48.0000;depend=base");

        SdpValidationResult result = ValidateRids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [TestCase("bad.id send")]
    [TestCase("good SEND")]
    [TestCase("good send pt=128")]
    [TestCase("good send depend=base;")]
    public void MalformedRidSyntax_IsReportedByProfileValidation(string value)
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], value);

        SdpValidationResult result = ValidateRids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo(SdpAttributeNames.Rid));
        });
    }

    [Test]
    public void RidIdentifiersAndRestrictionNames_MustBeUniqueAndUseRegisteredCase()
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], "one send max-width=1280;max-width=640;MAX-BR=1000");
        AddRid(description.MediaDescriptions[0], "one recv");

        SdpValidationResult result = ValidateRids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(2));
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationRid && issue.Detail.Contains("case-sensitive")), Is.True);
        });
    }

    [Test]
    public void RidPayloadLists_RejectDuplicatesAndMissingMediaFormats()
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], "one send pt=31,31,96");

        SdpValidationResult result = ValidateRids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationPayloadType));
            Assert.That(result.Issues[0].Detail, Does.Contain("must not be repeated"));
            Assert.That(result.Issues[1].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationDanglingPayloadReference));
        });
    }

    [Test]
    public void NumericRestrictions_RejectInvalidOverflowAndOutOfRangeValues()
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], "overflow send max-width=18446744073709551616");
        AddRid(description.MediaDescriptions[0], "negative send max-height=-1");
        AddRid(description.MediaDescriptions[0], "zero send max-bpp=0.0000");
        AddRid(description.MediaDescriptions[0], "large send max-bpp=48.0001");
        AddRid(description.MediaDescriptions[0], "precise send max-bpp=1.00000");

        SdpValidationResult result = ValidateRids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(5));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ParseNumberOverflow));
            Assert.That(result.Issues[1].Code, Is.EqualTo(SdpDiagnosticCodes.ParseInvalidNumber));
            Assert.That(result.Issues.Skip(2).All(issue => issue.Code == SdpDiagnosticCodes.ValidationRid), Is.True);
            Assert.That(result.Issues.Skip(2).All(issue => issue.Detail.Contains("0.0001 through 48.0")), Is.True);
        });
    }

    [Test]
    public void Dependencies_RequireUniqueLocalReferencesAndAnAcyclicGraph()
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], "base send");
        AddRid(description.MediaDescriptions[0], "missing send depend=unknown");
        AddRid(description.MediaDescriptions[0], "repeated send depend=base,base");
        AddRid(description.MediaDescriptions[0], "a send depend=b");
        AddRid(description.MediaDescriptions[0], "b send depend=c");
        AddRid(description.MediaDescriptions[0], "c send depend=a");

        SdpValidationResult result = ValidateRids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationRid), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("unambiguously identify")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("must not repeat")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("must not contain a cycle")), Is.True);
        });
    }

    [Test]
    public void DependRestriction_RequiresAValue()
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], "one send depend");

        SdpValidationResult result = ValidateRids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationRid));
            Assert.That(result.Issues[0].Detail, Does.Contain("requires a comma-separated"));
        });
    }

    [Test]
    public void RidAttributes_RequireMediaScopeAndRtpTransport()
    {
        SdpSessionDescription description = VideoDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Rid, "session send"));
        SdpMediaDescription data = new SdpMediaDescription { MediaType = "application", Protocol = "UDP/DTLS/SCTP", Port = 5002 };
        AddRid(data, "data send");
        description.MediaDescriptions.Add(data);

        SdpValidationResult result = ValidateRids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.True);
            Assert.That(result.Issues.Select(issue => issue.Scope), Is.EqualTo(new[] { SdpScope.Session, SdpScope.Media }));
        });
    }

    [TestCase(SdpValidationProfile.Rtp, true)]
    [TestCase(SdpValidationProfile.WebRtc, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true)]
    [TestCase(SdpValidationProfile.SyntaxOnly, false)]
    [TestCase(SdpValidationProfile.Bundle, false)]
    public void RidRules_RunOnlyForProfilesThatIncludeRtp(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], "one send pt=96");

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationRid || issue.Code == SdpDiagnosticCodes.ValidationDanglingPayloadReference), Is.EqualTo(expectedIssue));
    }

    static SdpValidationResult ValidateRids(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rtp });
    }

    static SdpSessionDescription VideoDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Video,
            MediaKind = SdpMediaKind.Video,
            Port = 5000,
            Protocol = "RTP/AVP",
            Location = new SdpSourceLocation { LineNumber = 4, ColumnNumber = 1 }
        };
        media.Formats.Add("31");
        description.MediaDescriptions.Add(media);
        return description;
    }

    static void AddRid(SdpMediaDescription media, string value)
    {
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Rid, value));
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
