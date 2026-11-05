using fake;

namespace fake.Tests;

[TestFixture]
public class PhoneFalsehoodEdgeTests
{
    [Test]
    public void EveryEdgeCaseIsDeterministicAndAvoidsUnsupportedInferences()
    {
        PhoneEdgeCaseRecord[] first = Fake.Create(9701).Phone().EdgeCases();
        PhoneEdgeCaseRecord[] second = Fake.Create(9701).Phone().EdgeCases();

        Assert.That(first, Has.Length.EqualTo(6));
        int index = 0;
        while (index < first.Length)
        {
            Assert.Multiple(() =>
            {
                AssertEquivalent(first[index], second[index]);
                Assert.That(first[index].Case, Is.EqualTo((PhoneEdgeCase)index));
                Assert.That(first[index].ReachabilityKnown, Is.False);
                Assert.That(first[index].SubscriberIdentityKnown, Is.False);
                Assert.That(first[index].GeographicLocationKnown, Is.False);
            });
            index++;
        }
    }

    [Test]
    public void VariableLengthCaseRetainsAValidLookingNumberPrefix()
    {
        PhoneEdgeCaseRecord record = Fake.Create(9702).Phone().EdgeCase(
            PhoneEdgeCase.VariableLength);

        Assert.Multiple(() =>
        {
            Assert.That(record.Number, Does.Match("^\\+999 12 [0-9]{4}$"));
            Assert.That(record.RelatedNumber, Does.Match("^\\+999 12 [0-9]{6}$"));
            Assert.That(record.RelatedNumber, Does.StartWith(record.Number));
            Assert.That(record.CallingCode, Is.EqualTo("+999"));
            Assert.That(record.RegionCodes, Is.EqualTo(new string[] { "XZ" }));
            Assert.That(record.HasVariableLength, Is.True);
        });
    }

    [Test]
    public void SharedCallingCodeCaseRetainsEveryPossibleRegion()
    {
        PhoneEdgeCaseRecord record = Fake.Create(9703).Phone().EdgeCase(
            PhoneEdgeCase.SharedCallingCode);

        Assert.Multiple(() =>
        {
            Assert.That(record.Number, Does.Match("^\\+1 [2-9][0-9]{2} 555 01[0-9]{2}$"));
            Assert.That(record.CallingCode, Is.EqualTo("+1"));
            Assert.That(record.RegionCodes, Is.EqualTo(new string[] { "US", "CA" }));
            Assert.That(record.CallingCodeIsShared, Is.True);
            Assert.That(record.GeographicLocationKnown, Is.False);
        });
    }

    [Test]
    public void SignificantLeadingZeroCasePreservesTheZeroAfterTheCallingCode()
    {
        PhoneEdgeCaseRecord record = Fake.Create(9704).Phone().EdgeCase(
            PhoneEdgeCase.SignificantLeadingZero);

        Assert.Multiple(() =>
        {
            Assert.That(record.Number, Does.Match("^\\+39 0[0-9]{7}$"));
            Assert.That(record.CallingCode, Is.EqualTo("+39"));
            Assert.That(record.RegionCodes, Is.EqualTo(new string[] { "IT" }));
            Assert.That(record.PossibleCategories,
                Is.EqualTo(new PhoneNumberCategory[] { PhoneNumberCategory.Landline }));
            Assert.That(record.LeadingZeroIsSignificant, Is.True);
        });
    }

    [Test]
    public void ExtensionCaseRetainsTheExtensionAsASeparateString()
    {
        PhoneEdgeCaseRecord record = Fake.Create(9705).Phone().EdgeCase(
            PhoneEdgeCase.Extension);

        Assert.Multiple(() =>
        {
            Assert.That(record.Number,
                Does.Match("^\\+1 [2-9][0-9]{2} 555 01[0-9]{2} ext\\. [0-9]{4}$"));
            Assert.That(record.Number, Does.EndWith(" ext. " + record.Extension));
            Assert.That(record.Extension, Does.Match("^[0-9]{4}$"));
            Assert.That(record.HasExtension, Is.True);
        });
    }

    [Test]
    public void NonGeographicServiceCaseDoesNotInventARegion()
    {
        PhoneEdgeCaseRecord record = Fake.Create(9706).Phone().EdgeCase(
            PhoneEdgeCase.NonGeographicService);

        Assert.Multiple(() =>
        {
            Assert.That(record.Number, Does.Match("^\\+800 [0-9]{8}$"));
            Assert.That(record.CallingCode, Is.EqualTo("+800"));
            Assert.That(record.RegionCodes, Is.Empty);
            Assert.That(record.PossibleCategories,
                Is.EqualTo(new PhoneNumberCategory[] { PhoneNumberCategory.TollFree }));
            Assert.That(record.IsNonGeographic, Is.True);
        });
    }

    [Test]
    public void AmbiguousCategoryCaseRetainsEveryPossibleCategory()
    {
        PhoneEdgeCaseRecord record = Fake.Create(9707).Phone().EdgeCase(
            PhoneEdgeCase.AmbiguousCategory);

        Assert.Multiple(() =>
        {
            Assert.That(record.PossibleCategories, Is.EqualTo(new PhoneNumberCategory[]
            {
                PhoneNumberCategory.Landline,
                PhoneNumberCategory.Mobile
            }));
            Assert.That(record.CategoryIsAmbiguous, Is.True);
            Assert.That(record.CallingCodeIsShared, Is.True);
            Assert.That(record.SubscriberIdentityKnown, Is.False);
        });
    }

    [Test]
    public void EdgeCaseResultsAndTheirArraysAreIsolated()
    {
        PhoneEdgeCaseRecord[] first = Fake.Create(9708).Phone().EdgeCases();
        PhoneEdgeCaseRecord[] matching = Fake.Create(9708).Phone().EdgeCases();

        first[1].RegionCodes[0] = "XX";
        first[2].PossibleCategories[0] = PhoneNumberCategory.Premium;
        first[0].Number = "changed";

        Assert.Multiple(() =>
        {
            Assert.That(matching[1].RegionCodes, Is.EqualTo(new string[] { "US", "CA" }));
            Assert.That(matching[2].PossibleCategories,
                Is.EqualTo(new PhoneNumberCategory[] { PhoneNumberCategory.Landline }));
            Assert.That(matching[0].Number, Is.Not.EqualTo("changed"));
        });
    }

    [Test]
    public void EdgeCaseApisRejectInvalidCasesAndHonorConfiguredLimits()
    {
        const long seed = 9709;
        PhoneProvider inspected = Fake.Create(seed).Phone();
        PhoneProvider untouched = Fake.Create(seed).Phone();
        FakeOptions countOptions = FakeOptions.Create(seed);
        countOptions.MaximumCount = 5;
        FakeOptions outputOptions = FakeOptions.Create(seed);
        outputOptions.MaximumOutputSize = 10;

        Assert.That(
            () => inspected.EdgeCase((PhoneEdgeCase)999),
            Throws.TypeOf<ArgumentOutOfRangeException>()
                .With.Property("ParamName").EqualTo("edgeCase"));
        Assert.That(
            inspected.EdgeCase(PhoneEdgeCase.SharedCallingCode).Number,
            Is.EqualTo(untouched.EdgeCase(PhoneEdgeCase.SharedCallingCode).Number));
        Assert.That(
            () => Fake.Create(countOptions).Phone().EdgeCases(),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(
            () => Fake.Create(outputOptions).Phone().EdgeCase(PhoneEdgeCase.VariableLength),
            Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static void AssertEquivalent(PhoneEdgeCaseRecord actual, PhoneEdgeCaseRecord expected)
    {
        Assert.That(actual.Case, Is.EqualTo(expected.Case));
        Assert.That(actual.Number, Is.EqualTo(expected.Number));
        Assert.That(actual.RelatedNumber, Is.EqualTo(expected.RelatedNumber));
        Assert.That(actual.CallingCode, Is.EqualTo(expected.CallingCode));
        Assert.That(actual.RegionCodes, Is.EqualTo(expected.RegionCodes));
        Assert.That(actual.PossibleCategories, Is.EqualTo(expected.PossibleCategories));
        Assert.That(actual.HasVariableLength, Is.EqualTo(expected.HasVariableLength));
        Assert.That(actual.CallingCodeIsShared, Is.EqualTo(expected.CallingCodeIsShared));
        Assert.That(actual.LeadingZeroIsSignificant, Is.EqualTo(expected.LeadingZeroIsSignificant));
        Assert.That(actual.HasExtension, Is.EqualTo(expected.HasExtension));
        Assert.That(actual.Extension, Is.EqualTo(expected.Extension));
        Assert.That(actual.IsNonGeographic, Is.EqualTo(expected.IsNonGeographic));
        Assert.That(actual.CategoryIsAmbiguous, Is.EqualTo(expected.CategoryIsAmbiguous));
    }
}
