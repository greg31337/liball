using rtp;

namespace rtp.Tests.Validation;

public class CopyIsolationTests
{
    [Test]
    public void ApiCopyIsolatesPacketFeedbackRepairAndSessionInputs()
    {
        RtpPacket packet = new RtpPacket();
        packet.Payload = new byte[] { 1, 2, 3 };
        RtcpPacket controlPacket = new RtcpPacket();
        controlPacket.Padding = new byte[] { 0, 2 };
        RtcpCompoundPacket compoundPacket = new RtcpCompoundPacket();
        compoundPacket.Packets = new RtcpPacket[] { controlPacket };
        RtpPacketContext context = new RtpPacketContext();
        context.TransportIdentity = "path-a";
        RtpFeedbackRequest feedbackRequest = new RtpFeedbackRequest();
        feedbackRequest.Feedback = new RtcpFeedbackPacket();
        feedbackRequest.Feedback.RawFeedbackControlInformation = new byte[] { 4, 5 };
        RtpRepairRequest repairRequest = new RtpRepairRequest();
        repairRequest.SequenceNumbers = new ushort[] { 10, 11 };
        RtpRepairResult repairResult = new RtpRepairResult();
        repairResult.RepairedPackets = new RtpPacket[] { packet };
        RtpSessionSnapshot snapshot = new RtpSessionSnapshot();
        snapshot.LocalSourceIdentifiers = new uint[] { 0x12345678U };
        RtpSessionUpdate update = new RtpSessionUpdate();
        update.AcceptedPackets = new RtpPacket[] { packet };

        RtpPacket packetCopy = RtpApiCopy.CopyRtpPacket(packet);
        RtcpPacket controlCopy = RtpApiCopy.CopyRtcpPacket(controlPacket);
        RtcpCompoundPacket compoundCopy = RtpApiCopy.CopyRtcpCompoundPacket(compoundPacket);
        RtpPacketContext contextCopy = RtpApiCopy.CopyRtpPacketContext(context);
        RtpFeedbackRequest feedbackCopy = RtpApiCopy.CopyRtpFeedbackRequest(feedbackRequest);
        RtpRepairRequest repairRequestCopy = RtpApiCopy.CopyRtpRepairRequest(repairRequest);
        RtpRepairResult repairResultCopy = RtpApiCopy.CopyRtpRepairResult(repairResult);
        RtpSessionSnapshot snapshotCopy = RtpApiCopy.CopyRtpSessionSnapshot(snapshot);
        RtpSessionUpdate updateCopy = RtpApiCopy.CopyRtpSessionUpdate(update);
        packet.Payload[0] = 99;
        controlPacket.Padding[0] = 99;
        context.TransportIdentity = "changed";
        feedbackRequest.Feedback.RawFeedbackControlInformation[0] = 99;
        repairRequest.SequenceNumbers[0] = 99;
        snapshot.LocalSourceIdentifiers[0] = 0U;

        Assert.Multiple(() =>
        {
            Assert.That(packetCopy.Payload, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(controlCopy.Padding, Is.EqualTo(new byte[] { 0, 2 }));
            Assert.That(compoundCopy.Packets[0].Padding, Is.EqualTo(new byte[] { 0, 2 }));
            Assert.That(contextCopy.TransportIdentity, Is.EqualTo("path-a"));
            Assert.That(feedbackCopy.Feedback?.RawFeedbackControlInformation,
                Is.EqualTo(new byte[] { 4, 5 }));
            Assert.That(repairRequestCopy.SequenceNumbers,
                Is.EqualTo(new ushort[] { 10, 11 }));
            Assert.That(repairResultCopy.RepairedPackets[0].Payload,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(snapshotCopy.LocalSourceIdentifiers,
                Is.EqualTo(new uint[] { 0x12345678U }));
            Assert.That(updateCopy.AcceptedPackets[0].Payload,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void ApiCopyRejectsMissingInputsBeforeCopying()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => RtpApiCopy.CopyRtpPacket(null!));
            Assert.Throws<ArgumentNullException>(() => RtpApiCopy.CopyRtcpPacket(null!));
            Assert.Throws<ArgumentNullException>(() => RtpApiCopy.CopyRtpPacketContext(null!));
            Assert.Throws<ArgumentNullException>(() => RtpApiCopy.CopyRtpSessionUpdate(null!));
        });
    }
}
