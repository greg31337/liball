namespace dns.Tests.Internal.Messages;

public class OpaqueEdnsPolicyTests
{
    [TestCase(0)]
    [TestCase(1)]
    [TestCase(4)]
    [TestCase(16)]
    [TestCase(17)]
    [TestCase(20)]
    [TestCase(21)]
    [TestCase(22)]
    [TestCase(23)]
    [TestCase(24)]
    [TestCase(25)]
    [TestCase(26)]
    [TestCase(20292)]
    [TestCase(26946)]
    [TestCase(65000)]
    [TestCase(65001)]
    [TestCase(65534)]
    [TestCase(65535)]
    public void ReservedAssignedFutureAndExperimentalCodesStayOpaque(
        int code)
    {
        byte[] frame = Frame(code, [0, 255, 85]);
        IDnsRecordDataCodec codec = new DnsRecordDataCodec();

        DnsEdnsOptionResult decoded = codec.TryDecodeEdnsOption(
            frame, new DnsDecodeOptions());
        frame[4] = 99;
        DnsEdnsOptionResult encoded = codec.TryEncodeEdnsOption(
            decoded.Option!);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Option!.CodeValue, Is.EqualTo(code));
            Assert.That(decoded.Option.UseRawData, Is.True);
            Assert.That(decoded.Option.RawData,
                Is.EqualTo(new byte[] { 0, 255, 85 }));
            Assert.That(encoded.Success, Is.True);
            Assert.That(encoded.Bytes,
                Is.EqualTo(Frame(code, [0, 255, 85])));
        });
    }

    [Test]
    public void MessageRoundTripPreservesMixedOptionOrderAndDuplicates()
    {
        DnsEdnsOption privateFirst = Opaque(65001, [1, 2]);
        DnsEdnsOption firstCookie = Cookie(1);
        DnsEdnsOption privateSecond = Opaque(65001, [3]);
        DnsEdnsOption secondCookie = Cookie(2);
        DnsEdnsOption padding = Padding([0, 255]);
        DnsEdnsOption llq = Opaque(1, [9, 8, 7]);
        DnsMessage message = Message(
            privateFirst,
            firstCookie,
            privateSecond,
            secondCookie,
            padding,
            llq);

        DnsEncodeResult encoded = Encode(message);
        DnsDecodeResult decoded = Decode(encoded.Bytes);
        DnsEdnsData edns =
            decoded.Message!.Additionals[0].Data!.Edns!;
        DnsEncodeResult reencoded = Encode(decoded.Message);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(edns.Options.Select(option => option.CodeValue),
                Is.EqualTo(new[] { 65001, 10, 65001, 10, 12, 1 }));
            Assert.That(edns.Options[0].RawData,
                Is.EqualTo(new byte[] { 1, 2 }));
            Assert.That(edns.Options[2].RawData,
                Is.EqualTo(new byte[] { 3 }));
            Assert.That(edns.Options[1].Cookie!.ClientCookie[0],
                Is.EqualTo(1));
            Assert.That(edns.Options[3].Cookie!.ClientCookie[0],
                Is.EqualTo(2));
            Assert.That(reencoded.Bytes, Is.EqualTo(encoded.Bytes));
        });
    }

    [Test]
    public void DuplicatePaddingIsRejectedOnEncodeAndDecode()
    {
        DnsEncodeResult encoded = Encode(
            Message(Padding([]), Padding([0])));
        byte[] optionBytes = Frame(12, [])
            .Concat(Frame(12, [0]))
            .ToArray();
        DnsDecodeResult decoded = Decode(
            Header().Concat(OptWire(optionBytes)).ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.False);
            Assert.That(encoded.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.padding"));
            Assert.That(encoded.Diagnostics[0].HasOptionIndex, Is.True);
            Assert.That(encoded.Diagnostics[0].OptionIndex, Is.EqualTo(1));
            Assert.That(decoded.Success, Is.False);
            Assert.That(decoded.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.padding"));
            Assert.That(decoded.Diagnostics[0].OptionIndex, Is.EqualTo(1));
        });
    }

    [Test]
    public void MalformedOptionFramingReportsExactOptionIndex()
    {
        byte[] first = Frame(65001, [170]);
        byte[] shortHeader = first.Concat(new byte[] { 0, 1, 0 }).ToArray();
        byte[] shortPayload = first
            .Concat(new byte[] { 0, 1, 0, 2, 187 })
            .ToArray();

        DnsDecodeResult headerResult = Decode(
            Header().Concat(OptWire(shortHeader)).ToArray());
        DnsDecodeResult lengthResult = Decode(
            Header().Concat(OptWire(shortPayload)).ToArray());

        Assert.Multiple(() =>
        {
            AssertOptionDiagnostic(
                headerResult, "edns.option.header", 1);
            AssertOptionDiagnostic(
                lengthResult, "edns.option.length", 1);
        });
    }

    [Test]
    public void EncodingFailureReportsExactOptionIndex()
    {
        DnsEdnsOption malformedCookie = Cookie(3);
        malformedCookie.Cookie!.ClientCookie = new byte[7];
        DnsMessage message = Message(
            Opaque(65001, [1]), malformedCookie);

        DnsEncodeResult result = Encode(message);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics[0].Field,
                Is.EqualTo("ednsOption.cookie.clientCookie"));
            Assert.That(result.Diagnostics[0].HasOptionIndex, Is.True);
            Assert.That(result.Diagnostics[0].OptionIndex, Is.EqualTo(1));
            Assert.That(result.Diagnostics[0].Section,
                Is.EqualTo(DnsMessageSection.Additional));
            Assert.That(result.Diagnostics[0].RecordIndex, Is.Zero);
        });
    }

    [Test]
    public void TypedFallbackPreservesMalformedPayloadAndFollowingOptions()
    {
        byte[] malformedSubnet =
            Frame(8, [0, 1, 25, 0, 192, 0, 2, 129]);
        byte[] privateOption = Frame(65001, [4, 5, 6]);
        byte[] wire = Header()
            .Concat(OptWire(malformedSubnet.Concat(privateOption).ToArray()))
            .ToArray();
        DnsDecodeOptions fallbackOptions = new DnsDecodeOptions();
        fallbackOptions.FallBackToOpaqueRecordData = true;

        DnsDecodeResult strict = Decode(wire);
        DnsDecodeResult fallback = new DnsMessageCodec().TryDecodeMessage(
            wire, fallbackOptions);
        DnsEncodeResult reencoded = Encode(fallback.Message!);

        Assert.Multiple(() =>
        {
            Assert.That(strict.Success, Is.False);
            Assert.That(strict.Diagnostics[0].OptionIndex, Is.Zero);
            Assert.That(fallback.Success, Is.True);
            Assert.That(fallback.Message!.Additionals[0].Data!.Edns!
                .Options[0].UseRawData, Is.True);
            Assert.That(fallback.Message.Additionals[0].Data!.Edns!
                .Options[0].RawData,
                Is.EqualTo(new byte[] { 0, 1, 25, 0, 192, 0, 2, 129 }));
            Assert.That(reencoded.Bytes, Is.EqualTo(wire));
        });
    }

    static void AssertOptionDiagnostic(
        DnsDecodeResult result,
        string field,
        int optionIndex)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics[0].Field, Is.EqualTo(field));
        Assert.That(result.Diagnostics[0].HasOptionIndex, Is.True);
        Assert.That(result.Diagnostics[0].OptionIndex,
            Is.EqualTo(optionIndex));
        Assert.That(result.Diagnostics[0].Section,
            Is.EqualTo(DnsMessageSection.Additional));
        Assert.That(result.Diagnostics[0].RecordIndex, Is.Zero);
    }

    static DnsEdnsOption Opaque(int code, byte[] payload)
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = (DnsEdnsOptionCode)code;
        option.CodeValue = code;
        option.RawData = payload;
        option.UseRawData = true;
        return option;
    }

    static DnsEdnsOption Cookie(byte firstByte)
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.Cookie;
        option.CodeValue = 10;
        option.Cookie = new DnsEdnsCookie();
        option.Cookie.ClientCookie =
            [firstByte, 0, 0, 0, 0, 0, 0, 0];
        return option;
    }

    static DnsEdnsOption Padding(byte[] bytes)
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.Padding;
        option.CodeValue = 12;
        option.Padding = bytes;
        return option;
    }

    static DnsMessage Message(params DnsEdnsOption[] options)
    {
        DnsEdnsData edns = new DnsEdnsData();
        edns.Options.AddRange(options);
        DnsRecordData data = new DnsRecordData();
        data.Edns = edns;
        DnsResourceRecord opt = new DnsResourceRecord();
        opt.Name = new DnsNameService().ParseName(".", null);
        opt.RecordType = DnsRecordType.Opt;
        opt.RecordTypeValue = 41;
        opt.Data = data;
        DnsMessage message = new DnsMessage();
        message.Additionals.Add(opt);
        return message;
    }

    static DnsEncodeResult Encode(DnsMessage message)
    {
        return new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
    }

    static DnsDecodeResult Decode(byte[] wire)
    {
        return new DnsMessageCodec().TryDecodeMessage(
            wire, new DnsDecodeOptions());
    }

    static byte[] Frame(int code, byte[] payload)
    {
        byte[] frame = new byte[4 + payload.Length];
        frame[0] = (byte)(code >> 8);
        frame[1] = (byte)code;
        frame[2] = (byte)(payload.Length >> 8);
        frame[3] = (byte)payload.Length;
        payload.CopyTo(frame, 4);
        return frame;
    }

    static byte[] Header()
    {
        byte[] header = new byte[12];
        header[11] = 1;
        return header;
    }

    static byte[] OptWire(byte[] options)
    {
        byte[] fixedFields =
        [
            0,
            0, 41,
            4, 208,
            0, 0, 0, 0,
            (byte)(options.Length >> 8), (byte)options.Length
        ];
        return fixedFields.Concat(options).ToArray();
    }
}
