using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class NanoIdGeneratorTests
{
    [Test]
    public void New_ReturnsDefaultLengthUsingDefaultAlphabet()
    {
        string id = new NanoId().New();

        Assert.That(id.Length, Is.EqualTo(NanoIdSettings.DefaultSize));
        Assert.That(ContainsOnlyAlphabetCharacters(id, NanoIdSettings.DefaultAlphabet), Is.True);
    }

    [Test]
    public void NewWithSize_ReturnsRequestedLengthUsingDefaultAlphabet()
    {
        string id = new NanoId().NewWithSize(32);

        Assert.That(id.Length, Is.EqualTo(32));
        Assert.That(ContainsOnlyAlphabetCharacters(id, NanoIdSettings.DefaultAlphabet), Is.True);
    }

    [Test]
    public void NewWithAlphabet_ReturnsRequestedLengthUsingCustomAlphabet()
    {
        string id = new NanoId().NewWithAlphabet("abc123", 64);

        Assert.That(id.Length, Is.EqualTo(64));
        Assert.That(ContainsOnlyAlphabetCharacters(id, "abc123"), Is.True);
    }

    [Test]
    public void NewWithAlphabet_UsesMinimumAndMaximumAlphabetBounds()
    {
        string minimumAlphabetId = new NanoId().NewWithAlphabet("01", 128);
        string maximumAlphabet = BuildAlphabet(256);
        string maximumAlphabetId = new NanoId().NewWithAlphabet(maximumAlphabet, 128);

        Assert.That(minimumAlphabetId.Length, Is.EqualTo(128));
        Assert.That(ContainsOnlyAlphabetCharacters(minimumAlphabetId, "01"), Is.True);
        Assert.That(maximumAlphabetId.Length, Is.EqualTo(128));
        Assert.That(ContainsOnlyAlphabetCharacters(maximumAlphabetId, maximumAlphabet), Is.True);
    }

    [Test]
    public void NewWithAlphabet_CanReachEveryIndexForSmallAlphabet()
    {
        bool[] seen = new bool[3];
        int iteration = 0;
        while (iteration < 2000)
        {
            string id = new NanoId().NewWithAlphabet("abc", 1);
            int index = "abc".IndexOf(id[0]);
            seen[index] = true;
            iteration = iteration + 1;
        }

        Assert.That(seen[0], Is.True);
        Assert.That(seen[1], Is.True);
        Assert.That(seen[2], Is.True);
    }

    [Test]
    public void NewWithSettings_UsesProvidedSettings()
    {
        NanoIdSettings settings = new NanoIdSettings("abcdef", 16);

        string id = new NanoId().NewWithSettings(settings);

        Assert.That(id.Length, Is.EqualTo(16));
        Assert.That(ContainsOnlyAlphabetCharacters(id, "abcdef"), Is.True);
    }

    [Test]
    public void NewWithSize_AllowsExplicitZeroLength()
    {
        string id = new NanoId().NewWithSize(0);

        Assert.That(id, Is.EqualTo(""));
    }

    [Test]
    public void Generation_RejectsInvalidSettings()
    {
        NanoIdSettings duplicateAlphabet = new NanoIdSettings("abc", 8);
        duplicateAlphabet.Alphabet = "abca";
        NanoIdSettings negativeSize = new NanoIdSettings("abc", 8);
        negativeSize.Size = -1;

        Assert.That(() => new NanoId().NewWithSettings(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new NanoId().NewWithSize(-1), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().NewWithAlphabet(null, 8), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().NewWithAlphabet("a", 8), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().NewWithAlphabet("abca", 8), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().NewWithAlphabet(BuildAlphabet(257), 8), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().NewWithSettings(duplicateAlphabet), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().NewWithSettings(negativeSize), Throws.TypeOf<IdentifierException>());
    }

    static bool ContainsOnlyAlphabetCharacters(string text, string alphabet)
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
}
