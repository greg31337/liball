using System;
using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class NanoIdPublicApiBehaviorTests
{
    [Test]
    public void NanoIdInterface_ExposesEveryPublicOperation()
    {
        INanoId nanoId = new NanoId();
        NanoIdSettings settings = new NanoIdSettings("abcd", 4);

        string generated = nanoId.New();
        string empty = nanoId.NewWithSize(0);
        string custom = nanoId.NewWithAlphabet("abcd", 8);
        string configured = nanoId.NewWithSettings(settings);
        NanoIdSettings defaults = nanoId.DefaultSettings();

        Assert.That(generated, Has.Length.EqualTo(NanoIdSettings.DefaultSize));
        Assert.That(ContainsOnly(generated, NanoIdSettings.DefaultAlphabet), Is.True);
        Assert.That(empty, Is.EqualTo(""));
        Assert.That(custom, Has.Length.EqualTo(8));
        Assert.That(ContainsOnly(custom, "abcd"), Is.True);
        Assert.That(configured, Has.Length.EqualTo(4));
        Assert.That(ContainsOnly(configured, "abcd"), Is.True);
        Assert.That(defaults.Alphabet, Is.EqualTo(NanoIdSettings.DefaultAlphabet));
        Assert.That(defaults.Size, Is.EqualTo(NanoIdSettings.DefaultSize));
        Assert.That(nanoId.ValidateAlphabet("abcd").Ok, Is.True);
        Assert.That(nanoId.Validate(generated).Ok, Is.True);
        Assert.That(nanoId.ValidateWithSettings("abcd", settings).Ok, Is.True);
        Assert.That(nanoId.CollisionSpace(), Is.EqualTo(Math.Pow(64, 21)));
        Assert.That(nanoId.CollisionSpaceForSize(2), Is.EqualTo(4096));
        Assert.That(nanoId.CollisionSpaceForAlphabet("abcd", 3), Is.EqualTo(64));
        Assert.That(nanoId.CollisionSpaceForSettings(settings), Is.EqualTo(256));
        Assert.That(nanoId.CollisionSpaceForDimensions(4, 4), Is.EqualTo(256));
        Assert.That(nanoId.BitsOfEntropy(), Is.EqualTo(126).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropyForSize(2), Is.EqualTo(12).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropyForAlphabet("abcd", 3), Is.EqualTo(6).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropyForSettings(settings), Is.EqualTo(8).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropyForDimensions(4, 4), Is.EqualTo(8).Within(0.000001));
        Assert.That(nanoId.ApproximateCollisionProbability(4, 4, 10), Is.GreaterThan(0));
        Assert.That(nanoId.ApproximateCollisionProbabilityForSettings(settings, 10), Is.GreaterThan(0));
    }

    [Test]
    public void ConstantsAndDefaultSettings_HaveExactIndependentValues()
    {
        INanoId nanoId = new NanoId();
        NanoIdSettings first = nanoId.DefaultSettings();
        NanoIdSettings second = nanoId.DefaultSettings();

        first.Alphabet = "ab";
        first.Size = 0;

        Assert.That(NanoIdSettings.DefaultAlphabet, Is.EqualTo("useandom-26T198340PX75pxJACKVERYMINDBUSHWOLF_GQZbfghjklqvwyzrict"));
        Assert.That(NanoIdSettings.DefaultAlphabet, Has.Length.EqualTo(64));
        Assert.That(NanoIdSettings.DefaultSize, Is.EqualTo(21));
        Assert.That(second.Alphabet, Is.EqualTo(NanoIdSettings.DefaultAlphabet));
        Assert.That(second.Size, Is.EqualTo(NanoIdSettings.DefaultSize));
    }

    [Test]
    public void DeterministicGeneration_ExercisesAcceptedRejectedMinimumAndMaximumRandomIndexes()
    {
        SequenceRandomSource rejectionSource = new SequenceRandomSource(new byte[] { 254, 255, 0 });
        SequenceRandomSource minimumSource = new SequenceRandomSource(new byte[] { 0, 255 });
        SequenceRandomSource maximumSource = new SequenceRandomSource(new byte[] { 0, 255 });
        string maximumAlphabet = BuildAlphabet(256);

        string rejection = NanoId.NewWithSettings(new NanoIdSettings("abc", 2), rejectionSource);
        string minimum = NanoId.NewWithSettings(new NanoIdSettings("ab", 2), minimumSource);
        string maximum = NanoId.NewWithSettings(new NanoIdSettings(maximumAlphabet, 2), maximumSource);

        Assert.That(rejection, Is.EqualTo("ca"));
        Assert.That(rejectionSource.BytesRead, Is.EqualTo(3));
        Assert.That(minimum, Is.EqualTo("ab"));
        Assert.That(minimumSource.BytesRead, Is.EqualTo(2));
        Assert.That(maximum[0], Is.EqualTo(maximumAlphabet[0]));
        Assert.That(maximum[1], Is.EqualTo(maximumAlphabet[255]));
        Assert.That(maximumSource.BytesRead, Is.EqualTo(2));
        Assert.That(RandomTools.UnbiasedByteLimit(2), Is.EqualTo(256));
        Assert.That(RandomTools.UnbiasedByteLimit(3), Is.EqualTo(255));
        Assert.That(RandomTools.UnbiasedByteLimit(255), Is.EqualTo(255));
        Assert.That(RandomTools.UnbiasedByteLimit(256), Is.EqualTo(256));
    }

    [Test]
    public void Generation_ReportsEveryNullAlphabetAndSizeFailureWithStableCodes()
    {
        INanoId nanoId = new NanoId();
        NanoIdSettings duplicate = new NanoIdSettings("abc", 4);
        duplicate.Alphabet = "abca";
        NanoIdSettings negative = new NanoIdSettings("abc", 4);
        negative.Size = -1;

        ArgumentNullException settingsNull = Assert.Throws<ArgumentNullException>(() => nanoId.NewWithSettings(null));
        ArgumentNullException sourceNull = Assert.Throws<ArgumentNullException>(() => NanoId.NewWithSettings(new NanoIdSettings("ab", 0), null));

        Assert.That(settingsNull.ParamName, Is.EqualTo("settings"));
        Assert.That(sourceNull.ParamName, Is.EqualTo("source"));
        AssertIdentifierException(() => nanoId.NewWithSize(-1), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        AssertIdentifierException(() => nanoId.NewWithAlphabet(null, 4), IdentifierErrorCode.AlphabetError, "Nano ID alphabet is null");
        AssertIdentifierException(() => nanoId.NewWithAlphabet("a", 4), IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        AssertIdentifierException(() => nanoId.NewWithAlphabet("abca", 4), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 3");
        AssertIdentifierException(() => nanoId.NewWithAlphabet(BuildAlphabet(257), 4), IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        AssertIdentifierException(() => nanoId.NewWithSettings(duplicate), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 3");
        AssertIdentifierException(() => nanoId.NewWithSettings(negative), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
    }

    [Test]
    public void SettingsConstruction_StoresBoundariesAndReportsEveryCodedFailure()
    {
        NanoIdSettings minimum = new NanoIdSettings("ab", 0);
        NanoIdSettings maximum = new NanoIdSettings(BuildAlphabet(256), int.MaxValue);

        Assert.That(minimum.Alphabet, Is.EqualTo("ab"));
        Assert.That(minimum.Size, Is.EqualTo(0));
        Assert.That(maximum.Alphabet, Has.Length.EqualTo(256));
        Assert.That(maximum.Size, Is.EqualTo(int.MaxValue));
        AssertIdentifierException(() => new NanoIdSettings(null, 1), IdentifierErrorCode.AlphabetError, "Nano ID alphabet is null");
        AssertIdentifierException(() => new NanoIdSettings("", 1), IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        AssertIdentifierException(() => new NanoIdSettings("a", 1), IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        AssertIdentifierException(() => new NanoIdSettings(BuildAlphabet(257), 1), IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        AssertIdentifierException(() => new NanoIdSettings("aabc", 1), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 1");
        AssertIdentifierException(() => new NanoIdSettings("ab", -1), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
    }

    [Test]
    public void ValidateAlphabet_ReturnsCompleteSuccessAndFailureResults()
    {
        INanoId nanoId = new NanoId();

        IdentifierValidationResult minimum = nanoId.ValidateAlphabet("ab");
        IdentifierValidationResult maximum = nanoId.ValidateAlphabet(BuildAlphabet(256));

        AssertValidationSuccess(minimum, "ab");
        AssertValidationSuccess(maximum, BuildAlphabet(256));
        AssertValidationFailure(nanoId.ValidateAlphabet(null), IdentifierErrorCode.AlphabetError, "Nano ID alphabet is null");
        AssertValidationFailure(nanoId.ValidateAlphabet(""), IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        AssertValidationFailure(nanoId.ValidateAlphabet("a"), IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        AssertValidationFailure(nanoId.ValidateAlphabet(BuildAlphabet(257)), IdentifierErrorCode.AlphabetError, "Nano ID alphabet must contain between 2 and 256 characters");
        AssertValidationFailure(nanoId.ValidateAlphabet("abca"), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 3");
    }

    [Test]
    public void TextValidation_CoversDefaultCustomEmptyNullInvalidSettingsLengthAndCharacterPaths()
    {
        INanoId nanoId = new NanoId();
        string defaultText = NanoIdSettings.DefaultAlphabet.Substring(0, NanoIdSettings.DefaultSize);
        NanoIdSettings custom = new NanoIdSettings("abc", 3);
        NanoIdSettings empty = new NanoIdSettings("ab", 0);
        NanoIdSettings invalidAlphabet = new NanoIdSettings("abc", 3);
        invalidAlphabet.Alphabet = "abca";
        NanoIdSettings invalidSize = new NanoIdSettings("abc", 3);
        invalidSize.Size = -1;

        AssertValidationSuccess(nanoId.Validate(defaultText), defaultText);
        AssertValidationSuccess(nanoId.ValidateWithSettings("abc", custom), "abc");
        AssertValidationSuccess(nanoId.ValidateWithSettings("", empty), "");
        AssertValidationFailure(nanoId.Validate(null), IdentifierErrorCode.NullInput, "Nano ID text is null");
        AssertValidationFailure(nanoId.Validate("short"), IdentifierErrorCode.InvalidLength, "expected 21 Nano ID characters but found 5");
        AssertValidationFailure(nanoId.Validate("!!!!!!!!!!!!!!!!!!!!!"), IdentifierErrorCode.InvalidCharacter, "invalid Nano ID character at index 0");
        AssertValidationFailure(nanoId.ValidateWithSettings("abc", null), IdentifierErrorCode.NullInput, "Nano ID settings is null");
        AssertValidationFailure(nanoId.ValidateWithSettings("abc", invalidAlphabet), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 3");
        AssertValidationFailure(nanoId.ValidateWithSettings("abc", invalidSize), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        AssertValidationFailure(nanoId.ValidateWithSettings(null, custom), IdentifierErrorCode.NullInput, "Nano ID text is null");
        AssertValidationFailure(nanoId.ValidateWithSettings("ab", custom), IdentifierErrorCode.InvalidLength, "expected 3 Nano ID characters but found 2");
        AssertValidationFailure(nanoId.ValidateWithSettings("abd", custom), IdentifierErrorCode.InvalidCharacter, "invalid Nano ID character at index 2");
    }

    [Test]
    public void CollisionSpaceAndEntropy_CoverZeroExactDefaultAndInfinityBoundaries()
    {
        INanoId nanoId = new NanoId();
        NanoIdSettings settings = new NanoIdSettings("abcd", 3);

        Assert.That(nanoId.CollisionSpaceForDimensions(2, 0), Is.EqualTo(1));
        Assert.That(nanoId.CollisionSpaceForDimensions(2, 10), Is.EqualTo(1024));
        Assert.That(nanoId.CollisionSpaceForSize(2), Is.EqualTo(4096));
        Assert.That(nanoId.CollisionSpaceForAlphabet("abcd", 3), Is.EqualTo(64));
        Assert.That(nanoId.CollisionSpaceForSettings(settings), Is.EqualTo(64));
        Assert.That(nanoId.CollisionSpace(), Is.EqualTo(Math.Pow(64, 21)));
        Assert.That(double.IsPositiveInfinity(nanoId.CollisionSpaceForDimensions(256, 128)), Is.True);
        Assert.That(nanoId.BitsOfEntropyForDimensions(2, 0), Is.EqualTo(0));
        Assert.That(nanoId.BitsOfEntropyForDimensions(2, 10), Is.EqualTo(10).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropyForSize(2), Is.EqualTo(12).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropyForAlphabet("abcd", 3), Is.EqualTo(6).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropyForSettings(settings), Is.EqualTo(6).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropy(), Is.EqualTo(126).Within(0.000001));
        Assert.That(nanoId.BitsOfEntropyForDimensions(256, 128), Is.EqualTo(1024).Within(0.000001));
    }

    [Test]
    public void CollisionAndEntropyHelpers_ReportEveryDocumentedFailure()
    {
        INanoId nanoId = new NanoId();
        NanoIdSettings invalidAlphabet = new NanoIdSettings("abc", 3);
        invalidAlphabet.Alphabet = "abca";
        NanoIdSettings invalidSize = new NanoIdSettings("abc", 3);
        invalidSize.Size = -1;

        AssertIdentifierException(() => nanoId.CollisionSpaceForDimensions(1, 1), IdentifierErrorCode.AlphabetError, "alphabet length must be between 2 and 256");
        AssertIdentifierException(() => nanoId.CollisionSpaceForDimensions(257, 1), IdentifierErrorCode.AlphabetError, "alphabet length must be between 2 and 256");
        AssertIdentifierException(() => nanoId.CollisionSpaceForDimensions(2, -1), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        AssertIdentifierException(() => nanoId.CollisionSpaceForSize(-1), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        AssertIdentifierException(() => nanoId.CollisionSpaceForAlphabet(null, 1), IdentifierErrorCode.AlphabetError, "Nano ID alphabet is null");
        AssertIdentifierException(() => nanoId.CollisionSpaceForAlphabet("aa", 1), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 1");
        AssertIdentifierException(() => nanoId.CollisionSpaceForSettings(invalidAlphabet), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 3");
        AssertIdentifierException(() => nanoId.CollisionSpaceForSettings(invalidSize), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        AssertIdentifierException(() => nanoId.BitsOfEntropyForDimensions(1, 1), IdentifierErrorCode.AlphabetError, "alphabet length must be between 2 and 256");
        AssertIdentifierException(() => nanoId.BitsOfEntropyForDimensions(257, 1), IdentifierErrorCode.AlphabetError, "alphabet length must be between 2 and 256");
        AssertIdentifierException(() => nanoId.BitsOfEntropyForDimensions(2, -1), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        AssertIdentifierException(() => nanoId.BitsOfEntropyForSize(-1), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        AssertIdentifierException(() => nanoId.BitsOfEntropyForAlphabet(null, 1), IdentifierErrorCode.AlphabetError, "Nano ID alphabet is null");
        AssertIdentifierException(() => nanoId.BitsOfEntropyForSettings(invalidAlphabet), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 3");
        AssertIdentifierException(() => nanoId.BitsOfEntropyForSettings(invalidSize), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");

        ArgumentNullException collisionNull = Assert.Throws<ArgumentNullException>(() => nanoId.CollisionSpaceForSettings(null));
        ArgumentNullException entropyNull = Assert.Throws<ArgumentNullException>(() => nanoId.BitsOfEntropyForSettings(null));
        Assert.That(collisionNull.ParamName, Is.EqualTo("settings"));
        Assert.That(entropyNull.ParamName, Is.EqualTo("settings"));
    }

    [Test]
    public void CollisionProbability_CoversBirthdayBoundSettingsAndProbabilityBoundaries()
    {
        INanoId nanoId = new NanoId();
        NanoIdSettings settings = new NanoIdSettings("0123456789", 6);
        double expected = 1 - Math.Exp(-((1000.0 * 999.0) / (2.0 * 1000000.0)));

        Assert.That(nanoId.ApproximateCollisionProbability(10, 6, 0), Is.EqualTo(0));
        Assert.That(nanoId.ApproximateCollisionProbability(10, 6, 1), Is.EqualTo(0));
        Assert.That(nanoId.ApproximateCollisionProbability(2, 0, 2), Is.EqualTo(1));
        Assert.That(nanoId.ApproximateCollisionProbability(10, 6, 1000), Is.EqualTo(expected).Within(0.000001));
        Assert.That(nanoId.ApproximateCollisionProbabilityForSettings(settings, 1000), Is.EqualTo(expected).Within(0.000001));
        Assert.That(nanoId.ApproximateCollisionProbability(256, 128, double.MaxValue), Is.EqualTo(1));
        Assert.That(nanoId.ApproximateCollisionProbability(256, 128, double.PositiveInfinity), Is.EqualTo(1));
    }

    [Test]
    public void CollisionProbability_ReportsDimensionSettingsAndCountFailures()
    {
        INanoId nanoId = new NanoId();
        NanoIdSettings invalidAlphabet = new NanoIdSettings("abc", 3);
        invalidAlphabet.Alphabet = "abca";
        NanoIdSettings invalidSize = new NanoIdSettings("abc", 3);
        invalidSize.Size = -1;

        AssertIdentifierException(() => nanoId.ApproximateCollisionProbability(1, 1, 2), IdentifierErrorCode.AlphabetError, "alphabet length must be between 2 and 256");
        AssertIdentifierException(() => nanoId.ApproximateCollisionProbability(257, 1, 2), IdentifierErrorCode.AlphabetError, "alphabet length must be between 2 and 256");
        AssertIdentifierException(() => nanoId.ApproximateCollisionProbability(2, -1, 2), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");
        AssertIdentifierException(() => nanoId.ApproximateCollisionProbabilityForSettings(invalidAlphabet, 2), IdentifierErrorCode.AlphabetError, "duplicate Nano ID alphabet character at index 3");
        AssertIdentifierException(() => nanoId.ApproximateCollisionProbabilityForSettings(invalidSize, 2), IdentifierErrorCode.InvalidLength, "Nano ID size cannot be negative");

        ArgumentOutOfRangeException negative = Assert.Throws<ArgumentOutOfRangeException>(() => nanoId.ApproximateCollisionProbability(2, 1, -1));
        ArgumentOutOfRangeException negativeSettings = Assert.Throws<ArgumentOutOfRangeException>(() => nanoId.ApproximateCollisionProbabilityForSettings(new NanoIdSettings("ab", 1), -1));
        ArgumentOutOfRangeException notANumber = Assert.Throws<ArgumentOutOfRangeException>(() => nanoId.ApproximateCollisionProbability(2, 1, double.NaN));
        ArgumentNullException nullSettings = Assert.Throws<ArgumentNullException>(() => nanoId.ApproximateCollisionProbabilityForSettings(null, 2));

        Assert.That(negative.ParamName, Is.EqualTo("generatedCount"));
        Assert.That(negativeSettings.ParamName, Is.EqualTo("generatedCount"));
        Assert.That(notANumber.ParamName, Is.EqualTo("generatedCount"));
        Assert.That(nullSettings.ParamName, Is.EqualTo("settings"));
    }

    static bool ContainsOnly(string text, string alphabet)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (alphabet.IndexOf(text[index]) < 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    static string BuildAlphabet(int length)
    {
        char[] characters = new char[length];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = (char)(index + 1);
            index = index + 1;
        }

        return new string(characters);
    }

    static void AssertValidationSuccess(IdentifierValidationResult result, string normalizedText)
    {
        Assert.That(result.Ok, Is.True);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.NanoId));
        Assert.That(result.NormalizedText, Is.EqualTo(normalizedText));
        Assert.That(result.Error, Is.EqualTo(""));
        Assert.That(result.Code, Is.EqualTo(IdentifierErrorCode.None));
    }

    static void AssertValidationFailure(IdentifierValidationResult result, IdentifierErrorCode code, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Code, Is.EqualTo(code));
    }

    static void AssertIdentifierException(TestDelegate action, IdentifierErrorCode code, string message)
    {
        IdentifierException exception = Assert.Throws<IdentifierException>(action);
        Assert.That(exception.Code, Is.EqualTo(code));
        Assert.That(exception.Message, Is.EqualTo(message));
    }

    class SequenceRandomSource : IRandomSource
    {
        byte[] Bytes;
        public int BytesRead;

        public SequenceRandomSource(byte[] bytes)
        {
            Bytes = bytes;
        }

        public void FillBytes(byte[] destination)
        {
            int index = 0;
            while (index < destination.Length)
            {
                if (BytesRead >= Bytes.Length)
                {
                    throw new InvalidOperationException("scripted random bytes exhausted");
                }

                destination[index] = Bytes[BytesRead];
                BytesRead = BytesRead + 1;
                index = index + 1;
            }
        }
    }
}
