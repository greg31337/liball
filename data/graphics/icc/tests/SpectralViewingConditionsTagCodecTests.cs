namespace icc.Tests;

public class SpectralViewingConditionsTagCodecTests
{
    [Test]
    public void CustomObserverAndIlluminantRoundTripTheirRangesSamplesAndColorimetry()
    {
        IccSpectralViewingConditions source = CreateCustomConditions();

        byte[] payload = SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(source);
        TagDecodeState state = Decode(payload, IccVersionKind.Version5);
        IccTagData clone = ProfileClone.CloneTagDataStandalone(state.Data);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(96));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.SpectralViewingConditions));
            Assert.That(state.Data.SpectralViewingConditions, Is.Not.Null);
            Assert.That(state.Data.SpectralViewingConditions!.Observer, Is.EqualTo(IccMeasurementObserver.Unknown));
            Assert.That(state.Data.SpectralViewingConditions.ObserverFunctions!.SpectrumCount, Is.EqualTo(3));
            Assert.That(state.Data.SpectralViewingConditions.ObserverFunctions.Range.StartWavelength, Is.EqualTo(400.0));
            Assert.That(state.Data.SpectralViewingConditions.ObserverFunctions.Values, Is.EqualTo(new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6 }).Within(0.000001));
            Assert.That(state.Data.SpectralViewingConditions.IlluminantSpectrum!.SpectrumCount, Is.EqualTo(1));
            Assert.That(state.Data.SpectralViewingConditions.IlluminantSpectrum.Values, Is.EqualTo(new double[] { 0.7, 0.8, 0.9 }).Within(0.000001));
            Assert.That(state.Data.SpectralViewingConditions.IlluminantColor.X, Is.EqualTo(0.9505).Within(0.000001));
            Assert.That(state.Data.SpectralViewingConditions.SurroundColor.Y, Is.EqualTo(0.2).Within(0.000001));
            Assert.That(SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(state.Data.SpectralViewingConditions), Is.EqualTo(payload));
            Assert.That(clone.SpectralViewingConditions, Is.Not.SameAs(state.Data.SpectralViewingConditions));
            Assert.That(clone.SpectralViewingConditions!.ObserverFunctions!.Values, Is.Not.SameAs(state.Data.SpectralViewingConditions.ObserverFunctions.Values));
            Assert.That(clone.SpectralViewingConditions.SurroundColor, Is.Not.SameAs(state.Data.SpectralViewingConditions.SurroundColor));
        });
    }

    [Test]
    public void StandardSelectorsAndTemperatureDefinedIlluminantsRemainLateBound()
    {
        IccSpectralViewingConditions conditions = new IccSpectralViewingConditions
        {
            Observer = IccMeasurementObserver.Cie1931TwoDegree,
            Illuminant = IccStandardIlluminant.BlackBody,
            ColorTemperature = 6500.0,
            IlluminantColor = new IccXyzColor { X = 95.0, Y = 100.0, Z = 108.0 },
            SurroundColor = new IccXyzColor { X = 19.0, Y = 20.0, Z = 21.0 }
        };

        byte[] payload = SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(conditions);
        TagDecodeState state = Decode(payload, IccVersionKind.Version5);

        Assert.Multiple(() =>
        {
            Assert.That(payload, Has.Length.EqualTo(60));
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.SpectralViewingConditions!.Observer, Is.EqualTo(IccMeasurementObserver.Cie1931TwoDegree));
            Assert.That(state.Data.SpectralViewingConditions.ObserverFunctions, Is.Null);
            Assert.That(state.Data.SpectralViewingConditions.Illuminant, Is.EqualTo(IccStandardIlluminant.BlackBody));
            Assert.That(state.Data.SpectralViewingConditions.IlluminantSpectrum, Is.Null);
            Assert.That(state.Data.SpectralViewingConditions.ColorTemperature, Is.EqualTo(6500.0));
            Assert.That(state.Data.SpectralViewingConditions.IlluminantColor.Y, Is.EqualTo(100.0));
        });
    }

    [Test]
    public void DecoderRejectsUndefinedSelectorsReservedBytesNonFiniteSamplesAndBadSizes()
    {
        byte[] valid = SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(CreateCustomConditions());
        byte[] observer = (byte[])valid.Clone();
        WriteUInt32(observer, 8, 3);
        byte[] reserved = (byte[])valid.Clone();
        reserved[18] = 1;
        byte[] nonFinite = (byte[])valid.Clone();
        WriteUInt32(nonFinite, 20, 0x7f800000);
        byte[] subnormal = (byte[])valid.Clone();
        WriteUInt32(subnormal, 24, 1);
        byte[] trailing = new byte[valid.Length + 1];
        Array.Copy(valid, trailing, valid.Length);

        TagDecodeState observerState = Decode(observer, IccVersionKind.Version5);
        TagDecodeState reservedState = Decode(reserved, IccVersionKind.Version5);
        TagDecodeState nonFiniteState = Decode(nonFinite, IccVersionKind.Version5);
        TagDecodeState subnormalState = Decode(subnormal, IccVersionKind.Version5);
        TagDecodeState trailingState = Decode(trailing, IccVersionKind.Version5);

        Assert.Multiple(() =>
        {
            Assert.That(observerState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(observerState.Result.Message, Does.Contain("observer encoding"));
            Assert.That(reservedState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(reservedState.Result.Message, Does.Contain("reserved bytes"));
            Assert.That(nonFiniteState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(nonFiniteState.Result.Message, Does.Contain("finite"));
            Assert.That(subnormalState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(subnormalState.Result.Message, Does.Contain("finite"));
            Assert.That(trailingState.Result.Status, Is.EqualTo(IccOperationStatus.InvalidData));
            Assert.That(trailingState.Result.Message, Does.Contain("require"));
        });
    }

    [Test]
    public void WriterRejectsMissingCustomObserverInconsistentSpectraAndInvalidTemperature()
    {
        IccSpectralViewingConditions missingObserver = new IccSpectralViewingConditions
        {
            Illuminant = IccStandardIlluminant.D50,
            IlluminantColor = new IccXyzColor(),
            SurroundColor = new IccXyzColor()
        };
        IccSpectralViewingConditions inconsistent = CreateCustomConditions();
        inconsistent.ObserverFunctions!.Values = new double[] { 0.1 };
        IccSpectralViewingConditions negativeTemperature = CreateCustomConditions();
        negativeTemperature.ColorTemperature = -1.0;
        IccSpectralViewingConditions missingTemperature = CreateCustomConditions();
        missingTemperature.Illuminant = IccStandardIlluminant.Daylight;

        Assert.Multiple(() =>
        {
            Assert.Throws<IccProfileWriteException>(() => SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(missingObserver));
            Assert.Throws<IccProfileWriteException>(() => SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(inconsistent));
            Assert.Throws<IccProfileWriteException>(() => SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(negativeTemperature));
            Assert.Throws<IccProfileWriteException>(() => SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(missingTemperature));
        });
    }

    [Test]
    public void NestedSpectralViewingConditionsPopulateThePccProjection()
    {
        IccSpectralViewingConditions conditions = new IccSpectralViewingConditions
        {
            Observer = IccMeasurementObserver.Cie1964TenDegree,
            Illuminant = IccStandardIlluminant.F11,
            ColorTemperature = 4000.0,
            IlluminantColor = new IccXyzColor { X = 0.96, Y = 1.0, Z = 0.82 },
            SurroundColor = new IccXyzColor { X = 0.1, Y = 0.2, Z = 0.3 }
        };
        IccTagData viewingData = new IccTagData
        {
            Kind = IccTagDataKind.SpectralViewingConditions,
            SpectralViewingConditions = conditions
        };
        IccTagStructure structure = new IccTagStructure
        {
            StructureSignature = IccStructureSignatures.ProfileConnectionConditions,
            Members = new IccStructureMember[]
            {
                CreateMember(StructuredTagProjectionCodec.PccIlluminant, CreateXyzData(0.96, 1.0, 0.82)),
                CreateMember(StructuredTagProjectionCodec.PccMediaWhitePoint, CreateXyzData(0.8, 0.9, 0.7)),
                CreateMember(StructuredTagProjectionCodec.PccViewingConditions, viewingData)
            }
        };

        byte[] payload = HierarchicalTagCodec.SerializeTagStructure(structure);
        TagDecodeState state = Decode(payload, IccVersionKind.Version5);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.Success));
            Assert.That(state.Data.ProfileConnectionConditions, Is.Not.Null);
            Assert.That(state.Data.ProfileConnectionConditions!.ViewingConditions.Observer, Is.EqualTo(IccMeasurementObserver.Cie1964TenDegree));
            Assert.That(state.Data.ProfileConnectionConditions.ViewingConditions.Illuminant, Is.EqualTo(IccStandardIlluminant.F11));
            Assert.That(state.Data.ProfileConnectionConditions.ViewingConditions.ColorTemperature, Is.EqualTo(4000.0));
            Assert.That(state.Data.ProfileConnectionConditions.ViewingConditions.SurroundColor.Y, Is.EqualTo(0.2).Within(0.000001));
        });
    }

    [Test]
    public void SpectralViewingConditionsRemainRawForEarlierProfileVersions()
    {
        IccSpectralViewingConditions conditions = new IccSpectralViewingConditions
        {
            Observer = IccMeasurementObserver.Cie1931TwoDegree,
            Illuminant = IccStandardIlluminant.D50,
            IlluminantColor = new IccXyzColor(),
            SurroundColor = new IccXyzColor()
        };
        byte[] payload = SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(conditions);

        TagDecodeState state = Decode(payload, IccVersionKind.Version4);

        Assert.Multiple(() =>
        {
            Assert.That(state.Result.Status, Is.EqualTo(IccOperationStatus.UnsupportedFeature));
            Assert.That(state.Data.Kind, Is.EqualTo(IccTagDataKind.Raw));
            Assert.That(state.Data.RawData.Bytes, Is.EqualTo(payload));
        });
    }

    private static IccSpectralViewingConditions CreateCustomConditions()
    {
        return new IccSpectralViewingConditions
        {
            Observer = IccMeasurementObserver.Unknown,
            ObserverFunctions = new IccSpectralData
            {
                Range = new IccSpectralRange { StartWavelength = 400.0, EndWavelength = 500.0, Steps = 2 },
                SpectrumCount = 3,
                Values = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6 }
            },
            Illuminant = IccStandardIlluminant.Unknown,
            IlluminantSpectrum = new IccSpectralData
            {
                Range = new IccSpectralRange { StartWavelength = 400.0, EndWavelength = 600.0, Steps = 3 },
                SpectrumCount = 1,
                Values = new double[] { 0.7, 0.8, 0.9 }
            },
            IlluminantColor = new IccXyzColor { X = 0.9505, Y = 1.0, Z = 1.089 },
            SurroundColor = new IccXyzColor { X = 0.1, Y = 0.2, Z = 0.3 }
        };
    }

    private static IccTagData CreateXyzData(double x, double y, double z)
    {
        return new IccTagData
        {
            Kind = IccTagDataKind.XyzArray,
            XyzValues = new IccXyzNumber[] { new IccXyzNumber { X = x, Y = y, Z = z } }
        };
    }

    private static IccStructureMember CreateMember(uint signature, IccTagData data)
    {
        return new IccStructureMember { Signature = signature, Data = data };
    }

    private static TagDecodeState Decode(byte[] payload, IccVersionKind version)
    {
        TagDecodeState state = new TagDecodeState(new TagDecodeSource(payload), 0, payload.Length, 900, version, true);
        TagCodecDispatcher.DecodeTagPayload(state);
        return state;
    }

    private static void WriteUInt32(byte[] destination, int offset, uint value)
    {
        destination[offset] = (byte)(value >> 24);
        destination[offset + 1] = (byte)(value >> 16);
        destination[offset + 2] = (byte)(value >> 8);
        destination[offset + 3] = (byte)value;
    }
}
