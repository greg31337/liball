using System;
using System.Net;

namespace diameter.Tests;

public class CodecDataTests
{
    [Test]
    public void CodecResultDefaultsAreUsable()
    {
        DiameterEncodeResult encode = new DiameterEncodeResult();
        DiameterDecodeResult decode = new DiameterDecodeResult();
        DiameterAvpDecodeResult avpDecode = new DiameterAvpDecodeResult();
        DiameterFramingResult framing = new DiameterFramingResult();
        DiameterCodecIssue issue = new DiameterCodecIssue();
        DiameterValueResult value = new DiameterValueResult();

        Assert.Multiple(() =>
        {
            Assert.That(encode.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encode.RequiredCount, Is.Zero);
            Assert.That(encode.WrittenCount, Is.Zero);
            Assert.That(encode.Data, Is.Empty);
            Assert.That(encode.Issues, Is.Empty);

            Assert.That(decode.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decode.RequiredCount, Is.Zero);
            Assert.That(decode.ConsumedCount, Is.Zero);
            Assert.That(decode.Message, Is.Null);
            Assert.That(decode.Issues, Is.Empty);

            Assert.That(avpDecode.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(avpDecode.RequiredCount, Is.Zero);
            Assert.That(avpDecode.ConsumedCount, Is.Zero);
            Assert.That(avpDecode.Avp, Is.Null);
            Assert.That(avpDecode.Issues, Is.Empty);

            Assert.That(framing.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(framing.RequiredCount, Is.Zero);
            Assert.That(framing.ConsumedCount, Is.Zero);
            Assert.That(framing.Frames, Is.Empty);
            Assert.That(framing.Remainder, Is.Empty);
            Assert.That(framing.EndOfStream, Is.False);
            Assert.That(framing.CleanEndOfStream, Is.False);
            Assert.That(framing.Issues, Is.Empty);

            Assert.That(issue.Severity, Is.EqualTo(DiameterIssueSeverity.Information));
            Assert.That(issue.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(issue.Offset, Is.Zero);
            Assert.That(issue.Code, Is.Empty);
            Assert.That(issue.Message, Is.Empty);

            Assert.That(value.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(value.RequiredCount, Is.Zero);
            Assert.That(value.ConsumedCount, Is.Zero);
            Assert.That(value.WrittenCount, Is.Zero);
            Assert.That(value.Data, Is.Empty);
            Assert.That(value.Integer32, Is.Zero);
            Assert.That(value.Integer64, Is.Zero);
            Assert.That(value.Unsigned32, Is.Zero);
            Assert.That(value.Unsigned64, Is.Zero);
            Assert.That(value.Float32, Is.Zero);
            Assert.That(value.Float64, Is.Zero);
            Assert.That(value.Text, Is.Empty);
            Assert.That(value.Address, Is.Null);
            Assert.That(value.Time, Is.EqualTo(default(DateTimeOffset)));
            Assert.That(value.Identity, Is.Null);
            Assert.That(value.Uri, Is.Null);
            Assert.That(value.Avps, Is.Empty);
            Assert.That(value.Issues, Is.Empty);
        });
    }

    [Test]
    public void ResultsRepresentSuccessfulIncompleteAndInvalidOutcomes()
    {
        DiameterEncodeResult encode = new DiameterEncodeResult();
        encode.Status = DiameterCodecStatus.DestinationTooSmall;
        encode.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;

        DiameterEncodeResult encodeSuccess = new DiameterEncodeResult();
        encodeSuccess.Status = DiameterCodecStatus.Success;
        encodeSuccess.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
        encodeSuccess.WrittenCount = DiameterProtocolConstants.MessageHeaderSize;
        encodeSuccess.Data = new byte[DiameterProtocolConstants.MessageHeaderSize];

        DiameterDecodeResult decode = new DiameterDecodeResult();
        decode.Status = DiameterCodecStatus.Incomplete;
        decode.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
        decode.ConsumedCount = 0;

        DiameterCodecIssue issue = new DiameterCodecIssue();
        issue.Severity = DiameterIssueSeverity.Error;
        issue.Status = DiameterCodecStatus.Invalid;
        issue.Offset = 4;
        issue.Code = "invalid-length";
        issue.Message = "The declared AVP length is invalid.";

        DiameterAvpDecodeResult avpDecode = new DiameterAvpDecodeResult();
        avpDecode.Status = DiameterCodecStatus.Invalid;
        avpDecode.RequiredCount = DiameterProtocolConstants.AvpHeaderSize;
        avpDecode.ConsumedCount = 0;
        avpDecode.Issues.Add(issue);

        DiameterMessage message = new DiameterMessage();
        DiameterDecodeResult success = new DiameterDecodeResult();
        success.Status = DiameterCodecStatus.Success;
        success.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
        success.ConsumedCount = DiameterProtocolConstants.MessageHeaderSize;
        success.Message = message;

        Assert.Multiple(() =>
        {
            Assert.That(encode.Status, Is.EqualTo(DiameterCodecStatus.DestinationTooSmall));
            Assert.That(encode.RequiredCount, Is.EqualTo(20));
            Assert.That(encode.WrittenCount, Is.Zero);
            Assert.That(encodeSuccess.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encodeSuccess.WrittenCount, Is.EqualTo(20));
            Assert.That(encodeSuccess.Data, Has.Length.EqualTo(20));
            Assert.That(decode.Status, Is.EqualTo(DiameterCodecStatus.Incomplete));
            Assert.That(decode.RequiredCount, Is.EqualTo(20));
            Assert.That(decode.Message, Is.Null);
            Assert.That(avpDecode.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(avpDecode.Issues, Has.Count.EqualTo(1));
            Assert.That(avpDecode.Issues[0], Is.SameAs(issue));
            Assert.That(issue.Offset, Is.EqualTo(4));
            Assert.That(issue.Code, Is.EqualTo("invalid-length"));
            Assert.That(issue.Severity, Is.EqualTo(DiameterIssueSeverity.Error));
            Assert.That(issue.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(issue.Message, Is.EqualTo("The declared AVP length is invalid."));
            Assert.That(success.Message, Is.SameAs(message));
            Assert.That(success.ConsumedCount, Is.EqualTo(20));
        });
    }

    [Test]
    public void FramingResultRepresentsFramesRemainderAndEndOfStream()
    {
        byte[] completeFrame = new byte[DiameterProtocolConstants.MessageHeaderSize];
        byte[] remainder = new byte[] { 1, 0, 0, 20 };

        DiameterFramingResult incomplete = new DiameterFramingResult();
        incomplete.Status = DiameterCodecStatus.Incomplete;
        incomplete.RequiredCount = DiameterProtocolConstants.MessageHeaderSize;
        incomplete.ConsumedCount = completeFrame.Length;
        incomplete.Frames.Add(completeFrame);
        incomplete.Remainder = remainder;

        DiameterFramingResult cleanEnd = new DiameterFramingResult();
        cleanEnd.Status = DiameterCodecStatus.Success;
        cleanEnd.EndOfStream = true;
        cleanEnd.CleanEndOfStream = true;

        DiameterFramingResult truncatedEnd = new DiameterFramingResult();
        truncatedEnd.Status = DiameterCodecStatus.Invalid;
        truncatedEnd.EndOfStream = true;
        truncatedEnd.CleanEndOfStream = false;
        truncatedEnd.Remainder = remainder;

        Assert.Multiple(() =>
        {
            Assert.That(incomplete.Frames, Has.Count.EqualTo(1));
            Assert.That(incomplete.Frames[0], Is.SameAs(completeFrame));
            Assert.That(incomplete.Remainder, Is.SameAs(remainder));
            Assert.That(incomplete.RequiredCount, Is.EqualTo(20));
            Assert.That(incomplete.ConsumedCount, Is.EqualTo(20));
            Assert.That(cleanEnd.EndOfStream, Is.True);
            Assert.That(cleanEnd.CleanEndOfStream, Is.True);
            Assert.That(cleanEnd.Remainder, Is.Empty);
            Assert.That(truncatedEnd.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(truncatedEnd.EndOfStream, Is.True);
            Assert.That(truncatedEnd.CleanEndOfStream, Is.False);
            Assert.That(truncatedEnd.Remainder, Is.Not.Empty);
        });
    }

    [Test]
    public void ValueResultRoundTripsTypedValueStorage()
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "peer.example";
        DiameterUri uri = new DiameterUri();
        uri.Host = identity;
        DiameterAvp avp = new DiameterAvp();
        DateTimeOffset time = new DateTimeOffset(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

        DiameterValueResult value = new DiameterValueResult();
        value.Status = DiameterCodecStatus.Success;
        value.RequiredCount = 8;
        value.ConsumedCount = 8;
        value.WrittenCount = 8;
        value.Data = new byte[] { 1, 2, 3 };
        value.Integer32 = -1;
        value.Integer64 = -2;
        value.Unsigned32 = uint.MaxValue;
        value.Unsigned64 = ulong.MaxValue;
        value.Float32 = 1.5F;
        value.Float64 = 2.5;
        value.Text = "allow in ip from any to any";
        value.Address = IPAddress.Parse("192.0.2.1");
        value.Time = time;
        value.Identity = identity;
        value.Uri = uri;
        value.Avps.Add(avp);

        Assert.Multiple(() =>
        {
            Assert.That(value.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(value.RequiredCount, Is.EqualTo(8));
            Assert.That(value.ConsumedCount, Is.EqualTo(8));
            Assert.That(value.WrittenCount, Is.EqualTo(8));
            Assert.That(value.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(value.Integer32, Is.EqualTo(-1));
            Assert.That(value.Integer64, Is.EqualTo(-2L));
            Assert.That(value.Unsigned32, Is.EqualTo(uint.MaxValue));
            Assert.That(value.Unsigned64, Is.EqualTo(ulong.MaxValue));
            Assert.That(value.Float32, Is.EqualTo(1.5F));
            Assert.That(value.Float64, Is.EqualTo(2.5));
            Assert.That(value.Text, Is.EqualTo("allow in ip from any to any"));
            Assert.That(value.Address, Is.EqualTo(IPAddress.Parse("192.0.2.1")));
            Assert.That(value.Time, Is.EqualTo(time));
            Assert.That(value.Identity, Is.SameAs(identity));
            Assert.That(value.Uri, Is.SameAs(uri));
            Assert.That(value.Avps[0], Is.SameAs(avp));
        });
    }

    [Test]
    public void ResultStorageIsIndependentAndCanBeAttachedToEvents()
    {
        DiameterEncodeResult first = new DiameterEncodeResult();
        DiameterEncodeResult second = new DiameterEncodeResult();
        DiameterDecodeResult decode = new DiameterDecodeResult();
        DiameterAvpDecodeResult avpDecode = new DiameterAvpDecodeResult();
        DiameterFramingResult framing = new DiameterFramingResult();
        DiameterValueResult value = new DiameterValueResult();

        first.Data = new byte[] { 1 };
        first.Issues.Add(new DiameterCodecIssue());

        DiameterOperationalEvent eventData = new DiameterOperationalEvent();
        eventData.EncodeResult = first;
        eventData.DecodeResult = decode;
        eventData.AvpDecodeResult = avpDecode;
        eventData.FramingResult = framing;
        eventData.ValueResult = value;

        Assert.Multiple(() =>
        {
            Assert.That(second.Data, Is.Empty);
            Assert.That(second.Issues, Is.Empty);
            Assert.That(first.Data, Is.Not.SameAs(second.Data));
            Assert.That(first.Issues, Is.Not.SameAs(second.Issues));
            Assert.That(eventData.EncodeResult, Is.SameAs(first));
            Assert.That(eventData.DecodeResult, Is.SameAs(decode));
            Assert.That(eventData.AvpDecodeResult, Is.SameAs(avpDecode));
            Assert.That(eventData.FramingResult, Is.SameAs(framing));
            Assert.That(eventData.ValueResult, Is.SameAs(value));
        });
    }
}
