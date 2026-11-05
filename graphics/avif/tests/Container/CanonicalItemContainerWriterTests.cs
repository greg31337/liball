using System.Buffers.Binary;

namespace avif.Tests;

public class CanonicalItemContainerWriterTests
{
    [Test]
    public void CanonicalMdatLayoutRoundTripsMeasuredOffsetsAndPayloads()
    {
        CanonicalItemContainerRequest request = CreateRequest(false, false, false);

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        ParsedItem? first = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1);
        ParsedItem? second = parsed.Meta.FindItem(2);
        EncodedItemExtractionResult firstPayload = parsed.Meta.ExtractEncodedItem(written.Data, 1);
        EncodedItemExtractionResult secondPayload = parsed.Meta.ExtractEncodedItem(written.Data, 2);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(written.Data.LongLength, Is.EqualTo(written.Layout.FileSize));
            Assert.That(written.Layout.UsesItemData, Is.False);
            Assert.That(written.Layout.OffsetSize, Is.EqualTo(4));
            Assert.That(written.Layout.LengthSize, Is.EqualTo(4));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Select(box => box.Type),
                Is.EqualTo(new string[] { "ftyp", "meta", "mdat" }));
            Assert.That(parsed.Meta.GetPrimaryItemId(), Is.EqualTo(1));
            Assert.That((first ?? throw new InvalidOperationException("Invalid internal state.")).GetName(), Is.EqualTo("first"));
            Assert.That((second ?? throw new InvalidOperationException("Invalid internal state.")).IsHidden(), Is.True);
            Assert.That((first.GetLocation() ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod(), Is.Zero);
            Assert.That(((first.GetLocation() ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentOffset(),
                Is.EqualTo((ulong)written.Layout.GetItemOffset(0)));
            Assert.That(((second.GetLocation() ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentOffset(),
                Is.EqualTo((ulong)written.Layout.GetItemOffset(1)));
            Assert.That((firstPayload.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That((secondPayload.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 4, 5 }));
        });

        (firstPayload.Data ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        (secondPayload.Data ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void ItemDataPlacementUsesRelativeOffsetsAndOmitsTopLevelMediaData()
    {
        CanonicalItemContainerRequest request = CreateRequest(true, false, false);

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        ParsedItem? first = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1);
        ParsedItem? second = parsed.Meta.FindItem(2);
        EncodedItemExtractionResult secondPayload = parsed.Meta.ExtractEncodedItem(written.Data, 2);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(written.Layout.UsesItemData, Is.True);
            Assert.That(written.Layout.MediaPayloadOffset, Is.Zero);
            Assert.That(written.Layout.GetItemOffset(0), Is.Zero);
            Assert.That(written.Layout.GetItemOffset(1), Is.EqualTo(3));
            Assert.That((parsed.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Select(box => box.Type),
                Is.EqualTo(new string[] { "ftyp", "meta" }));
            Assert.That(parsed.Meta.HasItemDataBox(), Is.True);
            Assert.That(parsed.Meta.GetMediaDataBoxCount(), Is.Zero);
            Assert.That(((first ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation() ?? throw new InvalidOperationException("Invalid internal state.")).GetConstructionMethod(), Is.EqualTo(1));
            Assert.That((((second ?? throw new InvalidOperationException("Invalid internal state.")).GetLocation() ?? throw new InvalidOperationException("Invalid internal state.")).GetExtent(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetExtentOffset(), Is.EqualTo(3));
            Assert.That((secondPayload.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 4, 5 }));
        });

        (secondPayload.Data ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void ForcedWideOffsetsAndLargeHeadersAreEmittedAndParsed()
    {
        CanonicalItemContainerRequest request = CreateRequest(false, true, true);

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        IsoBoxParseResult boxes = IsoBoxReader.Parse(written.Data, CreateLimits(), false);
        IsoBox metaBox = boxes.Boxes.Single(box => box.Type == "meta");
        IsoBox mediaBox = boxes.Boxes.Single(box => box.Type == "mdat");
        IsoBox? locationBox = MetaBoxParser.FindChild(metaBox, "iloc");
        ItemLocationBoxParseResult locations = ItemLocationBox.Parse(
            written.Data,
            locationBox,
            metaBox,
            boxes.Resources);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(written.Layout.UsesLargeMetaBox, Is.True);
            Assert.That(written.Layout.UsesLargeMediaBox, Is.True);
            Assert.That(written.Layout.OffsetSize, Is.EqualTo(8));
            Assert.That(written.Layout.LengthSize, Is.EqualTo(8));
            Assert.That(metaBox.UsesLargeSize, Is.True);
            Assert.That(metaBox.HeaderSize, Is.EqualTo(16));
            Assert.That(metaBox.HasFullBoxFields, Is.True);
            Assert.That(mediaBox.UsesLargeSize, Is.True);
            Assert.That(locations.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(locations.OffsetSize, Is.EqualTo(8));
            Assert.That(locations.LengthSize, Is.EqualTo(8));
            Assert.That(mediaBox.PayloadOffset, Is.EqualTo(written.Layout.MediaPayloadOffset));
        });
    }

    [Test]
    public void MeasurementSelectsWideFieldsAtUintBoundariesWithoutAllocatingPayloads()
    {
        CanonicalItemContainerOptions options = new CanonicalItemContainerOptions();
        options.MaximumOutputBytes = long.MaxValue;
        List<CanonicalItemPayload> items = new List<CanonicalItemPayload>
        {
            CanonicalItemPayload.Create(70000, "av01", "large", false, (long)uint.MaxValue + 1),
            CanonicalItemPayload.Create(70001, "av01", "tail", false, 1)
        };
        CanonicalItemContainerRequest request = CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1", "miaf" },
            70000,
            items,
            options);

        CanonicalItemContainerLayout layout = CanonicalItemContainerWriter.Measure(request);
        CanonicalItemContainerWriteResult write = CanonicalItemContainerWriter.Write(request);

        Assert.Multiple(() =>
        {
            Assert.That(layout.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(layout.ItemLocationVersion, Is.EqualTo(2));
            Assert.That(layout.OffsetSize, Is.EqualTo(8));
            Assert.That(layout.LengthSize, Is.EqualTo(8));
            Assert.That(layout.UsesLargeMediaBox, Is.True);
            Assert.That(layout.GetItemOffset(1), Is.GreaterThan(uint.MaxValue));
            Assert.That(layout.FileSize, Is.GreaterThan(uint.MaxValue));
            Assert.That(write.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(write.Data, Is.Empty);
        });
    }

    [Test]
    public void NonSeekableOutputIsStagedAtomicallyAndMatchesByteArrayOutput()
    {
        CanonicalItemContainerRequest request = CreateRequest(false, false, false);
        CanonicalItemContainerWriteResult expected = CanonicalItemContainerWriter.Write(request);
        NonSeekableWriteStream destination = new NonSeekableWriteStream();

        AvifResultCode code = CanonicalItemContainerWriter.Write(destination, request, true);
        byte[] actual = destination.ToArray();

        CanonicalItemContainerRequest limited = CreateRequest(false, false, false);
        limited.Options.MaximumOutputBytes = 16;
        AvifResultCode limitedCode = CanonicalItemContainerWriter.Write(
            destination,
            limited,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(destination.CanSeek, Is.False);
            Assert.That(destination.Closed, Is.False);
            Assert.That(actual, Is.EqualTo(expected.Data));
            Assert.That(limitedCode, Is.EqualTo(AvifResultCode.LimitExceeded));
            Assert.That(destination.ToArray(), Is.EqualTo(actual));
        });
    }

    [Test]
    public void InvalidRequestsFailBeforeProducingOutput()
    {
        CanonicalItemContainerRequest duplicate = CreateRequest(false, false, false);
        (duplicate.Items ?? throw new InvalidOperationException("Invalid internal state."))[1] = CanonicalItemPayload.Create(
            1,
            "av01",
            "duplicate",
            false,
            new byte[] { 1 });
        CanonicalItemContainerRequest missingPrimary = CreateRequest(false, false, false);
        missingPrimary.PrimaryItemId = 99;
        CanonicalItemContainerRequest missingData = CreateRequest(false, false, false);
        (missingData.Items ?? throw new InvalidOperationException("Invalid internal state."))[0] = CanonicalItemPayload.Create(1, "av01", "first", false, 3);

        CanonicalItemContainerWriteResult duplicateResult = CanonicalItemContainerWriter.Write(
            duplicate);
        CanonicalItemContainerLayout primaryLayout = CanonicalItemContainerWriter.Measure(
            missingPrimary);
        CanonicalItemContainerWriteResult missingDataResult = CanonicalItemContainerWriter.Write(
            missingData);

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(duplicateResult.Data, Is.Empty);
            Assert.That(primaryLayout.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(missingDataResult.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(missingDataResult.Data, Is.Empty);
        });
    }

    [Test]
    public void PropertiesAreInternedAssociatedAndOpaqueBytesRoundTrip()
    {
        CanonicalItemContainerRequest request = CreateRequest(false, false, false);
        byte[] opaque = BuildRawProperty("zzzz", new byte[] { 9, 8, 7 });
        (request.Items ?? throw new InvalidOperationException("Invalid internal state."))[0].Properties.Add(
            new CanonicalItemPropertyAssociation(opaque, false));
        request.Items[1].Properties.Add(
            new CanonicalItemPropertyAssociation(opaque, false));

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        ParsedItem? first = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1);
        ParsedItem? second = parsed.Meta.FindItem(2);
        ParsedPropertyAssociation? firstOpaque = (first ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyAssociation(3);
        ParsedPropertyAssociation? secondOpaque = (second ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyAssociation(3);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Meta.GetPropertyCount(), Is.EqualTo(4));
            Assert.That(first.GetPropertyCount(), Is.EqualTo(4));
            Assert.That((first.GetPropertyAssociation(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsEssential(), Is.True);
            Assert.That((first.GetPropertyAssociation(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty().GetBoxType(),
                Is.EqualTo("av1C"));
            Assert.That((firstOpaque ?? throw new InvalidOperationException("Invalid internal state.")).IsEssential(), Is.False);
            Assert.That(firstOpaque.GetProperty().GetRawData(), Is.EqualTo(opaque));
            Assert.That((secondOpaque ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty(), Is.SameAs(firstOpaque.GetProperty()));
        });
    }

    [Test]
    public void EverySupportedPropertyRoundTripsWithTypedSemantics()
    {
        CanonicalItemContainerRequest request = CreateRequest(false, false, false);
        List<byte[]> optionalProperties = BuildEverySupportedOptionalProperty();
        int propertyIndex = 0;
        while (propertyIndex < optionalProperties.Count)
        {
            (request.Items ?? throw new InvalidOperationException("Invalid internal state."))[0].Properties.Add(new CanonicalItemPropertyAssociation(
                optionalProperties[propertyIndex],
                propertyIndex == 3));
            propertyIndex++;
        }

        CanonicalItemContainerWriteResult written =
            CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        ParsedItem? item = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).FindItem(1);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((written.SelfValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(20));
        });

        propertyIndex = 0;
        while (propertyIndex < (item ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount())
        {
            ParsedItemProperty property = (item
                .GetPropertyAssociation(propertyIndex) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty();
            Assert.That(
                AvifContainerValidator.HasSupportedPropertySemantics(property),
                Is.True,
                property.GetBoxType());
            propertyIndex++;
        }

        propertyIndex = 0;
        while (propertyIndex < optionalProperties.Count)
        {
            Assert.That(
                (item.GetPropertyAssociation(propertyIndex + 3) ?? throw new InvalidOperationException("Invalid internal state."))
                    .GetProperty()
                    .GetRawData(),
                Is.EqualTo(optionalProperties[propertyIndex]));
            propertyIndex++;
        }
    }

    [Test]
    public void AssociationWriterSelectsWideItemAndPropertyIndices()
    {
        CanonicalItemContainerOptions options = new CanonicalItemContainerOptions();
        options.MaximumOutputBytes = 131072;
        CanonicalItemPayload item = CanonicalItemPayload.Create(
            70000,
            "av01",
            "wide",
            false,
            new byte[] { 1 });
        AddRequiredAv1Properties(item, 0, 13, 320, 240);
        int propertyIndex = 0;
        while (propertyIndex < 130)
        {
            byte[] payload = new byte[4];
            BinaryPrimitives.WriteInt32BigEndian(payload, propertyIndex);
            item.Properties.Add(new CanonicalItemPropertyAssociation(
                BuildRawProperty("zzzz", payload),
                false));
            propertyIndex++;
        }

        CanonicalItemContainerRequest request = CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1" },
            70000,
            new List<CanonicalItemPayload> { item },
            options);

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        IsoBox meta = (parsed.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Single(box => box.Type == "meta");
        IsoBox? properties = MetaBoxParser.FindChild(meta, "iprp");
        IsoBox associations = (properties ?? throw new InvalidOperationException("Invalid internal state.")).Children.Single(box => box.Type == "ipma");
        IsoFullBox fullBox = IsoFullBox.Parse(written.Data, associations);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(133));
            Assert.That((parsed.Meta.FindItem(70000) ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(133));
            Assert.That(fullBox.Version, Is.EqualTo(1));
            Assert.That(fullBox.Flags, Is.EqualTo(1));
        });
    }

    [Test]
    public void ReferencesAndGroupsRoundTripWithCanonicalOrdering()
    {
        CanonicalItemContainerRequest request = CreateRelationshipRequest(true);

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        IsoBox meta = (parsed.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Single(box => box.Type == "meta");
        IsoFullBox references = IsoFullBox.Parse(
            written.Data,
            MetaBoxParser.FindChild(meta, "iref"));

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(references.Version, Is.EqualTo(1));
            Assert.That((parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceCount(), Is.EqualTo(2));
            Assert.That((parsed.Meta.GetReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType(), Is.EqualTo("auxl"));
            Assert.That((parsed.Meta.GetReference(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType(), Is.EqualTo("dimg"));
            Assert.That((parsed.Meta.GetReference(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetItemId(0), Is.EqualTo(70000));
            Assert.That((parsed.Meta.GetReference(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetTargetItemId(1), Is.EqualTo(70001));
            Assert.That(parsed.Meta.GetEntityGroupCount(), Is.EqualTo(2));
            Assert.That((parsed.Meta.GetEntityGroup(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupId(), Is.EqualTo(80000));
            Assert.That((parsed.Meta.GetEntityGroup(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupingType(), Is.EqualTo("ster"));
            Assert.That((parsed.Meta.GetEntityGroup(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupId(), Is.EqualTo(80001));
            Assert.That((parsed.Meta.GetEntityGroup(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetGroupingType(), Is.EqualTo("altr"));
        });
    }

    [Test]
    public void CanonicalWriterSelectsEveryIdDependentBoxVersion()
    {
        CanonicalItemContainerRequest small = CreateRequest(false, false, false);
        (small.References ?? throw new InvalidOperationException("Invalid internal state.")).Add(new CanonicalItemReference(
            "cdsc",
            2,
            new List<uint> { 1 }));
        CanonicalItemContainerRequest wide = CreateRelationshipRequest(false);

        CanonicalItemContainerWriteResult smallWrite =
            CanonicalItemContainerWriter.Write(small);
        CanonicalItemContainerWriteResult wideWrite =
            CanonicalItemContainerWriter.Write(wide);
        IsoBox smallMeta = (MetaBoxParser.Parse(
            smallWrite.Data,
            CreateLimits()).Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Single(box => box.Type == "meta");
        IsoBox wideMeta = (MetaBoxParser.Parse(
            wideWrite.Data,
            CreateLimits()).Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Single(box => box.Type == "meta");

        Assert.Multiple(() =>
        {
            Assert.That(smallWrite.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(smallWrite.Layout.ItemLocationVersion, Is.EqualTo(1));
            Assert.That(GetFullBoxVersion(smallWrite.Data, smallMeta, "pitm"), Is.Zero);
            Assert.That(GetFullBoxVersion(smallWrite.Data, smallMeta, "iloc"), Is.EqualTo(1));
            Assert.That(GetFullBoxVersion(smallWrite.Data, smallMeta, "iinf"), Is.EqualTo(1));
            Assert.That(GetItemInfoVersion(smallWrite.Data, smallMeta), Is.EqualTo(3));
            Assert.That(GetPropertyAssociationVersion(smallWrite.Data, smallMeta), Is.Zero);
            Assert.That(GetFullBoxVersion(smallWrite.Data, smallMeta, "iref"), Is.Zero);

            Assert.That(wideWrite.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(wideWrite.Layout.ItemLocationVersion, Is.EqualTo(2));
            Assert.That(GetFullBoxVersion(wideWrite.Data, wideMeta, "pitm"), Is.EqualTo(1));
            Assert.That(GetFullBoxVersion(wideWrite.Data, wideMeta, "iloc"), Is.EqualTo(2));
            Assert.That(GetFullBoxVersion(wideWrite.Data, wideMeta, "iinf"), Is.EqualTo(1));
            Assert.That(GetItemInfoVersion(wideWrite.Data, wideMeta), Is.EqualTo(3));
            Assert.That(GetPropertyAssociationVersion(wideWrite.Data, wideMeta), Is.EqualTo(1));
            Assert.That(GetFullBoxVersion(wideWrite.Data, wideMeta, "iref"), Is.EqualTo(1));
        });
    }

    [Test]
    public void EveryRelationshipAndEntityGroupKindRoundTripsTogether()
    {
        CanonicalItemContainerRequest request = CreateRelationshipRequest(false);
        (request.References ?? throw new InvalidOperationException("Invalid internal state.")).Add(new CanonicalItemReference(
            "thmb",
            70001,
            new List<uint> { 70000 }));
        request.References.Add(new CanonicalItemReference(
            "prem",
            70000,
            new List<uint> { 70001 }));
        request.References.Add(new CanonicalItemReference(
            "cdsc",
            70000,
            new List<uint> { 70002 }));
        request.References.Add(new CanonicalItemReference(
            "zzzz",
            70002,
            new List<uint> { 70000 }));

        CanonicalItemContainerWriteResult written =
            CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((written.SelfValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetReferenceCount(), Is.EqualTo(6));
            Assert.That((parsed.Meta.GetReference(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(ParsedItemReferenceKind.Auxiliary));
            Assert.That((parsed.Meta.GetReference(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(ParsedItemReferenceKind.ContentDescription));
            Assert.That((parsed.Meta.GetReference(2) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(ParsedItemReferenceKind.DerivedImage));
            Assert.That((parsed.Meta.GetReference(3) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(ParsedItemReferenceKind.Premultiplied));
            Assert.That((parsed.Meta.GetReference(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(ParsedItemReferenceKind.Thumbnail));
            Assert.That((parsed.Meta.GetReference(5) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(ParsedItemReferenceKind.Unknown));
            Assert.That((parsed.Meta.GetReference(5) ?? throw new InvalidOperationException("Invalid internal state.")).GetBoxType(), Is.EqualTo("zzzz"));
            Assert.That(parsed.Meta.GetEntityGroupCount(), Is.EqualTo(2));
            Assert.That((parsed.Meta.GetEntityGroup(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(ParsedEntityGroupKind.Stereo));
            Assert.That((parsed.Meta.GetEntityGroup(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetKind(),
                Is.EqualTo(ParsedEntityGroupKind.Alternate));
        });
    }

    [Test]
    public void FinalSelfValidationRejectsAuthoredDependencyCycles()
    {
        CanonicalItemContainerRequest request = CreateRelationshipRequest(false);
        (request.References ?? throw new InvalidOperationException("Invalid internal state.")).Add(new CanonicalItemReference(
            "dimg",
            70000,
            new List<uint> { 70002 }));

        CanonicalItemContainerWriteResult written =
            CanonicalItemContainerWriter.Write(request);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That(written.Data, Is.Empty);
            Assert.That(written.SelfValidation, Is.Not.Null);
            Assert.That((written.SelfValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.InvalidItem));
            Assert.That((written.SelfValidation.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).BoxPath,
                Is.EqualTo("/meta/iref"));
        });
    }

    [Test]
    public void EquivalentUnorderedInputsProduceIdenticalBytes()
    {
        CanonicalItemContainerRequest first = CreateRelationshipRequest(false);
        CanonicalItemContainerRequest second = CreateRelationshipRequest(true);

        CanonicalItemContainerWriteResult firstResult = CanonicalItemContainerWriter.Write(first);
        CanonicalItemContainerWriteResult secondResult = CanonicalItemContainerWriter.Write(second);
        MetaParseResult parsed = MetaBoxParser.Parse(secondResult.Data, CreateLimits());
        EncodedItemExtractionResult payload = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ExtractEncodedItem(
            secondResult.Data,
            70000);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondResult.Data, Is.EqualTo(firstResult.Data));
            Assert.That(payload.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((payload.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 1 }));
        });

        (payload.Data ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void UnknownEssentialPropertiesAreRejectedBeforeOutput()
    {
        CanonicalItemContainerRequest request = CreateRequest(false, false, false);
        (request.Items ?? throw new InvalidOperationException("Invalid internal state."))[0].Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("zzzz", new byte[] { 1 }),
            true));

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(written.Data, Is.Empty);
        });
    }

    [Test]
    public void StillWriterNormalizesRequiredBrandsAndSelectsBaselineProfile()
    {
        CanonicalItemContainerRequest request = CreateRequest(false, false, false);
        request.MajorBrand = "avis";
        request.CompatibleBrands = new List<string> { "msf1", "MA1A", "test" };

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        FileTypeParseResult parsed = FileTypeParser.Parse(written.Data, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(written.SelfValidation, Is.Not.Null);
            Assert.That((written.SelfValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.FileType ?? throw new InvalidOperationException("Invalid internal state.")).MajorBrand, Is.EqualTo("avif"));
            Assert.That(parsed.FileType.HasBrand("mif1"), Is.True);
            Assert.That(parsed.FileType.HasBrand("miaf"), Is.True);
            Assert.That(parsed.FileType.HasBrand("MA1B"), Is.True);
            Assert.That(parsed.FileType.HasBrand("MA1A"), Is.False);
            Assert.That(parsed.FileType.HasBrand("avis"), Is.False);
            Assert.That(parsed.FileType.HasBrand("msf1"), Is.False);
            Assert.That(parsed.FileType.HasBrand("test"), Is.True);
        });
    }

    [Test]
    public void ProfileSelectionUsesAdvancedOrGeneralFallbackForAllCodedItems()
    {
        CanonicalItemContainerRequest advanced = CreateRequest(false, false, false);
        int advancedIndex = 0;
        while (advancedIndex < (advanced.Items ?? throw new InvalidOperationException("Invalid internal state.")).Count)
        {
            advanced.Items[advancedIndex].Properties.Clear();
            AddRequiredAv1Properties(
                advanced.Items[advancedIndex],
                1,
                16,
                4096,
                4096);
            advancedIndex++;
        }

        CanonicalItemContainerRequest mixed = CreateRequest(false, false, false);
        (mixed.Items ?? throw new InvalidOperationException("Invalid internal state."))[1].Properties.Clear();
        AddRequiredAv1Properties(mixed.Items[1], 1, 13, 320, 240);

        CanonicalItemContainerWriteResult advancedResult =
            CanonicalItemContainerWriter.Write(advanced);
        CanonicalItemContainerWriteResult mixedResult =
            CanonicalItemContainerWriter.Write(mixed);
        FileTypeBox? advancedBrands = FileTypeParser.Parse(
            advancedResult.Data,
            CreateLimits()).FileType;
        FileTypeBox? mixedBrands = FileTypeParser.Parse(
            mixedResult.Data,
            CreateLimits()).FileType;

        Assert.Multiple(() =>
        {
            Assert.That(advancedResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((advancedBrands ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("MA1A"), Is.True);
            Assert.That(advancedBrands.HasBrand("MA1B"), Is.False);
            Assert.That(mixedResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((mixedBrands ?? throw new InvalidOperationException("Invalid internal state.")).HasBrand("MA1A"), Is.False);
            Assert.That(mixedBrands.HasBrand("MA1B"), Is.False);
            Assert.That(mixedBrands.HasBrand("avif"), Is.True);
            Assert.That(mixedBrands.HasBrand("mif1"), Is.True);
            Assert.That(mixedBrands.HasBrand("miaf"), Is.True);
        });
    }

    [Test]
    public void FinalSelfValidationRejectsNonconformingAuthoredFiles()
    {
        CanonicalItemContainerRequest request = CreateRequest(false, false, false);
        (request.Items ?? throw new InvalidOperationException("Invalid internal state."))[0].Properties.RemoveAt(2);

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(written.Data, Is.Empty);
            Assert.That(written.SelfValidation, Is.Not.Null);
            Assert.That((written.SelfValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(written.SelfValidation.Diagnostics.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void MinimizedHeaderWritesMif3MiniAndRoundTripsEquivalentModel()
    {
        CanonicalItemContainerRequest request = CreateMinimizedRequest();
        request.Options.HeaderFormat = AvifHeaderFormat.Minimized;

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        FileTypeParseResult fileType = FileTypeParser.Parse(written.Data, CreateLimits());
        EncodedItemExtractionResult payload = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).ExtractEncodedItem(written.Data, 1);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(written.Layout.UsesMinimizedFormat, Is.True);
            Assert.That(written.Layout.MinimizedImageSize, Is.GreaterThan(8));
            Assert.That((written.SelfValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That((parsed.Boxes ?? throw new InvalidOperationException("Invalid internal state.")).Boxes.Select(box => box.Type),
                Is.EqualTo(new string[] { "ftyp", "mini" }));
            Assert.That((fileType.FileType ?? throw new InvalidOperationException("Invalid internal state.")).MajorBrand, Is.EqualTo("mif3"));
            Assert.That(fileType.FileType.MinorVersion, Is.EqualTo(IsoBoxType.GetValue("avif")));
            Assert.That((parsed.Meta.GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(4));
            Assert.That((((parsed.Meta.GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyAssociation(1) ?? throw new InvalidOperationException("Invalid internal state."))
                .GetProperty().GetSpatialExtents() ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth(), Is.EqualTo(320));
            Assert.That((payload.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        });

        (payload.Data ?? throw new InvalidOperationException("Invalid internal state.")).Close();
    }

    [Test]
    public void AutomaticHeaderUsesSmallerMiniOutputDeterministically()
    {
        CanonicalItemContainerRequest first = CreateMinimizedRequest();
        CanonicalItemContainerRequest second = CreateMinimizedRequest();
        first.Options.HeaderFormat = AvifHeaderFormat.Automatic;
        second.Options.HeaderFormat = AvifHeaderFormat.Automatic;

        CanonicalItemContainerLayout full = CanonicalItemContainerWriter.MeasureFull(first);
        CanonicalItemContainerWriteResult firstResult = CanonicalItemContainerWriter.Write(first);
        CanonicalItemContainerWriteResult secondResult = CanonicalItemContainerWriter.Write(second);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstResult.Layout.UsesMinimizedFormat, Is.True);
            Assert.That(firstResult.Layout.FileSize, Is.LessThan(full.FileSize));
            Assert.That(secondResult.Data, Is.EqualTo(firstResult.Data));
        });
    }

    [Test]
    public void MinimizedHeaderPreservesIccAndOrientationProperties()
    {
        CanonicalItemContainerRequest request = CreateMinimizedRequest();
        request.Options.HeaderFormat = AvifHeaderFormat.Minimized;
        (request.Items ?? throw new InvalidOperationException("Invalid internal state."))[0].Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("colr", new byte[]
            {
                (byte)'p', (byte)'r', (byte)'o', (byte)'f', 7, 8
            }),
            true));
        request.Items[0].Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("irot", new byte[] { 1 }),
            true));
        request.Items[0].Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("imir", new byte[] { 0 }),
            true));

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        MetaParseResult parsed = MetaBoxParser.Parse(written.Data, CreateLimits());
        ParsedItem? primary = (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItem();

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(written.Layout.UsesMinimizedFormat, Is.True);
            Assert.That((primary ?? throw new InvalidOperationException("Invalid internal state.")).GetPropertyCount(), Is.EqualTo(7));
            Assert.That(((primary.GetPropertyAssociation(4) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty()
                .GetColourInformation() ?? throw new InvalidOperationException("Invalid internal state.")).GetData(), Is.EqualTo(new byte[] { 7, 8 }));
            Assert.That(((primary.GetPropertyAssociation(5) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty()
                .GetImageRotation() ?? throw new InvalidOperationException("Invalid internal state.")).GetQuarterTurnsCounterClockwise(), Is.EqualTo(1));
            Assert.That(((primary.GetPropertyAssociation(6) ?? throw new InvalidOperationException("Invalid internal state.")).GetProperty()
                .GetImageMirror() ?? throw new InvalidOperationException("Invalid internal state.")).GetAxis(), Is.Zero);
        });
    }

    [Test]
    public void UnrepresentableMinimizedRequestFallsBackToRegularContainer()
    {
        CanonicalItemContainerRequest request = CreateMinimizedRequest();
        request.Options.HeaderFormat = AvifHeaderFormat.Minimized;
        (request.Items ?? throw new InvalidOperationException("Invalid internal state."))[0].Name = "must be preserved";

        CanonicalItemContainerWriteResult written = CanonicalItemContainerWriter.Write(request);
        IsoBoxParseResult parsed = IsoBoxReader.Parse(written.Data, CreateLimits(), false);

        Assert.Multiple(() =>
        {
            Assert.That(written.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(written.Layout.UsesMinimizedFormat, Is.False);
            Assert.That(parsed.Boxes.Select(box => box.Type),
                Is.EqualTo(new string[] { "ftyp", "meta", "mdat" }));
            Assert.That(((MetaBoxParser.Parse(written.Data, CreateLimits())
                .Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItem() ?? throw new InvalidOperationException("Invalid internal state.")).GetName(), Is.EqualTo("must be preserved"));
        });
    }

    private static CanonicalItemContainerRequest CreateMinimizedRequest()
    {
        CanonicalItemContainerOptions options = new CanonicalItemContainerOptions();
        options.MaximumOutputBytes = 65536;
        CanonicalItemPayload item = CanonicalItemPayload.Create(
            1,
            "av01",
            string.Empty,
            false,
            new byte[] { 1, 2, 3 });
        AddRequiredAv1Properties(item, 0, 13, 320, 240);
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildNclxProperty(1, 13, 6, true),
            true));
        return CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1", "miaf" },
            1,
            new List<CanonicalItemPayload> { item },
            options);
    }

    private static int GetFullBoxVersion(byte[] source, IsoBox parent, string type)
    {
        return IsoFullBox.Parse(source, MetaBoxParser.FindChild(parent, type)).Version;
    }

    private static int GetItemInfoVersion(byte[] source, IsoBox meta)
    {
        IsoBox? itemInfo = MetaBoxParser.FindChild(meta, "iinf");
        return IsoFullBox.Parse(source, (itemInfo ?? throw new InvalidOperationException("Invalid internal state.")).Children[0]).Version;
    }

    private static int GetPropertyAssociationVersion(byte[] source, IsoBox meta)
    {
        IsoBox? properties = MetaBoxParser.FindChild(meta, "iprp");
        IsoBox associations = (properties ?? throw new InvalidOperationException("Invalid internal state.")).Children.Single(box => box.Type == "ipma");
        return IsoFullBox.Parse(source, associations).Version;
    }

    private static byte[] BuildNclxProperty(
        ushort primaries,
        ushort transfer,
        ushort matrix,
        bool fullRange)
    {
        byte[] raw = new byte[19];
        BinaryPrimitives.WriteUInt32BigEndian(raw, 19);
        raw[4] = (byte)'c';
        raw[5] = (byte)'o';
        raw[6] = (byte)'l';
        raw[7] = (byte)'r';
        raw[8] = (byte)'n';
        raw[9] = (byte)'c';
        raw[10] = (byte)'l';
        raw[11] = (byte)'x';
        BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(12, 2), primaries);
        BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(14, 2), transfer);
        BinaryPrimitives.WriteUInt16BigEndian(raw.AsSpan(16, 2), matrix);
        raw[18] = fullRange ? (byte)128 : (byte)0;
        return raw;
    }

    private static List<byte[]> BuildEverySupportedOptionalProperty()
    {
        List<byte[]> properties = new List<byte[]>();
        properties.Add(BuildNclxProperty(1, 13, 6, true));

        byte[] aspectRatio = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(aspectRatio.AsSpan(0, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(aspectRatio.AsSpan(4, 4), 1);
        properties.Add(BuildRawProperty("pasp", aspectRatio));

        byte[] auxiliaryType = new byte[]
        {
            0, 0, 0, 0,
            (byte)'a', (byte)'l', (byte)'p', (byte)'h', (byte)'a', 0
        };
        properties.Add(BuildRawProperty("auxC", auxiliaryType));
        properties.Add(BuildRawProperty("a1op", new byte[] { 0 }));
        properties.Add(BuildRawProperty("lsel", new byte[] { 255, 255 }));
        properties.Add(BuildRawProperty(
            "a1lx",
            new byte[] { 0, 0, 1, 0, 1, 0, 0 }));

        properties.Add(BuildRawProperty("clli", new byte[] { 0, 10, 0, 5 }));
        byte[] masteringDisplay = new byte[24];
        BinaryPrimitives.WriteUInt32BigEndian(masteringDisplay.AsSpan(16, 4), 100);
        BinaryPrimitives.WriteUInt32BigEndian(masteringDisplay.AsSpan(20, 4), 1);
        properties.Add(BuildRawProperty("mdcv", masteringDisplay));
        properties.Add(BuildRawProperty("cclv", new byte[] { 8, 0, 0, 0, 100 }));
        properties.Add(BuildRawProperty(
            "amve",
            new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 }));
        properties.Add(BuildRawProperty("reve", new byte[20]));
        properties.Add(BuildRawProperty("ndwt", new byte[8]));

        byte[] intrinsic = new byte[16];
        intrinsic[2] = 2;
        BinaryPrimitives.WriteInt32BigEndian(intrinsic.AsSpan(4, 4), 400);
        BinaryPrimitives.WriteInt32BigEndian(intrinsic.AsSpan(8, 4), 200);
        BinaryPrimitives.WriteInt32BigEndian(intrinsic.AsSpan(12, 4), 100);
        properties.Add(BuildRawProperty("cmin", intrinsic));
        properties.Add(BuildRawProperty("cmex", new byte[4]));

        byte[] cleanAperture = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(cleanAperture.AsSpan(0, 4), 320);
        BinaryPrimitives.WriteUInt32BigEndian(cleanAperture.AsSpan(4, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(cleanAperture.AsSpan(8, 4), 240);
        BinaryPrimitives.WriteUInt32BigEndian(cleanAperture.AsSpan(12, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(cleanAperture.AsSpan(20, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(cleanAperture.AsSpan(28, 4), 1);
        properties.Add(BuildRawProperty("clap", cleanAperture));
        properties.Add(BuildRawProperty("irot", new byte[] { 1 }));
        properties.Add(BuildRawProperty("imir", new byte[] { 0 }));
        return properties;
    }

    private static CanonicalItemContainerRequest CreateRelationshipRequest(bool reversed)
    {
        CanonicalItemContainerOptions options = new CanonicalItemContainerOptions();
        options.MaximumOutputBytes = 131072;
        CanonicalItemPayload first = CanonicalItemPayload.Create(
            70000,
            "av01",
            "first",
            false,
            new byte[] { 1 });
        CanonicalItemPayload second = CanonicalItemPayload.Create(
            70001,
            "av01",
            "second",
            false,
            new byte[] { 2 });
        CanonicalItemPayload third = CanonicalItemPayload.Create(
            70002,
            "grid",
            "third",
            false,
            new byte[] { 3 });
        AddRequiredAv1Properties(first, 0, 13, 320, 240);
        AddRequiredAv1Properties(second, 0, 13, 320, 240);
        byte[] sharedProperty = BuildRawProperty("zzzz", new byte[] { 4, 5 });
        first.Properties.Add(new CanonicalItemPropertyAssociation(sharedProperty, false));
        second.Properties.Add(new CanonicalItemPropertyAssociation(sharedProperty, false));

        List<CanonicalItemPayload> items = reversed
            ? new List<CanonicalItemPayload> { third, first, second }
            : new List<CanonicalItemPayload> { second, third, first };
        CanonicalItemReference auxiliary = new CanonicalItemReference(
            "auxl",
            70001,
            new List<uint> { 70000 });
        CanonicalItemReference derived = new CanonicalItemReference(
            "dimg",
            70002,
            new List<uint> { 70000, 70001 });
        List<CanonicalItemReference> references = reversed
            ? new List<CanonicalItemReference> { derived, auxiliary }
            : new List<CanonicalItemReference> { auxiliary, derived };
        CanonicalEntityGroup stereo = new CanonicalEntityGroup(
            "ster",
            80000,
            new List<uint> { 70000, 70001 });
        CanonicalEntityGroup alternate = new CanonicalEntityGroup(
            "altr",
            80001,
            new List<uint> { 70000, 70002 });
        List<CanonicalEntityGroup> groups = reversed
            ? new List<CanonicalEntityGroup> { alternate, stereo }
            : new List<CanonicalEntityGroup> { stereo, alternate };
        List<string> brands = reversed
            ? new List<string> { "miaf", "mif1" }
            : new List<string> { "mif1", "miaf" };
        return CanonicalItemContainerRequest.Create(
            "avif",
            0,
            brands,
            70000,
            items,
            references,
            groups,
            options);
    }

    private static byte[] BuildRawProperty(string type, byte[] payload)
    {
        byte[] raw = new byte[8 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(raw, (uint)raw.Length);
        raw[4] = (byte)type[0];
        raw[5] = (byte)type[1];
        raw[6] = (byte)type[2];
        raw[7] = (byte)type[3];
        payload.CopyTo(raw, 8);
        return raw;
    }

    private static byte[] BuildSpatialExtentsProperty(uint width, uint height)
    {
        byte[] raw = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(raw, 20);
        raw[4] = (byte)'i';
        raw[5] = (byte)'s';
        raw[6] = (byte)'p';
        raw[7] = (byte)'e';
        BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(12, 4), width);
        BinaryPrimitives.WriteUInt32BigEndian(raw.AsSpan(16, 4), height);
        return raw;
    }

    private static void AddRequiredAv1Properties(
        CanonicalItemPayload item,
        int profile,
        int level,
        uint width,
        uint height)
    {
        byte[] configuration = new byte[]
        {
            129,
            (byte)(profile << 5 | level),
            (byte)(profile == 0 ? 12 : 0),
            0
        };
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildRawProperty("av1C", configuration),
            true));
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            BuildSpatialExtentsProperty(width, height),
            false));

        byte[] pixelInformation = new byte[16];
        BinaryPrimitives.WriteUInt32BigEndian(pixelInformation, 16);
        pixelInformation[4] = (byte)'p';
        pixelInformation[5] = (byte)'i';
        pixelInformation[6] = (byte)'x';
        pixelInformation[7] = (byte)'i';
        pixelInformation[12] = 3;
        pixelInformation[13] = 8;
        pixelInformation[14] = 8;
        pixelInformation[15] = 8;
        item.Properties.Add(new CanonicalItemPropertyAssociation(
            pixelInformation,
            false));
    }

    private static CanonicalItemContainerRequest CreateRequest(
        bool useItemData,
        bool force64BitOffsets,
        bool forceLargeHeaders)
    {
        CanonicalItemContainerOptions options = new CanonicalItemContainerOptions();
        options.UseItemData = useItemData;
        options.Force64BitOffsets = force64BitOffsets;
        options.ForceLargeBoxHeaders = forceLargeHeaders;
        options.MaximumOutputBytes = 65536;
        List<CanonicalItemPayload> items = new List<CanonicalItemPayload>
        {
            CanonicalItemPayload.Create(
                1,
                "av01",
                "first",
                false,
                new byte[] { 1, 2, 3 }),
            CanonicalItemPayload.Create(
                2,
                "av01",
                "second",
                true,
                new byte[] { 4, 5 })
        };
        AddRequiredAv1Properties(items[0], 0, 13, 320, 240);
        AddRequiredAv1Properties(items[1], 0, 13, 320, 240);
        return CanonicalItemContainerRequest.Create(
            "avif",
            0,
            new List<string> { "mif1", "miaf" },
            1,
            items,
            options);
    }

    private static AvifSecurityLimits CreateLimits()
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 131072;
        limits.MaxBoxDepth = 16;
        limits.MaxBoxCount = 512;
        limits.MaxItemCount = 64;
        limits.MaxPropertyCount = 256;
        limits.MaxReferenceCount = 256;
        limits.MaxTemporaryBytes = 65536;
        return limits;
    }

    private class NonSeekableWriteStream : Stream
    {
        MemoryStream destination;
        internal bool Closed;

        internal NonSeekableWriteStream()
        {
            destination = new MemoryStream();
            Closed = false;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => !Closed;

        public override long Length => destination.Length;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            destination.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            destination.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            destination.Write(buffer);
        }

        protected override void Dispose(bool disposing)
        {
            Closed = true;
            destination.Dispose();
            base.Dispose(disposing);
        }

        internal byte[] ToArray()
        {
            return destination.ToArray();
        }
    }
}
