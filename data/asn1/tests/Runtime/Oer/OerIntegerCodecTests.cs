using asn1;

namespace asn1.Tests;

public class OerIntegerCodecTests
{
    [Test]
    public void EffectiveBoundsSelectEveryUnsignedFixedWidth()
    {
        OerIntegerFormat one = Codec().DeriveFormat(Type(
            Constraint([Interval(0, 255)], false)));
        OerIntegerFormat two = Codec().DeriveFormat(Type(
            Constraint([Interval(0, 256)], false)));
        OerIntegerFormat four = Codec().DeriveFormat(Type(
            Constraint([Interval(0, 65536)], false)));
        OerIntegerFormat eight = Codec().DeriveFormat(Type(
            Constraint([Interval(0, System.Numerics.BigInteger.One << 32)], false)));
        OerIntegerFormat variable = Codec().DeriveFormat(Type(
            Constraint([
                Interval(0, System.Numerics.BigInteger.One << 64)
            ], false)));

        Assert.Multiple(() =>
        {
            Assert.That(one.FixedOctetCount, Is.EqualTo(1));
            Assert.That(two.FixedOctetCount, Is.EqualTo(2));
            Assert.That(four.FixedOctetCount, Is.EqualTo(4));
            Assert.That(eight.FixedOctetCount, Is.EqualTo(8));
            Assert.That(variable.FixedOctetCount, Is.Zero);
            Assert.That(one.Signed, Is.False);
            Assert.That(variable.Signed, Is.False);
        });
    }

    [Test]
    public void EffectiveBoundsSelectEverySignedFixedWidth()
    {
        OerIntegerFormat one = Codec().DeriveFormat(Type(
            Constraint([Interval(-128, 127)], false)));
        OerIntegerFormat two = Codec().DeriveFormat(Type(
            Constraint([Interval(-129, 127)], false)));
        OerIntegerFormat four = Codec().DeriveFormat(Type(
            Constraint([Interval(-32769, 32767)], false)));
        OerIntegerFormat eight = Codec().DeriveFormat(Type(
            Constraint([
                Interval(
                    -(System.Numerics.BigInteger.One << 31) - 1,
                    (System.Numerics.BigInteger.One << 31) - 1)
            ], false)));
        OerIntegerFormat variable = Codec().DeriveFormat(Type(
            Constraint([
                Interval(
                    -(System.Numerics.BigInteger.One << 63) - 1,
                    (System.Numerics.BigInteger.One << 63) - 1)
            ], false)));

        Assert.Multiple(() =>
        {
            Assert.That(one.FixedOctetCount, Is.EqualTo(1));
            Assert.That(two.FixedOctetCount, Is.EqualTo(2));
            Assert.That(four.FixedOctetCount, Is.EqualTo(4));
            Assert.That(eight.FixedOctetCount, Is.EqualTo(8));
            Assert.That(variable.FixedOctetCount, Is.Zero);
            Assert.That(one.Signed, Is.True);
            Assert.That(variable.Signed, Is.True);
        });
    }

    [Test]
    public void OerHullUsesClosedEffectiveApplicationsOnly()
    {
        AsnConstraintDescriptor extensible = Constraint(
            [Interval(0, 255)],
            true);
        AsnConstraintDescriptor sparse = Constraint(
            [Interval(0, 0), Interval(1000, 1000)],
            false);
        AsnConstraintDescriptor intersecting = Constraint(
            [Interval(500, 1500)],
            false);
        AsnConstraintDescriptor effective = Constraint([], false);
        effective.Applications = [extensible, sparse, intersecting];

        OerIntegerFormat format = Codec().DeriveFormat(Type(effective));

        Assert.Multiple(() =>
        {
            Assert.That(format.Success, Is.True);
            Assert.That(format.HasLowerBound, Is.True);
            Assert.That(format.LowerBound,
                Is.EqualTo(new System.Numerics.BigInteger(1000)));
            Assert.That(format.HasUpperBound, Is.True);
            Assert.That(format.UpperBound,
                Is.EqualTo(new System.Numerics.BigInteger(1000)));
            Assert.That(format.FixedOctetCount, Is.EqualTo(2));
            Assert.That(format.Signed, Is.False);
        });
    }

    [Test]
    public void ExtensibleConstraintAloneLeavesTheIntegerUnbounded()
    {
        OerIntegerFormat format = Codec().DeriveFormat(Type(
            Constraint([Interval(0, 255)], true)));

        Assert.Multiple(() =>
        {
            Assert.That(format.Success, Is.True);
            Assert.That(format.HasLowerBound, Is.False);
            Assert.That(format.HasUpperBound, Is.False);
            Assert.That(format.FixedOctetCount, Is.Zero);
            Assert.That(format.Signed, Is.True);
        });
    }

    [Test]
    public void FixedSignedAndUnsignedFormsUseNetworkOctetOrder()
    {
        AsnTypeDescriptor unsignedType = Type(
            Constraint([Interval(0, 65535)], false));
        AsnTypeDescriptor signedType = Type(
            Constraint([Interval(-32768, 32767)], false));

        OerIntegerResult unsignedEncoded = Codec().EncodeInteger(
            unsignedType,
            0x1234);
        OerIntegerResult unsignedDecoded = Codec().DecodeInteger(
            unsignedType,
            unsignedEncoded.Data,
            0,
            unsignedEncoded.Data.Length,
            true);
        OerIntegerResult signedEncoded = Codec().EncodeInteger(
            signedType,
            -129);
        OerIntegerResult signedDecoded = Codec().DecodeInteger(
            signedType,
            signedEncoded.Data,
            0,
            signedEncoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(unsignedEncoded.Data,
                Is.EqualTo(new byte[] { 0x12, 0x34 }));
            Assert.That(unsignedDecoded.Value,
                Is.EqualTo(new System.Numerics.BigInteger(0x1234)));
            Assert.That(unsignedDecoded.ConsumedCount, Is.EqualTo(2));
            Assert.That(signedEncoded.Data,
                Is.EqualTo(new byte[] { 0xff, 0x7f }));
            Assert.That(signedDecoded.Value,
                Is.EqualTo(new System.Numerics.BigInteger(-129)));
            Assert.That(signedDecoded.Signed, Is.True);
        });
    }

    [Test]
    public void UnboundedSignedFormsUseMinimumTwosComplementContent()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer
        };
        System.Numerics.BigInteger[] values =
        [
            -129,
            -128,
            0,
            127,
            128
        ];
        byte[][] expected =
        [
            [0x02, 0xff, 0x7f],
            [0x01, 0x80],
            [0x01, 0x00],
            [0x01, 0x7f],
            [0x02, 0x00, 0x80]
        ];

        int index = 0;
        while (index < values.Length)
        {
            OerIntegerResult encoded = Codec().EncodeInteger(type, values[index]);
            OerIntegerResult decoded = Codec().DecodeInteger(
                type,
                encoded.Data,
                0,
                encoded.Data.Length,
                true);

            Assert.That(encoded.Data, Is.EqualTo(expected[index]));
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value, Is.EqualTo(values[index]));
            index = index + 1;
        }
    }

    [Test]
    public void LowerBoundedFormsUseMinimumNonNegativeContent()
    {
        AsnTypeDescriptor type = Type(Constraint([
            new AsnIntervalDescriptor
            {
                HasLowerBound = true,
                LowerBound = 0
            }
        ], false));

        OerIntegerResult encoded = Codec().EncodeInteger(type, 128);
        OerIntegerResult decoded = Codec().DecodeInteger(
            type,
            encoded.Data,
            0,
            encoded.Data.Length,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(encoded.Data,
                Is.EqualTo(new byte[] { 0x01, 0x80 }));
            Assert.That(encoded.FixedOctetCount, Is.Zero);
            Assert.That(encoded.Signed, Is.False);
            Assert.That(decoded.Success, Is.True);
            Assert.That(decoded.Value,
                Is.EqualTo(new System.Numerics.BigInteger(128)));
        });
    }

    [Test]
    public void BasicOerAcceptsNonminimalVariableFormsThatCoerRejects()
    {
        AsnTypeDescriptor type = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer
        };
        OerIntegerResult basicLength = Codec().DecodeInteger(
            type,
            [0x81, 0x01, 0x7f],
            0,
            3,
            false);
        OerIntegerResult canonicalLength = Codec().DecodeInteger(
            type,
            [0x81, 0x01, 0x7f],
            0,
            3,
            true);
        OerIntegerResult basicContent = Codec().DecodeInteger(
            type,
            [0x02, 0x00, 0x7f],
            0,
            3,
            false);
        OerIntegerResult canonicalContent = Codec().DecodeInteger(
            type,
            [0x02, 0x00, 0x7f],
            0,
            3,
            true);

        Assert.Multiple(() =>
        {
            Assert.That(basicLength.Success, Is.True);
            Assert.That(basicLength.Minimal, Is.False);
            Assert.That(canonicalLength.ErrorCode, Is.EqualTo("ASNO0004"));
            Assert.That(basicContent.Success, Is.True);
            Assert.That(basicContent.Value,
                Is.EqualTo(new System.Numerics.BigInteger(127)));
            Assert.That(basicContent.Minimal, Is.False);
            Assert.That(canonicalContent.ErrorCode, Is.EqualTo("ASNO0015"));
        });
    }

    [Test]
    public void BoundsEmptyContentAndWrongTypesAreRejected()
    {
        AsnTypeDescriptor bounded = Type(
            Constraint([Interval(0, 10)], false));
        OerIntegerResult encodeOutside = Codec().EncodeInteger(bounded, 11);
        OerIntegerResult decodeOutside = Codec().DecodeInteger(
            bounded,
            [0xff],
            0,
            1,
            false);
        OerIntegerResult empty = Codec().DecodeInteger(
            new AsnTypeDescriptor { Kind = AsnTypeKind.Integer },
            [0x00],
            0,
            1,
            false);
        OerIntegerResult wrongType = Codec().EncodeInteger(
            new AsnTypeDescriptor { Kind = AsnTypeKind.Boolean },
            0);

        Assert.Multiple(() =>
        {
            Assert.That(encodeOutside.ErrorCode, Is.EqualTo("ASNO0012"));
            Assert.That(decodeOutside.ErrorCode, Is.EqualTo("ASNO0012"));
            Assert.That(empty.ErrorCode, Is.EqualTo("ASNO0014"));
            Assert.That(wrongType.ErrorCode, Is.EqualTo("ASNO0010"));
        });
    }

    [Test]
    public void TruncationAndIntegerLimitsFailWithoutPartialValues()
    {
        AsnTypeDescriptor variable = new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer
        };
        OerIntegerResult truncated = Codec().DecodeInteger(
            variable,
            [0x02, 0x01],
            0,
            2,
            false);
        OerIntegerCodec limited = new OerIntegerCodec(new AsnResourceLimits
        {
            MaximumIntegerBits = 8,
            MaximumTotalAllocationBytes = 100
        });
        OerIntegerResult encodeLimited = limited.EncodeInteger(variable, 128);
        OerIntegerResult decodeLimited = new OerIntegerCodec(
            new AsnResourceLimits
            {
                MaximumIntegerBits = 8,
                MaximumTotalAllocationBytes = 100
            }).DecodeInteger(
                variable,
                [0x02, 0x00, 0x80],
                0,
                3,
                false);

        Assert.Multiple(() =>
        {
            Assert.That(truncated.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            Assert.That(truncated.Success, Is.False);
            Assert.That(encodeLimited.ErrorCode, Is.EqualTo("ASNR0010"));
            Assert.That(encodeLimited.LimitExceeded, Is.True);
            Assert.That(decodeLimited.ErrorCode, Is.EqualTo("ASNR0010"));
            Assert.That(decodeLimited.LimitExceeded, Is.True);
        });
    }

    private static OerIntegerCodec Codec()
    {
        return new OerIntegerCodec(new AsnResourceLimits
        {
            MaximumLengthOctets = 9
        });
    }

    private static AsnTypeDescriptor Type(AsnConstraintDescriptor constraint)
    {
        return new AsnTypeDescriptor
        {
            Kind = AsnTypeKind.Integer,
            Constraints = [constraint]
        };
    }

    private static AsnConstraintDescriptor Constraint(
        AsnIntervalDescriptor[] intervals,
        bool extensible)
    {
        return new AsnConstraintDescriptor
        {
            DomainKind = AsnTypeKind.Integer,
            RootIntervals = intervals,
            Extensible = extensible
        };
    }

    private static AsnIntervalDescriptor Interval(
        System.Numerics.BigInteger lower,
        System.Numerics.BigInteger upper)
    {
        return new AsnIntervalDescriptor
        {
            HasLowerBound = true,
            LowerBound = lower,
            HasUpperBound = true,
            UpperBound = upper
        };
    }
}
