namespace sdp.Tests.Unit.Validation;

public class SdpSimulcastValidationTests
{
    [Test]
    public void ModernSimulcast_AcceptsMatchingRidsPauseCapabilityAndStreamIdExtension()
    {
        SdpSessionDescription description = VideoDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        AddRid(media, "high send pt=31");
        AddRid(media, "low send pt=32");
        AddRid(media, "remote recv pt=31");
        AddValueAttribute(media, SdpAttributeNames.RtcpFeedback, "32 ccm pause nowait");
        AddStreamIdExtension(media);
        AddSimulcast(media, "send high;~low recv remote");

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void SimulcastAttributes_RequireMediaScopeRtpTransportAndSingleOccurrence()
    {
        SdpSessionDescription description = VideoDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.Simulcast, "send session"));
        SdpMediaDescription media = description.MediaDescriptions[0];
        AddRid(media, "one send");
        AddStreamIdExtension(media);
        AddSimulcast(media, "send one");
        AddSimulcast(media, "send one");
        SdpMediaDescription data = new SdpMediaDescription
        {
            MediaType = "application",
            Port = 5002,
            Protocol = SdpTransportProtocols.UdpDtlsSctp
        };
        AddSimulcast(data, "send data");
        description.MediaDescriptions.Add(data);

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeScope), Is.EqualTo(2));
            Assert.That(result.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationAttributeCardinality), Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedSimulcastSyntax_IsReportedByProfileValidation()
    {
        SdpSessionDescription description = VideoDescription();
        AddSimulcast(description.MediaDescriptions[0], "send high;");

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(result.Issues[0].AttributeName, Is.EqualTo(SdpAttributeNames.Simulcast));
        });
    }

    [Test]
    public void Alternatives_RequireUniqueExistingRidsWithMatchingDirections()
    {
        SdpSessionDescription description = VideoDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        AddRid(media, "sending send");
        AddRid(media, "receiving recv");
        AddStreamIdExtension(media);
        AddSimulcast(media, "send receiving,sending;sending,missing");

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(3));
            Assert.That(result.Issues.All(issue => issue.Code == SdpDiagnosticCodes.ValidationSimulcast), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("direction")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("more than once")), Is.True);
            Assert.That(result.Issues.Any(issue => issue.Detail.Contains("existing rid")), Is.True);
        });
    }

    [Test]
    public void PausedAlternative_RequiresPauseFeedbackForEveryRelatedPayload()
    {
        SdpSessionDescription description = VideoDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        AddRid(media, "layer send pt=31,32");
        AddValueAttribute(media, SdpAttributeNames.RtcpFeedback, "31 ccm pause");
        AddStreamIdExtension(media);
        AddSimulcast(media, "send ~layer");

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationSimulcast));
            Assert.That(result.Issues[0].Detail, Does.Contain("all related payload types"));
        });
    }

    [Test]
    public void WildcardPauseFeedback_CoversEveryRelatedPayload()
    {
        SdpSessionDescription description = VideoDescription();
        SdpMediaDescription media = description.MediaDescriptions[0];
        AddRid(media, "layer send pt=31,32");
        AddValueAttribute(media, SdpAttributeNames.RtcpFeedback, "* ccm pause");
        AddStreamIdExtension(media);
        AddSimulcast(media, "send ~layer");

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void ModernSimulcast_RequiresAnEffectiveRtpStreamIdExtension()
    {
        SdpSessionDescription description = VideoDescription();
        AddRid(description.MediaDescriptions[0], "one send");
        AddSimulcast(description.MediaDescriptions[0], "send one");

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationSimulcast));
            Assert.That(result.Issues[0].Detail, Does.Contain("RtpStreamId"));
        });
    }

    [Test]
    public void SessionLevelRtpStreamIdExtension_IsEffectiveForSimulcastMedia()
    {
        SdpSessionDescription description = VideoDescription();
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.ExtMap, "1 " + SdpHeaderExtensionUris.RtpStreamId));
        AddRid(description.MediaDescriptions[0], "one send");
        AddSimulcast(description.MediaDescriptions[0], "send one");

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void LegacySimulcastFallback_RemainsValidWithoutModernProjectionDependencies()
    {
        SdpSessionDescription description = VideoDescription();
        AddSimulcast(description.MediaDescriptions[0], " send rid=high;low paused=low");

        SdpValidationResult result = ValidateSimulcast(description);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void AnswerSimulcast_AcceptsReversedDirectionOrderedSubsets()
    {
        SdpSessionDescription offer = SimulcastOffer();
        SdpSessionDescription answer = VideoDescription();
        SdpMediaDescription media = answer.MediaDescriptions[0];
        AddRid(media, "a recv");
        AddRid(media, "b recv");
        AddRid(media, "x send");
        AddStreamIdExtension(media);
        AddSimulcast(media, "recv a;b send x");

        SdpValidationResult result = ValidateAnswer(answer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void AnswerSimulcast_RejectsReorderedStreams()
    {
        SdpSessionDescription offer = SimulcastOffer();
        SdpSessionDescription answer = VideoDescription();
        SdpMediaDescription media = answer.MediaDescriptions[0];
        AddRid(media, "a recv");
        AddRid(media, "b recv");
        AddRid(media, "x send");
        AddStreamIdExtension(media);
        AddSimulcast(media, "recv b;a send x");

        SdpValidationResult result = ValidateAnswer(answer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationSimulcast));
            Assert.That(result.Issues[0].Detail, Does.Contain("offered stream and alternative order"));
        });
    }

    [Test]
    public void AnswerSimulcast_RejectsSignalingAbsentFromOffer()
    {
        SdpSessionDescription offer = VideoDescription();
        AddStreamIdExtension(offer.MediaDescriptions[0]);
        SdpSessionDescription answer = VideoDescription();
        SdpMediaDescription media = answer.MediaDescriptions[0];
        AddRid(media, "one send");
        AddStreamIdExtension(media);
        AddSimulcast(media, "send one");

        SdpValidationResult result = ValidateAnswer(answer, offer);

        Assert.Multiple(() =>
        {
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo(SdpDiagnosticCodes.ValidationSimulcast));
            Assert.That(result.Issues[0].Detail, Does.Contain("absent from the offer"));
        });
    }

    [TestCase(SdpValidationProfile.Rtp, true)]
    [TestCase(SdpValidationProfile.WebRtc, true)]
    [TestCase(SdpValidationProfile.CombinedStrict, true)]
    [TestCase(SdpValidationProfile.SyntaxOnly, false)]
    [TestCase(SdpValidationProfile.Bundle, false)]
    public void SimulcastRules_RunOnlyForProfilesThatIncludeRtp(SdpValidationProfile profile, bool expectedIssue)
    {
        SdpSessionDescription description = VideoDescription();
        AddStreamIdExtension(description.MediaDescriptions[0]);
        AddSimulcast(description.MediaDescriptions[0], "send missing");

        SdpValidationResult result = new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = profile });

        Assert.That(result.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationSimulcast), Is.EqualTo(expectedIssue));
    }

    static SdpSessionDescription SimulcastOffer()
    {
        SdpSessionDescription offer = VideoDescription();
        SdpMediaDescription media = offer.MediaDescriptions[0];
        AddRid(media, "a send");
        AddRid(media, "b send");
        AddRid(media, "c send");
        AddRid(media, "x recv");
        AddRid(media, "y recv");
        AddStreamIdExtension(media);
        AddSimulcast(media, "send a;b,c recv x;y");
        return offer;
    }

    static SdpValidationResult ValidateSimulcast(SdpSessionDescription description)
    {
        return new SdpValidator().ValidateDescription(description, new SdpValidationOptions { Profile = SdpValidationProfile.Rtp });
    }

    static SdpValidationResult ValidateAnswer(SdpSessionDescription answer, SdpSessionDescription offer)
    {
        return new SdpValidator().ValidateDescription(answer, new SdpValidationOptions
        {
            Profile = SdpValidationProfile.Rtp,
            NegotiationRole = SdpNegotiationRole.Answerer,
            OfferDescription = offer
        });
    }

    static SdpSessionDescription VideoDescription()
    {
        SdpSessionDescription description = new SdpSessionDescription();
        SdpMediaDescription media = new SdpMediaDescription
        {
            MediaType = SdpMediaTypes.Video,
            MediaKind = SdpMediaKind.Video,
            Port = 5000,
            Protocol = SdpTransportProtocols.RtpAvpf,
            Location = new SdpSourceLocation { LineNumber = 4, ColumnNumber = 1 }
        };
        media.Formats.Add("31");
        media.Formats.Add("32");
        description.MediaDescriptions.Add(media);
        return description;
    }

    static void AddRid(SdpMediaDescription media, string value)
    {
        AddValueAttribute(media, SdpAttributeNames.Rid, value);
    }

    static void AddSimulcast(SdpMediaDescription media, string value)
    {
        AddValueAttribute(media, SdpAttributeNames.Simulcast, value);
    }

    static void AddStreamIdExtension(SdpMediaDescription media)
    {
        AddValueAttribute(media, SdpAttributeNames.ExtMap, "1 " + SdpHeaderExtensionUris.RtpStreamId);
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
