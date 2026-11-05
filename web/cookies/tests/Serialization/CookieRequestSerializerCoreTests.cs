namespace cookies.Tests;

public class CookieRequestSerializerCoreTests
{
    [Test]
    public void PairSerializationPreservesOrderDuplicatesAndEmptyValues()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "a", Value = "2" },
            new CookiePair { Name = "empty", Value = "" },
            new CookiePair { Name = "tail", Value = "x=y" }
        ];

        CookieStringSerializationResult result = CookieRequestSerializerCore.SerializePairs(
            pairs,
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Issues, Is.Empty);
        Assert.That(result.Value, Is.EqualTo("a=1; a=2; empty=; tail=x=y"));
    }

    [Test]
    public void EmptyPairSerializationProducesAnEmptyValue()
    {
        CookieStringSerializationResult result = CookieRequestSerializerCore.SerializePairs(
            [],
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.Empty);
    }

    [Test]
    public void CompatiblePairSerializationPreservesRequestedSafeQuotes()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "quoted", Value = "two words", IsQuoted = true },
            new CookiePair { Name = "plain", Value = "value", IsQuoted = true }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible,
            PreserveQuotedValues = true
        };

        CookieStringSerializationResult result = CookieRequestSerializerCore.SerializePairs(pairs, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("quoted=\"two words\"; plain=\"value\""));
    }

    [Test]
    public void CanonicalPairSerializationRemovesQuoteHints()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "one", IsQuoted = true },
            new CookiePair { Name = "b", Value = "two", IsQuoted = true }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible,
            PreserveQuotedValues = false
        };

        CookieStringSerializationResult result = CookieRequestSerializerCore.SerializePairs(pairs, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo("a=one; b=two"));
    }

    [Test]
    public void FieldSerializationGreedilySplitsOnlyBetweenPairs()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "bb", Value = "22" },
            new CookiePair { Name = "c", Value = "3" },
            new CookiePair { Name = "dd", Value = "44" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 10;

        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(pairs, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorCode, Is.EqualTo(CookieErrorCode.None));
        Assert.That(result.Pairs, Is.Not.SameAs(pairs));
        Assert.That(result.Pairs.Select(pair => pair.Name), Is.EqualTo(new string[] { "a", "bb", "c", "dd" }));
        Assert.That(result.FieldValues, Is.EqualTo(new string[] { "a=1; bb=22", "c=3; dd=44" }));
        Assert.That(result.Issues, Is.Empty);

        pairs[0].Name = "changed";
        Assert.That(result.Pairs[0].Name, Is.EqualTo("a"));
    }

    [Test]
    public void ExactHeaderBoundaryDoesNotCauseAnExtraSplit()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "bb", Value = "22" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 10;

        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(pairs, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FieldValues, Is.EqualTo(new string[] { "a=1; bb=22" }));
    }

    [Test]
    public void QuotedValueOctetsParticipateInSplitBoundaries()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "x y", IsQuoted = true },
            new CookiePair { Name = "b", Value = "2" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            ComplianceMode = CookieComplianceMode.Compatible,
            PreserveQuotedValues = true
        };
        options.Limits.MaximumRequestHeaderOctets = 7;

        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(pairs, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FieldValues, Is.EqualTo(new string[] { "a=\"x y\"", "b=2" }));
    }

    [Test]
    public void DisabledSizeLimitProducesOneFieldWithoutSplitting()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "b", Value = "2" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 0;

        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(pairs, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FieldValues, Is.EqualTo(new string[] { "a=1; b=2" }));
    }

    [Test]
    public void SplittingCanBeDisabledForAFieldWithinTheLimit()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "b", Value = "2" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            SplitCookieFields = false
        };
        options.Limits.MaximumRequestHeaderOctets = 8;

        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(pairs, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.FieldValues, Is.EqualTo(new string[] { "a=1; b=2" }));
    }

    [Test]
    public void DisabledSplittingReturnsAnExplicitCombinedLimitFailure()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "b", Value = "2" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions
        {
            SplitCookieFields = false
        };
        options.Limits.MaximumRequestHeaderOctets = 7;

        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(pairs, options);

        AssertFailure(result, CookieErrorCode.HeaderLimitExceeded);
    }

    [Test]
    public void SingleFieldSerializationRejectsACombinedOverLimitValueEvenWhenSplittingIsEnabled()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "a", Value = "1" },
            new CookiePair { Name = "b", Value = "2" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 3;

        CookieStringSerializationResult single = CookieRequestSerializerCore.SerializePairs(pairs, options);
        CookieSelectionResult split = CookieRequestSerializerCore.SerializeFields(pairs, options);

        Assert.That(single.Success, Is.False);
        Assert.That(single.ErrorCode, Is.EqualTo(CookieErrorCode.HeaderLimitExceeded));
        Assert.That(single.Value, Is.Empty);
        Assert.That(single.Issues, Has.Length.EqualTo(1));
        Assert.That(split.Success, Is.True);
        Assert.That(split.FieldValues, Is.EqualTo(new string[] { "a=1", "b=2" }));
    }

    [Test]
    public void IndivisibleOverLimitPairReturnsNoPartialFields()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "large", Value = "value" },
            new CookiePair { Name = "ok", Value = "1" }
        ];
        CookieSerializationOptions options = new CookieSerializationOptions();
        options.Limits.MaximumRequestHeaderOctets = 10;

        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(pairs, options);

        AssertFailure(result, CookieErrorCode.HeaderLimitExceeded);
        Assert.That(result.FieldValues, Is.Empty);
        Assert.That(result.Pairs, Is.Empty);
    }

    [Test]
    public void InvalidPairsReturnValidationFailureWithoutPartialOutput()
    {
        CookiePair[] pairs =
        [
            new CookiePair { Name = "ok", Value = "1" },
            new CookiePair { Name = "bad name", Value = "2" }
        ];

        CookieStringSerializationResult stringResult = CookieRequestSerializerCore.SerializePairs(
            pairs,
            new CookieSerializationOptions());
        CookieSelectionResult fieldResult = CookieRequestSerializerCore.SerializeFields(
            pairs,
            new CookieSerializationOptions());

        Assert.That(stringResult.Success, Is.False);
        Assert.That(stringResult.ErrorCode, Is.EqualTo(CookieErrorCode.InvalidName));
        Assert.That(stringResult.Value, Is.Empty);
        AssertFailure(fieldResult, CookieErrorCode.InvalidName);
    }

    [Test]
    public void EmptyPairCollectionProducesNoRequestFields()
    {
        CookieSelectionResult result = CookieRequestSerializerCore.SerializeFields(
            [],
            new CookieSerializationOptions());

        Assert.That(result.Success, Is.True);
        Assert.That(result.Pairs, Is.Empty);
        Assert.That(result.FieldValues, Is.Empty);
    }

    static void AssertFailure(CookieSelectionResult result, CookieErrorCode expected)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo(expected));
        Assert.That(result.FieldValues, Is.Empty);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].ErrorCode, Is.EqualTo(expected));
        Assert.That(result.Issues[0].Severity, Is.EqualTo(CookieParseSeverity.Error));
    }
}
