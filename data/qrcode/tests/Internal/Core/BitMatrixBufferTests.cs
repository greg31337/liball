using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class BitMatrixBufferTests
{
    [Test]
    public void StoresModulesInPackedRowMajorMostSignificantBitFirstOrder()
    {
        BitMatrixBuffer matrix = new BitMatrixBuffer(3, 3);
        matrix.SetModule(0, 0, true);
        matrix.SetModule(1, 1, true);
        matrix.SetModule(2, 2, true);
        byte[] packed = new byte[2];

        matrix.CopyPackedBytes(packed);

        Assert.Multiple(() =>
        {
            Assert.That(matrix.GetWidth(), Is.EqualTo(3));
            Assert.That(matrix.GetHeight(), Is.EqualTo(3));
            Assert.That(matrix.GetPackedByteLength(), Is.EqualTo(2));
            Assert.That(matrix.GetModule(0, 0), Is.True);
            Assert.That(matrix.GetModule(0, 1), Is.False);
            Assert.That(matrix.GetModule(1, 1), Is.True);
            Assert.That(matrix.GetModule(2, 2), Is.True);
            Assert.That(packed, Is.EqualTo(new byte[] { 0x88, 0x80 }));
        });
    }

    [Test]
    public void ToggleAndClearChangeOnlyLogicalModules()
    {
        BitMatrixBuffer matrix = new BitMatrixBuffer(3, 3);
        matrix.ClearModules(true);
        byte[] allSet = new byte[2];
        matrix.CopyPackedBytes(allSet);

        matrix.ToggleModule(1, 1);
        Assert.That(matrix.GetModule(1, 1), Is.False);
        matrix.ToggleModule(1, 1);
        Assert.That(matrix.GetModule(1, 1), Is.True);

        matrix.ClearModules(false);
        byte[] allClear = new byte[2];
        matrix.CopyPackedBytes(allClear);

        Assert.Multiple(() =>
        {
            Assert.That(allSet, Is.EqualTo(new byte[] { 0xFF, 0x80 }));
            Assert.That(allClear, Is.EqualTo(new byte[] { 0, 0 }));
        });
    }

    [Test]
    public void PackedLoadMasksUnusedTrailingBits()
    {
        BitMatrixBuffer matrix = new BitMatrixBuffer(3, 3);
        byte[] packed = new byte[2];

        matrix.LoadPackedBytes(new byte[] { 0xFF, 0xFF });
        matrix.CopyPackedBytes(packed);

        Assert.Multiple(() =>
        {
            Assert.That(packed, Is.EqualTo(new byte[] { 0xFF, 0x80 }));
            Assert.That(matrix.GetModule(2, 2), Is.True);
        });
    }

    [Test]
    public void RejectsInvalidCoordinatesAndShortPackedBuffers()
    {
        BitMatrixBuffer matrix = new BitMatrixBuffer(3, 3);

        QrInputException row = Assert.Throws<QrInputException>(() => matrix.GetModule(3, 0))!;
        QrInputException column = Assert.Throws<QrInputException>(() => matrix.SetModule(0, 3, true))!;
        QrInputException source = Assert.Throws<QrInputException>(() => matrix.LoadPackedBytes(new byte[1]))!;
        QrInputException destination = Assert.Throws<QrInputException>(() => matrix.CopyPackedBytes(new byte[1]))!;

        Assert.Multiple(() =>
        {
            Assert.That(row.InputName, Is.EqualTo("row"));
            Assert.That(column.InputName, Is.EqualTo("column"));
            Assert.That(source.InputName, Is.EqualTo("source"));
            Assert.That(destination.InputName, Is.EqualTo("destination"));
        });
    }
}
