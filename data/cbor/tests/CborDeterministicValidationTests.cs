using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborDeterministicValidationTests
{
    [TestCase("1800")]
    [TestCase("3800")]
    [TestCase("5800")]
    [TestCase("7800")]
    [TestCase("9800")]
    [TestCase("B800")]
    [TestCase("D80000")]
    public void NonMinimalHeadsAreRejectedForEveryArgumentBearingMajorType(
        string hex)
    {
        CborValidationResult result = Validate(
            Convert.FromHexString(hex),
            CborEncodingProfile.CoreDeterministic
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_DETERMINISTIC_NON_MINIMAL_HEAD"));
            Assert.That(result.Issues.Last().ByteOffset, Is.Zero);
            Assert.That(result.Issues.Last().Path, Is.EqualTo("$"));
        });
    }

    [Test]
    public void MinimalBoundaryHeadsRemainValid()
    {
        byte[][] encodings =
        [
            [0x18, 0x18],
            [0x38, 0x18],
            [0x58, 0x18, .. new byte[24]],
            [0x80],
            [0xa0],
            [0xd8, 0x18, 0x41, 0x00],
            [0xf8, 0x20]
        ];

        foreach (byte[] encoding in encodings)
        {
            CborValidationResult result = Validate(
                encoding,
                CborEncodingProfile.CoreDeterministic
            );
            Assert.That(result.IsValid, Is.True, Convert.ToHexString(encoding));
        }
    }

    [Test]
    public void IndefiniteLengthsAreRejectedOnlyAtDeterministicAndHigherLevels()
    {
        byte[] source = [0x9f, 0x01, 0xff];
        CborValidator validator = new CborValidator();
        CborValidationResult basic = validator.ValidateEncoding(
            source,
            CborValidationLevel.Basic
        );
        CborValidationResult deterministic = Validate(
            source,
            CborEncodingProfile.CoreDeterministic
        );

        Assert.Multiple(() =>
        {
            Assert.That(basic.IsValid, Is.True);
            Assert.That(deterministic.IsValid, Is.False);
            Assert.That(deterministic.Issues, Has.Length.EqualTo(1));
            Assert.That(
                deterministic.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_INDEFINITE_LENGTH")
            );
            Assert.That(deterministic.Issues[0].ByteOffset, Is.Zero);
        });
    }

    [Test]
    public void FiniteFloatsUseTheShortestLosslessWidth()
    {
        CborValidationResult wideHalf = Validate(
            Convert.FromHexString("FB3FF8000000000000"),
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult half = Validate(
            Convert.FromHexString("F93E00"),
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult single = Validate(
            Convert.FromHexString("FA49742408"),
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult doubleValue = Validate(
            Convert.FromHexString("FB3FF199999999999A"),
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult wideNegativeZero = Validate(
            Convert.FromHexString("FB8000000000000000"),
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult halfNegativeZero = Validate(
            Convert.FromHexString("F98000"),
            CborEncodingProfile.CoreDeterministic
        );

        Assert.Multiple(() =>
        {
            Assert.That(wideHalf.IsValid, Is.False);
            Assert.That(wideHalf.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_FLOAT_WIDTH"));
            Assert.That(half.IsValid, Is.True);
            Assert.That(single.IsValid, Is.True);
            Assert.That(doubleValue.IsValid, Is.True);
            Assert.That(wideNegativeZero.IsValid, Is.False);
            Assert.That(wideNegativeZero.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_FLOAT_WIDTH"));
            Assert.That(halfNegativeZero.IsValid, Is.True);
        });
    }

    [Test]
    public void OnlyTheCanonicalHalfPrecisionNanIsDeterministic()
    {
        CborValidationResult canonical = Validate(
            Convert.FromHexString("F97E00"),
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult halfPayload = Validate(
            Convert.FromHexString("F97E01"),
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult signaling = Validate(
            Convert.FromHexString("F97D00"),
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult single = Validate(
            Convert.FromHexString("FA7FC00000"),
            CborEncodingProfile.CoreDeterministic
        );

        Assert.Multiple(() =>
        {
            Assert.That(canonical.IsValid, Is.True);
            Assert.That(halfPayload.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_NAN"));
            Assert.That(signaling.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_NAN"));
            Assert.That(single.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_NAN"));
        });
    }

    [Test]
    public void CoreProfileUsesLexicalEncodedKeyOrder()
    {
        byte[] ordered = Convert.FromHexString("A21864F620F6");
        byte[] reversed = Convert.FromHexString("A220F61864F6");

        CborValidationResult valid = Validate(
            ordered,
            CborEncodingProfile.CoreDeterministic
        );
        CborValidationResult invalid = Validate(
            reversed,
            CborEncodingProfile.CoreDeterministic
        );

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_MAP_KEY_ORDER"));
            Assert.That(invalid.Issues[0].ByteOffset, Is.EqualTo(3));
            Assert.That(invalid.Issues[0].Path, Is.EqualTo("$[1].key"));
        });
    }

    [Test]
    public void LengthFirstProfileSortsByEncodedLengthBeforeLexicalBytes()
    {
        byte[] ordered = Convert.FromHexString("A220F61864F6");
        byte[] reversed = Convert.FromHexString("A21864F620F6");

        CborValidationResult valid = Validate(
            ordered,
            CborEncodingProfile.LengthFirstCoreDeterministic
        );
        CborValidationResult invalid = Validate(
            reversed,
            CborEncodingProfile.LengthFirstCoreDeterministic
        );

        Assert.Multiple(() =>
        {
            Assert.That(valid.IsValid, Is.True);
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_MAP_KEY_ORDER"));
            Assert.That(invalid.Issues[0].ByteOffset, Is.EqualTo(4));
            Assert.That(invalid.Issues[0].Path, Is.EqualTo("$[1].key"));
        });
    }

    [Test]
    public void NestedMapOrderIssuesRetainTheirFullPathAndOffset()
    {
        CborValidationResult result = Validate(
            Convert.FromHexString("81A2616200616100"),
            CborEncodingProfile.CoreDeterministic
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_MAP_KEY_ORDER"));
            Assert.That(result.Issues[0].ByteOffset, Is.EqualTo(5));
            Assert.That(result.Issues[0].Path, Is.EqualTo("$[0][1].key"));
        });
    }

    [Test]
    public void CollectionAndFailFastModesApplyToDeterministicIssues()
    {
        byte[] source = Convert.FromHexString("9F1800FB3FF8000000000000FF");
        CborDecodeOptions collectOptions = Options(
            CborEncodingProfile.CoreDeterministic
        );
        collectOptions.CollectValidationIssues = true;
        CborValidationResult collected = new CborValidator().ValidateEncodingWithOptions(
            source,
            collectOptions
        );
        CborDecodeOptions failFastOptions = Options(
            CborEncodingProfile.CoreDeterministic
        );
        failFastOptions.CollectValidationIssues = false;
        CborValidationResult failFast = new CborValidator().ValidateEncodingWithOptions(
            source,
            failFastOptions
        );

        Assert.Multiple(() =>
        {
            Assert.That(collected.Issues.Select(issue => issue.Code), Is.EquivalentTo(new[]
            {
                "CBOR_DETERMINISTIC_INDEFINITE_LENGTH",
                "CBOR_DETERMINISTIC_NON_MINIMAL_HEAD",
                "CBOR_DETERMINISTIC_FLOAT_WIDTH"
            }));
            Assert.That(failFast.Issues, Has.Length.EqualTo(1));
            Assert.That(failFast.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_INDEFINITE_LENGTH"));
        });
    }

    [Test]
    public void DeterministicValidationRejectsANonDeterministicProfileSelection()
    {
        CborValidationResult result = Validate(
            [0x00],
            CborEncodingProfile.Basic
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Code,
                Is.EqualTo("CBOR_DETERMINISTIC_PROFILE_INVALID"));
            Assert.That(result.Issues[0].ByteOffset, Is.EqualTo(-1));
        });
    }

    private static CborValidationResult Validate(
        byte[] source,
        CborEncodingProfile profile)
    {
        return new CborValidator().ValidateEncodingWithOptions(
            source,
            Options(profile)
        );
    }

    private static CborDecodeOptions Options(CborEncodingProfile profile)
    {
        return new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.Deterministic,
            DeterministicEncodingProfile = profile,
            MaxValidationIssues = 20,
            CollectValidationIssues = true
        };
    }
}
