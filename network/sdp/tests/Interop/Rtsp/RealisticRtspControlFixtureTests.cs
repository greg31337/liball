namespace sdp.Tests.Interop.Rtsp;

public class RealisticRtspControlFixtureTests
{
    [Test]
    public void NetworkCameraPresentation_ProjectsAggregateTrackAndVendorControls()
    {
        string input = string.Join("\r\n", new[]
        {
            "v=0",
            "o=- 1700000100 1700000101 IN IP4 192.0.2.80",
            "s=ONVIF network camera",
            "i=Primary camera profile",
            "u=rtsp://camera.example.test/live",
            "t=0 0",
            "a=control:rtsp://camera.example.test/live",
            "a=range:npt=now-",
            "a=x-server-version:camera-os/4.2",
            "m=video 0 RTP/AVP 96",
            "c=IN IP4 239.255.0.1/16",
            "b=AS:4096",
            "a=rtpmap:96 H264/90000",
            "a=fmtp:96 packetization-mode=1;profile-level-id=4d0029;sprop-parameter-sets=Z00AKp2oHgCJ+WbgICAoAAADAAgAAAMBlHixck=,aO48gA==",
            "a=control:trackID=0",
            "a=x-dimensions:1920,1080",
            "m=audio 0 RTP/AVP 97",
            "c=IN IP4 239.255.0.2/16",
            "b=AS:128",
            "a=rtpmap:97 MPEG4-GENERIC/48000/2",
            "a=fmtp:97 streamtype=5;profile-level-id=1;mode=AAC-hbr;config=1190",
            "a=control:trackID=1",
            "m=application 0 RTP/AVP 107",
            "c=IN IP6 ff15::101/2",
            "a=rtpmap:107 vnd.onvif.metadata/90000",
            "a=control:trackID=2",
            "a=x-onvif-track:metadata",
            ""
        });

        SdpParseResult result = new SdpParser().ParseText(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpFixtureRoundTripAssert.AssertCanonical(input, new SdpParseOptions { Mode = SdpParseMode.Interoperable });
        SdpSessionDescription description = result.Description!;
        List<string> controls = description.Attributes
            .Concat(description.MediaDescriptions.SelectMany(media => media.Attributes))
            .Where(attribute => attribute.Name == "control")
            .Select(attribute => attribute.Value!)
            .ToList();
        SdpConnectionData videoConnection = description.MediaDescriptions[0].Connections.Single();
        SdpConnectionData metadataConnection = description.MediaDescriptions[2].Connections.Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, DiagnosticText(result));
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(description.Uri, Is.EqualTo("rtsp://camera.example.test/live"));
            Assert.That(description.MediaDescriptions.Select(media => media.MediaKind), Is.EqualTo(new[] { SdpMediaKind.Video, SdpMediaKind.Audio, SdpMediaKind.Application }));
            Assert.That(controls, Is.EqualTo(new[] { "rtsp://camera.example.test/live", "trackID=0", "trackID=1", "trackID=2" }));
            Assert.That(videoConnection.AddressKind, Is.EqualTo(SdpAddressKind.Ipv4Multicast));
            Assert.That(videoConnection.TimeToLive, Is.EqualTo(16));
            Assert.That(metadataConnection.AddressKind, Is.EqualTo(SdpAddressKind.Ipv6Multicast));
            Assert.That(metadataConnection.AddressCount, Is.EqualTo(2));
            Assert.That(description.Attributes.Single(attribute => attribute.Name == "x-server-version").Value, Is.EqualTo("camera-os/4.2"));
            Assert.That(description.MediaDescriptions[2].Attributes.Single(attribute => attribute.Name == "x-onvif-track").Value, Is.EqualTo("metadata"));
        });
    }

    static string DiagnosticText(SdpParseResult result)
    {
        return string.Join("|", result.Diagnostics.Select(diagnostic => diagnostic.Code + ":" + diagnostic.Detail));
    }
}
