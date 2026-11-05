using fake;

namespace fake.Tests;

[TestFixture]
public class GenerationModeTests
{
    [Test]
    public void ContextCapturesEveryModeCombinationIndependently()
    {
        foreach (GenerationMode generation in Enum.GetValues(typeof(GenerationMode)))
        {
            foreach (SafetyMode safety in Enum.GetValues(typeof(SafetyMode)))
            {
                foreach (StrictnessMode strictness in Enum.GetValues(typeof(StrictnessMode)))
                {
                    foreach (NormalizationMode normalization in Enum.GetValues(typeof(NormalizationMode)))
                    {
                        FakeOptions options = new FakeOptions();
                        options.GenerationMode = generation;
                        options.SafetyMode = safety;
                        options.StrictnessMode = strictness;
                        options.NormalizationMode = normalization;

                        GenerationContext context = new GenerationContext(options);

                        Assert.Multiple(() =>
                        {
                            Assert.That(context.GenerationModeValue, Is.EqualTo(generation));
                            Assert.That(context.SafetyModeValue, Is.EqualTo(safety));
                            Assert.That(context.StrictnessModeValue, Is.EqualTo(strictness));
                            Assert.That(context.NormalizationModeValue, Is.EqualTo(normalization));
                        });
                    }
                }
            }
        }
    }

    [Test]
    public void IdentifierMetadataCombinesCapturedModesWithIndependentValidity()
    {
        FakeOptions options = FakeOptions.Create("fr-CA", 146L);
        options.GenerationMode = GenerationMode.Mixed;
        options.SafetyMode = SafetyMode.UnrestrictedShape;
        options.StrictnessMode = StrictnessMode.Strict;
        options.NormalizationMode = NormalizationMode.Nfkd;
        IdentifierProvider identifiers = Fake.Create(options).Identifier();
        string validValue = identifiers.Ean8();
        char replacement = validValue[7] == '9' ? '0' : (char)(validValue[7] + 1);
        string invalidValue = validValue.Substring(0, 7) + replacement;

        GenerationMetadata valid = identifiers.IdentifierMetadata(validValue, IdentifierKind.Ean8);
        GenerationMetadata invalid = identifiers.IdentifierMetadata(invalidValue, IdentifierKind.Ean8);

        Assert.Multiple(() =>
        {
            AssertCapturedModes(valid);
            AssertCapturedModes(invalid);
            Assert.That(valid.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(valid.ChecksumValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(valid.Validation.Valid, Is.True);
            Assert.That(invalid.StructuralValidity, Is.EqualTo(ValidityMode.Valid));
            Assert.That(invalid.ChecksumValidity, Is.EqualTo(ValidityMode.Invalid));
            Assert.That(invalid.Validation.Valid, Is.False);
        });
    }

    [Test]
    public void RootForkPropagatesEveryCapturedMode()
    {
        FakeOptions options = FakeOptions.Create(146L);
        options.GenerationMode = GenerationMode.Adversarial;
        options.SafetyMode = SafetyMode.SyntheticPlausible;
        options.StrictnessMode = StrictnessMode.Strict;
        options.NormalizationMode = NormalizationMode.Nfd;
        FakeOptions forked = Fake.Create(options).Fork("mode-propagation").OptionsSnapshot();

        Assert.Multiple(() =>
        {
            Assert.That(forked.GenerationMode, Is.EqualTo(GenerationMode.Adversarial));
            Assert.That(forked.SafetyMode, Is.EqualTo(SafetyMode.SyntheticPlausible));
            Assert.That(forked.StrictnessMode, Is.EqualTo(StrictnessMode.Strict));
            Assert.That(forked.NormalizationMode, Is.EqualTo(NormalizationMode.Nfd));
        });
    }

    static void AssertCapturedModes(GenerationMetadata metadata)
    {
        Assert.That(metadata.GenerationMode, Is.EqualTo(GenerationMode.Mixed));
        Assert.That(metadata.SafetyMode, Is.EqualTo(SafetyMode.UnrestrictedShape));
        Assert.That(metadata.StrictnessMode, Is.EqualTo(StrictnessMode.Strict));
        Assert.That(metadata.NormalizationMode, Is.EqualTo(NormalizationMode.Nfkd));
    }
}
