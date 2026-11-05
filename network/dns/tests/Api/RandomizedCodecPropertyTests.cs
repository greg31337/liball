using System.Text;

namespace dns.Tests.Api;

public class RandomizedCodecPropertyTests
{
    [Test]
    public void NamesRoundTripAndRejectMalformedLabelKindsWithinBound()
    {
        Random random = new Random(16201);
        IDnsNameService service = new DnsNameService();
        int iteration = 0;
        while (iteration < 96)
        {
            string text = RandomLabel(random) + "." +
                RandomLabel(random) + ".example.";
            DnsName name = service.ParseName(text, null);
            DnsEncodeResult encoded = service.EncodeName(
                name,
                new DnsEncodeOptions());
            DnsDecodeResult decoded = service.TryDecodeName(
                encoded.Bytes,
                0,
                new DnsDecodeOptions());
            DnsEncodeResult reencoded = service.TryEncodeName(
                decoded.Name!,
                new DnsEncodeOptions());
            byte[] malformed = new byte[random.Next(2, 12)];
            random.NextBytes(malformed);
            malformed[0] = (byte)random.Next(64, 192);
            DnsDecodeResult rejected = service.TryDecodeName(
                malformed,
                0,
                new DnsDecodeOptions());

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Success, Is.True, text);
                Assert.That(reencoded.Bytes, Is.EqualTo(encoded.Bytes), text);
                Assert.That(service.NamesEqual(name, decoded.Name!), Is.True,
                    text);
                Assert.That(rejected.Success, Is.False,
                    Convert.ToHexString(malformed));
            });
            iteration = iteration + 1;
        }
    }

    [Test]
    public void OpaqueEdnsOptionsRoundTripAndRejectTruncationWithinBound()
    {
        Random random = new Random(16202);
        IDnsRecordDataCodec codec = new DnsRecordDataCodec();
        int iteration = 0;
        while (iteration < 64)
        {
            DnsEdnsOption option = new DnsEdnsOption();
            option.Code = (DnsEdnsOptionCode)(65000 + iteration);
            option.CodeValue = 65000 + iteration;
            option.UseRawData = true;
            option.RawData = new byte[random.Next(1, 33)];
            random.NextBytes(option.RawData);
            DnsEdnsOptionResult encoded = codec.TryEncodeEdnsOption(option);
            DnsEdnsOptionResult decoded = codec.TryDecodeEdnsOption(
                encoded.Bytes,
                new DnsDecodeOptions());
            DnsEdnsOptionResult reencoded = codec.TryEncodeEdnsOption(
                decoded.Option!);
            byte[] truncated = new byte[encoded.Bytes.Length - 1];
            Array.Copy(encoded.Bytes, truncated, truncated.Length);
            DnsEdnsOptionResult malformed = codec.TryDecodeEdnsOption(
                truncated,
                new DnsDecodeOptions());

            Assert.Multiple(() =>
            {
                Assert.That(decoded.Success, Is.True);
                Assert.That(decoded.Option!.RawData,
                    Is.EqualTo(option.RawData));
                Assert.That(reencoded.Bytes, Is.EqualTo(encoded.Bytes));
                Assert.That(malformed.Success, Is.False);
            });
            iteration = iteration + 1;
        }
    }

    [Test]
    public void MessagesAndTcpFramesRoundTripRandomizedDataWithinBound()
    {
        Random random = new Random(16203);
        IDnsMessageCodec codec = new DnsMessageCodec();
        int iteration = 0;
        while (iteration < 64)
        {
            DnsMessage message = Message(random, iteration);
            DnsEncodeResult encoded = codec.TryEncodeMessage(
                message,
                new DnsEncodeOptions());
            DnsDecodeResult decoded = codec.TryDecodeMessage(
                encoded.Bytes,
                new DnsDecodeOptions());
            DnsEncodeResult reencoded = codec.TryEncodeMessage(
                decoded.Message!,
                new DnsEncodeOptions());
            DnsDecodeResult decodedAgain = codec.TryDecodeMessage(
                reencoded.Bytes,
                new DnsDecodeOptions());
            DnsTcpFrame frame = new DnsTcpFrame();
            frame.MessageBytes = encoded.Bytes;
            frame.Complete = true;
            DnsEncodeResult framed = codec.TryEncodeTcpFrame(
                frame,
                new DnsEncodeOptions());
            DnsDecodeResult decodedFrame = codec.TryDecodeTcpFrame(
                framed.Bytes,
                new DnsDecodeOptions());
            byte[] partialBytes = new byte[framed.Bytes.Length - 1];
            Array.Copy(framed.Bytes, partialBytes, partialBytes.Length);
            DnsDecodeResult partial = codec.TryDecodeTcpFrame(
                partialBytes,
                new DnsDecodeOptions());
            byte[] malformedBytes = new byte[random.Next(0, 12)];
            random.NextBytes(malformedBytes);
            DnsDecodeResult malformed = codec.TryDecodeMessage(
                malformedBytes,
                new DnsDecodeOptions());

            Assert.Multiple(() =>
            {
                Assert.That(encoded.Success, Is.True);
                Assert.That(decoded.Success, Is.True);
                Assert.That(reencoded.Bytes, Is.EqualTo(encoded.Bytes));
                Assert.That(decodedAgain.Success, Is.True);
                Assert.That(codec.MessagesEqual(
                    decoded.Message!,
                    decodedAgain.Message!,
                    new DnsCanonicalOrderOptions()), Is.True);
                Assert.That(decodedFrame.Success, Is.True);
                Assert.That(decodedFrame.TcpFrame!.Complete, Is.True);
                Assert.That(decodedFrame.TcpFrame.MessageBytes,
                    Is.EqualTo(encoded.Bytes));
                Assert.That(partial.Success, Is.True);
                Assert.That(partial.TcpFrame!.Complete, Is.False);
                Assert.That(malformed.Success, Is.False);
            });
            iteration = iteration + 1;
        }
    }

    [Test]
    public void ZoneTextRoundTripsAndRejectsMalformedAddressesWithinBound()
    {
        Random random = new Random(16204);
        DnsZoneCodec codec = new DnsZoneCodec();
        int iteration = 0;
        while (iteration < 32)
        {
            string owner = RandomLabel(random);
            int octet = random.Next(1, 255);
            string source =
                "$ORIGIN example.\n" +
                "@ 300 SOA ns.example. hostmaster.example. " +
                    (iteration + 1) + " 3600 600 86400 300\n" +
                "@ 300 NS ns.example.\n" +
                "ns 300 A 192.0.2.53\n" +
                owner + " 300 A 192.0.2." + octet + "\n" +
                owner + " 300 TXT \"value-" + iteration + "\"\n";
            DnsZoneLoadOptions loadOptions = new DnsZoneLoadOptions();
            loadOptions.ValidateZone = true;
            DnsZoneLoadResult loaded = codec.ParseZone(source, loadOptions);
            DnsTextOptions textOptions = new DnsTextOptions();
            textOptions.Canonical = iteration % 2 == 0;
            DnsTextResult formatted = codec.FormatZone(
                loaded.Zone!,
                textOptions);
            DnsZoneLoadResult reparsed = codec.ParseZone(
                formatted.Text,
                loadOptions);
            DnsZoneLoadResult malformed = codec.ParseZone(
                "$ORIGIN example.\nbad 300 A 999.999.999." +
                    random.Next(256, 1000) + "\n",
                new DnsZoneLoadOptions());

            Assert.Multiple(() =>
            {
                Assert.That(loaded.Success, Is.True, source);
                Assert.That(loaded.RecordsLoaded, Is.EqualTo(5));
                Assert.That(formatted.Success, Is.True);
                Assert.That(reparsed.Success, Is.True, formatted.Text);
                Assert.That(reparsed.RecordsLoaded, Is.EqualTo(5));
                Assert.That(malformed.Success, Is.False);
            });
            iteration = iteration + 1;
        }
    }

    static DnsMessage Message(Random random, int iteration)
    {
        string owner = RandomLabel(random) + ".example.";
        DnsName name = new DnsNameService().ParseName(owner, null);
        DnsMessage result = new DnsMessage();
        result.Header.Identifier = (ushort)random.Next(0, 65536);
        result.Header.Flags = DnsHeaderFlag.Response |
            DnsHeaderFlag.AuthoritativeAnswer;
        DnsQuestion question = new DnsQuestion();
        question.Name = name;
        question.RecordType = DnsRecordType.A;
        question.RecordTypeValue = (int)DnsRecordType.A;
        question.Class = DnsClass.Internet;
        question.ClassValue = (int)DnsClass.Internet;
        result.Questions.Add(question);
        DnsResourceRecord answer = new DnsResourceRecord();
        answer.Name = name;
        answer.RecordType = DnsRecordType.A;
        answer.RecordTypeValue = (int)DnsRecordType.A;
        answer.Class = DnsClass.Internet;
        answer.ClassValue = (int)DnsClass.Internet;
        answer.TimeToLive = (uint)random.Next(0, 86401);
        answer.Data = new DnsRecordData();
        answer.Data.Address = new DnsAddressData();
        answer.Data.Address.Address =
            [192, 0, (byte)iteration, (byte)random.Next(1, 255)];
        result.Answers.Add(answer);
        return result;
    }

    static string RandomLabel(Random random)
    {
        int length = random.Next(1, 16);
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < length)
        {
            result.Append((char)('a' + random.Next(0, 26)));
            index = index + 1;
        }

        return result.ToString();
    }
}
