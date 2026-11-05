using System.Text;
using htmlgui;

namespace htmlgui.Tests;

[TestFixture]
public class StableHashTests
{
    [Test]
    public void HashTextUsesStableLowercaseSha256()
    {
        Assert.That(
            StableHash.HashText("abc"),
            Is.EqualTo("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad"));
        Assert.That(StableHash.HashBytes(Encoding.UTF8.GetBytes("abc")), Is.EqualTo(StableHash.HashText("abc")));
        Assert.Throws<ArgumentNullException>(() => StableHash.HashText(null));
        Assert.Throws<ArgumentNullException>(() => StableHash.HashBytes(null!));
    }

    [Test]
    public void HashPartsUsesUnambiguousLengthPrefixes()
    {
        string first = StableHash.HashParts(new string[] { "ab", "c" });
        string second = StableHash.HashParts(new string[] { "a", "bc" });

        Assert.That(first, Is.Not.EqualTo(second));
        Assert.That(first, Is.EqualTo(StableHash.HashParts(new string[] { "ab", "c" })));
        Assert.Throws<ArgumentNullException>(() => StableHash.HashParts(null!));
        Assert.Throws<ArgumentException>(() => StableHash.HashParts(new string[] { "valid", null! }));
    }

    [Test]
    public void ShortTokenValidatesAndReturnsRequestedLength()
    {
        Assert.That(StableHash.ShortToken("abc", 8), Is.EqualTo("ba7816bf"));
        Assert.Throws<ArgumentOutOfRangeException>(() => StableHash.ShortToken("abc", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => StableHash.ShortToken("abc", 65));
        Assert.Throws<ArgumentNullException>(() => StableHash.ShortToken(null, 8));
    }
}
