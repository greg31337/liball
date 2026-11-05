namespace uri.Tests;

public class UriCharacterSetsTests
{
    [Test]
    public void CharacterSetsMatchRfc3986()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UriCharacterSets.GeneralDelimiters, Is.EqualTo(":/?#[]@"));
            Assert.That(UriCharacterSets.SubDelimiters, Is.EqualTo("!$&'()*+,;="));
            Assert.That(UriCharacterSets.Reserved, Is.EqualTo(":/?#[]@!$&'()*+,;="));
            Assert.That(
                UriCharacterSets.Unreserved,
                Is.EqualTo("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~"));
        });
    }

    [Test]
    public void ReservedSetIsOrderedUnionOfDelimiterSets()
    {
        Assert.That(
            UriCharacterSets.Reserved,
            Is.EqualTo(UriCharacterSets.GeneralDelimiters + UriCharacterSets.SubDelimiters));
    }
}
