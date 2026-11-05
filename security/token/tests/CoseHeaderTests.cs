using token;

namespace token.Tests;

public class CoseHeaderTests
{
    [Test]
    public void ReadParsesProtectedUnprotectedAndPrivateParameters()
    {
        CborItem critical = Array(
            Int(CoseHeaderLabels.Algorithm),
            Text("application-control"));
        byte[] protectedBytes = Encode(Map(
            Entry(CoseHeaderLabels.ContentType, Text("application/cwt")),
            Entry(CoseHeaderLabels.Algorithm, Int(CoseAlgorithms.EcdsaP256Sha256)),
            Entry(CoseHeaderLabels.Critical, critical)));
        CborItem buckets = Array(
            Bytes(protectedBytes),
            Map(
                Entry(CoseHeaderLabels.KeyId, Bytes(new byte[] { 1, 2, 3 })),
                new CborMapEntry(Text("application-control"), Boolean(true))));

        CoseHeaderResult result = CoseHeader.Read(Encode(buckets));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Header.ProtectedBytes, Is.EqualTo(protectedBytes));
            Assert.That(result.Header.HasAlgorithm, Is.True);
            Assert.That(result.Header.Algorithm,
                Is.EqualTo(CoseAlgorithms.EcdsaP256Sha256));
            Assert.That(result.Header.AlgorithmText, Is.Null);
            Assert.That(result.Header.ContentTypeText,
                Is.EqualTo("application/cwt"));
            Assert.That(result.Header.KeyId, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(result.Header.Critical, Has.Length.EqualTo(2));
            Assert.That(result.Header.GetProtected(CoseHeaderLabels.Algorithm),
                Is.Not.Null);
            Assert.That(result.Header.GetUnprotectedByTextLabel("application-control")
                .BooleanValue, Is.True);
        });
    }

    [Test]
    public void EmptyProtectedBucketAcceptsPreferredAndEncodedEmptyMap()
    {
        byte[] preferred = Encode(Array(Bytes(System.Array.Empty<byte>()), Map()));
        byte[] encodedMap = Encode(Array(Bytes(new byte[] { 0xA0 }), Map()));

        CoseHeaderResult first = CoseHeader.Read(preferred);
        CoseHeaderResult second = CoseHeader.Read(encodedMap);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Header.ProtectedParameters, Is.Empty);
            Assert.That(first.Header.ProtectedBytes, Is.Empty);
            Assert.That(second.Success, Is.True);
            Assert.That(second.Header.ProtectedParameters, Is.Empty);
            Assert.That(second.Header.ProtectedBytes,
                Is.EqualTo(new byte[] { 0xA0 }));
        });
    }

    [Test]
    public void WriteUsesDeterministicMapsAndPreferredEmptyBucket()
    {
        CoseHeader empty = new CoseHeader();
        TokenBytesResult emptyBytes = empty.Write();
        CoseHeader header = CoseHeader.Create(
            new CborMapEntry[]
            {
                Entry(CoseHeaderLabels.KeyId, Bytes(new byte[] { 9, 8 })),
                Entry(CoseHeaderLabels.Algorithm,
                    Int(CoseAlgorithms.EcdsaP256Sha256))
            },
            new CborMapEntry[]
            {
                new CborMapEntry(Text("z"), Unsigned(1)),
                new CborMapEntry(Text("a"), Unsigned(2))
            });

        TokenBytesResult written = header.Write();
        CoseHeaderResult roundTrip = CoseHeader.Read(written.Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(emptyBytes.Success, Is.True);
            Assert.That(emptyBytes.Bytes,
                Is.EqualTo(new byte[] { 0x82, 0x40, 0xA0 }));
            Assert.That(written.Success, Is.True);
            Assert.That(header.ProtectedBytes,
                Is.EqualTo(new byte[] { 0xA2, 0x01, 0x28, 0x04, 0x42, 0x09, 0x08 }));
            Assert.That(roundTrip.Success, Is.True);
            Assert.That(roundTrip.Header.UnprotectedParameters[0].Key.Text,
                Is.EqualTo("a"));
            Assert.That(roundTrip.Header.UnprotectedParameters[1].Key.Text,
                Is.EqualTo("z"));
        });
    }

    [Test]
    public void WriteProtectedReturnsCryptographicMapBytes()
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(
            CoseHeaderLabels.Algorithm,
            Int(CoseAlgorithms.HmacSha256));

        TokenBytesResult result = CoseHeader.WriteProtected(
            header,
            new TokenPolicy());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Bytes,
                Is.EqualTo(new byte[] { 0xA1, 0x01, 0x05 }));
            Assert.That(header.ProtectedBytes, Is.Empty);
        });
    }

    [Test]
    public void CommonTextAndNumericFormsAreNormalized()
    {
        CoseHeader textAlgorithm = CoseHeader.Create(
            new CborMapEntry[]
            {
                Entry(CoseHeaderLabels.Algorithm, Text("private-algorithm")),
                Entry(CoseHeaderLabels.ContentType, Unsigned(42))
            },
            System.Array.Empty<CborMapEntry>());

        CoseHeaderResult result = CoseHeader.Read(textAlgorithm.Write().Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Header.HasAlgorithm, Is.False);
            Assert.That(result.Header.AlgorithmText,
                Is.EqualTo("private-algorithm"));
            Assert.That(result.Header.ContentTypeText, Is.Null);
            Assert.That(result.Header.HasContentTypeNumber, Is.True);
            Assert.That(result.Header.ContentTypeNumber, Is.EqualTo(42));
        });
    }

    [Test]
    public void SetGetReplaceAndRemoveSupportIntegerAndTextLabels()
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(CoseHeaderLabels.Algorithm, Int(-7));
        header.SetUnprotectedByTextLabel("private", Text("first"));
        header.SetUnprotectedByTextLabel("private", Text("second"));

        bool removedAlgorithm = header.Remove(CoseHeaderLabels.Algorithm);
        bool removedMissing = header.RemoveByTextLabel("missing");

        Assert.Multiple(() =>
        {
            Assert.That(removedAlgorithm, Is.True);
            Assert.That(removedMissing, Is.False);
            Assert.That(header.Get(CoseHeaderLabels.Algorithm), Is.Null);
            Assert.That(header.GetByTextLabel("private").Text, Is.EqualTo("second"));
            Assert.That(header.ProtectedParameters, Is.Empty);
            Assert.That(header.UnprotectedParameters, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void SetterPreventsCrossBucketLabelCollision()
    {
        CoseHeader header = new CoseHeader();
        header.SetProtected(99, Text("protected"));

        Assert.That(
            () => header.SetUnprotected(99, Text("unprotected")),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void PrivateParametersRoundTripWithoutInterpretation()
    {
        CborItem nested = Map(
            new CborMapEntry(Text("enabled"), Boolean(true)),
            new CborMapEntry(Text("values"), Array(Unsigned(1), Null())));
        CoseHeader header = new CoseHeader();
        header.SetProtected(-70000, nested);
        header.SetUnprotectedByTextLabel("urn:example:hint", Bytes(new byte[] { 5, 6 }));

        CoseHeaderResult result = CoseHeader.Read(header.Write().Bytes);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Header.GetProtected(-70000).Kind,
                Is.EqualTo(TokenCborValueKind.Map));
            Assert.That(result.Header.GetProtected(-70000).MapValue[0].Key.Text,
                Is.EqualTo("values"));
            Assert.That(result.Header.GetUnprotectedByTextLabel("urn:example:hint").Bytes,
                Is.EqualTo(new byte[] { 5, 6 }));
        });
    }

    [Test]
    public void StructuralErrorsAreRejected()
    {
        CborItem validProtected = Bytes(System.Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            AssertFailure(CoseHeader.Read(Encode(Map())),
                "invalid_cose_header_structure");
            AssertFailure(CoseHeader.Read(Encode(Array(validProtected))),
                "invalid_cose_header_structure");
            AssertFailure(CoseHeader.Read(Encode(Array(Map(), Map()))),
                "invalid_cose_protected_header");
            AssertFailure(CoseHeader.Read(Encode(Array(validProtected, Array()))),
                "invalid_cose_unprotected_header");
            AssertFailure(CoseHeader.Read(Encode(Array(Bytes(new byte[] { 1 }), Map()))),
                "invalid_cose_protected_header");
            AssertFailure(CoseHeader.Read(Encode(Array(
                    Bytes(new byte[] { 0xA0, 0x00 }), Map()))),
                "invalid_cose_protected_header");
            AssertFailure(CoseHeader.Read(
                    Encode(Array(validProtected, Map())).Concat(new byte[] { 0 }).ToArray()),
                "trailing_cose_header_data");
        });
    }

    [Test]
    public void HeaderLabelsMustBeIntegersOrText()
    {
        byte[] protectedBytes = Encode(Map(
            new CborMapEntry(Bytes(new byte[] { 1 }), Boolean(true))));
        CborItem unprotected = Map(
            new CborMapEntry(Array(Unsigned(1)), Boolean(true)));

        Assert.Multiple(() =>
        {
            AssertFailure(
                CoseHeader.Read(Encode(Array(Bytes(protectedBytes), Map()))),
                "invalid_cose_header_label");
            AssertFailure(
                CoseHeader.Read(Encode(Array(Bytes(System.Array.Empty<byte>()), unprotected))),
                "invalid_cose_header_label");
        });
    }

    [Test]
    public void DuplicateLabelsWithinAndAcrossBucketsAreRejected()
    {
        byte[] duplicateProtected = new byte[]
        {
            0x82, 0x45, 0xA2, 0x01, 0x26, 0x01, 0x05, 0xA0
        };
        byte[] protectedBytes = Encode(Map(
            Entry(CoseHeaderLabels.Algorithm, Int(-7))));
        byte[] crossBucket = Encode(Array(
            Bytes(protectedBytes),
            Map(Entry(CoseHeaderLabels.Algorithm, Int(5)))));

        Assert.Multiple(() =>
        {
            AssertFailure(CoseHeader.Read(duplicateProtected),
                "invalid_cose_protected_header");
            AssertFailure(CoseHeader.Read(crossBucket),
                "duplicate_cose_header_label");
        });
    }

    [TestCase(1, "boolean", "invalid_cose_header_parameter")]
    [TestCase(2, "boolean", "invalid_cose_header_parameter")]
    [TestCase(3, "negative", "invalid_cose_header_parameter")]
    [TestCase(4, "text", "invalid_cose_header_parameter")]
    [TestCase(5, "text", "invalid_cose_header_parameter")]
    [TestCase(6, "text", "invalid_cose_header_parameter")]
    public void RegisteredCommonParameterTypesAreValidated(
        int label,
        string kind,
        string code)
    {
        CborItem value = kind == "boolean"
            ? Boolean(true)
            : kind == "negative"
                ? Int(-1)
                : Text("wrong");
        byte[] protectedBytes = Encode(Map(Entry(label, value)));

        AssertFailure(
            CoseHeader.Read(Encode(Array(Bytes(protectedBytes), Map()))),
            code);
    }

    [Test]
    public void CriticalArrayMustBeNonemptyUniqueLabels()
    {
        CborItem empty = Array();
        CborItem wrong = Array(Boolean(true));
        CborItem duplicate = Array(Unsigned(42), Unsigned(42));

        Assert.Multiple(() =>
        {
            AssertInvalidProtected(CoseHeaderLabels.Critical, empty,
                "invalid_cose_header_parameter");
            AssertInvalidProtected(CoseHeaderLabels.Critical, wrong,
                "invalid_cose_header_parameter");
            AssertInvalidProtected(CoseHeaderLabels.Critical, duplicate,
                "invalid_cose_header_parameter");
        });
    }

    [Test]
    public void FullAndPartialInitializationVectorsConflictAcrossBuckets()
    {
        byte[] protectedBytes = Encode(Map(
            Entry(CoseHeaderLabels.InitializationVector,
                Bytes(new byte[] { 1 }))));
        CborItem unprotected = Map(
            Entry(CoseHeaderLabels.PartialInitializationVector,
                Bytes(new byte[] { 2 })));

        AssertFailure(
            CoseHeader.Read(Encode(Array(Bytes(protectedBytes), unprotected))),
            "conflicting_cose_initialization_vectors");
    }

    [Test]
    public void WriteRejectsInvalidMutableModels()
    {
        CoseHeader missing = new CoseHeader();
        missing.ProtectedParameters = null;
        CoseHeader invalidLabel = CoseHeader.Create(
            new CborMapEntry[]
            {
                new CborMapEntry(Boolean(true), Unsigned(1))
            },
            System.Array.Empty<CborMapEntry>());
        CoseHeader collision = CoseHeader.Create(
            new CborMapEntry[] { Entry(42, Unsigned(1)) },
            new CborMapEntry[] { Entry(42, Unsigned(2)) });

        Assert.Multiple(() =>
        {
            AssertBytesFailure(missing.Write(), "invalid_cose_header");
            AssertBytesFailure(invalidLabel.Write(), "invalid_cose_header_label");
            AssertBytesFailure(collision.Write(), "duplicate_cose_header_label");
        });
    }

    [Test]
    public void SizeDepthAndPolicyLimitsAreEnforced()
    {
        byte[] encoded = Encode(Array(Bytes(System.Array.Empty<byte>()), Map()));
        TokenPolicy size = new TokenPolicy();
        size.MaximumTokenSizeInBytes = encoded.Length - 1;
        TokenPolicy depth = new TokenPolicy();
        depth.MaximumCborDepth = 1;
        TokenPolicy invalid = new TokenPolicy();
        invalid.MaximumCborDepth = 0;

        Assert.Multiple(() =>
        {
            AssertFailure(CoseHeader.Read(encoded, size), "cbor_size_exceeded");
            AssertFailure(CoseHeader.Read(encoded, depth), "cbor_depth_exceeded");
            AssertFailure(CoseHeader.Read(encoded, invalid), "invalid_policy");
            AssertBytesFailure(new CoseHeader().WriteWithPolicy(invalid), "invalid_policy");
        });
    }

    [Test]
    public void PublicMethodsRequireArguments()
    {
        CoseHeader header = new CoseHeader();
        CborItem bytes = Bytes(System.Array.Empty<byte>());
        CborItem map = Map();

        Assert.Multiple(() =>
        {
            Assert.That(() => CoseHeader.Read(null), Throws.ArgumentNullException);
            Assert.That(() => CoseHeader.Read(new byte[0], null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseHeader.FromBuckets(null, map, new TokenPolicy()),
                Throws.ArgumentNullException);
            Assert.That(() => CoseHeader.FromBuckets(bytes, null, new TokenPolicy()),
                Throws.ArgumentNullException);
            Assert.That(() => CoseHeader.FromBuckets(bytes, map, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseHeader.Write(null, new TokenPolicy()),
                Throws.ArgumentNullException);
            Assert.That(() => CoseHeader.Write(header, null),
                Throws.ArgumentNullException);
            Assert.That(() => CoseHeader.WriteProtected(null, new TokenPolicy()),
                Throws.ArgumentNullException);
            Assert.That(() => header.GetByTextLabel((string)null),
                Throws.ArgumentNullException);
            Assert.That(() => header.SetProtected(1, null),
                Throws.ArgumentNullException);
        });
    }

    static void AssertInvalidProtected(int label, CborItem value, string code)
    {
        byte[] protectedBytes = Encode(Map(Entry(label, value)));
        AssertFailure(
            CoseHeader.Read(Encode(Array(Bytes(protectedBytes), Map()))),
            code);
    }

    static byte[] Encode(CborItem item)
    {
        CborWriter writer = CborWriter.Create();
        TokenResult written = writer.WriteItem(item);
        Assert.That(written.Success, Is.True);
        return writer.ToArray();
    }

    static CborMapEntry Entry(int label, CborItem value)
    {
        return new CborMapEntry(Int(label), value);
    }

    static CborItem Int(int value)
    {
        if (value >= 0)
        {
            return Unsigned((ulong)value);
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.NegativeInteger;
        item.IntegerArgument = (ulong)(-(long)value - 1L);
        return item;
    }

    static CborItem Unsigned(ulong value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.UnsignedInteger;
        item.IntegerArgument = value;
        return item;
    }

    static CborItem Text(string value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.TextString;
        item.Text = value;
        return item;
    }

    static CborItem Bytes(byte[] value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.ByteString;
        item.Bytes = value;
        return item;
    }

    static CborItem Boolean(bool value)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Boolean;
        item.BooleanValue = value;
        return item;
    }

    static CborItem Null()
    {
        return new CborItem();
    }

    static CborItem Array(params CborItem[] values)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = values;
        return item;
    }

    static CborItem Map(params CborMapEntry[] entries)
    {
        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return item;
    }

    static void AssertFailure(CoseHeaderResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Header, Is.Null);
        });
    }

    static void AssertBytesFailure(TokenBytesResult result, string code)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Bytes, Is.Null);
        });
    }
}
