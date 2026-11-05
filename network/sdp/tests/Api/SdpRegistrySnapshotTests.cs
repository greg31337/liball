using System;

namespace sdp.Tests.Api;

public class SdpRegistrySnapshotTests
{
    [Test]
    public void SnapshotMetadata_MatchesCheckedInRegistryVersions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SdpRegistrySnapshot.FormatVersion, Is.EqualTo(1));
            Assert.That(SdpRegistrySnapshot.SdpUpdated, Is.EqualTo("2026-06-01"));
            Assert.That(SdpRegistrySnapshot.SdpRegistryCount, Is.EqualTo(25));
            Assert.That(SdpRegistrySnapshot.SdpRecordCount, Is.EqualTo(460));
            Assert.That(SdpRegistrySnapshot.RtpUpdated, Is.EqualTo("2025-04-17"));
            Assert.That(SdpRegistrySnapshot.RtpRegistryCount, Is.EqualTo(16));
            Assert.That(SdpRegistrySnapshot.RtpRecordCount, Is.EqualTo(359));
        });
    }

    [Test]
    public void SnapshotRecordCounts_MatchNormalizedLines()
    {
        string[] sdpDefinitions = SdpRegistrySnapshot.SdpRegistryDefinitions.Split('\n');
        string[] sdpRecords = SdpRegistrySnapshot.SdpRecords.Split('\n');
        string[] rtpDefinitions = SdpRegistrySnapshot.RtpRegistryDefinitions.Split('\n');
        string[] rtpRecords = SdpRegistrySnapshot.RtpRecords.Split('\n');

        Assert.Multiple(() =>
        {
            Assert.That(sdpDefinitions, Has.Length.EqualTo(SdpRegistrySnapshot.SdpRegistryCount));
            Assert.That(sdpRecords, Has.Length.EqualTo(SdpRegistrySnapshot.SdpRecordCount));
            Assert.That(rtpDefinitions, Has.Length.EqualTo(SdpRegistrySnapshot.RtpRegistryCount));
            Assert.That(rtpRecords, Has.Length.EqualTo(SdpRegistrySnapshot.RtpRecordCount));
        });
    }

    [Test]
    public void SdpSnapshot_PreservesTokensAndAttributeMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SdpRegistrySnapshot.SdpRecords, Does.Contain("sdp-parameters-1\ttype=media\tname=audio\txref=rfc:rfc8866"));
            Assert.That(SdpRegistrySnapshot.SdpRecords, Does.Contain("type=media\tname=haptics\txref=rfc:rfc9993"));
            Assert.That(SdpRegistrySnapshot.SdpRecords, Does.Contain("name=RTP/AVP\txref=rfc:rfc8866"));
            Assert.That(SdpRegistrySnapshot.SdpRecords, Does.Contain("name=candidate\tusage=media\tmux=TRANSPORT\txref=rfc:rfc8839"));
            Assert.That(SdpRegistrySnapshot.SdpRecords, Does.Contain("name=extmap\tusage=session, media\tmux=SPECIAL"));
            Assert.That(SdpRegistrySnapshot.SdpRecords, Does.Contain("description=Media bundling\tname=BUNDLE\tmux=NORMAL"));
        });
    }

    [Test]
    public void RtpSnapshot_PreservesPayloadAndHeaderExtensionMetadata()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SdpRegistrySnapshot.RtpRecords, Does.Contain("value=0\tname=PCMU\tav=A\tclock_rate=8000\tchannels=1"));
            Assert.That(SdpRegistrySnapshot.RtpRecords, Does.Contain("type=audio\tsubtype=opus\tclock_rate=48000"));
            Assert.That(SdpRegistrySnapshot.RtpRecords, Does.Contain("type=video\tsubtype=VP9\tclock_rate=90000"));
            Assert.That(SdpRegistrySnapshot.RtpRecords, Does.Contain("name=urn:ietf:params:rtp-hdrext:toffset\tdescription=Transmission Time offsets"));
            Assert.That(SdpRegistrySnapshot.RtpRecords, Does.Contain("name=urn:ietf:params:rtp-hdrext:sdes:mid\tdescription=Media identification"));
        });
    }

    [Test]
    public void SnapshotEncoding_EscapesFieldDelimitersInsideValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SdpRegistrySnapshot.SdpRecords, Does.Contain("?type%3Dfolders&lang%3De&parent%3DT-REC-T.38"));
            Assert.That(SdpRegistrySnapshot.SdpRecords, Does.Not.Contain("<record>"));
            Assert.That(SdpRegistrySnapshot.RtpRecords, Does.Not.Contain("<record>"));
            Assert.That(SdpRegistrySnapshot.SdpRecords.EndsWith("\n", StringComparison.Ordinal), Is.False);
            Assert.That(SdpRegistrySnapshot.RtpRecords.EndsWith("\n", StringComparison.Ordinal), Is.False);
        });
    }
}
