using System;
using System.Collections.Generic;

namespace diameter.Tests;

public class GroupedValueCodecTests
{
    [Test]
    public void EmptyGroupedValueMeasuresEncodesAndDecodesSuccessfully()
    {
        DiameterLimitConfiguration limits = CreateGroupedLimits(2, 4, 64);
        List<DiameterAvp> avps = new List<DiameterAvp>();
        List<GroupedAvpType> groupedTypes = new List<GroupedAvpType>();

        DiameterValueResult measurement = ValueCodecs.MeasureGrouped(
            avps,
            groupedTypes,
            limits);
        DiameterValueResult encoding = ValueCodecs.EncodeGrouped(
            new byte[0],
            avps,
            groupedTypes,
            limits);
        DiameterValueResult decoding = ValueCodecs.DecodeGrouped(
            new byte[0],
            groupedTypes,
            limits);

        Assert.Multiple(() =>
        {
            AssertSuccess(measurement, 0, 0, 0);
            AssertSuccess(encoding, 0, 0, 0);
            AssertSuccess(decoding, 0, 0, 0);
            Assert.That(decoding.Avps, Is.Empty);
        });
    }

    [Test]
    public void GroupedCodecRoundTripsOrderedAvpsAndRecursivelyValidatesKnownGroups()
    {
        DiameterAvp innerLeaf = CreateLeafAvp(2001U, new byte[] { 1, 2, 3, 4 });
        byte[] innerPayload = EncodeSingleAvp(innerLeaf);
        DiameterAvp nested = CreateLeafAvp(1000U, innerPayload);
        DiameterAvp sibling = CreateLeafAvp(2002U, new byte[] { 9 });
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(nested);
        avps.Add(sibling);
        List<GroupedAvpType> groupedTypes = CreateGroupedTypes(1000U, 0U);
        DiameterLimitConfiguration limits = CreateGroupedLimits(3, 8, 128);

        DiameterValueResult measurement = ValueCodecs.MeasureGrouped(
            avps,
            groupedTypes,
            limits);
        byte[] encoded = new byte[measurement.RequiredCount];
        DiameterValueResult encoding = ValueCodecs.EncodeGrouped(
            encoded,
            avps,
            groupedTypes,
            limits);
        DiameterValueResult decoding = ValueCodecs.DecodeGrouped(
            encoded,
            groupedTypes,
            limits);
        DiameterValueResult nestedDecoding = ValueCodecs.DecodeGrouped(
            decoding.Avps[0].Data,
            groupedTypes,
            limits);
        encoded[8] = 99;

        Assert.Multiple(() =>
        {
            AssertSuccess(measurement, 32, 0, 0);
            AssertSuccess(encoding, 32, 32, 0);
            AssertSuccess(decoding, 32, 0, 32);
            Assert.That(decoding.Avps, Has.Count.EqualTo(2));
            Assert.That(decoding.Avps[0].Code, Is.EqualTo(1000U));
            Assert.That(decoding.Avps[1].Code, Is.EqualTo(2002U));
            Assert.That(decoding.Avps[1].Data, Is.EqualTo(new byte[] { 9 }));
            Assert.That(nestedDecoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(nestedDecoding.Avps, Has.Count.EqualTo(1));
            Assert.That(nestedDecoding.Avps[0].Code, Is.EqualTo(2001U));
            Assert.That(nestedDecoding.Avps[0].Data, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(decoding.Avps[0].Data[0], Is.Zero);
        });
    }

    [Test]
    public void GroupedTypeIdentityIncludesVendorIdAndLeavesOtherPayloadsOpaque()
    {
        DiameterAvp vendorLeaf = new DiameterAvp();
        vendorLeaf.Code = 1000U;
        vendorLeaf.Flags = DiameterAvpFlags.VendorSpecific;
        vendorLeaf.VendorId = DiameterVendorIds.ThreeGpp;
        vendorLeaf.Data = new byte[] { 1, 2, 3 };
        byte[] encoded = EncodeSingleAvp(vendorLeaf);
        List<GroupedAvpType> groupedTypes = CreateGroupedTypes(1000U, 0U);
        DiameterLimitConfiguration limits = CreateGroupedLimits(1, 2, 64);

        DiameterValueResult result = ValueCodecs.DecodeGrouped(
            encoded,
            groupedTypes,
            limits);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.Avps, Has.Count.EqualTo(1));
            Assert.That(result.Avps[0].VendorId, Is.EqualTo(DiameterVendorIds.ThreeGpp));
            Assert.That(result.Avps[0].Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void GroupedDepthLimitIsAppliedDuringMeasurementAndDecoding()
    {
        DiameterAvp leaf = CreateLeafAvp(2001U, new byte[] { 1 });
        DiameterAvp secondLevel = CreateLeafAvp(1000U, EncodeSingleAvp(leaf));
        DiameterAvp firstLevel = CreateLeafAvp(1000U, EncodeSingleAvp(secondLevel));
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(firstLevel);
        byte[] encoded = EncodeSingleAvp(firstLevel);
        List<GroupedAvpType> groupedTypes = CreateGroupedTypes(1000U, 0U);
        DiameterLimitConfiguration shallow = CreateGroupedLimits(2, 8, 128);
        DiameterLimitConfiguration sufficient = CreateGroupedLimits(3, 8, 128);

        DiameterValueResult measuredFailure = ValueCodecs.MeasureGrouped(
            avps,
            groupedTypes,
            shallow);
        DiameterValueResult decodedFailure = ValueCodecs.DecodeGrouped(
            encoded,
            groupedTypes,
            shallow);
        DiameterValueResult measuredSuccess = ValueCodecs.MeasureGrouped(
            avps,
            groupedTypes,
            sufficient);
        DiameterValueResult decodedSuccess = ValueCodecs.DecodeGrouped(
            encoded,
            groupedTypes,
            sufficient);

        Assert.Multiple(() =>
        {
            AssertFailure(
                measuredFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-depth-limit");
            AssertFailure(
                decodedFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-depth-limit");
            Assert.That(measuredFailure.Issues[0].Offset, Is.EqualTo(16));
            Assert.That(decodedFailure.Issues[0].Offset, Is.EqualTo(16));
            Assert.That(measuredSuccess.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decodedSuccess.Status, Is.EqualTo(DiameterCodecStatus.Success));
        });
    }

    [Test]
    public void GroupedItemCountLimitRetainsOnlyCompletelyDecodedItems()
    {
        DiameterAvp first = CreateLeafAvp(1U, new byte[] { 1 });
        DiameterAvp second = CreateLeafAvp(2U, new byte[] { 2 });
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(first);
        avps.Add(second);
        byte[] encoded = CombineAvps(first, second);
        DiameterLimitConfiguration limits = CreateGroupedLimits(2, 1, 64);

        DiameterValueResult measurement = ValueCodecs.MeasureGrouped(
            avps,
            null,
            limits);
        DiameterValueResult decoding = ValueCodecs.DecodeGrouped(
            encoded,
            null,
            limits);

        Assert.Multiple(() =>
        {
            AssertFailure(
                measurement,
                DiameterCodecStatus.LimitExceeded,
                "grouped-avp-count-limit");
            AssertFailure(
                decoding,
                DiameterCodecStatus.LimitExceeded,
                "grouped-avp-count-limit");
            Assert.That(decoding.ConsumedCount, Is.EqualTo(12));
            Assert.That(decoding.Avps, Has.Count.EqualTo(1));
            Assert.That(decoding.Avps[0].Code, Is.EqualTo(1U));
            Assert.That(decoding.Issues[0].Offset, Is.EqualTo(12));
        });
    }

    [Test]
    public void GroupedByteLimitAllowsExactBoundaryAndRejectsLargerValues()
    {
        DiameterAvp avp = CreateLeafAvp(1U, new byte[] { 1 });
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(avp);
        byte[] encoded = EncodeSingleAvp(avp);
        DiameterLimitConfiguration exact = CreateGroupedLimits(2, 2, 12);
        DiameterLimitConfiguration shortLimit = CreateGroupedLimits(2, 2, 11);

        DiameterValueResult exactMeasurement = ValueCodecs.MeasureGrouped(avps, null, exact);
        DiameterValueResult exactDecoding = ValueCodecs.DecodeGrouped(encoded, null, exact);
        DiameterValueResult measuredFailure = ValueCodecs.MeasureGrouped(
            avps,
            null,
            shortLimit);
        DiameterValueResult decodedFailure = ValueCodecs.DecodeGrouped(
            encoded,
            null,
            shortLimit);

        Assert.Multiple(() =>
        {
            Assert.That(exactMeasurement.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(exactDecoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            AssertFailure(
                measuredFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-byte-limit");
            AssertFailure(
                decodedFailure,
                DiameterCodecStatus.LimitExceeded,
                "grouped-byte-limit");
        });
    }

    [Test]
    public void NestedGroupedPayloadMustContainAnExactAvpSequence()
    {
        DiameterAvp malformedGroup = CreateLeafAvp(1000U, new byte[7]);
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(malformedGroup);
        byte[] encoded = EncodeSingleAvp(malformedGroup);
        List<GroupedAvpType> groupedTypes = CreateGroupedTypes(1000U, 0U);
        DiameterLimitConfiguration limits = CreateGroupedLimits(2, 4, 64);

        DiameterValueResult measurement = ValueCodecs.MeasureGrouped(
            avps,
            groupedTypes,
            limits);
        DiameterValueResult decoding = ValueCodecs.DecodeGrouped(
            encoded,
            groupedTypes,
            limits);

        Assert.Multiple(() =>
        {
            AssertFailure(measurement, DiameterCodecStatus.Invalid, "invalid-grouped-content");
            AssertFailure(decoding, DiameterCodecStatus.Invalid, "invalid-grouped-content");
            Assert.That(measurement.Issues[0].Offset, Is.EqualTo(15));
            Assert.That(decoding.Issues[0].Offset, Is.EqualTo(15));
            Assert.That(decoding.Avps, Is.Empty);
        });
    }

    [Test]
    public void RootTruncationReportsRequiredSizeAndRetainsEarlierAvps()
    {
        DiameterAvp first = CreateLeafAvp(1U, new byte[] { 1 });
        DiameterAvp second = CreateLeafAvp(2U, new byte[] { 2 });
        byte[] complete = CombineAvps(first, second);
        byte[] truncated = new byte[complete.Length - 1];
        Array.Copy(complete, truncated, truncated.Length);
        DiameterLimitConfiguration limits = CreateGroupedLimits(2, 4, 64);

        DiameterValueResult result = ValueCodecs.DecodeGrouped(truncated, null, limits);

        Assert.Multiple(() =>
        {
            AssertFailure(result, DiameterCodecStatus.Incomplete, "incomplete-avp");
            Assert.That(result.RequiredCount, Is.EqualTo(24));
            Assert.That(result.ConsumedCount, Is.EqualTo(12));
            Assert.That(result.Avps, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Offset, Is.EqualTo(23));
        });
    }

    [Test]
    public void GroupedEncodingRejectsShortDestinationWithoutWriting()
    {
        DiameterAvp avp = CreateLeafAvp(1U, new byte[] { 1 });
        List<DiameterAvp> avps = new List<DiameterAvp>();
        avps.Add(avp);
        byte[] destination = new byte[11];
        Array.Fill(destination, (byte)170);

        DiameterValueResult result = ValueCodecs.EncodeGrouped(
            destination,
            avps,
            null,
            CreateGroupedLimits(2, 4, 64));

        Assert.Multiple(() =>
        {
            AssertFailure(
                result,
                DiameterCodecStatus.DestinationTooSmall,
                "destination-too-small");
            Assert.That(result.RequiredCount, Is.EqualTo(12));
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    [Test]
    public void GroupedCodecRejectsMissingValuesAndInvalidLimits()
    {
        DiameterLimitConfiguration invalidLimits = CreateGroupedLimits(0, 1, 1);

        DiameterValueResult missingAvps = ValueCodecs.MeasureGrouped(
            null,
            null,
            new DiameterLimitConfiguration());
        DiameterValueResult missingLimits = ValueCodecs.DecodeGrouped(
            new byte[0],
            null,
            null);
        DiameterValueResult invalid = ValueCodecs.MeasureGrouped(
            new List<DiameterAvp>(),
            null,
            invalidLimits);

        Assert.Multiple(() =>
        {
            AssertFailure(missingAvps, DiameterCodecStatus.Invalid, "missing-grouped-avps");
            AssertFailure(missingLimits, DiameterCodecStatus.Invalid, "invalid-grouped-limits");
            AssertFailure(invalid, DiameterCodecStatus.Invalid, "invalid-grouped-limits");
        });
    }

    private static DiameterAvp CreateLeafAvp(uint code, byte[] data)
    {
        DiameterAvp avp = new DiameterAvp();
        avp.Code = code;
        avp.Flags = DiameterAvpFlags.Mandatory;
        avp.Data = data;
        return avp;
    }

    private static byte[] EncodeSingleAvp(DiameterAvp avp)
    {
        DiameterEncodeResult measurement = AvpEncoder.MeasureAvp(avp);
        byte[] encoded = new byte[measurement.RequiredCount];
        DiameterEncodeResult result = AvpEncoder.EncodeAvp(encoded, avp);
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        return encoded;
    }

    private static byte[] CombineAvps(DiameterAvp first, DiameterAvp second)
    {
        byte[] firstBytes = EncodeSingleAvp(first);
        byte[] secondBytes = EncodeSingleAvp(second);
        byte[] encoded = new byte[firstBytes.Length + secondBytes.Length];
        Array.Copy(firstBytes, 0, encoded, 0, firstBytes.Length);
        Array.Copy(secondBytes, 0, encoded, firstBytes.Length, secondBytes.Length);
        return encoded;
    }

    private static List<GroupedAvpType> CreateGroupedTypes(uint code, uint vendorId)
    {
        GroupedAvpType type = new GroupedAvpType();
        type.Code = code;
        type.VendorId = vendorId;
        List<GroupedAvpType> types = new List<GroupedAvpType>();
        types.Add(type);
        return types;
    }

    private static DiameterLimitConfiguration CreateGroupedLimits(
        int depth,
        int count,
        int bytes)
    {
        DiameterLimitConfiguration limits = new DiameterLimitConfiguration();
        limits.MaximumGroupedDepth = depth;
        limits.MaximumGroupedAvpCount = count;
        limits.MaximumGroupedBytes = bytes;
        return limits;
    }

    private static void AssertSuccess(
        DiameterValueResult result,
        int requiredCount,
        int writtenCount,
        int consumedCount)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
        Assert.That(result.RequiredCount, Is.EqualTo(requiredCount));
        Assert.That(result.WrittenCount, Is.EqualTo(writtenCount));
        Assert.That(result.ConsumedCount, Is.EqualTo(consumedCount));
        Assert.That(result.Issues, Is.Empty);
    }

    private static void AssertFailure(
        DiameterValueResult result,
        DiameterCodecStatus status,
        string code)
    {
        Assert.That(result.Status, Is.EqualTo(status));
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Status, Is.EqualTo(status));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].Message, Is.Not.Empty);
    }
}
