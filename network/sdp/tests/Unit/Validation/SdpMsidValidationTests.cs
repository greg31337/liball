namespace sdp.Tests.Unit.Validation;

public class SdpMsidValidationTests
{
    [Test]
    public void MultipleStreamAssociations_AcceptIdenticalAppdataAndCurrentRfcWithoutSemanticDeclaration()
    {
        SdpSessionDescription description = DescriptionWithRtpMedia();
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream-one track-a");
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream-two track-a");
        description.MediaDescriptions.Add(RtpMedia(5002));
        AddMediaValue(description.MediaDescriptions[1], SdpAttributeNames.MediaStreamId, "stream-one track-b");

        SdpValidationResult result = ValidateMsids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void MsidIdentifiers_EnforceStreamAndAppdataLengthBoundaries()
    {
        string maximum = new string('a', 64);
        string tooLong = new string('b', 65);
        SdpSessionDescription description = DescriptionWithRtpMedia();
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, maximum + " " + maximum);
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, tooLong + " track");
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream " + tooLong);
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaStreamIdSemantic, "WMS " + tooLong));

        SdpValidationResult result = ValidateMsids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationAttribute), Is.True);
            Assert.That(result.Issues.All(issue => issue.Detail.Contains("64 token characters")), Is.True);
        });
    }

    [Test]
    public void OneMediaSection_RequiresIdenticalAppdataAndUniqueAssociations()
    {
        SdpSessionDescription description = DescriptionWithRtpMedia();
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream-one track-a");
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream-two track-b");
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream-one track-a");

        SdpValidationResult result = ValidateMsids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(2));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationMsid));
            Assert.That(result.Issues[0].Detail, Does.Contain("identical appdata"));
            Assert.That(result.Issues[1].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttributeCardinality));
            Assert.That(result.Issues[1].Path.OccurrenceIndex, Is.EqualTo(2));
        });
    }

    [Test]
    public void SameStreamAndAppdataPair_MustNotAppearInMultipleMediaSections()
    {
        SdpSessionDescription description = DescriptionWithRtpMedia();
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "shared track-a");
        description.MediaDescriptions.Add(RtpMedia(5002));
        AddMediaValue(description.MediaDescriptions[1], SdpAttributeNames.MediaStreamId, "other track-b");
        AddMediaValue(description.MediaDescriptions[1], SdpAttributeNames.MediaStreamId, "shared track-a");

        SdpValidationResult result = ValidateMsids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationMsid), Is.EqualTo(2));
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("identical appdata")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("multiple media sections") && issue.MediaIndex == 1 && issue.Path.OccurrenceIndex == 1), Is.True);
        });
    }

    [Test]
    public void ExplicitAndWildcardSemanticDeclarations_CoverMediaStreamIdentifiers()
    {
        SdpSessionDescription explicitDescription = DescriptionWithRtpMedia();
        AddMediaValue(explicitDescription.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream-one track");
        AddMediaValue(explicitDescription.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream-two track");
        explicitDescription.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaStreamIdSemantic, "WMS stream-one stream-two"));
        SdpSessionDescription wildcardDescription = DescriptionWithRtpMedia();
        AddMediaValue(wildcardDescription.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream-three track");
        wildcardDescription.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaStreamIdSemantic, "WMS *"));

        SdpValidationResult explicitResult = ValidateMsids(explicitDescription);
        SdpValidationResult wildcardResult = ValidateMsids(wildcardDescription);

        Assert.Multiple(() =>
        {
            Assert.That(explicitResult.Success, Is.True);
            Assert.That(explicitResult.Issues, Is.Empty);
            Assert.That(wildcardResult.Success, Is.True);
            Assert.That(wildcardResult.Issues, Is.Empty);
        });
    }

    [Test]
    public void SemanticDeclarations_RejectDuplicateTokensReferencesAndMissingAssociations()
    {
        SdpSessionDescription description = DescriptionWithRtpMedia();
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "declared track");
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "undeclared track");
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaStreamIdSemantic, "WMS declared declared"));
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaStreamIdSemantic, "WMS missing"));

        SdpValidationResult result = ValidateMsids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality && issue.Detail.Contains("semantic token")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationMsid && issue.Detail.Contains("only one semantic declaration")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationMsid && issue.Detail.Contains("absent from all media sections")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationMsid && issue.Detail.Contains("absent from the session semantic declarations")), Is.True);
        });
    }

    [Test]
    public void MsidAttributes_EnforceSessionMediaScopesAndRtpApplicability()
    {
        SdpSessionDescription description = DescriptionWithRtpMedia();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaStreamId, "session track"));
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamIdSemantic, "WMS stream");
        SdpMediaDescription data = new SdpMediaDescription { MediaType = "application", Protocol = "UDP/DTLS/SCTP", Port = 5002 };
        AddMediaValue(data, SdpAttributeNames.MediaStreamId, "data track");
        description.MediaDescriptions.Add(data);

        SdpValidationResult result = ValidateMsids(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.True);
            Assert.That(result.Issues.Select(issue => issue.Scope), Is.EqualTo(new[] { SdpScope.Session, SdpScope.Media, SdpScope.Media }));
        });
    }

    [TestCase(SdpValidationProfile.Rtp, true)]
    [TestCase(SdpValidationProfile.WebRtc, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true)]
    [TestCase(SdpValidationProfile.SyntaxOnly, false)]
    [TestCase(SdpValidationProfile.Bundle, false)]
    public void MsidRules_RunOnlyForProfilesThatIncludeRtp(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = DescriptionWithRtpMedia();
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "stream track-one");
        AddMediaValue(description.MediaDescriptions[0], SdpAttributeNames.MediaStreamId, "other track-two");

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationMsid), Is.EqualTo(expectedIssue));
    }

    static SdpValidationResult ValidateMsids(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rtp });
    }

    static SdpSessionDescription DescriptionWithRtpMedia()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        description.MediaDescriptions.Add(RtpMedia(5000));
        return description;
    }

    static SdpMediaDescription RtpMedia(int port)
    {
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Audio,
            MediaKind = SdpMediaKind.Audio,
            Port = port,
            Protocol = "RTP/AVP",
            Location = new SdpSourceLocation { LineNumber = 4, ColumnNumber = 1 }
        };
        media.Formats.Add("0");
        return media;
    }

    static void AddMediaValue(SdpMediaDescription media, string name, string value)
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
