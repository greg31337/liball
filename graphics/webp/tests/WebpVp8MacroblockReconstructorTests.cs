using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8MacroblockReconstructorTests
{
    [Test]
    public void ReconstructLumaMacroblock_WritesSixteenResidualBlocksAtMacroblockPosition()
    {
        byte[] destination = new byte[32 * 32];
        byte[] prediction = NewFilledBytes(16 * 16, 100);
        int[][] residuals = NewResidualBlocks(16);
        residuals[0] = NewFilledBlock(1);
        residuals[5] = NewFilledBlock(2);
        residuals[15] = NewFilledBlock(-3);

        WebpVp8MacroblockReconstructor.ReconstructLumaMacroblock(destination, 1, 1, 32, prediction, residuals);

        Assert.That(destination[(16 * 32) + 16], Is.EqualTo(101));
        Assert.That(destination[(20 * 32) + 20], Is.EqualTo(102));
        Assert.That(destination[(31 * 32) + 31], Is.EqualTo(97));
        Assert.That(destination[0], Is.EqualTo(0));
    }

    [Test]
    public void ReconstructChromaMacroblock_WritesFourResidualBlocksAtChromaPosition()
    {
        byte[] destination = new byte[16 * 16];
        byte[] prediction = NewFilledBytes(8 * 8, 120);
        int[][] residuals = NewResidualBlocks(4);
        residuals[0] = NewFilledBlock(-20);
        residuals[3] = NewFilledBlock(10);

        WebpVp8MacroblockReconstructor.ReconstructChromaMacroblock(destination, 1, 1, 16, prediction, residuals);

        Assert.That(destination[(8 * 16) + 8], Is.EqualTo(100));
        Assert.That(destination[(15 * 16) + 15], Is.EqualTo(130));
        Assert.That(destination[0], Is.EqualTo(0));
    }

    [Test]
    public void PadPlaneEdges_ReplicatesRightAndBottomEdges()
    {
        byte[] plane = new byte[6 * 6];
        int row = 0;
        while (row < 3)
        {
            int column = 0;
            while (column < 5)
            {
                plane[(row * 6) + column] = (byte)((row * 10) + column);
                column = column + 1;
            }

            row = row + 1;
        }

        WebpVp8MacroblockReconstructor.PadPlaneEdges(plane, 5, 3, 6, 6, 6);

        Assert.That(plane[5], Is.EqualTo(4));
        Assert.That(plane[11], Is.EqualTo(14));
        Assert.That(plane[17], Is.EqualTo(24));
        Assert.That(plane[18], Is.EqualTo(20));
        Assert.That(plane[23], Is.EqualTo(24));
        Assert.That(plane[35], Is.EqualTo(24));
    }

    [Test]
    public void MacroblockAlignedSizes_RoundToLumaAndChromaBlockBoundaries()
    {
        Assert.That(WebpVp8MacroblockReconstructor.MacroblockAlignedLumaSize(1), Is.EqualTo(16));
        Assert.That(WebpVp8MacroblockReconstructor.MacroblockAlignedLumaSize(16), Is.EqualTo(16));
        Assert.That(WebpVp8MacroblockReconstructor.MacroblockAlignedLumaSize(17), Is.EqualTo(32));
        Assert.That(WebpVp8MacroblockReconstructor.MacroblockAlignedChromaSize(1), Is.EqualTo(8));
        Assert.That(WebpVp8MacroblockReconstructor.MacroblockAlignedChromaSize(8), Is.EqualTo(8));
        Assert.That(WebpVp8MacroblockReconstructor.MacroblockAlignedChromaSize(9), Is.EqualTo(16));
    }

    [Test]
    public void Reconstructor_RejectsInvalidInputs()
    {
        byte[] destination = new byte[16 * 16];
        byte[] prediction = NewFilledBytes(16 * 16, 0);
        int[][] residuals = NewResidualBlocks(16);

        Assert.Throws<ArgumentNullException>(() => WebpVp8MacroblockReconstructor.ReconstructLumaMacroblock(null!, 0, 0, 16, prediction, residuals));
        Assert.Throws<ArgumentNullException>(() => WebpVp8MacroblockReconstructor.ReconstructLumaMacroblock(destination, 0, 0, 16, null!, residuals));
        Assert.Throws<ArgumentNullException>(() => WebpVp8MacroblockReconstructor.ReconstructLumaMacroblock(destination, 0, 0, 16, prediction, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8MacroblockReconstructor.ReconstructLumaMacroblock(destination, -1, 0, 16, prediction, residuals));
        Assert.Throws<ArgumentException>(() => WebpVp8MacroblockReconstructor.ReconstructLumaMacroblock(destination, 0, 0, 16, prediction, NewResidualBlocks(15)));
        Assert.Throws<ArgumentException>(() => WebpVp8MacroblockReconstructor.ReconstructLumaMacroblock(destination, 0, 0, 16, new byte[15], residuals));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8MacroblockReconstructor.PadPlaneEdges(destination, 0, 1, 16, 16, 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8MacroblockReconstructor.PadPlaneEdges(destination, 8, 8, 7, 8, 8));
        Assert.Throws<ArgumentException>(() => WebpVp8MacroblockReconstructor.PadPlaneEdges(new byte[15], 4, 4, 4, 4, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8MacroblockReconstructor.MacroblockAlignedLumaSize(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => WebpVp8MacroblockReconstructor.MacroblockAlignedChromaSize(0));
    }

    static byte[] NewFilledBytes(int length, byte value)
    {
        byte[] data = new byte[length];
        int index = 0;
        while (index < data.Length)
        {
            data[index] = value;
            index = index + 1;
        }

        return data;
    }

    static int[][] NewResidualBlocks(int count)
    {
        int[][] blocks = new int[count][];
        int index = 0;
        while (index < blocks.Length)
        {
            blocks[index] = new int[16];
            index = index + 1;
        }

        return blocks;
    }

    static int[] NewFilledBlock(int value)
    {
        int[] block = new int[16];
        int index = 0;
        while (index < block.Length)
        {
            block[index] = value;
            index = index + 1;
        }

        return block;
    }
}
