namespace dns.Tests.Internal.Records;

public class EdnsCookieOptionCodecTests
{
    [Test]
    public void RoundTripsRequestAndMinimumAndMaximumServerCookies()
    {
        DnsEdnsOption request = CookieOption(
            [0, 1, 2, 3, 4, 5, 6, 7], []);
        DnsEdnsOption minimum = CookieOption(
            [8, 9, 10, 11, 12, 13, 14, 15],
            [16, 17, 18, 19, 20, 21, 22, 23]);
        DnsEdnsOption maximum = CookieOption(
            [1, 1, 1, 1, 1, 1, 1, 1],
            Enumerable.Range(0, 32).Select(value => (byte)value).ToArray());

        DnsEdnsOption decodedRequest = Decode(Encode(request).Bytes);
        DnsEdnsOption decodedMinimum = Decode(Encode(minimum).Bytes);
        DnsEdnsOption decodedMaximum = Decode(Encode(maximum).Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(decodedRequest.UseRawData, Is.False);
            Assert.That(decodedRequest.Cookie!.ClientCookie,
                Is.EqualTo(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }));
            Assert.That(decodedRequest.Cookie.ServerCookie, Is.Empty);
            Assert.That(decodedMinimum.Cookie!.ServerCookie,
                Is.EqualTo(new byte[] { 16, 17, 18, 19, 20, 21, 22, 23 }));
            Assert.That(decodedMaximum.Cookie!.ServerCookie,
                Has.Length.EqualTo(32));
        });
    }

    [Test]
    public void RejectsMalformedWireLengthsAndCanPreserveOpaqueData()
    {
        int[] payloadLengths = [0, 7, 9, 15, 41];
        IDnsRecordDataCodec codec = new DnsRecordDataCodec();

        int index = 0;
        while (index < payloadLengths.Length)
        {
            byte[] frame = Frame(payloadLengths[index]);
            DnsEdnsOptionResult rejected = codec.TryDecodeEdnsOption(
                frame, new DnsDecodeOptions());
            DnsDecodeOptions fallbackOptions = new DnsDecodeOptions();
            fallbackOptions.FallBackToOpaqueRecordData = true;
            DnsEdnsOptionResult preserved = codec.TryDecodeEdnsOption(
                frame, fallbackOptions);

            Assert.Multiple(() =>
            {
                Assert.That(rejected.Success, Is.False);
                Assert.That(rejected.Diagnostics[0].Field,
                    Is.EqualTo("ednsOption.cookie.length"));
                Assert.That(preserved.Success, Is.True);
                Assert.That(preserved.Option!.UseRawData, Is.True);
                Assert.That(preserved.Option.RawData,
                    Has.Length.EqualTo(payloadLengths[index]));
            });
            index = index + 1;
        }
    }

    [Test]
    public void RejectsInvalidTypedClientAndServerLengths()
    {
        DnsEdnsOption missing = Option();
        DnsEdnsOption shortClient = CookieOption(new byte[7], []);
        DnsEdnsOption longClient = CookieOption(new byte[9], []);
        DnsEdnsOption shortServer = CookieOption(new byte[8], new byte[7]);
        DnsEdnsOption longServer = CookieOption(new byte[8], new byte[33]);

        Assert.Multiple(() =>
        {
            Assert.That(Encode(missing).Success, Is.False);
            Assert.That(Encode(shortClient).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.cookie.clientCookie"));
            Assert.That(Encode(longClient).Success, Is.False);
            Assert.That(Encode(shortServer).Diagnostics[0].Field,
                Is.EqualTo("ednsOption.cookie.serverCookie"));
            Assert.That(Encode(longServer).Success, Is.False);
        });
    }

    [Test]
    public void WholeMessageRoundTripUsesTypedCookieData()
    {
        DnsEdnsOption option = CookieOption(
            [36, 100, 196, 171, 207, 16, 201, 87],
            [1, 0, 0, 0, 92, 247, 159, 17,
                31, 129, 48, 195, 238, 226, 148, 128]);
        DnsMessage message = Message(option);

        DnsEncodeResult encoded = new DnsMessageCodec().TryEncodeMessage(
            message, new DnsEncodeOptions());
        DnsDecodeResult decoded = new DnsMessageCodec().TryDecodeMessage(
            encoded.Bytes, new DnsDecodeOptions());
        DnsEdnsCookie cookie = decoded.Message!.Additionals[0]
            .Data!.Edns!.Options[0].Cookie!;

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Success, Is.True);
            Assert.That(decoded.Success, Is.True);
            Assert.That(cookie.ClientCookie,
                Is.EqualTo(option.Cookie!.ClientCookie));
            Assert.That(cookie.ServerCookie,
                Is.EqualTo(option.Cookie.ServerCookie));
        });
    }

    static DnsEdnsOption Option()
    {
        DnsEdnsOption option = new DnsEdnsOption();
        option.Code = DnsEdnsOptionCode.Cookie;
        option.CodeValue = 10;
        return option;
    }

    static DnsEdnsOption CookieOption(
        byte[] clientCookie,
        byte[] serverCookie)
    {
        DnsEdnsOption option = Option();
        option.Cookie = new DnsEdnsCookie();
        option.Cookie.ClientCookie = clientCookie;
        option.Cookie.ServerCookie = serverCookie;
        return option;
    }

    static DnsEdnsOptionResult Encode(DnsEdnsOption option)
    {
        return new DnsRecordDataCodec().TryEncodeEdnsOption(option);
    }

    static DnsEdnsOption Decode(byte[] frame)
    {
        return new DnsRecordDataCodec().DecodeEdnsOption(
            frame, new DnsDecodeOptions());
    }

    static byte[] Frame(int payloadLength)
    {
        byte[] frame = new byte[4 + payloadLength];
        frame[1] = 10;
        frame[2] = (byte)(payloadLength >> 8);
        frame[3] = (byte)payloadLength;
        return frame;
    }

    static DnsMessage Message(DnsEdnsOption option)
    {
        DnsEdnsData edns = new DnsEdnsData();
        edns.Options.Add(option);
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
}
