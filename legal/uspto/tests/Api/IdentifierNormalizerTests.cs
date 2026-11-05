using uspto;

namespace uspto.Tests;

public class IdentifierNormalizerTests
{
    [TestCase("14/412,875", "14412875")]
    [TestCase("09123456", "09123456")]
    [TestCase("US 2024/0012345 A1", "US20240012345A1")]
    [TestCase("US 12,156,579 B2", "US12156579B2")]
    [TestCase("D/475502/S1", "USD475502S1")]
    [TestCase("5,778 A", "US0005778A")]
    [TestCase("PCT/US2025/058371", "PCTUS2025058371")]
    [TestCase("PCTUS0719317", "PCTUS0719317")]
    [TestCase("50123456", "US50123456")]
    public void StrictPatentNormalizationReturnsCanonicalIdentifiers(string input, string canonical)
    {
        IIdentifierNormalizer normalizer = new IdentifierNormalizer();

        string result = normalizer.NormalizePatentIdentifier(input);

        Assert.That(result, Is.EqualTo(canonical));
    }

    [TestCase("11646472")]
    [TestCase("1234567")]
    [TestCase("US 17248024")]
    public void StrictPatentNormalizationRejectsAmbiguousBareNumbers(string input)
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();

        ValidationException? exception = Assert.Throws<ValidationException>(() => normalizer.NormalizePatentIdentifier(input));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.FieldName, Is.EqualTo("input"));
            Assert.That(exception.ErrorCode, Is.EqualTo("AMBIGUOUS_PATENT_IDENTIFIER"));
            Assert.That(exception.Message, Does.Contain("ambiguous"));
        });
    }

    [TestCase("11646472")]
    [TestCase("1234567")]
    [TestCase("US 17248024")]
    public void ExceptionlessPatentNormalizationReturnsNullForAmbiguity(string input)
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();

        string? result = normalizer.TryNormalizePatentIdentifier(input);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InternalNormalizationResultRetainsEveryAmbiguityCandidate()
    {
        IdentifierNormalizationResult result = IdentifierNormalization.NormalizePatent("11646472");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.IsAmbiguous, Is.True);
            Assert.That(result.PatentKind, Is.EqualTo(IdentifierKind.Ambiguous));
            Assert.That(result.FailureCode, Is.EqualTo("AMBIGUOUS_PATENT_IDENTIFIER"));
            Assert.That(result.CandidateCount, Is.EqualTo(2));
            Assert.That(result.Candidates[0].Kind, Is.EqualTo(IdentifierKind.PatentApplication));
            Assert.That(result.Candidates[0].CanonicalNumber, Is.EqualTo("11646472"));
            Assert.That(result.Candidates[1].Kind, Is.EqualTo(IdentifierKind.PatentGrant));
            Assert.That(result.Candidates[1].CanonicalNumber, Is.EqualTo("US11646472"));
        });
    }

    [TestCase("14/000,000", "INVALID_APPLICATION_SERIAL")]
    [TestCase("US20240000000A1", "INVALID_PUBLICATION_SEQUENCE")]
    [TestCase("PCT/US9999/000001", "INVALID_PCT_YEAR")]
    [TestCase("US 12_156_579 B2", "INVALID_CHARACTER")]
    [TestCase("", "EMPTY_IDENTIFIER")]
    public void StrictPatentNormalizationReportsStableValidationCodes(string input, string failureCode)
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();

        ValidationException? exception = Assert.Throws<ValidationException>(() => normalizer.NormalizePatentIdentifier(input));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.FieldName, Is.EqualTo("input"));
            Assert.That(exception.ErrorCode, Is.EqualTo(failureCode));
            Assert.That(exception.Message, Is.EqualTo("The patent identifier is invalid."));
        });
    }

    [Test]
    public void MissingPatentInputThrowsStrictlyAndReturnsNullExceptionlessly()
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();

        ValidationException? exception = Assert.Throws<ValidationException>(() => normalizer.NormalizePatentIdentifier(null!));
        string? result = normalizer.TryNormalizePatentIdentifier(null!);

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(result, Is.Null);
        });
    }

    [TestCase("14/412,875", "14412875")]
    [TestCase("US 2024/0012345 A9", "US20240012345A9")]
    [TestCase("RE 38,134 E1", "USRE38134E1")]
    [TestCase("PCT/BR2000/000001", "PCTBR2000000001")]
    public void ExceptionlessPatentNormalizationReturnsCanonicalValidValues(string input, string canonical)
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();

        string? result = normalizer.TryNormalizePatentIdentifier(input);

        Assert.That(result, Is.EqualTo(canonical));
    }

    [TestCase("sn 87/650,005", "sn87650005")]
    [TestCase("RN 88,725", "rn0088725")]
    [TestCase("ref:Z1231384", "refZ1231384")]
    [TestCase("6556072", "rn6556072")]
    public void StrictTrademarkNormalizationReturnsTsdrCanonicalIdentifiers(string input, string canonical)
    {
        IIdentifierNormalizer normalizer = new IdentifierNormalizer();

        string result = normalizer.NormalizeTrademarkIdentifier(input);

        Assert.That(result, Is.EqualTo(canonical));
    }

    [TestCase("sn123", "INVALID_TRADEMARK_SERIAL")]
    [TestCase("rn0000000", "INVALID_TRADEMARK_REGISTRATION")]
    [TestCase("Z1231384", "UNSUPPORTED_TRADEMARK_PREFIX")]
    [TestCase("refZ123_1384", "INVALID_CHARACTER")]
    [TestCase("", "EMPTY_IDENTIFIER")]
    public void StrictTrademarkNormalizationReportsStableValidationCodes(string input, string failureCode)
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();

        ValidationException? exception = Assert.Throws<ValidationException>(() => normalizer.NormalizeTrademarkIdentifier(input));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.FieldName, Is.EqualTo("input"));
            Assert.That(exception.ErrorCode, Is.EqualTo(failureCode));
        });
    }

    [TestCase("sn123")]
    [TestCase("rn0000000")]
    [TestCase("Z1231384")]
    [TestCase("refZ123_1384")]
    [TestCase("")]
    public void ExceptionlessTrademarkNormalizationReturnsNullForInvalidValues(string input)
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();

        string? result = normalizer.TryNormalizeTrademarkIdentifier(input);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void MissingTrademarkInputThrowsStrictlyAndReturnsNullExceptionlessly()
    {
        IdentifierNormalizer normalizer = new IdentifierNormalizer();

        ValidationException? exception = Assert.Throws<ValidationException>(() => normalizer.NormalizeTrademarkIdentifier(null!));
        string? result = normalizer.TryNormalizeTrademarkIdentifier(null!);

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.ErrorCode, Is.EqualTo("REQUIRED"));
            Assert.That(result, Is.Null);
        });
    }
}
