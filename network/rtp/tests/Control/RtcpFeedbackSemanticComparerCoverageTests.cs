using rtp;

namespace rtp.Tests.Control;

public class RtcpFeedbackSemanticComparerCoverageTests
{
    [Test]
    public void UnderstoodFeedbackFormatsCoverEveryTypedSemanticFamily()
    {
        RtcpTransportFeedbackFormat[] transportFormats =
        [
            RtcpTransportFeedbackFormat.GenericNack,
            RtcpTransportFeedbackFormat.TemporaryMaximumBitrateRequest,
            RtcpTransportFeedbackFormat.TemporaryMaximumBitrateNotification,
            RtcpTransportFeedbackFormat.RapidResynchronizationRequest,
            RtcpTransportFeedbackFormat.CongestionControlFeedback
        ];
        int index = 0;
        while (index < transportFormats.Length)
        {
            RtcpFeedbackPacket feedback = CreateTransportFeedback(
                transportFormats[index]);
            Assert.That(RtcpFeedbackSemanticComparer.HasUnderstoodFeedbackSemantics(
                feedback,
                (byte)transportFormats[index]), Is.True);
            index = index + 1;
        }

        RtcpPayloadFeedbackFormat[] payloadFormats =
        [
            RtcpPayloadFeedbackFormat.PictureLossIndication,
            RtcpPayloadFeedbackFormat.SliceLossIndication,
            RtcpPayloadFeedbackFormat.ReferencePictureSelectionIndication,
            RtcpPayloadFeedbackFormat.FullIntraRequest,
            RtcpPayloadFeedbackFormat.TemporalSpatialTradeoffRequest,
            RtcpPayloadFeedbackFormat.TemporalSpatialTradeoffNotification
        ];
        index = 0;
        while (index < payloadFormats.Length)
        {
            RtcpFeedbackPacket feedback = CreatePayloadFeedback(
                payloadFormats[index]);
            Assert.That(RtcpFeedbackSemanticComparer.HasUnderstoodFeedbackSemantics(
                feedback,
                (byte)payloadFormats[index]), Is.True);
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(RtcpFeedbackSemanticComparer.HasUnderstoodFeedbackSemantics(
                CreateTransportFeedback(RtcpTransportFeedbackFormat.Unknown),
                30), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.HasUnderstoodFeedbackSemantics(
                CreatePayloadFeedback(RtcpPayloadFeedbackFormat.Unknown),
                30), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.HasUnderstoodFeedbackSemantics(
                new RtcpFeedbackPacket(),
                0), Is.False);
        });
    }

    [Test]
    public void FeedbackEnvelopeAndEncodedComparisonCoverMismatchAndEquality()
    {
        RtcpFeedbackPacket first = CreatePictureLossFeedback(10U);
        RtcpFeedbackPacket same = CreatePictureLossFeedback(10U);
        same.SenderSourceIdentifier = 99U;
        RtcpFeedbackPacket otherPacketType = CreateTransportFeedback(
            RtcpTransportFeedbackFormat.GenericNack);
        otherPacketType.MediaSourceIdentifier = 10U;
        RtcpFeedbackPacket otherSource = CreatePictureLossFeedback(11U);
        RtcpFeedbackPacket otherFormat = CreatePayloadFeedback(
            RtcpPayloadFeedbackFormat.SliceLossIndication);
        otherFormat.MediaSourceIdentifier = 10U;
        otherFormat.SliceLossIndication = new RtcpSliceLossIndication();

        Assert.Multiple(() =>
        {
            Assert.That(RtcpFeedbackSemanticComparer.HasMatchingFeedbackEnvelope(
                first,
                same), Is.True);
            Assert.That(RtcpFeedbackSemanticComparer.HasMatchingFeedbackEnvelope(
                first,
                otherPacketType), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.HasMatchingFeedbackEnvelope(
                first,
                otherSource), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.HasMatchingFeedbackEnvelope(
                first,
                otherFormat), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.HasSameEncodedFeedback(
                first,
                same), Is.True);
            Assert.That(RtcpFeedbackSemanticComparer.HasSameEncodedFeedback(
                first,
                otherSource), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.CanCoalesceFeedback(
                first,
                same), Is.True);
            Assert.That(RtcpFeedbackSemanticComparer.CanCoalesceFeedback(
                first,
                otherSource), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                first,
                otherSource), Is.False);
        });
    }

    [Test]
    public void GenericNackComparisonAndMergeCoverSupersetAndMissingLoss()
    {
        RtcpFeedbackPacket requested = CreateGenericNackFeedback(
            new ushort[] { 100, 101 });
        RtcpFeedbackPacket superset = CreateGenericNackFeedback(
            new ushort[] { 100, 101, 105 });
        RtcpFeedbackPacket partial = CreateGenericNackFeedback(
            new ushort[] { 100 });
        RtcpFeedbackPacket missingBody = CreateTransportFeedback(
            RtcpTransportFeedbackFormat.GenericNack);

        RtcpFeedbackPacket merged = RtcpFeedbackSemanticComparer
            .MergeCoalescedFeedback(requested, superset);
        ushort[] mergedLoss = RtcpGenericNackWireCodec.ExpandLostSequenceNumbers(
            merged.GenericNack!);

        Assert.Multiple(() =>
        {
            Assert.That(RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                superset,
                requested), Is.True);
            Assert.That(RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                partial,
                requested), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                missingBody,
                requested), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.IsGenericNackSuperset(
                requested,
                missingBody), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.CanCoalesceFeedback(
                requested,
                superset), Is.True);
            Assert.That(mergedLoss,
                Is.EquivalentTo(new ushort[] { 100, 101, 105 }));
        });
    }

    [Test]
    public void SliceLossAndFullIntraComparisonCoverSupersetsAndMissingEntries()
    {
        RtcpFeedbackPacket requestedSlice = CreateSliceLossFeedback(
            new int[] { 10 });
        RtcpFeedbackPacket supersetSlice = CreateSliceLossFeedback(
            new int[] { 10, 20 });
        RtcpFeedbackPacket partialSlice = CreateSliceLossFeedback(
            new int[] { 20 });
        RtcpFeedbackPacket noSlice = CreatePayloadFeedback(
            RtcpPayloadFeedbackFormat.SliceLossIndication);
        RtcpFeedbackPacket requestedFir = CreateFullIntraFeedback(
            new uint[] { 10U });
        RtcpFeedbackPacket supersetFir = CreateFullIntraFeedback(
            new uint[] { 10U, 20U });
        RtcpFeedbackPacket partialFir = CreateFullIntraFeedback(
            new uint[] { 20U });
        RtcpFeedbackPacket noFir = CreatePayloadFeedback(
            RtcpPayloadFeedbackFormat.FullIntraRequest);

        Assert.Multiple(() =>
        {
            Assert.That(RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                supersetSlice,
                requestedSlice), Is.True);
            Assert.That(RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                partialSlice,
                requestedSlice), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.IsSliceLossSuperset(
                noSlice,
                requestedSlice), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.IsSliceLossSuperset(
                requestedSlice,
                noSlice), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                supersetFir,
                requestedFir), Is.True);
            Assert.That(RtcpFeedbackSemanticComparer.IsFeedbackSuperset(
                partialFir,
                requestedFir), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.IsFullIntraRequestSuperset(
                noFir,
                requestedFir), Is.False);
            Assert.That(RtcpFeedbackSemanticComparer.IsFullIntraRequestSuperset(
                requestedFir,
                noFir), Is.False);
        });
    }

    [Test]
    public void NonNackMergeReturnsAnIndependentEquivalentCopy()
    {
        RtcpFeedbackPacket first = CreatePictureLossFeedback(10U);
        RtcpFeedbackPacket second = CreatePictureLossFeedback(10U);

        RtcpFeedbackPacket merged = RtcpFeedbackSemanticComparer
            .MergeCoalescedFeedback(first, second);
        first.PictureLossIndication!.Requested = false;

        Assert.Multiple(() =>
        {
            Assert.That(merged, Is.Not.SameAs(first));
            Assert.That(merged.PictureLossIndication, Is.Not.SameAs(
                first.PictureLossIndication));
            Assert.That(merged.PictureLossIndication?.Requested, Is.True);
        });
    }

    static RtcpFeedbackPacket CreateTransportFeedback(
        RtcpTransportFeedbackFormat format)
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.TransportLayer;
        feedback.TransportFormat = format;
        return feedback;
    }

    static RtcpFeedbackPacket CreatePayloadFeedback(
        RtcpPayloadFeedbackFormat format)
    {
        RtcpFeedbackPacket feedback = new RtcpFeedbackPacket();
        feedback.PacketType = RtcpFeedbackPacketType.PayloadSpecific;
        feedback.PayloadFormat = format;
        return feedback;
    }

    static RtcpFeedbackPacket CreatePictureLossFeedback(uint sourceIdentifier)
    {
        RtcpFeedbackPacket feedback = CreatePayloadFeedback(
            RtcpPayloadFeedbackFormat.PictureLossIndication);
        feedback.MediaSourceIdentifier = sourceIdentifier;
        feedback.PictureLossIndication = new RtcpPictureLossIndication();
        feedback.PictureLossIndication.Requested = true;
        return feedback;
    }

    static RtcpFeedbackPacket CreateGenericNackFeedback(ushort[] sequenceNumbers)
    {
        RtcpFeedbackPacket feedback = CreateTransportFeedback(
            RtcpTransportFeedbackFormat.GenericNack);
        feedback.MediaSourceIdentifier = 77U;
        feedback.GenericNack = RtcpGenericNackWireCodec.PackLostSequenceNumbers(
            sequenceNumbers);
        return feedback;
    }

    static RtcpFeedbackPacket CreateSliceLossFeedback(int[] firstMacroblocks)
    {
        RtcpFeedbackPacket feedback = CreatePayloadFeedback(
            RtcpPayloadFeedbackFormat.SliceLossIndication);
        feedback.MediaSourceIdentifier = 77U;
        RtcpSliceLossIndication indication = new RtcpSliceLossIndication();
        indication.Entries = new RtcpSliceLossEntry[firstMacroblocks.Length];
        int index = 0;
        while (index < firstMacroblocks.Length)
        {
            RtcpSliceLossEntry entry = new RtcpSliceLossEntry();
            entry.FirstMacroblock = (ushort)firstMacroblocks[index];
            entry.MacroblockCount = 1;
            entry.PictureIdentifier = 2;
            indication.Entries[index] = entry;
            index = index + 1;
        }

        feedback.SliceLossIndication = indication;
        return feedback;
    }

    static RtcpFeedbackPacket CreateFullIntraFeedback(uint[] sourceIdentifiers)
    {
        RtcpFeedbackPacket feedback = CreatePayloadFeedback(
            RtcpPayloadFeedbackFormat.FullIntraRequest);
        RtcpFullIntraRequest request = new RtcpFullIntraRequest();
        request.Entries = new RtcpFullIntraRequestEntry[sourceIdentifiers.Length];
        int index = 0;
        while (index < sourceIdentifiers.Length)
        {
            RtcpFullIntraRequestEntry entry = new RtcpFullIntraRequestEntry();
            entry.SourceIdentifier = sourceIdentifiers[index];
            entry.SequenceNumber = (byte)index;
            request.Entries[index] = entry;
            index = index + 1;
        }

        feedback.FullIntraRequest = request;
        return feedback;
    }
}
