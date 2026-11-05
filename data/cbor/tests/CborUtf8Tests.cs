using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborUtf8Tests
{
    [Test]
    public void ValidTextDecodesAcrossEveryByteBoundary()
    {
        byte[] bytes =
        [0x24, 0xc2, 0xa2, 0xe2, 0x82, 0xac, 0xf0, 0x90, 0x8d, 0x88];
        CborUtf8 decoder = new CborUtf8();

        for (int index = 0; index < bytes.Length; index++)
        {
            CborUtf8Result fragment = decoder.ProcessChunk([bytes[index]], false);
            Assert.Multiple(() =>
            {
                Assert.That(fragment.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
                Assert.That(fragment.Consumed, Is.EqualTo(1));
                Assert.That(fragment.TotalBytes, Is.EqualTo(index + 1));
                Assert.That(fragment.ErrorCode, Is.Null);
            });
        }

        CborUtf8Result completed = decoder.ProcessChunk([], true);
        Assert.Multiple(() =>
        {
            Assert.That(completed.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(completed.Text, Is.EqualTo("$¢€𐍈"));
            Assert.That(completed.TotalBytes, Is.EqualTo(bytes.Length));
            Assert.That(completed.ErrorOffset, Is.EqualTo(-1));
        });
    }

    [Test]
    public void BoundaryScalarValuesDecodeSuccessfully()
    {
        byte[] bytes =
        [
            0x7f,
            0xc2, 0x80,
            0xdf, 0xbf,
            0xe0, 0xa0, 0x80,
            0xed, 0x9f, 0xbf,
            0xee, 0x80, 0x80,
            0xef, 0xbf, 0xbf,
            0xf0, 0x90, 0x80, 0x80,
            0xf4, 0x8f, 0xbf, 0xbf
        ];
        CborUtf8 decoder = new CborUtf8();

        CborUtf8Result result = decoder.ProcessChunk(bytes, true);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(result.Text, Is.EqualTo("\u007f\u0080\u07ff\u0800\ud7ff\ue000\uffff\U00010000\U0010ffff"));
            Assert.That(result.Consumed, Is.EqualTo(bytes.Length));
            Assert.That(result.TotalBytes, Is.EqualTo(bytes.Length));
        });
    }

    [Test]
    public void NonShortestFormsAreRejected()
    {
        CborUtf8Result twoByte = new CborUtf8().ProcessChunk([0xc0, 0x80], true);
        CborUtf8Result threeByte = new CborUtf8().ProcessChunk([0xe0, 0x80, 0x80], true);
        CborUtf8Result fourByte = new CborUtf8().ProcessChunk([0xf0, 0x80, 0x80, 0x80], true);

        Assert.Multiple(() =>
        {
            Assert.That(twoByte.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(twoByte.ErrorCode, Is.EqualTo("CBOR_UTF8_NON_SHORTEST"));
            Assert.That(twoByte.ErrorOffset, Is.EqualTo(0));
            Assert.That(threeByte.ErrorCode, Is.EqualTo("CBOR_UTF8_NON_SHORTEST"));
            Assert.That(threeByte.ErrorOffset, Is.EqualTo(0));
            Assert.That(fourByte.ErrorCode, Is.EqualTo("CBOR_UTF8_NON_SHORTEST"));
            Assert.That(fourByte.ErrorOffset, Is.EqualTo(0));
        });
    }

    [Test]
    public void SurrogatesAndOutOfRangeScalarsAreRejected()
    {
        CborUtf8Result highSurrogate = new CborUtf8().ProcessChunk([0xed, 0xa0, 0x80], true);
        CborUtf8Result lowSurrogate = new CborUtf8().ProcessChunk([0xed, 0xbf, 0xbf], true);
        CborUtf8Result aboveMaximum = new CborUtf8().ProcessChunk([0xf4, 0x90, 0x80, 0x80], true);
        CborUtf8Result invalidLead = new CborUtf8().ProcessChunk([0xf5, 0x80, 0x80, 0x80], true);

        Assert.Multiple(() =>
        {
            Assert.That(highSurrogate.ErrorCode, Is.EqualTo("CBOR_UTF8_SURROGATE"));
            Assert.That(highSurrogate.ErrorOffset, Is.EqualTo(0));
            Assert.That(lowSurrogate.ErrorCode, Is.EqualTo("CBOR_UTF8_SURROGATE"));
            Assert.That(lowSurrogate.ErrorOffset, Is.EqualTo(0));
            Assert.That(aboveMaximum.ErrorCode, Is.EqualTo("CBOR_UTF8_OUT_OF_RANGE"));
            Assert.That(aboveMaximum.ErrorOffset, Is.EqualTo(0));
            Assert.That(invalidLead.ErrorCode, Is.EqualTo("CBOR_UTF8_OUT_OF_RANGE"));
            Assert.That(invalidLead.ErrorOffset, Is.EqualTo(0));
        });
    }

    [Test]
    public void FinalIncompleteSequencesAreRejectedAtTheirStartOffsets()
    {
        CborUtf8Result twoByte = new CborUtf8().ProcessChunk([0x61, 0xc2], true);
        CborUtf8Result threeByte = new CborUtf8().ProcessChunk([0x61, 0xe2, 0x82], true);
        CborUtf8Result fourByte = new CborUtf8().ProcessChunk([0x61, 0xf0, 0x90, 0x8d], true);

        Assert.Multiple(() =>
        {
            Assert.That(twoByte.ErrorCode, Is.EqualTo("CBOR_UTF8_INCOMPLETE_SEQUENCE"));
            Assert.That(twoByte.ErrorOffset, Is.EqualTo(1));
            Assert.That(twoByte.Text, Is.EqualTo("a"));
            Assert.That(threeByte.ErrorCode, Is.EqualTo("CBOR_UTF8_INCOMPLETE_SEQUENCE"));
            Assert.That(threeByte.ErrorOffset, Is.EqualTo(1));
            Assert.That(threeByte.Text, Is.EqualTo("a"));
            Assert.That(fourByte.ErrorCode, Is.EqualTo("CBOR_UTF8_INCOMPLETE_SEQUENCE"));
            Assert.That(fourByte.ErrorOffset, Is.EqualTo(1));
            Assert.That(fourByte.Text, Is.EqualTo("a"));
        });
    }

    [Test]
    public void InvalidContinuationFaultsUntilResetAndKeepsAbsoluteOffset()
    {
        CborUtf8 decoder = new CborUtf8();
        CborUtf8Result first = decoder.ProcessChunk([0x61, 0xe2], false);
        CborUtf8Result failure = decoder.ProcessChunk([0x28], false);
        CborUtf8Result repeated = decoder.ProcessChunk([0x82, 0xac], true);

        Assert.Multiple(() =>
        {
            Assert.That(first.Status, Is.EqualTo(CborReadStatus.NeedMoreData));
            Assert.That(first.Text, Is.EqualTo("a"));
            Assert.That(failure.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(failure.ErrorCode, Is.EqualTo("CBOR_UTF8_INVALID_CONTINUATION"));
            Assert.That(failure.ErrorOffset, Is.EqualTo(2));
            Assert.That(failure.Consumed, Is.EqualTo(1));
            Assert.That(failure.Text, Is.EqualTo("a"));
            Assert.That(repeated.Status, Is.EqualTo(CborReadStatus.Error));
            Assert.That(repeated.Consumed, Is.Zero);
            Assert.That(repeated.ErrorOffset, Is.EqualTo(2));
        });

        decoder.Reset();
        CborUtf8Result recovered = decoder.ProcessChunk([0xe2, 0x82, 0xac], true);
        Assert.Multiple(() =>
        {
            Assert.That(recovered.Status, Is.EqualTo(CborReadStatus.Done));
            Assert.That(recovered.Text, Is.EqualTo("€"));
            Assert.That(recovered.ErrorCode, Is.Null);
            Assert.That(recovered.ErrorOffset, Is.EqualTo(-1));
            Assert.That(recovered.TotalBytes, Is.EqualTo(3));
        });
    }
}
