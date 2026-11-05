using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class AlphanumericEncoderTests
{
    [Test]
    public void MapsEveryRepertoireCharacterToItsExactValue()
    {
        for (int expectedValue = 0; expectedValue < QrCharacterSets.Alphanumeric.Length; expectedValue++)
        {
            char character = QrCharacterSets.Alphanumeric[expectedValue];
            Assert.That(AlphanumericEncoder.GetValue(character), Is.EqualTo(expectedValue), character.ToString());
        }
    }

    [TestCase(0, 0)]
    [TestCase(1, 6)]
    [TestCase(2, 11)]
    [TestCase(3, 17)]
    [TestCase(4, 22)]
    [TestCase(5, 28)]
    [TestCase(6, 33)]
    public void CalculatesExactPairAndTerminalBitLengths(int characterCount, int expectedBitLength)
    {
        Assert.That(AlphanumericEncoder.CalculateBitLength(characterCount), Is.EqualTo(expectedBitLength));
    }

    [Test]
    public void EncodesKnownPairsAndTerminalMostSignificantBitFirst()
    {
        BitBuffer buffer = new BitBuffer(0);

        AlphanumericEncoder.EncodeText("AC-42", buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(28));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0x39, 0xDC, 0xE4, 0x20 }));
        });
    }

    [Test]
    public void AppendsWithoutChangingExistingPrefixBits()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0b11U, 2);

        AlphanumericEncoder.EncodeText("A", buffer);

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetBitLength(), Is.EqualTo(8));
            Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0xCA }));
        });
    }

    [Test]
    public void RejectsCharactersOutsideRepertoireBeforeChangingDestination()
    {
        BitBuffer buffer = new BitBuffer(0);
        buffer.AppendBits(0xAU, 4);
        byte[] original = buffer.GetBytes();

        QrInputException lowercase = Assert.Throws<QrInputException>(() =>
            AlphanumericEncoder.EncodeText("ABCd", buffer))!;
        QrInputException underscore = Assert.Throws<QrInputException>(() =>
            AlphanumericEncoder.EncodeText("A_B", buffer))!;
        QrInputException nonAscii = Assert.Throws<QrInputException>(() =>
            AlphanumericEncoder.EncodeText("CAFÉ", buffer))!;

        Assert.Multiple(() =>
        {
            Assert.That(lowercase.InputName, Is.EqualTo("character"));
            Assert.That(underscore.InputName, Is.EqualTo("character"));
            Assert.That(nonAscii.InputName, Is.EqualTo("character"));
            Assert.That(buffer.GetBitLength(), Is.EqualTo(4));
            Assert.That(buffer.GetBytes(), Is.EqualTo(original));
        });
    }

    [Test]
    public void RejectsNullNegativeCountAndBitLengthOverflow()
    {
        QrInputException nullText = Assert.Throws<QrInputException>(() =>
            AlphanumericEncoder.ValidateText(null!))!;
        QrInputException negativeCount = Assert.Throws<QrInputException>(() =>
            AlphanumericEncoder.CalculateBitLength(-1))!;
        QrResourceLimitException overflow = Assert.Throws<QrResourceLimitException>(() =>
            AlphanumericEncoder.CalculateBitLength(int.MaxValue))!;

        Assert.Multiple(() =>
        {
            Assert.That(nullText.InputName, Is.EqualTo("text"));
            Assert.That(negativeCount.InputName, Is.EqualTo("characterCount"));
            Assert.That(overflow.LimitName, Is.EqualTo("AlphanumericBitLength"));
        });
    }
}
