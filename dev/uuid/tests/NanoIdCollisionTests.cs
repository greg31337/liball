using NUnit.Framework;

namespace uuid.Tests;

[TestFixture]
public class NanoIdCollisionTests
{
    [Test]
    public void CollisionSpace_ReturnsAlphabetLengthRaisedToSize()
    {
        NanoIdSettings settings = new NanoIdSettings("abc", 4);

        Assert.That(new NanoId().CollisionSpaceForDimensions(2, 10), Is.EqualTo(1024));
        Assert.That(new NanoId().CollisionSpaceForSettings(settings), Is.EqualTo(81));
        Assert.That(new NanoId().CollisionSpaceForAlphabet("abcd", 3), Is.EqualTo(64));
        Assert.That(new NanoId().CollisionSpaceForSize(0), Is.EqualTo(1));
    }

    [Test]
    public void CollisionSpace_DefaultUsesNanoIdDefaults()
    {
        double expected = Math.Pow(NanoIdSettings.DefaultAlphabet.Length, NanoIdSettings.DefaultSize);

        Assert.That(new NanoId().CollisionSpace(), Is.EqualTo(expected));
    }

    [Test]
    public void BitsOfEntropy_ReturnsLogBaseTwoCollisionSpace()
    {
        NanoIdSettings settings = new NanoIdSettings("abc", 4);

        Assert.That(new NanoId().BitsOfEntropyForDimensions(2, 10), Is.EqualTo(10).Within(0.000001));
        Assert.That(new NanoId().BitsOfEntropyForSettings(settings), Is.EqualTo(4 * (Math.Log(3) / Math.Log(2.0))).Within(0.000001));
        Assert.That(new NanoId().BitsOfEntropyForAlphabet("abcd", 3), Is.EqualTo(6).Within(0.000001));
        Assert.That(new NanoId().BitsOfEntropy(), Is.EqualTo(126).Within(0.000001));
    }

    [Test]
    public void ApproximateCollisionProbability_UsesBirthdayBound()
    {
        double expected = 1 - Math.Exp(-((1000.0 * 999.0) / (2.0 * 1000000.0)));

        double probability = new NanoId().ApproximateCollisionProbability(10, 6, 1000);

        Assert.That(probability, Is.EqualTo(expected).Within(0.000001));
    }

    [Test]
    public void ApproximateCollisionProbability_ReturnsZeroForZeroOrOneGeneratedIds()
    {
        Assert.That(new NanoId().ApproximateCollisionProbability(2, 10, 0), Is.EqualTo(0));
        Assert.That(new NanoId().ApproximateCollisionProbability(2, 10, 1), Is.EqualTo(0));
    }

    [Test]
    public void ApproximateCollisionProbability_ReturnsOneWhenSpaceHasSingleValue()
    {
        Assert.That(new NanoId().ApproximateCollisionProbability(2, 0, 2), Is.EqualTo(1));
    }

    [Test]
    public void CollisionHelpers_RejectInvalidInputs()
    {
        NanoIdSettings invalidAlphabet = new NanoIdSettings("abc", 8);
        invalidAlphabet.Alphabet = "abca";
        NanoIdSettings invalidSize = new NanoIdSettings("abc", 8);
        invalidSize.Size = -1;

        Assert.That(() => new NanoId().CollisionSpaceForDimensions(1, 10), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().CollisionSpaceForDimensions(257, 10), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().CollisionSpaceForDimensions(2, -1), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().CollisionSpaceForSettings(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new NanoId().CollisionSpaceForSettings(invalidAlphabet), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().CollisionSpaceForSettings(invalidSize), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().CollisionSpaceForAlphabet("aa", 4), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().BitsOfEntropyForSettings(null), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => new NanoId().BitsOfEntropyForDimensions(1, 10), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().BitsOfEntropyForDimensions(2, -1), Throws.TypeOf<IdentifierException>());
        Assert.That(() => new NanoId().ApproximateCollisionProbability(2, 10, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new NanoId().ApproximateCollisionProbabilityForSettings(null, 10), Throws.TypeOf<ArgumentNullException>());
    }
}
