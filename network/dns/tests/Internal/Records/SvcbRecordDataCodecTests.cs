namespace dns.Tests.Internal.Records;

public class SvcbRecordDataCodecTests
{
    [TestCase(64)]
    [TestCase(65)]
    public void KnownParametersRoundTripInCanonicalKeyOrder(int recordTypeValue)
    {
        DnsRecordData data = CompleteData();
        int[] originalOrder = data.Svcb!.Parameters
            .Select(parameter => ParameterKey(parameter)).ToArray();

        byte[] encoded = Encode(recordTypeValue, data);
        DnsDecodeResult decoded = Decode(recordTypeValue, encoded);
        DnsSvcbData svcb = decoded.RecordData!.Svcb!;

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(svcb.Priority, Is.EqualTo(1));
            Assert.That(Format(svcb.TargetName), Is.EqualTo("service.example."));
            Assert.That(svcb.Parameters.Select(parameter => parameter.KeyValue),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 65400 }));
            Assert.That(svcb.Parameters[0].MandatoryKeyValues,
                Is.EqualTo(new[] { 1, 3, 5, 7, 8, 65400 }));
            Assert.That(svcb.Parameters[1].AlpnIdentifiers[0],
                Is.EqualTo(new byte[] { 104, 50 }));
            Assert.That(svcb.Parameters[1].AlpnIdentifiers[1],
                Is.EqualTo(new byte[] { 104, 51 }));
            Assert.That(svcb.Parameters[2].NoDefaultAlpn, Is.True);
            Assert.That(svcb.Parameters[3].HasPort, Is.True);
            Assert.That(svcb.Parameters[3].Port, Is.EqualTo(8443));
            Assert.That(svcb.Parameters[4].Ipv4Hints[0],
                Is.EqualTo(new byte[] { 192, 0, 2, 1 }));
            Assert.That(svcb.Parameters[5].Ech,
                Is.EqualTo(new byte[] { 0, 4, 254, 13, 0, 0 }));
            Assert.That(svcb.Parameters[6].Ipv6Hints[0],
                Is.EqualTo(Sequence(16)));
            Assert.That(svcb.Parameters[7].DohPath,
                Is.EqualTo("/dns-query{?dns}"));
            Assert.That(svcb.Parameters[8].ObliviousHttp, Is.Empty);
            Assert.That(svcb.Parameters[9].RawValue,
                Is.EqualTo(new byte[] { 9, 8, 7 }));
            Assert.That(Encode(recordTypeValue, decoded.RecordData),
                Is.EqualTo(encoded));
            Assert.That(data.Svcb.Parameters.Select(parameter => ParameterKey(parameter)),
                Is.EqualTo(originalOrder));
        });
    }

    [Test]
    public void DecoderRejectsUnsortedDuplicateAndReservedKeys()
    {
        DnsDecodeResult unsorted = Decode(64, Wire(
            ParameterWire(4, new byte[] { 192, 0, 2, 1 }),
            ParameterWire(3, new byte[] { 0, 53 })));
        DnsDecodeResult duplicate = Decode(64, Wire(
            ParameterWire(3, new byte[] { 0, 53 }),
            ParameterWire(3, new byte[] { 1, 187 })));
        DnsDecodeResult reserved = Decode(64, Wire(
            ParameterWire(65535, [])));

        Assert.Multiple(() =>
        {
            AssertInvalidParameters(unsorted, "svcb.parameterKey");
            AssertInvalidParameters(duplicate, "svcb.parameterKey");
            AssertInvalidParameters(reserved, "svcb.parameterKey");
        });
    }

    [Test]
    public void DecoderRejectsTruncatedParameterFraming()
    {
        DnsDecodeResult header = Decode(64, [0, 1, 0, 0, 1, 0]);
        DnsDecodeResult value = Decode(64,
            [0, 1, 0, 0, 3, 0, 2, 1]);

        Assert.Multiple(() =>
        {
            AssertInvalidParameters(header, "svcb.parameters");
            AssertInvalidParameters(value, "svcb.parameterValue");
        });
    }

    [TestCase(0, new byte[0])]
    [TestCase(0, new byte[] { 0, 0 })]
    [TestCase(1, new byte[0])]
    [TestCase(1, new byte[] { 0 })]
    [TestCase(2, new byte[] { 1 })]
    [TestCase(3, new byte[] { 1 })]
    [TestCase(4, new byte[] { 192, 0, 2 })]
    [TestCase(5, new byte[] { 0, 1, 0 })]
    [TestCase(6, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 })]
    [TestCase(7, new byte[] { 255 })]
    [TestCase(7, new byte[] { 47, 110, 111, 45, 118, 97, 114 })]
    [TestCase(8, new byte[] { 1 })]
    public void DecoderRejectsMalformedKnownParameterValues(
        int key,
        byte[] value)
    {
        DnsDecodeResult result = Decode(65, Wire(ParameterWire(key, value)));

        AssertInvalidParameters(result, null);
    }

    [Test]
    public void DecoderEnforcesParameterDependencies()
    {
        DnsDecodeResult noAlpn = Decode(64, Wire(ParameterWire(2, [])));
        DnsDecodeResult missingMandatory = Decode(64, Wire(
            ParameterWire(0, new byte[] { 0, 3 }),
            ParameterWire(1, new byte[] { 2, 104, 50 })));

        Assert.Multiple(() =>
        {
            AssertInvalidParameters(noAlpn, "svcb.noDefaultAlpn");
            AssertInvalidParameters(missingMandatory, "svcb.mandatory");
        });
    }

    [TestCase(new byte[] { 0, 0 })]
    [TestCase(new byte[] { 0, 3, 0, 3 })]
    [TestCase(new byte[] { 0, 4, 0, 3 })]
    [TestCase(new byte[] { 255, 255 })]
    public void DecoderRejectsInvalidMandatoryKeyLists(byte[] value)
    {
        DnsDecodeResult result = Decode(
            65, Wire(ParameterWire(0, value)));

        AssertInvalidParameters(result, "svcb.mandatory");
    }

    [Test]
    public void AliasModePreservesButDoesNotApplyParameterDependencies()
    {
        byte[] wire = Wire(ParameterWire(2, []));
        wire[1] = 0;

        DnsDecodeResult decoded = Decode(64, wire);
        byte[] encoded = Encode(64, decoded.RecordData!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.RecordData!.Svcb!.Parameters[0].NoDefaultAlpn,
                Is.True);
            Assert.That(encoded, Is.EqualTo(wire));
        });
    }

    [Test]
    public void DecoderRejectsCompressedTargetNames()
    {
        byte[] message = [1, 120, 0, 0, 1, 192, 0];
        DnsDecodeResult result = ExtendedRecordDataCodec.DecodeRecordData(
            64, message, 3, 4, new DnsDecodeOptions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo("svcb.targetName"));
        });
    }

    [Test]
    public void UnknownParameterPreservesEmptyAndOpaqueValues()
    {
        byte[] wire = Wire(
            ParameterWire(65000, []),
            ParameterWire(65001, new byte[] { 0, 255, 1, 2 }));

        DnsDecodeResult decoded = Decode(64, wire);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.RecordData!.Svcb!.Parameters[0].RawValue, Is.Empty);
            Assert.That(decoded.RecordData.Svcb.Parameters[1].RawValue,
                Is.EqualTo(new byte[] { 0, 255, 1, 2 }));
            Assert.That(Encode(64, decoded.RecordData), Is.EqualTo(wire));
        });
    }

    [Test]
    public void EncoderRejectsDuplicateAndOutOfRangeKeys()
    {
        DnsRecordData duplicate = BasicData();
        duplicate.Svcb!.Parameters.Add(Port(53));
        duplicate.Svcb.Parameters.Add(Port(853));
        DnsRecordData reserved = BasicData();
        reserved.Svcb!.Parameters.Add(Unknown(65535, []));
        DnsRecordData negative = BasicData();
        negative.Svcb!.Parameters.Add(Unknown(-1, []));

        Assert.Multiple(() =>
        {
            AssertWriteInvalid(EncodeResult(64, duplicate), "svcb.parameterKey");
            AssertWriteInvalid(EncodeResult(64, reserved), "svcb.parameterKey");
            AssertWriteInvalid(EncodeResult(64, negative), "svcb.parameterKey");
        });
    }

    [Test]
    public void EncoderEnforcesTypedAndRawMandatoryDependencies()
    {
        DnsRecordData typed = BasicData();
        DnsSvcbParameter mandatory = Known(DnsSvcbParameterKey.Mandatory);
        mandatory.MandatoryKeyValues.Add(3);
        typed.Svcb!.Parameters.Add(mandatory);

        DnsRecordData raw = BasicData();
        DnsSvcbParameter rawMandatory = Known(DnsSvcbParameterKey.Mandatory);
        rawMandatory.UseRawValue = true;
        rawMandatory.RawValue = [0, 3];
        raw.Svcb!.Parameters.Add(rawMandatory);

        Assert.Multiple(() =>
        {
            AssertWriteInvalid(EncodeResult(64, typed), "svcb.mandatory");
            AssertWriteInvalid(EncodeResult(64, raw), "svcb.mandatory");
        });
    }

    [TestCase(new int[] { 0 })]
    [TestCase(new int[] { 3, 3 })]
    [TestCase(new int[] { 65535 })]
    public void EncoderRejectsInvalidTypedMandatoryKeyLists(int[] values)
    {
        DnsRecordData data = BasicData();
        data.Svcb!.Priority = 0;
        DnsSvcbParameter mandatory = Known(DnsSvcbParameterKey.Mandatory);
        mandatory.MandatoryKeyValues.AddRange(values);
        data.Svcb.Parameters.Add(mandatory);

        AssertWriteInvalid(EncodeResult(64, data), "svcb.mandatory");
    }

    [Test]
    public void EncoderRejectsMalformedRawKnownValues()
    {
        DnsRecordData data = BasicData();
        DnsSvcbParameter port = Known(DnsSvcbParameterKey.Port);
        port.UseRawValue = true;
        port.RawValue = [53];
        data.Svcb!.Parameters.Add(port);

        WireWriteResult result = EncodeResult(65, data);

        AssertWriteInvalid(result, "svcb.port");
    }

    [Test]
    public void EncoderRejectsInvalidTypedFields()
    {
        DnsRecordData noAlpn = BasicData();
        DnsSvcbParameter noDefault = Known(DnsSvcbParameterKey.NoDefaultAlpn);
        noDefault.NoDefaultAlpn = true;
        noAlpn.Svcb!.Parameters.Add(noDefault);

        DnsRecordData badEch = BasicData();
        DnsSvcbParameter ech = Known(DnsSvcbParameterKey.Ech);
        ech.Ech = [0, 1, 0];
        badEch.Svcb!.Parameters.Add(ech);

        DnsRecordData badPath = BasicData();
        DnsSvcbParameter path = Known(DnsSvcbParameterKey.DohPath);
        path.DohPath = "/dns-query";
        badPath.Svcb!.Parameters.Add(path);

        DnsRecordData badOhttp = BasicData();
        DnsSvcbParameter ohttp = Known(DnsSvcbParameterKey.ObliviousHttp);
        ohttp.ObliviousHttp = [1];
        badOhttp.Svcb!.Parameters.Add(ohttp);

        Assert.Multiple(() =>
        {
            AssertWriteInvalid(EncodeResult(64, noAlpn), "svcb.noDefaultAlpn");
            AssertWriteInvalid(EncodeResult(64, badEch), "svcb.ech");
            AssertWriteInvalid(EncodeResult(64, badPath), "svcb.dohPath");
            AssertWriteInvalid(EncodeResult(64, badOhttp), "svcb.obliviousHttp");
        });
    }

    [TestCase("dns-query{?dns}")]
    [TestCase("/dns-query{#dns}")]
    [TestCase("/dns-query{?dns,,x}")]
    [TestCase("/dns-query{?dns:0}")]
    [TestCase("/dns-query{?dns}%zz")]
    public void EncoderRejectsMalformedDohPathTemplates(string value)
    {
        DnsRecordData data = BasicData();
        DnsSvcbParameter path = Known(DnsSvcbParameterKey.DohPath);
        path.DohPath = value;
        data.Svcb!.Parameters.Add(path);

        AssertWriteInvalid(EncodeResult(64, data), "svcb.dohPath");
    }

    static DnsRecordData CompleteData()
    {
        DnsRecordData data = BasicData();
        DnsSvcbParameter mandatory = Known(DnsSvcbParameterKey.Mandatory);
        mandatory.MandatoryKeyValues.AddRange([65400, 8, 7, 5, 3, 1]);
        DnsSvcbParameter alpn = Known(DnsSvcbParameterKey.Alpn);
        alpn.AlpnIdentifiers.Add([104, 50]);
        alpn.AlpnIdentifiers.Add([104, 51]);
        DnsSvcbParameter noDefault = Known(DnsSvcbParameterKey.NoDefaultAlpn);
        noDefault.NoDefaultAlpn = true;
        DnsSvcbParameter ipv4 = Known(DnsSvcbParameterKey.Ipv4Hint);
        ipv4.Ipv4Hints.Add([192, 0, 2, 1]);
        DnsSvcbParameter ech = Known(DnsSvcbParameterKey.Ech);
        ech.Ech = [0, 4, 254, 13, 0, 0];
        DnsSvcbParameter ipv6 = Known(DnsSvcbParameterKey.Ipv6Hint);
        ipv6.Ipv6Hints.Add(Sequence(16));
        DnsSvcbParameter path = Known(DnsSvcbParameterKey.DohPath);
        path.DohPath = "/dns-query{?dns}";
        DnsSvcbParameter ohttp = Known(DnsSvcbParameterKey.ObliviousHttp);

        data.Svcb!.Parameters.Add(ohttp);
        data.Svcb.Parameters.Add(path);
        data.Svcb.Parameters.Add(Unknown(65400, [9, 8, 7]));
        data.Svcb.Parameters.Add(ipv6);
        data.Svcb.Parameters.Add(ech);
        data.Svcb.Parameters.Add(ipv4);
        data.Svcb.Parameters.Add(Port(8443));
        data.Svcb.Parameters.Add(noDefault);
        data.Svcb.Parameters.Add(alpn);
        data.Svcb.Parameters.Add(mandatory);
        return data;
    }

    static DnsRecordData BasicData()
    {
        DnsRecordData data = new DnsRecordData();
        data.Svcb = new DnsSvcbData();
        data.Svcb.Priority = 1;
        data.Svcb.TargetName = Parse("service.example.");
        return data;
    }

    static DnsSvcbParameter Known(DnsSvcbParameterKey key)
    {
        DnsSvcbParameter parameter = new DnsSvcbParameter();
        parameter.Key = key;
        return parameter;
    }

    static DnsSvcbParameter Port(int value)
    {
        DnsSvcbParameter parameter = Known(DnsSvcbParameterKey.Port);
        parameter.HasPort = true;
        parameter.Port = value;
        return parameter;
    }

    static DnsSvcbParameter Unknown(int key, byte[] value)
    {
        DnsSvcbParameter parameter = new DnsSvcbParameter();
        parameter.KeyValue = key;
        parameter.RawValue = value;
        return parameter;
    }

    static int ParameterKey(DnsSvcbParameter parameter)
    {
        return parameter.KeyValue != 0 || parameter.Key == 0 ?
            parameter.KeyValue : (int)parameter.Key;
    }

    static byte[] Wire(params byte[][] parameters)
    {
        List<byte> bytes = new List<byte> { 0, 1, 0 };
        foreach (byte[] parameter in parameters) bytes.AddRange(parameter);
        return bytes.ToArray();
    }

    static byte[] ParameterWire(int key, byte[] value)
    {
        List<byte> bytes = new List<byte>();
        bytes.Add((byte)(key >> 8));
        bytes.Add((byte)key);
        bytes.Add((byte)(value.Length >> 8));
        bytes.Add((byte)value.Length);
        bytes.AddRange(value);
        return bytes.ToArray();
    }

    static byte[] Sequence(int length)
    {
        return Enumerable.Range(1, length).Select(value => (byte)value).ToArray();
    }

    static DnsDecodeResult Decode(int recordTypeValue, byte[] bytes)
    {
        return ExtendedRecordDataCodec.DecodeRecordData(
            recordTypeValue, bytes, 0, bytes.Length, new DnsDecodeOptions());
    }

    static byte[] Encode(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[4096], 0, 4096);
        WireWriteResult result = ExtendedRecordDataCodec.EncodeRecordData(
            writer,
            new CompressionTable(0),
            recordTypeValue,
            data,
            new DnsEncodeOptions());
        Assert.That(result.Success, Is.True);
        return writer.CopyWrittenBytes();
    }

    static WireWriteResult EncodeResult(int recordTypeValue, DnsRecordData data)
    {
        WireWriter writer = new WireWriter(new byte[4096], 0, 4096);
        return ExtendedRecordDataCodec.EncodeRecordData(
            writer,
            new CompressionTable(0),
            recordTypeValue,
            data,
            new DnsEncodeOptions());
    }

    static void AssertInvalidParameters(DnsDecodeResult result, string? field)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidSvcbParameters));
        if (field != null)
        {
            Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
        }
    }

    static void AssertWriteInvalid(WireWriteResult result, string field)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Code,
            Is.EqualTo(DnsDiagnosticCode.InvalidSvcbParameters));
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
    }

    static DnsName Parse(string text)
    {
        return PresentationDnsName.ParseName(text, null).Name!;
    }

    static string Format(DnsName name)
    {
        return PresentationDnsName.FormatName(name).Text!;
    }
}
