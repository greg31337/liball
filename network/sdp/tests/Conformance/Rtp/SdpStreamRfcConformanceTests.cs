namespace sdp.Tests.Conformance.Rtp;

public class SdpStreamRfcConformanceTests
{
    [Test]
    public void CompleteStreamGraph_ParsesValidatesAndProjectsInSourceOrder()
    {
        SdpSessionDescription description = ParseDescription(Header()
            + "a=msid-semantic:WMS stream\r\n"
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + StreamIdExtension()
            + "a=rtcp-fb:* ccm pause\r\n"
            + "a=msid:stream track\r\n"
            + "a=ssrc-group:FID 1000 1001\r\n"
            + "a=ssrc:1000 cname:participant@example.test\r\n"
            + "a=ssrc:1000 msid:stream track\r\n"
            + "a=ssrc:1001 cname:participant@example.test\r\n"
            + "a=ssrc:1001 msid:stream track\r\n"
            + "a=rid:low send pt=96;max-width=640;max-height=360\r\n"
            + "a=rid:high send pt=96;max-width=1280;max-height=720;depend=low\r\n"
            + "a=rid:remote recv pt=97;max-fps=30\r\n"
            + "a=simulcast:send low;~high recv remote\r\n"
            + "a=imageattr:96 send [x=1280,y=720,q=1.0] recv *\r\n");
        ISdpStreamService service = new SdpStreamService();

        SdpValidationResult validation = ValidateRtp(description);
        List<SdpSourceAttribute> sources = service.ResolveSourceAttributes(description, 0);
        List<SdpSourceGroupAttribute> groups = service.ResolveSourceGroups(description, 0);
        List<SdpMediaStreamId> streamIds = service.ResolveMediaStreamIds(description, 0);
        List<SdpRidAttribute> rids = service.ResolveRids(description, 0);
        SdpSimulcastAttribute simulcast = service.ResolveSimulcast(description, 0)!;
        List<SdpImageAttribute> images = service.ResolveImageAttributes(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(validation.Issues, Is.Empty);
            Assert.That(sources.Select(source => source.SynchronizationSource), Is.EqualTo(new uint[] { 1000u, 1000u, 1001u, 1001u }));
            Assert.That(sources.Select(source => source.Name), Is.EqualTo(new[] { "cname", "msid", "cname", "msid" }));
            Assert.That(groups.Single().Semantic, Is.EqualTo(SdpGroupSemantics.FlowIdentification));
            Assert.That(groups.Single().SynchronizationSources, Is.EqualTo(new uint[] { 1000u, 1001u }));
            Assert.That(streamIds.Select(streamId => streamId.StreamId), Is.EqualTo(new[] { "stream", "stream", "stream" }));
            Assert.That(service.ResolveMediaStreamSemantics(description).Single().StreamIds, Is.EqualTo(new[] { "stream" }));
            Assert.That(rids.Select(rid => rid.Id), Is.EqualTo(new[] { "low", "high", "remote" }));
            Assert.That(rids[1].Restrictions.Select(restriction => restriction.Name), Is.EqualTo(new[] { "max-width", "max-height", "depend" }));
            Assert.That(simulcast.SendStreams.SelectMany(stream => stream.Alternatives).Select(alternative => alternative.RidId), Is.EqualTo(new[] { "low", "high" }));
            Assert.That(simulcast.SendStreams[1].Alternatives[0].Paused, Is.True);
            Assert.That(simulcast.ReceiveStreams.Single().Alternatives.Single().RidId, Is.EqualTo("remote"));
            Assert.That(images.Single().SendConstraints, Is.EqualTo("[x=1280,y=720,q=1.0]"));
            Assert.That(images.Single().ReceiveConstraints, Is.EqualTo("*"));
        });
    }

    [Test]
    public void RidDependencyGraph_PreservesRestrictionsDependenciesAndLayerOrder()
    {
        SdpSessionDescription description = ParseDescription(Header()
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + StreamIdExtension()
            + "a=rid:base send pt=96;max-width=320;max-height=180;max-fps=15\r\n"
            + "a=rid:middle send pt=96;max-width=640;max-height=360;depend=base\r\n"
            + "a=rid:high send pt=96;max-width=1280;max-height=720;depend=middle,base\r\n"
            + "a=simulcast:send base;middle;high\r\n");
        ISdpStreamService service = new SdpStreamService();

        SdpValidationResult validation = ValidateRtp(description);
        List<SdpRidAttribute> rids = service.ResolveRids(description, 0);
        SdpSimulcastAttribute simulcast = service.ResolveSimulcast(description, 0)!;

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.True);
            Assert.That(rids[0].Restrictions.Select(restriction => restriction.RawText), Is.EqualTo(new[] { "max-width=320", "max-height=180", "max-fps=15" }));
            Assert.That(rids[1].Restrictions.Last().Value, Is.EqualTo("base"));
            Assert.That(rids[2].Restrictions.Last().Value, Is.EqualTo("middle,base"));
            Assert.That(simulcast.SendStreams.Select(stream => stream.Alternatives.Single().RidId), Is.EqualTo(new[] { "base", "middle", "high" }));
        });
    }

    [Test]
    public void SimulcastAnswer_MayRemoveButMustNotReorderOfferedLayersAndAlternatives()
    {
        SdpSessionDescription offer = ParseDescription(Header()
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + StreamIdExtension()
            + "a=rid:low send\r\n"
            + "a=rid:medium send\r\n"
            + "a=rid:high send\r\n"
            + "a=simulcast:send low;medium,high\r\n");
        SdpSessionDescription accepted = ParseDescription(Header()
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + StreamIdExtension()
            + "a=rid:low recv\r\n"
            + "a=rid:high recv\r\n"
            + "a=simulcast:recv low;high\r\n");
        SdpSessionDescription reordered = ParseDescription(Header()
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + StreamIdExtension()
            + "a=rid:low recv\r\n"
            + "a=rid:high recv\r\n"
            + "a=simulcast:recv high;low\r\n");

        SdpValidationResult acceptedResult = ValidateAnswer(accepted, offer);
        SdpValidationResult reorderedResult = ValidateAnswer(reordered, offer);

        Assert.Multiple(() =>
        {
            Assert.That(acceptedResult.Success, Is.True);
            Assert.That(acceptedResult.Issues, Is.Empty);
            Assert.That(reorderedResult.Issues.Count(issue => issue.Code == SdpDiagnosticCodes.ValidationSimulcast), Is.EqualTo(1));
            Assert.That(reorderedResult.Issues.Single(issue => issue.Code == SdpDiagnosticCodes.ValidationSimulcast).Detail, Does.Contain("order"));
        });
    }

    [Test]
    public void LegacySimulcastFallback_PreservesItsDraftFormAcrossProjectionAndEncoding()
    {
        SdpSessionDescription description = ParseDescription(Header()
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + "a=simulcast: send rid=high;low paused=low\r\n");
        ISdpStreamService service = new SdpStreamService();

        SdpSimulcastAttribute projection = service.ResolveSimulcast(description, 0)!;
        SdpAttributeEncodeResult encoded = new SdpAttributeCodec().EncodeAttribute(projection);
        SdpValidationResult validation = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(projection.LegacySyntax, Is.True);
            Assert.That(projection.RawValue, Is.EqualTo(" send rid=high;low paused=low"));
            Assert.That(projection.SendStreams, Is.Empty);
            Assert.That(projection.ReceiveStreams, Is.Empty);
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Attribute!.RawText, Is.EqualTo("simulcast: send rid=high;low paused=low"));
            Assert.That(validation.Success, Is.True);
        });
    }

    [Test]
    public void ImageAttributes_ProjectBracketedAlternativesAndWildcardConstraints()
    {
        SdpSessionDescription description = ParseDescription(Header()
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + "a=imageattr:96 send [x=[320:16:640],y=[180,360],q=0.8] recv *\r\n"
            + "a=imageattr:* send *\r\n");

        List<SdpImageAttribute> images = new SdpStreamService().ResolveImageAttributes(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(images.Select(image => image.PayloadType), Is.EqualTo(new[] { "96", "*" }));
            Assert.That(images[0].HasSendConstraints, Is.True);
            Assert.That(images[0].SendConstraints, Is.EqualTo("[x=[320:16:640],y=[180,360],q=0.8]"));
            Assert.That(images[0].HasReceiveConstraints, Is.True);
            Assert.That(images[0].ReceiveConstraints, Is.EqualTo("*"));
            Assert.That(images[1].SendConstraints, Is.EqualTo("*"));
        });
    }

    [Test]
    public void ParsedLayerGraph_RemainsValidThroughRenameFilterAndRemoval()
    {
        SdpSessionDescription description = ParseDescription(Header()
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + StreamIdExtension()
            + "a=rid:base send pt=96\r\n"
            + "a=rid:middle send pt=96;depend=base\r\n"
            + "a=rid:high send pt=96;depend=middle\r\n"
            + "a=simulcast:send base;middle;high\r\n");
        ISdpStreamService service = new SdpStreamService();

        service.RenameRid(description, 0, "middle", "standard");
        SdpValidationResult renamedValidation = ValidateRtp(description);
        SdpSimulcastAttribute filtered = service.FilterSimulcastLayers(description, 0, new List<string> { "standard", "high" })!;
        SdpValidationResult filteredValidation = ValidateRtp(description);
        bool removed = service.RemoveRid(description, 0, "standard");
        SdpValidationResult removedValidation = ValidateRtp(description);
        List<SdpRidAttribute> remaining = service.ResolveRids(description, 0);

        Assert.Multiple(() =>
        {
            Assert.That(renamedValidation.Success, Is.True);
            Assert.That(filteredValidation.Success, Is.True);
            Assert.That(filtered.SendStreams.Select(stream => stream.Alternatives.Single().RidId), Is.EqualTo(new[] { "standard", "high" }));
            Assert.That(removed, Is.True);
            Assert.That(removedValidation.Success, Is.True);
            Assert.That(remaining.Select(rid => rid.Id), Is.EqualTo(new[] { "high" }));
            Assert.That(remaining.Single().Restrictions, Is.Empty);
            Assert.That(service.ResolveSimulcast(description, 0)!.SourceAttribute!.Value, Is.EqualTo("send high"));
        });
    }

    [Test]
    public void InvalidCrossReferences_ReportSourceMsidRidAndSimulcastDiagnostics()
    {
        SdpSessionDescription description = ParseDescription(Header()
            + "a=msid-semantic:WMS declared\r\n"
            + VideoMedia()
            + "a=rtpmap:96 VP8/90000\r\n"
            + "a=rtpmap:97 H264/90000\r\n"
            + "a=msid:undeclared track\r\n"
            + "a=ssrc:1 cname:participant@example.test\r\n"
            + "a=ssrc-group:FID 1 2\r\n"
            + "a=rid:a send pt=96;depend=b\r\n"
            + "a=rid:b send pt=96;depend=a\r\n"
            + "a=simulcast:send ~a;a,missing\r\n");

        SdpValidationResult validation = ValidateRtp(description);

        Assert.Multiple(() =>
        {
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationSource), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationMsid), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationRid && issue.Detail.Contains("cycle")), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationSimulcast && issue.Detail.Contains("more than once")), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationSimulcast && issue.Detail.Contains("pause")), Is.True);
            Assert.That(validation.Issues.Any(issue => issue.Code == SdpDiagnosticCodes.ValidationSimulcast && issue.Detail.Contains("existing rid")), Is.True);
        });
    }

    static SdpSessionDescription ParseDescription(string text)
    {
        SdpParseResult result = new SdpParser().ParseText(text, new SdpParseOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(diagnostic => diagnostic.Detail)));
        return result.Description!;
    }

    static SdpValidationResult ValidateRtp(SdpSessionDescription description)
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

    static string Header()
    {
        return "v=0\r\n"
            + "o=- 12345 1 IN IP4 192.0.2.1\r\n"
            + "s=RTP stream conformance\r\n"
            + "c=IN IP4 203.0.113.1\r\n"
            + "t=0 0\r\n";
    }

    static string VideoMedia()
    {
        return "m=video 5004 RTP/AVPF 96 97\r\n";
    }

    static string StreamIdExtension()
    {
        return "a=extmap:1 " + SdpHeaderExtensionUris.RtpStreamId + "\r\n";
    }
}
