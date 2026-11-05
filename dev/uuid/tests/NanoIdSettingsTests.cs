using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class NanoIdSettingsTests
{
    [Test]
    public void DefaultSettings_UseNanoIdDefaults()
    {
        NanoIdSettings settings = NanoIdSettings.Default();

        Assert.That(settings.Alphabet, Is.EqualTo("useandom-26T198340PX75pxJACKVERYMINDBUSHWOLF_GQZbfghjklqvwyzrict"));
        Assert.That(settings.Alphabet.Length, Is.EqualTo(64));
        Assert.That(settings.Alphabet.Contains("_"), Is.True);
        Assert.That(settings.Alphabet.Contains("-"), Is.True);
        Assert.That(settings.Size, Is.EqualTo(21));
    }

    [Test]
    public void Constructor_StoresCustomAlphabetAndSize()
    {
        NanoIdSettings settings = new NanoIdSettings("abc123", 12);

        Assert.That(settings.Alphabet, Is.EqualTo("abc123"));
        Assert.That(settings.Size, Is.EqualTo(12));
        Assert.That(settings.Validate().Ok, Is.True);
    }

    [Test]
    public void Constructor_AllowsZeroSizeForExplicitEmptyIds()
    {
        NanoIdSettings settings = new NanoIdSettings("abc123", 0);

        Assert.That(settings.Size, Is.EqualTo(0));
    }

    [Test]
    public void Default_ReturnsIndependentDefaultSettings()
    {
        NanoIdSettings first = NanoIdSettings.Default();
        NanoIdSettings second = NanoIdSettings.Default();

        first.Alphabet = "abc";
        first.Size = 3;

        Assert.That(second.Alphabet, Is.EqualTo(NanoIdSettings.DefaultAlphabet));
        Assert.That(second.Size, Is.EqualTo(NanoIdSettings.DefaultSize));
    }

    [Test]
    public void Copy_ReturnsIndependentSettings()
    {
        NanoIdSettings original = new NanoIdSettings("abc123", 12);
        NanoIdSettings copy = NanoIdSettings.Copy(original);

        original.Alphabet = "xyz";
        original.Size = 5;

        Assert.That(copy.Alphabet, Is.EqualTo("abc123"));
        Assert.That(copy.Size, Is.EqualTo(12));
    }

    [Test]
    public void ConstructorAndCopy_RejectInvalidInput()
    {
        Assert.That(() => new NanoIdSettings(null, 21), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoIdSettings("a", 21), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoIdSettings("abca", 21), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoIdSettings(BuildAlphabet(257), 21), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoIdSettings("abc", -1), Throws.TypeOf<IdentifierException>());
        Assert.That(() => NanoIdSettings.Copy(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => NanoIdSettings.ThrowIfInvalidAlphabet(null), Throws.TypeOf<IdentifierException>());
        Assert.That(() => NanoIdSettings.ThrowIfInvalidAlphabet("aa"), Throws.TypeOf<IdentifierException>());
        Assert.That(() => NanoIdSettings.ThrowIfInvalidSize(-1), Throws.TypeOf<IdentifierException>());
    }

    [Test]
    public void ValidateAlphabet_AcceptsValidUniqueAlphabets()
    {
        IdentifierValidationResult defaultResult = NanoIdSettings.ValidateAlphabet(NanoIdSettings.DefaultAlphabet);
        IdentifierValidationResult customResult = new NanoId().ValidateAlphabet("abc123");

        Assert.That(defaultResult.Ok, Is.True);
        Assert.That(defaultResult.Kind, Is.EqualTo(IdentifierKind.NanoId));
        Assert.That(defaultResult.NormalizedText, Is.EqualTo(NanoIdSettings.DefaultAlphabet));
        Assert.That(defaultResult.Error, Is.EqualTo(""));
        Assert.That(customResult.Ok, Is.True);
        Assert.That(customResult.NormalizedText, Is.EqualTo("abc123"));
    }

    [Test]
    public void ValidateAlphabet_RejectsNullAndInvalidLength()
    {
        AssertValidationFailure(NanoIdSettings.ValidateAlphabet(null), "Nano ID alphabet is null");
        AssertValidationFailure(NanoIdSettings.ValidateAlphabet(""), "Nano ID alphabet must contain between 2 and 256 characters");
        AssertValidationFailure(NanoIdSettings.ValidateAlphabet("a"), "Nano ID alphabet must contain between 2 and 256 characters");
        AssertValidationFailure(NanoIdSettings.ValidateAlphabet(BuildAlphabet(257)), "Nano ID alphabet must contain between 2 and 256 characters");
    }

    [Test]
    public void ValidateAlphabet_RejectsDuplicateCharacters()
    {
        AssertValidationFailure(NanoIdSettings.ValidateAlphabet("abca"), "duplicate Nano ID alphabet character at index 3");
        AssertValidationFailure(new NanoId().ValidateAlphabet("aabc"), "duplicate Nano ID alphabet character at index 1");
    }

    [Test]
    public void Validate_ReturnsSizeFailuresAfterAlphabetPasses()
    {
        NanoIdSettings settings = NanoIdSettings.Default();
        settings.Size = -1;

        IdentifierValidationResult result = settings.Validate();

        AssertValidationFailure(result, "Nano ID size cannot be negative");
    }

    static void AssertValidationFailure(IdentifierValidationResult result, string error)
    {
        Assert.That(result.Ok, Is.False);
        Assert.That(result.Kind, Is.EqualTo(IdentifierKind.Unknown));
        Assert.That(result.NormalizedText, Is.EqualTo(""));
        Assert.That(result.Error, Is.EqualTo(error));
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
}
