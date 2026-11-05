using System;
using System.Text;

namespace diameter.Tests;

public class TextAndIdentityCodecsTests
{
    [Test]
    public void Utf8StringRoundTripsAsciiAndMultibyteCodePoints()
    {
        string value = "Grüße 世界";
        byte[] destination = new byte[Encoding.UTF8.GetByteCount(value)];

        DiameterValueResult encoding = ValueCodecs.EncodeUtf8String(destination, value);
        DiameterValueResult decoding = ValueCodecs.DecodeUtf8String(destination);

        Assert.Multiple(() =>
        {
            Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encoding.RequiredCount, Is.EqualTo(destination.Length));
            Assert.That(encoding.WrittenCount, Is.EqualTo(destination.Length));
            Assert.That(
                destination,
                Is.EqualTo(new byte[]
                {
                    71, 114, 195, 188, 195, 159, 101, 32,
                    228, 184, 150, 231, 149, 140
                }));
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoding.RequiredCount, Is.EqualTo(destination.Length));
            Assert.That(decoding.ConsumedCount, Is.EqualTo(destination.Length));
            Assert.That(decoding.Text, Is.EqualTo(value));
        });
    }

    [Test]
    public void Utf8DecoderAcceptsEveryModernEncodingWidthBoundary()
    {
        byte[] source = new byte[]
        {
            1,
            127,
            194, 128,
            224, 160, 128,
            244, 143, 191, 191
        };

        DiameterValueResult result = ValueCodecs.DecodeUtf8String(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(result.ConsumedCount, Is.EqualTo(source.Length));
            Assert.That(result.Text.Length, Is.EqualTo(6));
            Assert.That(result.Text[0], Is.EqualTo('\u0001'));
            Assert.That(result.Text[1], Is.EqualTo('\u007f'));
            Assert.That(result.Text[2], Is.EqualTo('\u0080'));
            Assert.That(result.Text[3], Is.EqualTo('\u0800'));
            Assert.That(char.IsSurrogatePair(result.Text, 4), Is.True);
        });
    }

    [Test]
    public void Utf8DecoderRejectsNullOverlongSurrogateOutOfRangeAndTruncatedSequences()
    {
        byte[][] invalidValues = new byte[][]
        {
            new byte[] { 0 },
            new byte[] { 192, 128 },
            new byte[] { 224, 128, 128 },
            new byte[] { 237, 160, 128 },
            new byte[] { 244, 144, 128, 128 },
            new byte[] { 240, 159, 146 },
            new byte[] { 128 }
        };

        int index = 0;
        while (index < invalidValues.Length)
        {
            DiameterValueResult result = ValueCodecs.DecodeUtf8String(invalidValues[index]);
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Offset, Is.Zero);
            Assert.That(result.Issues[0].Code, Is.EqualTo("invalid-utf8"));
            index = index + 1;
        }
    }

    [Test]
    public void Utf8EncoderRejectsNullCodePointsAndUnpairedSurrogates()
    {
        DiameterValueResult missing = ValueCodecs.EncodeUtf8String(new byte[0], null);
        DiameterValueResult nullCodePoint = ValueCodecs.EncodeUtf8String(new byte[4], "a\0b");
        DiameterValueResult highSurrogate = ValueCodecs.EncodeUtf8String(new byte[4], "\ud800");
        DiameterValueResult lowSurrogate = ValueCodecs.EncodeUtf8String(new byte[4], "\udc00");

        Assert.Multiple(() =>
        {
            AssertInvalidText(missing, 0, "missing-utf8-string");
            AssertInvalidText(nullCodePoint, 1, "invalid-utf8-string");
            AssertInvalidText(highSurrogate, 0, "invalid-utf8-string");
            AssertInvalidText(lowSurrogate, 0, "invalid-utf8-string");
        });
    }

    [Test]
    public void Utf8EncodingReportsOctetSizeAndDoesNotPartiallyWrite()
    {
        byte[] destination = new byte[] { 170, 170, 170, 170 };

        DiameterValueResult result = ValueCodecs.EncodeUtf8String(destination, "ééé");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.DestinationTooSmall));
            Assert.That(result.RequiredCount, Is.EqualTo(6));
            Assert.That(result.WrittenCount, Is.Zero);
            Assert.That(destination, Is.All.EqualTo(170));
        });
    }

    [Test]
    public void IdentityAndRealmNormalizationCanonicalizeCaseAndRootDot()
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = "Node.Example.COM.";
        DiameterIdentity realm = new DiameterIdentity();
        realm.Value = "EXAMPLE.COM.";

        DiameterValueResult normalizedIdentity = ValueCodecs.NormalizeDiameterIdentity(identity);
        DiameterValueResult normalizedRealm = ValueCodecs.NormalizeDiameterRealm(realm);

        Assert.Multiple(() =>
        {
            Assert.That(normalizedIdentity.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(normalizedIdentity.Text, Is.EqualTo("node.example.com"));
            Assert.That(normalizedIdentity.Identity, Is.Not.Null);
            Assert.That(normalizedIdentity.Identity!.Value, Is.EqualTo("node.example.com"));
            Assert.That(normalizedRealm.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(normalizedRealm.Text, Is.EqualTo("example.com"));
            Assert.That(normalizedRealm.Identity!.Value, Is.EqualTo("example.com"));
            Assert.That(identity.Value, Is.EqualTo("Node.Example.COM."));
            Assert.That(realm.Value, Is.EqualTo("EXAMPLE.COM."));
        });
    }

    [Test]
    public void IdentityAndRealmComparisonsUseCanonicalDnsEquality()
    {
        DiameterIdentity firstIdentity = CreateIdentity("NODE.Example.com");
        DiameterIdentity secondIdentity = CreateIdentity("node.example.COM.");
        DiameterIdentity otherIdentity = CreateIdentity("other.example.com");
        DiameterIdentity firstRealm = CreateIdentity("Example.COM.");
        DiameterIdentity secondRealm = CreateIdentity("example.com");
        DiameterIdentity invalid = CreateIdentity("bad_name.example");

        Assert.Multiple(() =>
        {
            Assert.That(ValueCodecs.AreDiameterIdentitiesEqual(firstIdentity, secondIdentity), Is.True);
            Assert.That(ValueCodecs.AreDiameterIdentitiesEqual(firstIdentity, otherIdentity), Is.False);
            Assert.That(ValueCodecs.AreDiameterIdentitiesEqual(firstIdentity, invalid), Is.False);
            Assert.That(ValueCodecs.AreDiameterIdentitiesEqual(null, null), Is.False);
            Assert.That(ValueCodecs.AreDiameterRealmsEqual(firstRealm, secondRealm), Is.True);
            Assert.That(ValueCodecs.AreDiameterRealmsEqual(firstRealm, invalid), Is.False);
        });
    }

    [Test]
    public void InvalidAsciiDomainFormsAreRejected()
    {
        DiameterIdentity[] invalidValues = new DiameterIdentity[]
        {
            CreateIdentity(""),
            CreateIdentity("."),
            CreateIdentity(".example.com"),
            CreateIdentity("example..com"),
            CreateIdentity("-node.example"),
            CreateIdentity("node-.example"),
            CreateIdentity("bad_name.example"),
            CreateIdentity("nöd.example"),
            CreateIdentity(new string('a', 64) + ".example")
        };

        int index = 0;
        while (index < invalidValues.Length)
        {
            DiameterValueResult result = ValueCodecs.NormalizeDiameterIdentity(invalidValues[index]);
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(result.Identity, Is.Null);
            Assert.That(result.Issues, Has.Count.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo("invalid-diameter-identity"));
            index = index + 1;
        }

        DiameterValueResult missingIdentity = ValueCodecs.NormalizeDiameterIdentity(null);
        DiameterValueResult missingRealm = ValueCodecs.NormalizeDiameterRealm(null);
        Assert.That(missingIdentity.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
        Assert.That(missingRealm.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
        Assert.That(missingRealm.Issues[0].Code, Is.EqualTo("invalid-diameter-realm"));
    }

    [Test]
    public void DiameterIdentityEnforcesTheCanonicalDnsLengthBoundary()
    {
        string prefix = new string('a', 63) + "." +
            new string('b', 63) + "." +
            new string('c', 63) + ".";
        DiameterIdentity maximum = CreateIdentity(prefix + new string('d', 61) + ".");
        DiameterIdentity oversized = CreateIdentity(prefix + new string('d', 62));

        DiameterValueResult maximumResult = ValueCodecs.NormalizeDiameterIdentity(maximum);
        DiameterValueResult oversizedResult = ValueCodecs.NormalizeDiameterIdentity(oversized);

        Assert.Multiple(() =>
        {
            Assert.That(maximumResult.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(maximumResult.Text.Length, Is.EqualTo(253));
            Assert.That(oversizedResult.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(oversizedResult.Issues[0].Code, Is.EqualTo("invalid-diameter-identity"));
        });
    }

    [Test]
    public void DiameterIdentityCodecWritesAndReturnsCanonicalOwnedIdentity()
    {
        DiameterIdentity identity = CreateIdentity("NODE.Example.COM.");
        byte[] destination = new byte[16];

        DiameterValueResult encoding = ValueCodecs.EncodeDiameterIdentity(destination, identity);
        DiameterValueResult decoding = ValueCodecs.DecodeDiameterIdentity(destination);
        destination[0] = (byte)'x';

        Assert.Multiple(() =>
        {
            Assert.That(encoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(encoding.RequiredCount, Is.EqualTo(16));
            Assert.That(encoding.WrittenCount, Is.EqualTo(16));
            Assert.That(encoding.Text, Is.EqualTo("node.example.com"));
            Assert.That(decoding.Status, Is.EqualTo(DiameterCodecStatus.Success));
            Assert.That(decoding.RequiredCount, Is.EqualTo(16));
            Assert.That(decoding.ConsumedCount, Is.EqualTo(16));
            Assert.That(decoding.Identity, Is.Not.Null);
            Assert.That(decoding.Identity!.Value, Is.EqualTo("node.example.com"));
            Assert.That(decoding.Text, Is.EqualTo("node.example.com"));
        });
    }

    [Test]
    public void DiameterIdentityDecodingRejectsValidUtf8ThatIsNotAsciiDnsData()
    {
        byte[] unicodeIdentity = Encoding.UTF8.GetBytes("nöd.example");

        DiameterValueResult result = ValueCodecs.DecodeDiameterIdentity(unicodeIdentity);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
            Assert.That(result.RequiredCount, Is.EqualTo(unicodeIdentity.Length));
            Assert.That(result.ConsumedCount, Is.Zero);
            Assert.That(result.Issues[0].Code, Is.EqualTo("invalid-diameter-identity"));
        });
    }

    private static DiameterIdentity CreateIdentity(string value)
    {
        DiameterIdentity identity = new DiameterIdentity();
        identity.Value = value;
        return identity;
    }

    private static void AssertInvalidText(DiameterValueResult result, int offset, string code)
    {
        Assert.That(result.Status, Is.EqualTo(DiameterCodecStatus.Invalid));
        Assert.That(result.WrittenCount, Is.Zero);
        Assert.That(result.ConsumedCount, Is.Zero);
        Assert.That(result.Issues, Has.Count.EqualTo(1));
        Assert.That(result.Issues[0].Offset, Is.EqualTo(offset));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
    }
}
