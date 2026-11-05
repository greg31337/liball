using argon2;

namespace argon2.Tests.Security;

public class RandomSaltGeneratorTests
{
    [TestCase(8)]
    [TestCase(16)]
    [TestCase(32)]
    [TestCase(64)]
    public void CreatesIndependentSaltsWithTheExactRequestedLength(int length)
    {
        byte[] first = RandomSaltGenerator.CreateSalt(length);
        byte[] second = RandomSaltGenerator.CreateSalt(length);

        Assert.Multiple(() =>
        {
            Assert.That(first.Length, Is.EqualTo(length));
            Assert.That(second.Length, Is.EqualTo(length));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first, Is.Not.EqualTo(second));
        });
    }

    [Test]
    public void RejectsSaltLengthsOutsideTheSupportedManagedRange()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<Argon2ParameterException>(() => RandomSaltGenerator.CreateSalt(-1));
            Assert.Throws<Argon2ParameterException>(() => RandomSaltGenerator.CreateSalt(7));
            Assert.Throws<Argon2ParameterException>(
                () => RandomSaltGenerator.CreateSalt(Array.MaxLength + 1));
        });
    }
}
