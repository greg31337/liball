using asn1;

namespace asn1.Tests;

public class EcnPrimitiveCodecsTests
{
    [Test]
    public void EncodesBooleanPatternsAndSignedIntegerOrdering()
    {
        EcnFixture fixture = Compile(
            "Scalars ENCODING-DEFINITIONS ::= BEGIN\n"
                + "flags #BOOL ::= { ENCODING-SPACE, SIZE 1, "
                + "MULTIPLE OF octet, TRUE-PATTERN '10'B, "
                + "FALSE-PATTERN '01'B, PADDING zero }\n"
                + "numbers #INT ::= { REPRESENTATION twos-complement, "
                + "SIGNEDNESS signed, ENCODING-SPACE, SIZE 2, "
                + "MULTIPLE OF octet, BYTE-ORDER little-endian, "
                + "BIT-ORDER least-significant-first }\n"
                + "END");
        EcnPrimitiveCodecs codecs = CreateCodecs();
        EcnEncodingProperty[] flags = Properties(fixture, "flags");
        EcnEncodingProperty[] numbers = Properties(fixture, "numbers");

        EcnPrimitiveResult encodedTrue = codecs.EncodePrimitive(
            EcnClassCategory.Boolean,
            flags,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Boolean,
                HasBooleanValue = true,
                BooleanValue = true
            });
        EcnPrimitiveResult decodedTrue = codecs.DecodePrimitive(
            EcnClassCategory.Boolean,
            flags,
            encodedTrue.Data,
            encodedTrue.BitCount);
        EcnPrimitiveResult encodedInteger = codecs.EncodePrimitive(
            EcnClassCategory.Integer,
            numbers,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = -2
            });
        EcnPrimitiveResult decodedInteger = codecs.DecodePrimitive(
            EcnClassCategory.Integer,
            numbers,
            encodedInteger.Data,
            encodedInteger.BitCount);

        Assert.Multiple(() =>
        {
            Assert.That(encodedTrue.Success, Is.True);
            Assert.That(encodedTrue.Data, Is.EqualTo(new byte[] { 0x02 }));
            Assert.That(encodedTrue.BitCount, Is.EqualTo(8));
            Assert.That(decodedTrue.Value?.BooleanValue, Is.True);
            Assert.That(encodedInteger.Success, Is.True);
            Assert.That(encodedInteger.Data,
                Is.EqualTo(new byte[] { 0x7f, 0xff }));
            Assert.That(decodedInteger.Success, Is.True);
            Assert.That(decodedInteger.Value?.IntegerValue,
                Is.EqualTo(new System.Numerics.BigInteger(-2)));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void EncodesBitOctetAndTerminatedCharacterStrings()
    {
        EcnFixture fixture = Compile(
            "Strings ENCODING-DEFINITIONS ::= BEGIN\n"
                + "bits #BITS ::= { VALUE-REVERSAL TRUE }\n"
                + "octets #OCTETS ::= { BYTE-ORDER little-endian }\n"
                + "fixedOctets #OCTETS ::= { ENCODING-SPACE, SIZE 4, "
                + "MULTIPLE OF octet, VALUE-PADDING, JUSTIFIED right, "
                + "PADDING pattern, PATTERN '10'B, UNUSED BITS 16 }\n"
                + "text #CHARS ::= { TERMINATION '00'H }\n"
                + "END");
        EcnPrimitiveCodecs codecs = CreateCodecs();

        EcnPrimitiveResult encodedBits = codecs.EncodePrimitive(
            EcnClassCategory.BitString,
            Properties(fixture, "bits"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = [0xb0],
                    BitLength = 6
                }
            });
        EcnPrimitiveResult decodedBits = codecs.DecodePrimitive(
            EcnClassCategory.BitString,
            Properties(fixture, "bits"),
            encodedBits.Data,
            encodedBits.BitCount);
        EcnPrimitiveResult encodedOctets = codecs.EncodePrimitive(
            EcnClassCategory.OctetString,
            Properties(fixture, "octets"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = [1, 2, 3]
            });
        EcnPrimitiveResult decodedOctets = codecs.DecodePrimitive(
            EcnClassCategory.OctetString,
            Properties(fixture, "octets"),
            encodedOctets.Data,
            encodedOctets.BitCount);
        EcnPrimitiveResult encodedText = codecs.EncodePrimitive(
            EcnClassCategory.CharacterString,
            Properties(fixture, "text"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "Hi"
            });
        EcnPrimitiveResult decodedText = codecs.DecodePrimitive(
            EcnClassCategory.CharacterString,
            Properties(fixture, "text"),
            encodedText.Data,
            encodedText.BitCount);
        EcnPrimitiveResult fixedOctets = codecs.EncodePrimitive(
            EcnClassCategory.OctetString,
            Properties(fixture, "fixedOctets"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OctetString,
                Octets = [0xab, 0xcd]
            });
        EcnPrimitiveResult decodedFixedOctets = codecs.DecodePrimitive(
            EcnClassCategory.OctetString,
            Properties(fixture, "fixedOctets"),
            fixedOctets.Data,
            fixedOctets.BitCount);

        Assert.Multiple(() =>
        {
            Assert.That(encodedBits.Data,
                Is.EqualTo(new byte[] { 0x34 }));
            Assert.That(encodedBits.BitCount, Is.EqualTo(6));
            Assert.That(decodedBits.Value?.BitStringValue?.Bytes,
                Is.EqualTo(new byte[] { 0xb0 }));
            Assert.That(decodedBits.Value?.BitStringValue?.BitLength,
                Is.EqualTo(6));
            Assert.That(encodedOctets.Data,
                Is.EqualTo(new byte[] { 3, 2, 1 }));
            Assert.That(decodedOctets.Value?.Octets,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(encodedText.Data,
                Is.EqualTo(new byte[] { 0x48, 0x69, 0x00 }));
            Assert.That(decodedText.Value?.TextValue, Is.EqualTo("Hi"));
            Assert.That(fixedOctets.Data,
                Is.EqualTo(new byte[] { 0xaa, 0xaa, 0xab, 0xcd }));
            Assert.That(decodedFixedOctets.Value?.Octets,
                Is.EqualTo(new byte[] { 0xab, 0xcd }));
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void RoundTripsRealOidOpenTypeAndTagFields()
    {
        EcnPrimitiveCodecs codecs = CreateCodecs();
        EcnEncodingProperty[] properties = [];

        EcnPrimitiveResult real = codecs.EncodePrimitive(
            EcnClassCategory.Real,
            properties,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Real,
                RealValue = new AsnRealValue
                {
                    Kind = AsnRealKind.PositiveInfinity,
                    Normalized = true
                }
            });
        EcnPrimitiveResult decodedReal = codecs.DecodePrimitive(
            EcnClassCategory.Real,
            properties,
            real.Data,
            real.BitCount);
        EcnPrimitiveResult oid = codecs.EncodePrimitive(
            EcnClassCategory.ObjectIdentifier,
            properties,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.ObjectIdentifier,
                ObjectIdentifierValue = new AsnObjectIdentifierValue
                {
                    Arcs = [1, 2, 840, 113549]
                }
            });
        EcnPrimitiveResult decodedOid = codecs.DecodePrimitive(
            EcnClassCategory.ObjectIdentifier,
            properties,
            oid.Data,
            oid.BitCount);
        EcnPrimitiveResult open = codecs.EncodePrimitive(
            EcnClassCategory.OpenType,
            properties,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.OpenType,
                OpenTypeValue = new AsnOpenTypeValue
                {
                    Unresolved = true,
                    HasEncodingRules = true,
                    EncodingRules = AsnEncodingRules.Ecn,
                    HasOriginalData = true,
                    OriginalData = [0xde, 0xad]
                }
            });
        EcnPrimitiveResult decodedOpen = codecs.DecodePrimitive(
            EcnClassCategory.OpenType,
            properties,
            open.Data,
            open.BitCount);
        EcnPrimitiveResult tag = codecs.EncodePrimitive(
            EcnClassCategory.Tag,
            properties,
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 201,
                TextValue = "context-specific"
            });
        EcnPrimitiveResult decodedTag = codecs.DecodePrimitive(
            EcnClassCategory.Tag,
            properties,
            tag.Data,
            tag.BitCount);

        Assert.Multiple(() =>
        {
            Assert.That(decodedReal.Value?.RealValue?.Kind,
                Is.EqualTo(AsnRealKind.PositiveInfinity));
            Assert.That(decodedOid.Value?.ObjectIdentifierValue?.Arcs,
                Is.EqualTo(new System.Numerics.BigInteger[]
                {
                    1, 2, 840, 113549
                }));
            Assert.That(decodedOpen.Value?.OpenTypeValue?.OriginalData,
                Is.EqualTo(new byte[] { 0xde, 0xad }));
            Assert.That(tag.Data,
                Is.EqualTo(new byte[] { 0xbf, 0x81, 0x49 }));
            Assert.That(decodedTag.Tag?.Class,
                Is.EqualTo(AsnTagClass.ContextSpecific));
            Assert.That(decodedTag.Tag?.Number,
                Is.EqualTo(new System.Numerics.BigInteger(201)));
        });
    }

    [Test]
    public void EncodesNullAndPatternedPaddingSpaces()
    {
        EcnFixture fixture = Compile(
            "Empty ENCODING-DEFINITIONS ::= BEGIN\n"
                + "nothing #NUL ::= { ENCODING-SPACE, SIZE 3, PADDING one }\n"
                + "fill #PAD ::= { ENCODING-SPACE, SIZE 4, "
                + "PAD-PATTERN '10'B }\n"
                + "END");
        EcnPrimitiveCodecs codecs = CreateCodecs();
        EcnPrimitiveResult nullField = codecs.EncodePrimitive(
            EcnClassCategory.Null,
            Properties(fixture, "nothing"),
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null });
        EcnPrimitiveResult decodedNull = codecs.DecodePrimitive(
            EcnClassCategory.Null,
            Properties(fixture, "nothing"),
            nullField.Data,
            nullField.BitCount);
        EcnPrimitiveResult padding = codecs.EncodePrimitive(
            EcnClassCategory.Padding,
            Properties(fixture, "fill"),
            new AsnRuntimeValue());
        EcnPrimitiveResult decodedPadding = codecs.DecodePrimitive(
            EcnClassCategory.Padding,
            Properties(fixture, "fill"),
            padding.Data,
            padding.BitCount);

        Assert.Multiple(() =>
        {
            Assert.That(nullField.Data,
                Is.EqualTo(new byte[] { 0xe0 }));
            Assert.That(nullField.BitCount, Is.EqualTo(3));
            Assert.That(decodedNull.Value?.Kind,
                Is.EqualTo(AsnRuntimeValueKind.Null));
            Assert.That(padding.Data,
                Is.EqualTo(new byte[] { 0xa0 }));
            Assert.That(padding.BitCount, Is.EqualTo(4));
            Assert.That(decodedPadding.Success, Is.True);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void RejectsInvalidPropertyCombinationsValuesAndPayloads()
    {
        EcnFixture fixture = Compile(
            "Failures ENCODING-DEFINITIONS ::= BEGIN\n"
                + "small #INT ::= { ENCODING-SPACE, SIZE 1, "
                + "MULTIPLE OF octet, RANGE 0..200 }\n"
                + "terminated #CHARS ::= { TERMINATION '00'H }\n"
                + "alphabet #CHARS ::= { ALPHABET \"ABC\" }\n"
                + "orderedBits #BITS ::= { BIT-ORDER least-significant-first }\n"
                + "END");
        EcnPrimitiveCodecs codecs = CreateCodecs();
        EcnPrimitiveResult overflow = codecs.EncodePrimitive(
            EcnClassCategory.Integer,
            Properties(fixture, "small"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 128
            });
        EcnPrimitiveResult outsideRange = codecs.EncodePrimitive(
            EcnClassCategory.Integer,
            Properties(fixture, "small"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = 201
            });
        EcnPrimitiveResult missingTerminator = codecs.DecodePrimitive(
            EcnClassCategory.CharacterString,
            Properties(fixture, "terminated"),
            [0x41],
            8);
        EcnPrimitiveResult nonOctetOrder = codecs.EncodePrimitive(
            EcnClassCategory.BitString,
            Properties(fixture, "orderedBits"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.BitString,
                BitStringValue = new AsnBitStringValue
                {
                    Bytes = [0xa0],
                    BitLength = 3
                }
            });
        EcnPrimitiveResult wrongKind = codecs.EncodePrimitive(
            EcnClassCategory.Boolean,
            [],
            new AsnRuntimeValue { Kind = AsnRuntimeValueKind.Null });
        EcnPrimitiveResult outsideAlphabet = codecs.EncodePrimitive(
            EcnClassCategory.CharacterString,
            Properties(fixture, "alphabet"),
            new AsnRuntimeValue
            {
                Kind = AsnRuntimeValueKind.CharacterString,
                TextValue = "AD"
            });
        EcnPrimitiveCodecs limited = new EcnPrimitiveCodecs(
            new AsnResourceLimits { MaximumIntegerBits = 4 });
        EcnPrimitiveResult resource = limited.DecodePrimitive(
            EcnClassCategory.OctetString,
            [],
            [0xff],
            8);

        Assert.Multiple(() =>
        {
            Assert.That(overflow.Success, Is.False);
            Assert.That(overflow.ErrorCode, Is.EqualTo("ASNECN0108"));
            Assert.That(outsideRange.ErrorCode, Is.EqualTo("ASNECN0106"));
            Assert.That(missingTerminator.ErrorCode,
                Is.EqualTo("ASNECN0111"));
            Assert.That(nonOctetOrder.ErrorCode,
                Is.EqualTo("ASNECN0103"));
            Assert.That(wrongKind.ErrorCode, Is.EqualTo("ASNECN0118"));
            Assert.That(outsideAlphabet.ErrorCode,
                Is.EqualTo("ASNECN0119"));
            Assert.That(resource.LimitExceeded, Is.True);
            Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    private static EcnPrimitiveCodecs CreateCodecs()
    {
        return new EcnPrimitiveCodecs(new AsnResourceLimits());
    }

    private static EcnEncodingProperty[] Properties(
        EcnFixture fixture,
        string name)
    {
        return fixture.Table.FindSymbol(name, EcnSymbolKind.EncodingObject)
                ?.ObjectDefinition?.Properties
            ?? throw new AssertionException(
                "The encoding object '" + name + "' is missing.");
    }

    private static EcnFixture Compile(string source)
    {
        IncrementalLexSession session = new IncrementalLexSession(
            "primitive-codecs.edm",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        System.Collections.Generic.List<AsnToken> tokens =
            new System.Collections.Generic.List<AsnToken>();
        while (true)
        {
            IncrementalLexResult result = session.ScanNext();
            if (result.Status == IncrementalLexStatus.EndOfInput)
            {
                break;
            }

            if (result.Status == IncrementalLexStatus.NeedMoreData)
            {
                throw new AssertionException(
                    "Final ECN input requested more data.");
            }

            if (result.Token != null)
            {
                tokens.Add(result.Token);
            }
        }

        EcnParseResult parsed = new EcnSyntaxParser(tokens.ToArray())
            .ParseModuleSyntax();
        Assert.That(parsed.Success, Is.True, parsed.ErrorMessage);
        Assert.That(session.GetDiagnostics(), Is.Empty);
        EcnModuleSyntax module = parsed.Module
            ?? throw new AssertionException("No ECN module was parsed.");
        EcnSymbolTable table = new EcnSymbolTableBuilder(session.Diagnostics)
            .BuildTable(module, []);
        return new EcnFixture
        {
            Diagnostics = session.Diagnostics,
            Table = table
        };
    }

    private class EcnFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal EcnSymbolTable Table = null!;
    }
}
