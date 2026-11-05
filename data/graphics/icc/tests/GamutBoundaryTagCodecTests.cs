namespace icc.Tests;

public class GamutBoundaryTagCodecTests
{
    [Test]
    public void MeshWithPcsAndDeviceCoordinatesRoundTripsAndClonesWithoutAliases()
    {
        IccGamutBoundary source = CreateTetrahedron(3, 2);

        byte[] payload = GamutBoundaryTagCodec.SerializeGamutBoundary(source);
        TagDecodeState state = Decode(payload);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(148));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.GamutBoundary));
            Assert.That(state.Data.GamutBoundary, Is.Not.Null);
            Assert.That(state.Data.GamutBoundary!.PcsChannels, Is.EqualTo(3));
            Assert.That(state.Data.GamutBoundary.DeviceChannels, Is.EqualTo(2));
            Assert.That(state.Data.GamutBoundary.Vertices, Has.Length.EqualTo(4));
            Assert.That(state.Data.GamutBoundary.Vertices[2].PcsColor.Components, Is.EqualTo(new double[] { 0.0, 1.0, 0.0 }));
            Assert.That(state.Data.GamutBoundary.Vertices[2].DeviceColor.Components, Is.EqualTo(new double[] { 0.5, 0.6 }).Within(0.000001));
            Assert.That(state.Data.GamutBoundary.Triangles[3].First, Is.EqualTo(2));
            Assert.That(GamutBoundaryTagCodec.SerializeGamutBoundary(state.Data.GamutBoundary), Is.EqualTo(payload));
            Assert.That(clone.GamutBoundary, Is.Not.SameAs(state.Data.GamutBoundary));
            Assert.That(clone.GamutBoundary!.Vertices[0].PcsColor.Components, Is.Not.SameAs(state.Data.GamutBoundary.Vertices[0].PcsColor.Components));
            Assert.That(clone.GamutBoundary.Triangles[0], Is.Not.SameAs(state.Data.GamutBoundary.Triangles[0]));
        });
    }

    [Test]
    public void MeshCanOmitAllDeviceCoordinates()
    {
        IccGamutBoundary source = CreateTetrahedron(3, 0);

        byte[] payload = GamutBoundaryTagCodec.SerializeGamutBoundary(source);
        TagDecodeState state = Decode(payload);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(116));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.GamutBoundary!.DeviceChannels, Is.Zero);
            Assert.That(state.Data.GamutBoundary.Vertices.All(vertex => vertex.DeviceColor.Components.Length == 0), Is.True);
        });
    }

    [Test]
    public void DecoderRejectsInvalidCountsIndicesFloatsReservedBytesAndTrailingData()
    {
        byte[] valid = GamutBoundaryTagCodec.SerializeGamutBoundary(CreateTetrahedron(3, 0));
        byte[] channels = (byte[])valid.Clone();
        WriteUInt16(channels, 8, 2);
        byte[] counts = (byte[])valid.Clone();
        WriteUInt32(counts, 12, 3);
        byte[] index = (byte[])valid.Clone();
        WriteUInt32(index, 20, 4);
        byte[] nonFinite = (byte[])valid.Clone();
        WriteUInt32(nonFinite, 68, 0x7f800000);
        byte[] subnormal = (byte[])valid.Clone();
        WriteUInt32(subnormal, 72, 1);
        byte[] reserved = (byte[])valid.Clone();
        reserved[7] = 1;
        byte[] trailing = new byte[valid.Length + 1];
        Array.Copy(valid, trailing, valid.Length);

        TagDecodeState channelsState = Decode(channels);
        TagDecodeState countsState = Decode(counts);
        TagDecodeState indexState = Decode(index);
        TagDecodeState nonFiniteState = Decode(nonFinite);
        TagDecodeState subnormalState = Decode(subnormal);
        TagDecodeState reservedState = Decode(reserved);
        TagDecodeState trailingState = Decode(trailing);

        Assert.Multiple(() =>
        {
            Assert.That(channelsState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(channelsState.Result.Message, Does.Contain("three PCS channels"));
            Assert.That(countsState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(countsState.Result.Message, Does.Contain("four vertices"));
            Assert.That(indexState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(indexState.Result.Message, Does.Contain("vertex ID outside"));
            Assert.That(nonFiniteState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(nonFiniteState.Result.Message, Does.Contain("finite float32"));
            Assert.That(subnormalState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(subnormalState.Result.Message, Does.Contain("finite float32"));
            Assert.That(reservedState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(reservedState.Result.Message, Does.Contain("reserved bytes"));
            Assert.That(trailingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(trailingState.Result.Message, Does.Contain("counts require"));
        });
    }

    [Test]
    public void MeshTopologyRequiresDistinctFacesReferencedVerticesAndOppositeSharedEdges()
    {
        IccGamutBoundary repeatedVertex = CreateTetrahedron(3, 0);
        repeatedVertex.Triangles[0].Third = repeatedVertex.Triangles[0].Second;
        IccGamutBoundary duplicateFace = CreateTetrahedron(3, 0);
        duplicateFace.Triangles[3] = new IccGamutTriangle { First = 1, Second = 2, Third = 0 };
        IccGamutBoundary inconsistentWinding = CreateTetrahedron(3, 0);
        inconsistentWinding.Triangles[3] = new IccGamutTriangle { First = 2, Second = 3, Third = 0 };
        IccGamutBoundary unreferencedVertex = CreateTetrahedron(3, 0);
        unreferencedVertex.Vertices = unreferencedVertex.Vertices.Concat(new IccGamutVertex[]
        {
            new IccGamutVertex
            {
                PcsColor = new IccColorVector { Components = new double[] { 0.2, 0.2, 0.2 } },
                DeviceColor = new IccColorVector()
            }
        }).ToArray();
        byte[] inconsistentPayload = GamutBoundaryTagCodec.SerializeGamutBoundary(CreateTetrahedron(3, 0));
        WriteUInt32(inconsistentPayload, 60, 3);
        WriteUInt32(inconsistentPayload, 64, 0);

        TagDecodeState inconsistentState = Decode(inconsistentPayload);

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => GamutBoundaryTagCodec.SerializeGamutBoundary(repeatedVertex));
            Assert.Throws<IccProfileWriteException>(() => GamutBoundaryTagCodec.SerializeGamutBoundary(duplicateFace));
            Assert.Throws<IccProfileWriteException>(() => GamutBoundaryTagCodec.SerializeGamutBoundary(inconsistentWinding));
            Assert.Throws<IccProfileWriteException>(() => GamutBoundaryTagCodec.SerializeGamutBoundary(unreferencedVertex));
            Assert.That(inconsistentState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(inconsistentState.Result.Message, Does.Contain("closed, consistently oriented"));
        });
    }

    [TestCase(IccTagSignatures.GamutBoundary0)]
    [TestCase(IccTagSignatures.GamutBoundary1)]
    [TestCase(IccTagSignatures.GamutBoundary2)]
    [TestCase(IccTagSignatures.GamutBoundary3)]
    public void IntentSpecificGamutTagsAcceptMatchingProfileContext(uint tagSignature)
    {
        byte[] payload = GamutBoundaryTagCodec.SerializeGamutBoundary(CreateTetrahedron(3, 3));
        TagDecodeState state = DecodeWithContext(payload, tagSignature, IccProfileClass.Output,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab, 0);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.GamutBoundary!.Vertices[0].PcsColor.ColorSpace, Is.EqualTo(IccColorSpaceKind.Lab));
            Assert.That(state.Data.GamutBoundary.Vertices[0].PcsColor.ColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.Lab));
            Assert.That(state.Data.GamutBoundary.Vertices[0].DeviceColor.ColorSpace, Is.EqualTo(IccColorSpaceKind.Rgb));
        });
    }

    [Test]
    public void ContextRejectsWrongTagClassAndChannelCounts()
    {
        byte[] payload = GamutBoundaryTagCodec.SerializeGamutBoundary(CreateTetrahedron(3, 2));
        TagDecodeState wrongTag = DecodeWithContext(payload, IccTagSignatures.Copyright, IccProfileClass.Output,
            IccColorSpaceSignatures.TwoColor, IccColorSpaceSignatures.Lab, 0);
        TagDecodeState wrongClass = DecodeWithContext(payload, IccTagSignatures.GamutBoundary1, IccProfileClass.Abstract,
            IccColorSpaceSignatures.TwoColor, IccColorSpaceSignatures.Lab, 0);
        TagDecodeState wrongDevice = DecodeWithContext(payload, IccTagSignatures.GamutBoundary1, IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab, 0);
        byte[] fourPcs = GamutBoundaryTagCodec.SerializeGamutBoundary(CreateTetrahedron(4, 0));
        TagDecodeState wrongPcs = DecodeWithContext(fourPcs, IccTagSignatures.GamutBoundary1, IccProfileClass.Display,
            IccColorSpaceSignatures.Rgb, IccColorSpaceSignatures.Lab, 0);

        Assert.Multiple(() =>
        {
            Assert.That(wrongTag.Result.Message, Does.Contain("permitted only for"));
            Assert.That(wrongClass.Result.Message, Does.Contain("profiles"));
            Assert.That(wrongDevice.Result.Message, Does.Contain("profile data colour space"));
            Assert.That(wrongPcs.Result.Message, Does.Contain("does not match a PCS"));
        });
    }

    [Test]
    public void ProfileEnvelopePropagatesSpectralPcsContextToGamutVertices()
    {
        byte[] payload = GamutBoundaryTagCodec.SerializeGamutBoundary(CreateTetrahedron(4, 0));
        byte[] profileBytes = CreateProfile(payload, IccColorSpaceSignatures.ReflectanceSpectralPrefix | 4);

        IccProfile profile = new IccProfileReader().ReadProfileBytes(profileBytes, new IccReadOptions());
        IccProfile clone = ProfileClone.CloneProfile(profile);
        IccTagData data = TagDecoder.GetDecodedData(clone.Tags[0]);

        Assert.Multiple(() =>
        {
            Assert.That(clone.Tags[0].DecodeResult.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(data.GamutBoundary!.PcsChannels, Is.EqualTo(4));
            Assert.That(data.GamutBoundary.Vertices[0].PcsColor.ColorSpace, Is.EqualTo(IccColorSpaceKind.Spectral));
            Assert.That(data.GamutBoundary.Vertices[0].PcsColor.ColorSpaceSignature, Is.EqualTo(IccColorSpaceSignatures.ReflectanceSpectralPrefix | 4));
        });
    }

    [Test]
    public void GamutBoundaryTypeRemainsRawForEarlierProfileVersions()
    {
        byte[] payload = GamutBoundaryTagCodec.SerializeGamutBoundary(CreateTetrahedron(3, 0));

        TagDecodeState state = Decode(payload, IccVersionKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    private static IccGamutBoundary CreateTetrahedron(int pcsChannels, int deviceChannels)
    {
        IccGamutBoundary boundary = new IccGamutBoundary
        {
            PcsChannels = pcsChannels,
            DeviceChannels = deviceChannels,
            Vertices = new IccGamutVertex[]
            {
                CreateVertex(pcsChannels, deviceChannels, 0),
                CreateVertex(pcsChannels, deviceChannels, 1),
                CreateVertex(pcsChannels, deviceChannels, 2),
                CreateVertex(pcsChannels, deviceChannels, 3)
            },
            Triangles = new IccGamutTriangle[]
            {
                new IccGamutTriangle { First = 0, Second = 2, Third = 1 },
                new IccGamutTriangle { First = 0, Second = 1, Third = 3 },
                new IccGamutTriangle { First = 1, Second = 2, Third = 3 },
                new IccGamutTriangle { First = 2, Second = 0, Third = 3 }
            }
        };
        return boundary;
    }

    private static IccGamutVertex CreateVertex(int pcsChannels, int deviceChannels, int index)
    {
        double[][] pcsValues = new double[][]
        {
            new double[] { 0.0, 0.0, 0.0, 0.1 },
            new double[] { 1.0, 0.0, 0.0, 0.2 },
            new double[] { 0.0, 1.0, 0.0, 0.3 },
            new double[] { 0.0, 0.0, 1.0, 0.4 }
        };
        double[][] deviceValues = new double[][]
        {
            new double[] { 0.1, 0.2, 0.3 },
            new double[] { 0.3, 0.4, 0.5 },
            new double[] { 0.5, 0.6, 0.7 },
            new double[] { 0.7, 0.8, 0.9 }
        };
        return new IccGamutVertex
        {
            PcsColor = new IccColorVector { Components = pcsValues[index].Take(pcsChannels).ToArray() },
            DeviceColor = new IccColorVector { Components = deviceValues[index].Take(deviceChannels).ToArray() }
        };
    }

    private static TagDecodeState Decode(byte[] payload)
    {
        return Decode(payload, IccVersionKind.Version5);
    }

    private static TagDecodeState Decode(byte[] payload, IccVersionKind version)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 1000, version, true);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static TagDecodeState DecodeWithContext(byte[] payload, uint tagSignature, IccProfileClass profileClass,
        uint dataColorSpace, uint pcs, uint spectralPcs)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 1000, IccVersionKind.Version5, true);
        state.HasProfileContext = true;
        state.TagSignature = tagSignature;
        state.ProfileClass = profileClass;
        state.DataColorSpaceSignature = dataColorSpace;
        state.DataColorSpace = HeaderCodec.DecodeColorSpace(dataColorSpace);
        state.ProfileConnectionSpaceSignature = pcs;
        state.ProfileConnectionSpace = pcs == 0 ? HeaderCodec.DecodeExtendedPcs(HeaderCodec.DecodeSpectralPcs(spectralPcs), IccColorSpaceKind.Unknown) : HeaderCodec.DecodePcs(pcs);
        state.SpectralPcsSignature = spectralPcs;
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static byte[] CreateProfile(byte[] payload, uint spectralPcs)
    {
        int payloadEnd = 144 + payload.Length;
        int declaredSize = (payloadEnd + 3) & -4;
        byte[] profile = new byte[declaredSize];
        WriteUInt32(profile, 0, (uint)declaredSize);
        WriteUInt32(profile, 8, 0x05000000);
        WriteUInt32(profile, 12, IccProfileClassSignatures.Input);
        WriteUInt32(profile, 16, IccColorSpaceSignatures.Rgb);
        WriteUInt32(profile, 20, 0);
        WriteUInt32(profile, 36, IccConstants.ProfileFileSignature);
        WriteUInt32(profile, 100, spectralPcs);
        WriteUInt16(profile, 104, IccNumberCodec.EncodeFloat16Bits(380.0f, 104));
        WriteUInt16(profile, 106, IccNumberCodec.EncodeFloat16Bits(780.0f, 106));
        WriteUInt16(profile, 108, 4);
        WriteUInt32(profile, 128, 1);
        WriteUInt32(profile, 132, IccTagSignatures.GamutBoundary0);
        WriteUInt32(profile, 136, 144);
        WriteUInt32(profile, 140, (uint)payload.Length);
        Array.Copy(payload, 0, profile, 144, payload.Length);
        return profile;
    }

    private static void WriteUInt16(byte[] destination, int offset, int value)
    {
        destination[offset] = (byte)(value >> 8);
        destination[offset + 1] = (byte)value;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
