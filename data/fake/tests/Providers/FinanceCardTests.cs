using fake;

namespace fake.Tests;

[TestFixture]
public class FinanceCardTests
{
    [Test]
    public void IssuerPatternsExposeLengthsPrefixesAndSyntheticMarkers()
    {
        FinanceProvider finance = Fake.Create(9901).Finance();
        CardIssuerPattern[] patterns = finance.CardPatterns();

        Assert.Multiple(() =>
        {
            Assert.That(patterns.Select(value => value.Issuer), Is.EqualTo(new CardIssuer[]
            {
                CardIssuer.Visa,
                CardIssuer.Mastercard,
                CardIssuer.AmericanExpress,
                CardIssuer.Discover
            }));
            Assert.That(patterns[0].PrefixPatterns, Is.EqualTo(new string[] { "4" }));
            Assert.That(patterns[0].PanLengths, Is.EqualTo(new int[] { 13, 16, 19 }));
            Assert.That(patterns[1].PrefixPatterns,
                Is.EqualTo(new string[] { "51-55", "2221-2720" }));
            Assert.That(patterns[2].PrefixPatterns, Is.EqualTo(new string[] { "34", "37" }));
            Assert.That(patterns[2].PanLengths, Is.EqualTo(new int[] { 15 }));
            Assert.That(patterns[2].CvvLength, Is.EqualTo(4));
            Assert.That(patterns[3].PrefixPatterns,
                Is.EqualTo(new string[] { "6011", "644-649", "65" }));
            Assert.That(patterns.Select(value => value.IsSyntheticTestPattern), Has.All.True);
            Assert.That(patterns.Select(value => value.TestMarker).Distinct(),
                Is.EqualTo(new string[] { "fake.synthetic-card.v1" }));
        });
    }

    [Test]
    public void EveryGeneratedIssuerPanHasSupportedShapeAndValidLuhn()
    {
        FinanceProvider finance = Fake.Create(9902).Finance();
        LuhnChecksum luhn = Fake.Create(9902).Checksum().Luhn();

        foreach (CardIssuer issuer in Enum.GetValues<CardIssuer>())
        {
            CardIssuerPattern pattern = finance.CardPattern(issuer);
            string number = finance.CardNumberFromIssuer(issuer);

            Assert.Multiple(() =>
            {
                Assert.That(number, Is.EqualTo(pattern.TestPan));
                Assert.That(number, Does.Match("^[0-9]+$"));
                Assert.That(pattern.PanLengths, Does.Contain(number.Length));
                Assert.That(finance.CardIssuerFromNumber(number), Is.EqualTo(issuer));
                Assert.That(finance.IsCardNumberValid(number), Is.True);
                Assert.That(luhn.Validate(number), Is.True);
            });
        }
    }

    [Test]
    public void InvalidPansMutateOnlyTheLuhnDigitAndRetainIssuerShape()
    {
        FinanceProvider finance = Fake.Create(9903).Finance();

        foreach (CardIssuer issuer in Enum.GetValues<CardIssuer>())
        {
            string valid = finance.CardNumberFromIssuer(issuer);
            string invalid = finance.InvalidCardNumberFromIssuer(issuer);

            Assert.Multiple(() =>
            {
                Assert.That(invalid.Substring(0, invalid.Length - 1),
                    Is.EqualTo(valid.Substring(0, valid.Length - 1)));
                Assert.That(invalid[invalid.Length - 1], Is.Not.EqualTo(valid[valid.Length - 1]));
                Assert.That(finance.CardIssuerFromNumber(invalid), Is.EqualTo(issuer));
                Assert.That(finance.IsCardNumberValid(invalid), Is.False);
            });
        }
    }

    [Test]
    public void MaskingPreservesOnlyTheFinalFourPanDigits()
    {
        FinanceProvider finance = Fake.Create(9904).Finance();

        foreach (CardIssuer issuer in Enum.GetValues<CardIssuer>())
        {
            string number = finance.CardNumberFromIssuer(issuer);
            string masked = finance.MaskedCardFromNumber(number);

            Assert.Multiple(() =>
            {
                Assert.That(masked, Has.Length.EqualTo(number.Length));
                Assert.That(masked.Substring(0, masked.Length - 4),
                    Is.EqualTo(new string('*', masked.Length - 4)));
                Assert.That(masked.Substring(masked.Length - 4),
                    Is.EqualTo(number.Substring(number.Length - 4)));
                Assert.That(finance.MaskedCardFromIssuer(issuer), Is.EqualTo(masked));
            });
        }
    }

    [Test]
    public void CvvPinAndCardholderValuesAreDeterministicSyntheticShapes()
    {
        FinanceProvider first = Fake.Create(9905).Finance();
        FinanceProvider second = Fake.Create(9905).Finance();

        Assert.Multiple(() =>
        {
            Assert.That(first.CvvFromIssuer(CardIssuer.Visa),
                Is.EqualTo(second.CvvFromIssuer(CardIssuer.Visa)).And.Match("^[0-9]{3}$"));
            Assert.That(first.CvvFromIssuer(CardIssuer.AmericanExpress),
                Is.EqualTo(second.CvvFromIssuer(CardIssuer.AmericanExpress)).And.Match("^[0-9]{4}$"));
            Assert.That(first.Pin(),
                Is.EqualTo(second.Pin()).And.Match("^[0-9]{4}$"));
            Assert.That(first.Cardholder(),
                Is.EqualTo(second.Cardholder()).And.AnyOf(
                    "EXAMPLE CARDHOLDER",
                    "SAMPLE CARDHOLDER",
                    "TEST CARDHOLDER"));
        });
    }

    [Test]
    public void ExpiryUsesTheCapturedReferenceMonthAndAOneToSixtyMonthLead()
    {
        FakeOptions firstOptions = FakeOptions.Create(9906);
        firstOptions.ReferenceTime = new DateTimeOffset(
            2024,
            6,
            15,
            12,
            0,
            0,
            TimeSpan.Zero);
        FakeOptions secondOptions = FakeOptions.Create(firstOptions);
        FinanceProvider first = Fake.Create(firstOptions).Finance();
        FinanceProvider second = Fake.Create(secondOptions).Finance();
        CardRecord card = first.CardFromIssuer(CardIssuer.Visa);
        string direct = second.Expiry();
        int lead = (card.ExpiryYear - 2024) * 12 + card.ExpiryMonth - 6;

        Assert.Multiple(() =>
        {
            Assert.That(card.Expiry, Is.EqualTo(direct));
            Assert.That(card.Expiry, Does.Match("^(0[1-9]|1[0-2])/[0-9]{2}$"));
            Assert.That(card.ExpiryMonth, Is.InRange(1, 12));
            Assert.That(lead, Is.InRange(1, 60));
        });
    }

    [Test]
    public void ExpiryUsesTheLastRepresentableFutureMonthAtTheDateLimit()
    {
        FakeOptions options = FakeOptions.Create(9907);
        options.ReferenceTime = new DateTimeOffset(
            9999,
            11,
            30,
            23,
            59,
            59,
            TimeSpan.Zero);

        Assert.That(Fake.Create(options).Finance().Expiry(), Is.EqualTo("12/99"));
    }

    [Test]
    public void CardRecordsKeepEveryFieldCorrelatedAndExplicitlyTestOnly()
    {
        foreach (CardIssuer issuer in Enum.GetValues<CardIssuer>())
        {
            FinanceProvider finance = Fake.Create(9910 + (long)issuer).Finance();
            CardRecord card = finance.CardFromIssuer(issuer);
            CardIssuerPattern pattern = finance.CardPattern(issuer);

            Assert.Multiple(() =>
            {
                Assert.That(card.Issuer, Is.EqualTo(issuer));
                Assert.That(card.IssuerName, Is.EqualTo(pattern.Name));
                Assert.That(card.Number, Is.EqualTo(pattern.TestPan));
                Assert.That(card.MaskedNumber, Is.EqualTo(finance.MaskedCardFromNumber(card.Number)));
                Assert.That(card.Cvv, Does.Match("^[0-9]{" + pattern.CvvLength + "}$"));
                Assert.That(card.Pin, Does.Match("^[0-9]{4}$"));
                Assert.That(card.Cardholder, Does.Contain("CARDHOLDER"));
                Assert.That(card.IsLuhnValid, Is.True);
                Assert.That(card.IsSynthetic, Is.True);
                Assert.That(card.IsTestCard, Is.True);
                Assert.That(card.TestMarker, Is.EqualTo("fake.synthetic-card.v1"));
                Assert.That(card.IsAuthorized, Is.False);
                Assert.That(card.RepresentsActiveAccount, Is.False);
            });
        }
    }

    [Test]
    public void RandomCardRecordsAndIssuerSelectionsAreDeterministic()
    {
        FinanceProvider first = Fake.Create(9920).Finance();
        FinanceProvider second = Fake.Create(9920).Finance();
        CardRecord firstCard = first.Card();
        CardRecord secondCard = second.Card();

        Assert.Multiple(() =>
        {
            AssertEquivalent(firstCard, secondCard);
            Assert.That(firstCard.Number, Is.EqualTo(first.CardNumberFromIssuer(firstCard.Issuer)));
            Assert.That(first.IsCardNumberValid(firstCard.Number), Is.True);
        });
    }

    [Test]
    public void PatternResultsAndNestedArraysAreIsolated()
    {
        FinanceProvider finance = Fake.Create(9930).Finance();
        CardIssuerPattern first = finance.CardPattern(CardIssuer.Visa);
        CardIssuerPattern second = finance.CardPattern(CardIssuer.Visa);
        CardIssuerPattern[] patterns = finance.CardPatterns();

        first.PrefixPatterns[0] = "changed";
        first.PanLengths[0] = 1;
        patterns[0].TestPan = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(second.PrefixPatterns, Is.EqualTo(new string[] { "4" }));
            Assert.That(second.PanLengths, Is.EqualTo(new int[] { 13, 16, 19 }));
            Assert.That(finance.CardPattern(CardIssuer.Visa).TestPan,
                Is.EqualTo("4242424242424242"));
        });
    }

    [Test]
    public void CardApisRejectInvalidInputsWithoutConsumingEntropyAndHonorLimits()
    {
        const long seed = 9940;
        FinanceProvider inspected = Fake.Create(seed).Finance();
        FinanceProvider untouched = Fake.Create(seed).Finance();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 3;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 3;
        FakeOptions maximumDateOptions = FakeOptions.Create(seed);
        maximumDateOptions.ReferenceTime = DateTimeOffset.MaxValue;

        Assert.That(
            () => inspected.CardNumberFromIssuer((CardIssuer)999),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("issuer"));
        Assert.That(inspected.CardNumber(), Is.EqualTo(untouched.CardNumber()));
        Assert.That(() => inspected.CardIssuerFromNumber(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => inspected.CardIssuerFromNumber("not-digits"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => inspected.CardIssuerFromNumber("1234567890123456"), Throws.TypeOf<ArgumentException>());
        Assert.That(() => inspected.IsCardNumberValid(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(inspected.IsCardNumberValid(string.Empty), Is.False);
        Assert.That(inspected.IsCardNumberValid("4242-4242-4242-4242"), Is.False);
        Assert.That(() => inspected.MaskedCardFromNumber("1234"), Throws.TypeOf<ArgumentException>());
        Assert.That(
            () => Fake.Create(countOptions).Finance().CardPatterns(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(outputOptions).Finance().CardNumberFromIssuer(CardIssuer.Visa),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(outputOptions).Finance().Pin(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(maximumDateOptions).Finance().Expiry(),
            Throws.TypeOf<FakeException>());
    }

    static void AssertEquivalent(CardRecord actual, CardRecord expected)
    {
        Assert.That(actual.Issuer, Is.EqualTo(expected.Issuer));
        Assert.That(actual.IssuerName, Is.EqualTo(expected.IssuerName));
        Assert.That(actual.Number, Is.EqualTo(expected.Number));
        Assert.That(actual.MaskedNumber, Is.EqualTo(expected.MaskedNumber));
        Assert.That(actual.Cvv, Is.EqualTo(expected.Cvv));
        Assert.That(actual.Pin, Is.EqualTo(expected.Pin));
        Assert.That(actual.Expiry, Is.EqualTo(expected.Expiry));
        Assert.That(actual.ExpiryMonth, Is.EqualTo(expected.ExpiryMonth));
        Assert.That(actual.ExpiryYear, Is.EqualTo(expected.ExpiryYear));
        Assert.That(actual.Cardholder, Is.EqualTo(expected.Cardholder));
        Assert.That(actual.IsLuhnValid, Is.EqualTo(expected.IsLuhnValid));
        Assert.That(actual.IsSynthetic, Is.EqualTo(expected.IsSynthetic));
        Assert.That(actual.IsTestCard, Is.EqualTo(expected.IsTestCard));
        Assert.That(actual.TestMarker, Is.EqualTo(expected.TestMarker));
        Assert.That(actual.IsAuthorized, Is.EqualTo(expected.IsAuthorized));
        Assert.That(actual.RepresentsActiveAccount, Is.EqualTo(expected.RepresentsActiveAccount));
    }
}
