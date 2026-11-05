using argon2;

namespace argon2.Tests.Codecs;

public class Argon2PhcMalformedInputTests
{
    const string Canonical = "$argon2id$v=19$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA";

    [TestCase("", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,t=2,p=4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,t=2,p=4$c29tZXNhbHQ", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA$extra", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$extra$v=19$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$t=2,m=32,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,p=4,t=2$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$memory=32,t=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,iterations=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,t=2,lanes=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,t=2,t=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,t=2,p=4,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$v=19$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2íd$v=19$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.UnsupportedVariant)]
    [TestCase("$argon2id$v=1９$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$mé=32,t=2,p=4$c29tZXNhbHQ$dGVzdA", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("$argon2id$v=19$m=32,t=2,p=4$c29tZXNhbHé$dGVzdA", Argon2DecodeStatus.InvalidBase64)]
    public void StructuralMutationTableReturnsExpectedStatus(
        string encoded,
        Argon2DecodeStatus expectedStatus)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(encoded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.EncodedHash, Is.Null);
        });
    }

    [TestCase("019", "32", "2", "4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "+32", "2", "4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "-32", "2", "4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "032", "2", "4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "32", "+2", "4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "32", "-2", "4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "32", "02", "4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "32", "2", "+4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "32", "2", "-4", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "32", "2", "04", Argon2DecodeStatus.InvalidFormat)]
    [TestCase("19", "42949672960", "2", "4", Argon2DecodeStatus.InvalidParameters)]
    [TestCase("19", "32", "42949672960", "4", Argon2DecodeStatus.InvalidParameters)]
    [TestCase("19", "32", "2", "42949672960", Argon2DecodeStatus.InvalidParameters)]
    [TestCase("20", "32", "2", "4", Argon2DecodeStatus.UnsupportedVersion)]
    [TestCase("42949672960", "32", "2", "4", Argon2DecodeStatus.UnsupportedVersion)]
    public void DecimalMutationTableReturnsExpectedStatus(
        string version,
        string memory,
        string iterations,
        string parallelism,
        Argon2DecodeStatus expectedStatus)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(
            CreateEncoded(version, memory, iterations, parallelism, "c29tZXNhbHQ", "dGVzdA"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.EncodedHash, Is.Null);
        });
    }

    [TestCase("A", "dGVzdA", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("AAAAA", "dGVzdA", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("c29tZXNhbHR", "dGVzdA", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("c29tZXNhbHQ=", "dGVzdA", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("c29t ZXNhbHQ", "dGVzdA", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("c29tZXNhbHQ", "A", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("c29tZXNhbHQ", "AAAAA", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("c29tZXNhbHQ", "dGVzdB", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("c29tZXNhbHQ", "dGVzdA=", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("c29tZXNhbHQ", "dGVz dA", Argon2DecodeStatus.InvalidBase64)]
    [TestCase("", "dGVzdA", Argon2DecodeStatus.InvalidParameters)]
    [TestCase("c2FsdA", "dGVzdA", Argon2DecodeStatus.InvalidParameters)]
    [TestCase("c29tZXNhbHQ", "", Argon2DecodeStatus.InvalidParameters)]
    [TestCase("c29tZXNhbHQ", "YWJj", Argon2DecodeStatus.InvalidParameters)]
    public void BinaryComponentMutationTableReturnsExpectedStatus(
        string salt,
        string tag,
        Argon2DecodeStatus expectedStatus)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(
            CreateEncoded("19", "32", "2", "4", salt, tag));

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(expectedStatus));
            Assert.That(result.EncodedHash, Is.Null);
        });
    }

    [Test]
    public void RequiredFieldRemovalAndEveryInsertionOrDuplicationIsRejected()
    {
        string[] fields = Canonical.Split('$');
        int fieldIndex = 1;
        while (fieldIndex < fields.Length)
        {
            List<string> removed = fields.ToList();
            removed.RemoveAt(fieldIndex);
            string removedFieldEncoding = string.Join('$', removed);
            if (fields[fieldIndex] == "v=19")
            {
                Argon2DecodeResult legacyResult = Argon2PhcParser.Parse(removedFieldEncoding);
                Assert.Multiple(() =>
                {
                    Assert.That(legacyResult.Status, Is.EqualTo(Argon2DecodeStatus.Success));
                    Assert.That(
                        legacyResult.EncodedHash?.Parameters.Version,
                        Is.EqualTo(Argon2Version.Version10));
                });
            }
            else
            {
                AssertRejected(removedFieldEncoding);
            }

            List<string> inserted = fields.ToList();
            inserted.Insert(fieldIndex, "extra");
            AssertRejected(string.Join('$', inserted));

            List<string> duplicated = fields.ToList();
            duplicated.Insert(fieldIndex, fields[fieldIndex]);
            AssertRejected(string.Join('$', duplicated));
            fieldIndex = fieldIndex + 1;
        }
    }

    [Test]
    public void EveryNoncanonicalParameterOrderAndDuplicateIsRejected()
    {
        string[] parameterFields = new string[] { "m=32", "t=2", "p=4" };
        int firstIndex = 0;
        while (firstIndex < parameterFields.Length)
        {
            int secondIndex = 0;
            while (secondIndex < parameterFields.Length)
            {
                if (secondIndex != firstIndex)
                {
                    int thirdIndex = 0;
                    while (thirdIndex < parameterFields.Length)
                    {
                        if (thirdIndex != firstIndex && thirdIndex != secondIndex)
                        {
                            string parameters = parameterFields[firstIndex]
                                + "," + parameterFields[secondIndex]
                                + "," + parameterFields[thirdIndex];
                            if (parameters != "m=32,t=2,p=4")
                            {
                                AssertRejected(
                                    "$argon2id$v=19$" + parameters + "$c29tZXNhbHQ$dGVzdA");
                            }
                        }

                        thirdIndex = thirdIndex + 1;
                    }
                }

                secondIndex = secondIndex + 1;
            }

            string duplicate = parameterFields[firstIndex]
                + "," + string.Join(',', parameterFields);
            AssertRejected("$argon2id$v=19$" + duplicate + "$c29tZXNhbHQ$dGVzdA");
            firstIndex = firstIndex + 1;
        }
    }

    [Test]
    public void GeneratedNoncanonicalDecimalTokensAreRejectedInEveryNumericField()
    {
        string[] invalidTokens = new string[]
        {
            "",
            "+1",
            "-1",
            "01",
            " 1",
            "1 ",
            "１",
            "999999999999999999999999999999999999"
        };
        int index = 0;
        while (index < invalidTokens.Length)
        {
            string token = invalidTokens[index];
            AssertRejected(CreateEncoded(token, "32", "2", "4", "c29tZXNhbHQ", "dGVzdA"));
            AssertRejected(CreateEncoded("19", token, "2", "4", "c29tZXNhbHQ", "dGVzdA"));
            AssertRejected(CreateEncoded("19", "32", token, "4", "c29tZXNhbHQ", "dGVzdA"));
            AssertRejected(CreateEncoded("19", "32", "2", token, "c29tZXNhbHQ", "dGVzdA"));
            index = index + 1;
        }
    }

    [TestCase("argon2")]
    [TestCase("argon2x")]
    [TestCase("Argon2id")]
    [TestCase("argon2íd")]
    public void UnknownVariantsAreReportedWithoutDecodedData(string variant)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(
            "$" + variant + "$v=19$m=32,t=2,p=4$c29tZXNhbHQ$dGVzdA");

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(Argon2DecodeStatus.UnsupportedVariant));
            Assert.That(result.EncodedHash, Is.Null);
        });
    }

    static string CreateEncoded(
        string version,
        string memory,
        string iterations,
        string parallelism,
        string salt,
        string tag)
    {
        return "$argon2id$v=" + version
            + "$m=" + memory
            + ",t=" + iterations
            + ",p=" + parallelism
            + "$" + salt
            + "$" + tag;
    }

    static void AssertRejected(string encoded)
    {
        Argon2DecodeResult result = Argon2PhcParser.Parse(encoded);
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.Not.EqualTo(Argon2DecodeStatus.Success), encoded);
            Assert.That(result.EncodedHash, Is.Null, encoded);
        });
    }
}
