namespace dns.Tests.Internal.Records;

public class OpaqueRecordDataCodecTests
{
    [TestCase(10)]
    [TestCase(31)]
    [TestCase(32)]
    [TestCase(34)]
    [TestCase(40)]
    [TestCase(56)]
    [TestCase(57)]
    [TestCase(58)]
    [TestCase(69)]
    [TestCase(70)]
    [TestCase(100)]
    [TestCase(101)]
    [TestCase(102)]
    [TestCase(103)]
    [TestCase(258)]
    [TestCase(259)]
    [TestCase(262)]
    [TestCase(263)]
    [TestCase(264)]
    public void UntypedAssignedRecordsAreAutomaticallyOpaque(int recordTypeValue)
    {
        DnsDecodeOptions options = new DnsDecodeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(
                OpaqueRecordDataCodec.IsAutomaticallyOpaqueType(recordTypeValue),
                Is.True);
            Assert.That(
                OpaqueRecordDataCodec.ShouldUseOpaqueData(recordTypeValue, options),
                Is.True);
        });
    }

    [TestCase(54)]
    [TestCase(71)]
    [TestCase(110)]
    [TestCase(4096)]
    [TestCase(65000)]
    [TestCase(65535)]
    public void UnassignedPrivateAndFutureRecordsAreAutomaticallyOpaque(
        int recordTypeValue)
    {
        DnsDecodeOptions options = new DnsDecodeOptions();

        Assert.Multiple(() =>
        {
            Assert.That(RecordDataModelCatalog.ReadRecordDataModel(recordTypeValue),
                Is.EqualTo(RecordDataModel.Unassigned));
            Assert.That(
                OpaqueRecordDataCodec.IsAutomaticallyOpaqueType(recordTypeValue),
                Is.True);
            Assert.That(
                OpaqueRecordDataCodec.ShouldUseOpaqueData(recordTypeValue, options),
                Is.True);
        });
    }

    [TestCase(1)]
    [TestCase(43)]
    [TestCase(64)]
    [TestCase(67)]
    [TestCase(68)]
    [TestCase(249)]
    [TestCase(250)]
    [TestCase(41)]
    public void TypedRecordsUseOpaqueDataOnlyWhenExplicitlySelected(
        int recordTypeValue)
    {
        DnsDecodeOptions typed = new DnsDecodeOptions();
        DnsDecodeOptions opaque = new DnsDecodeOptions();
        opaque.DecodeTypedRecordData = false;

        Assert.Multiple(() =>
        {
            Assert.That(
                OpaqueRecordDataCodec.IsAutomaticallyOpaqueType(recordTypeValue),
                Is.False);
            Assert.That(
                OpaqueRecordDataCodec.ShouldUseOpaqueData(recordTypeValue, typed),
                Is.False);
            Assert.That(
                OpaqueRecordDataCodec.ShouldUseOpaqueData(recordTypeValue, opaque),
                Is.True);
        });
    }

    [TestCase(0)]
    [TestCase(128)]
    [TestCase(251)]
    [TestCase(252)]
    [TestCase(253)]
    [TestCase(254)]
    [TestCase(255)]
    public void MetaAndQueryOnlyTypesNeverCarryOpaqueRecordData(
        int recordTypeValue)
    {
        DnsDecodeOptions options = new DnsDecodeOptions();
        options.DecodeTypedRecordData = false;

        Assert.Multiple(() =>
        {
            Assert.That(
                OpaqueRecordDataCodec.IsAutomaticallyOpaqueType(recordTypeValue),
                Is.False);
            Assert.That(
                OpaqueRecordDataCodec.ShouldUseOpaqueData(recordTypeValue, options),
                Is.False);
            AssertDecodeFailure(
                OpaqueRecordDataCodec.DecodeRecordData(
                    recordTypeValue, [1], 0, 1, options),
                "recordType");
        });
    }

    [Test]
    public void AutomaticOpaqueDecodePreservesExactEmbeddedBytes()
    {
        byte[] message = [9, 9, 0, 255, 1, 2, 9];

        DnsDecodeResult result = OpaqueRecordDataCodec.DecodeRecordData(
            65000, message, 2, 4, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(4));
            Assert.That(result.RecordData!.Opaque!.Bytes,
                Is.EqualTo(new byte[] { 0, 255, 1, 2 }));
        });
    }

    [TestCase(1)]
    [TestCase(67)]
    [TestCase(250)]
    public void ExplicitOpaqueDecodeBypassesTypedSchemas(int recordTypeValue)
    {
        DnsDecodeOptions options = new DnsDecodeOptions();
        options.DecodeTypedRecordData = false;
        byte[] malformedForTypedSchema = [0, 255, 1];

        DnsDecodeResult result = OpaqueRecordDataCodec.DecodeRecordData(
            recordTypeValue,
            malformedForTypedSchema,
            0,
            malformedForTypedSchema.Length,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.RecordData!.Opaque!.Bytes,
                Is.EqualTo(malformedForTypedSchema));
        });
    }

    [Test]
    public void TypedSelectionAndFallbackPermissionDoNotPreemptTypedDecode()
    {
        DnsDecodeOptions options = new DnsDecodeOptions();
        options.FallBackToOpaqueRecordData = true;

        DnsDecodeResult result = OpaqueRecordDataCodec.DecodeRecordData(
            1, [192, 0, 2], 0, 3, options);

        Assert.Multiple(() =>
        {
            Assert.That(
                OpaqueRecordDataCodec.ShouldUseOpaqueData(1, options),
                Is.False);
            AssertDecodeFailure(result, "recordType");
        });
    }

    [Test]
    public void EmptyOpaqueValuesRoundTrip()
    {
        DnsDecodeResult decoded = OpaqueRecordDataCodec.DecodeRecordData(
            31, [], 0, 0, new DnsDecodeOptions());
        DnsRecordData data = new DnsRecordData();
        data.Opaque = new DnsOpaqueData();

        byte[] encoded = Encode(31, data);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.RecordData!.Opaque!.Bytes, Is.Empty);
            Assert.That(encoded, Is.Empty);
        });
    }

    [TestCase(31)]
    [TestCase(65000)]
    [TestCase(1)]
    public void OpaqueEncoderPreservesBytesForAutomaticAndForcedTypes(
        int recordTypeValue)
    {
        DnsRecordData data = new DnsRecordData();
        data.Opaque = new DnsOpaqueData();
        data.Opaque.Bytes = [0, 255, 1, 2, 3];

        byte[] encoded = Encode(recordTypeValue, data);

        Assert.That(encoded, Is.EqualTo(data.Opaque.Bytes));
    }

    [TestCase(54)]
    [TestCase(71)]
    public void UnknownRecordTypesRoundTripByteExactOpaqueData(
        int recordTypeValue)
    {
        AssertOpaqueRoundTrip(recordTypeValue, false, AllOctets());
    }

    [TestCase(65280)]
    [TestCase(65534)]
    public void PrivateUseRecordTypesRoundTripByteExactOpaqueData(
        int recordTypeValue)
    {
        AssertOpaqueRoundTrip(recordTypeValue, false, AllOctets());
    }

    [TestCase(10)]
    [TestCase(40)]
    public void ExperimentalRecordTypesRoundTripByteExactOpaqueData(
        int recordTypeValue)
    {
        AssertOpaqueRoundTrip(recordTypeValue, false, AllOctets());
    }

    [TestCase(3)]
    [TestCase(4)]
    [TestCase(30)]
    [TestCase(38)]
    [TestCase(99)]
    [TestCase(32769)]
    public void ObsoleteRecordTypesRoundTripWhenOpaqueDecodingIsForced(
        int recordTypeValue)
    {
        AssertOpaqueRoundTrip(recordTypeValue, true, AllOctets());
    }

    [TestCase(110)]
    [TestCase(265)]
    [TestCase(32767)]
    [TestCase(32770)]
    [TestCase(65279)]
    [TestCase(65535)]
    public void FutureUnassignedRecordTypesRoundTripByteExactOpaqueData(
        int recordTypeValue)
    {
        AssertOpaqueRoundTrip(recordTypeValue, false, AllOctets());
    }

    [Test]
    public void EveryAssignedDataTypeCanBeForcedToRoundTripAsOpaque()
    {
        DnsRecordType[] recordTypes = Enum.GetValues<DnsRecordType>();
        byte[] bytes = AllOctets();

        foreach (DnsRecordType recordType in recordTypes)
        {
            int recordTypeValue = (int)recordType;
            RecordDataModel model =
                RecordDataModelCatalog.ReadRecordDataModel(recordTypeValue);
            if (model != RecordDataModel.MetaQuery &&
                model != RecordDataModel.Edns &&
                !OpaqueRecordDataCodec.IsAutomaticallyOpaqueType(
                    recordTypeValue))
            {
                AssertOpaqueRoundTrip(recordTypeValue, true, bytes);
            }
        }
    }

    [Test]
    public void MaximumLengthUnknownRecordDataRoundTripsByteExactly()
    {
        byte[] bytes = new byte[65535];
        for (int index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(index % 251);
        }

        AssertOpaqueRoundTrip(65000, false, bytes);
    }

    [Test]
    public void OpaqueCodecRejectsInvalidTypesSchemasAndRegions()
    {
        DnsDecodeOptions options = new DnsDecodeOptions();
        options.DecodeTypedRecordData = false;
        DnsDecodeResult negative = OpaqueRecordDataCodec.DecodeRecordData(
            -1, [1], 0, 1, options);
        DnsDecodeResult tooLarge = OpaqueRecordDataCodec.DecodeRecordData(
            65536, [1], 0, 1, options);
        DnsDecodeResult badRegion = OpaqueRecordDataCodec.DecodeRecordData(
            65000, [1], 1, 1, options);
        WireWriteResult meta = EncodeResult(255, OpaqueData([1]));
        WireWriteResult missing = EncodeResult(65000, new DnsRecordData());

        Assert.Multiple(() =>
        {
            AssertDecodeFailure(negative, "recordType");
            AssertDecodeFailure(tooLarge, "recordType");
            Assert.That(badRegion.Success, Is.False);
            Assert.That(badRegion.Diagnostics[0].Code,
                Is.EqualTo(DnsDiagnosticCode.InvalidLength));
            AssertWriteFailure(meta, "recordType");
            AssertWriteFailure(missing, "opaque");
        });
    }

    static DnsRecordData OpaqueData(byte[] bytes)
    {
        DnsRecordData data = new DnsRecordData();
        data.Opaque = new DnsOpaqueData();
        data.Opaque.Bytes = bytes;
        return data;
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[1024], 0, 1024);
        WireWriteResult result = OpaqueRecordDataCodec.EncodeRecordData(
            writer, recordTypeValue, data);
        Assert.That(result.Success, Is.True);
        return writer.CopyWrittenBytes();
    }

    static void AssertOpaqueRoundTrip(
        int recordTypeValue,
        bool forceOpaque,
        byte[] bytes)
    {
        DnsDecodeOptions decodeOptions = new DnsDecodeOptions();
        decodeOptions.DecodeTypedRecordData = !forceOpaque;
        DnsDecodeResult decoded = ResourceRecordCodec.DecodeRecordData(
            recordTypeValue,
            bytes,
            0,
            bytes.Length,
            decodeOptions);

        Assert.That(decoded.Success, Is.True,
            $"TYPE{recordTypeValue} did not decode as opaque data.");
        Assert.That(decoded.BytesConsumed, Is.EqualTo(bytes.Length),
            $"TYPE{recordTypeValue} consumed the wrong RDATA length.");
        Assert.That(decoded.RecordData!.Opaque!.Bytes, Is.EqualTo(bytes),
            $"TYPE{recordTypeValue} did not preserve its decoded octets.");

        DnsEncodeResult encoded = ResourceRecordCodec.EncodeRecordData(
            recordTypeValue,
            decoded.RecordData,
            new DnsEncodeOptions());

        Assert.That(encoded.Success, Is.True,
            $"TYPE{recordTypeValue} did not re-encode its opaque data.");
        Assert.That(encoded.BytesWritten, Is.EqualTo(bytes.Length),
            $"TYPE{recordTypeValue} wrote the wrong RDATA length.");
        Assert.That(encoded.Bytes, Is.EqualTo(bytes),
            $"TYPE{recordTypeValue} did not round-trip byte exactly.");
    }

    static byte[] AllOctets()
    {
        byte[] bytes = new byte[256];
        for (int index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)index;
        }

        return bytes;
    }

    static WireWriteResult EncodeResult(
        int recordTypeValue,
        DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[1024], 0, 1024);
        return OpaqueRecordDataCodec.EncodeRecordData(
            writer, recordTypeValue, data);
    }

    static void AssertDecodeFailure(DnsDecodeResult result, string field)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
    }

    static void AssertWriteFailure(WireWriteResult result, string field)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidRecordData));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
    }
}
