namespace avif.Tests;

public class Av1SyntaxTableTests
{
    [Test]
    public void SyntaxConstantsAndEnumsMatchNormativeValues()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Av1SyntaxConstants.RefsPerFrame, Is.EqualTo(7));
            Assert.That(Av1SyntaxConstants.BlockSizeCount, Is.EqualTo(22));
            Assert.That(Av1SyntaxConstants.TransformSizeCount, Is.EqualTo(19));
            Assert.That(Av1SyntaxConstants.TransformTypeCount, Is.EqualTo(16));
            Assert.That(Av1SyntaxConstants.MaxSuperblockSize, Is.EqualTo(128));
            Assert.That(Av1SyntaxConstants.MiSize, Is.EqualTo(4));
            Assert.That(Av1SyntaxConstants.MaxTileWidth, Is.EqualTo(4096));
            Assert.That(Av1SyntaxConstants.SegmentFeatureCount, Is.EqualTo(8));
            Assert.That(Av1SyntaxConstants.MotionVectorClassCount, Is.EqualTo(11));
            Assert.That(Av1SyntaxConstants.SuperresScaleMask, Is.EqualTo(16383));
            Assert.That(Av1SyntaxConstants.ReferenceMotionVectorLimit, Is.EqualTo(4095));
            Assert.That(Av1SyntaxConstants.MaxQuantizerIndex, Is.EqualTo(255));
            Assert.That(Av1SyntaxConstants.CdfProbabilityTop, Is.EqualTo(32768));
            Assert.That((int)Av1ObuType.SequenceHeader, Is.EqualTo(1));
            Assert.That((int)Av1ObuType.TileList, Is.EqualTo(8));
            Assert.That((int)Av1ObuType.Padding, Is.EqualTo(15));
            Assert.That((int)Av1FrameType.Switch, Is.EqualTo(3));
            Assert.That((int)Av1ReferenceFrame.Alternate, Is.EqualTo(7));
            Assert.That((int)Av1BlockSize.Block64x16, Is.EqualTo(21));
            Assert.That((int)Av1BlockSize.Invalid, Is.EqualTo(22));
            Assert.That((int)Av1PartitionType.Vertical4, Is.EqualTo(9));
            Assert.That((int)Av1TransformSize.Transform64x16, Is.EqualTo(18));
            Assert.That((int)Av1TransformType.HorizontalFlipAdst, Is.EqualTo(15));
            Assert.That((int)Av1SegmentFeature.GlobalMotion, Is.EqualTo(7));
            Assert.That((int)Av1MotionVectorJoint.VerticalNonzero, Is.EqualTo(2));
            Assert.That((int)Av1MotionVectorClass.Class10, Is.EqualTo(10));
            Assert.That((int)Av1MetadataType.ItuTT35, Is.EqualTo(4));
        });
    }

    [Test]
    public void EveryBlockGeometryAndLookupRoundTrips()
    {
        int[] widths = new int[]
        {
            4, 4, 8, 8, 8, 16, 16, 16, 32, 32, 32,
            64, 64, 64, 128, 128, 4, 16, 8, 32, 16, 64
        };
        int[] heights = new int[]
        {
            4, 8, 4, 8, 16, 8, 16, 32, 16, 32, 64,
            32, 64, 128, 64, 128, 16, 4, 32, 8, 64, 16
        };

        int index = 0;
        while (index < widths.Length)
        {
            Av1BlockSize size = (Av1BlockSize)index;
            Assert.Multiple(() =>
            {
                Assert.That(Av1ConversionTables.GetBlockWidth(size), Is.EqualTo(widths[index]));
                Assert.That(Av1ConversionTables.GetBlockHeight(size), Is.EqualTo(heights[index]));
                Assert.That(Av1ConversionTables.GetBlockMiWidth(size),
                    Is.EqualTo(widths[index] / 4));
                Assert.That(Av1ConversionTables.GetBlockMiHeight(size),
                    Is.EqualTo(heights[index] / 4));
                Assert.That(Av1ConversionTables.FindBlockSize(widths[index], heights[index]),
                    Is.EqualTo(size));
                Assert.That(Av1ConversionTables.GetSizeGroup(size), Is.InRange(0, 3));
            });
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(Av1ConversionTables.FindBlockSize(12, 12),
                Is.EqualTo(Av1BlockSize.Invalid));
            Assert.That(Av1ConversionTables.GetBlockWidth(Av1BlockSize.Invalid), Is.Zero);
            Assert.That(Av1ConversionTables.GetMaxTransformDepth(
                Av1BlockSize.Block128x128), Is.EqualTo(4));
            Assert.That(Av1ConversionTables.GetMaxRectangularTransformSize(
                Av1BlockSize.Block16x64),
                Is.EqualTo(Av1TransformSize.Transform16x64));
            Assert.That(Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Horizontal4,
                Av1BlockSize.Block64x64),
                Is.EqualTo(Av1BlockSize.Block64x16));
            Assert.That(Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Vertical,
                Av1BlockSize.Block4x4),
                Is.EqualTo(Av1BlockSize.Invalid));
        });
    }

    [Test]
    public void TransformDimensionsSplitsComponentsAndClassesAreComplete()
    {
        int[] widths = new int[]
        {
            4, 8, 16, 32, 64, 4, 8, 8, 16, 16,
            32, 32, 64, 4, 16, 8, 32, 16, 64
        };
        int[] heights = new int[]
        {
            4, 8, 16, 32, 64, 8, 4, 16, 8, 32,
            16, 64, 32, 16, 4, 32, 8, 64, 16
        };

        int index = 0;
        while (index < widths.Length)
        {
            Av1TransformSize size = (Av1TransformSize)index;
            Assert.That(Av1ConversionTables.GetTransformWidth(size), Is.EqualTo(widths[index]));
            Assert.That(Av1ConversionTables.GetTransformHeight(size), Is.EqualTo(heights[index]));
            Assert.That(Av1ConversionTables.GetSplitTransformSize(size),
                Is.Not.EqualTo(Av1TransformSize.Invalid));
            index++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(Av1ConversionTables.GetAdjustedTransformSize(
                Av1TransformSize.Transform64x64),
                Is.EqualTo(Av1TransformSize.Transform32x32));
            Assert.That(Av1ConversionTables.GetAdjustedTransformSize(
                Av1TransformSize.Transform16x64),
                Is.EqualTo(Av1TransformSize.Transform16x32));
            Assert.That(Av1ConversionTables.GetTransformCoefficientCount(
                Av1TransformSize.Transform64x64), Is.EqualTo(1024));
            Assert.That(Av1ConversionTables.GetVerticalTransformType(
                Av1TransformType.FlipAdstDct),
                Is.EqualTo(Av1Transform1DType.FlipAdst));
            Assert.That(Av1ConversionTables.GetHorizontalTransformType(
                Av1TransformType.FlipAdstDct),
                Is.EqualTo(Av1Transform1DType.Dct));
            Assert.That(Av1ConversionTables.GetTransformClass(
                Av1TransformType.Identity),
                Is.EqualTo(Av1TransformClass.TwoDimensional));
            Assert.That(Av1ConversionTables.GetTransformClass(
                Av1TransformType.VerticalAdst),
                Is.EqualTo(Av1TransformClass.Vertical));
            Assert.That(Av1ConversionTables.GetTransformClass(
                Av1TransformType.HorizontalDct),
                Is.EqualTo(Av1TransformClass.Horizontal));
        });
    }

    [Test]
    public void ScanTablesMatchKnownVectorsAndEveryScanIsAPermutation()
    {
        Assert.That(
            Av1ScanTables.GetScan(
                Av1TransformSize.Transform4x4,
                Av1TransformType.DctDct),
            Is.EqualTo(new int[]
            {
                0, 4, 1, 2, 5, 8, 12, 9, 6, 3, 7, 10, 13, 14, 11, 15
            }));
        Assert.That(
            Av1ScanTables.GetScan(
                Av1TransformSize.Transform4x8,
                Av1TransformType.DctDct),
            Is.EqualTo(new int[]
            {
                0, 8, 1, 16, 9, 2, 24, 17, 10, 3, 25, 18, 11, 4, 26, 19,
                12, 5, 27, 20, 13, 6, 28, 21, 14, 7, 29, 22, 15, 30, 23, 31
            }));

        int sizeIndex = 0;
        while (sizeIndex < Av1SyntaxConstants.TransformSizeCount)
        {
            int typeIndex = 0;
            while (typeIndex < Av1SyntaxConstants.TransformTypeCount)
            {
                Av1TransformSize size = (Av1TransformSize)sizeIndex;
                Av1TransformType type = (Av1TransformType)typeIndex;
                int[] scan = Av1ScanTables.GetScan(size, type);
                int[] inverse = Av1ScanTables.GetInverseScan(size, type);
                bool[] found = new bool[scan.Length];
                int index = 0;
                while (index < scan.Length)
                {
                    Assert.That(scan[index], Is.InRange(0, scan.Length - 1));
                    Assert.That(found[scan[index]], Is.False);
                    Assert.That(inverse[scan[index]], Is.EqualTo(index));
                    found[scan[index]] = true;
                    index++;
                }

                typeIndex++;
            }

            sizeIndex++;
        }

        int[] first = Av1ScanTables.GetScan(
            Av1TransformSize.Transform4x4,
            Av1TransformType.DctDct);
        first[0] = 99;
        Assert.That(Av1ScanTables.GetScan(
            Av1TransformSize.Transform4x4,
            Av1TransformType.DctDct)[0], Is.Zero);
    }

    [Test]
    public void EveryQuantizerMatrixLevelPlaneAndSizeIsAvailable()
    {
        byte[] levelZeroLuma = Av1QuantizerMatrices.GetMatrix(
            0,
            0,
            Av1TransformSize.Transform4x4);
        byte[] levelZeroChroma = Av1QuantizerMatrices.GetMatrix(
            0,
            1,
            Av1TransformSize.Transform4x4);

        Assert.Multiple(() =>
        {
            Assert.That(levelZeroLuma, Is.EqualTo(new byte[]
            {
                32, 43, 73, 97, 43, 67, 94, 110,
                73, 94, 137, 150, 97, 110, 150, 200
            }));
            Assert.That(levelZeroChroma, Is.EqualTo(new byte[]
            {
                35, 46, 57, 66, 46, 60, 69, 71,
                57, 69, 90, 90, 66, 71, 90, 109
            }));
            Assert.That(Av1QuantizerMatrices.GetMatrixOffset(
                Av1TransformSize.Transform16x64), Is.EqualTo(1680));
            Assert.That(Av1QuantizerMatrices.GetWeight(
                0, 0, Av1TransformSize.Transform4x4, 3, 3), Is.EqualTo(200));
        });

        int level = 0;
        while (level < Av1SyntaxConstants.NumQuantizerMatrixLevels)
        {
            int plane = 0;
            while (plane < 2)
            {
                int sizeIndex = 0;
                while (sizeIndex < Av1SyntaxConstants.TransformSizeCount)
                {
                    Av1TransformSize size = (Av1TransformSize)sizeIndex;
                    byte[] matrix = Av1QuantizerMatrices.GetMatrix(level, plane, size);
                    Assert.That(matrix.Length, Is.EqualTo(
                        Av1ConversionTables.GetTransformCoefficientCount(size)));
                    Assert.That(matrix.All(weight => weight > 0), Is.True);
                    if (level == 15)
                    {
                        Assert.That(matrix.All(weight => weight == 32), Is.True);
                    }

                    sizeIndex++;
                }

                plane++;
            }

            level++;
        }

        levelZeroLuma[0] = 0;
        Assert.That(Av1QuantizerMatrices.GetMatrix(
            0, 0, Av1TransformSize.Transform4x4)[0], Is.EqualTo(32));
    }

    [Test]
    public void EveryNormativeDefaultCdfIsPresentAndDefensivelyReturned()
    {
        Assert.That(Av1DefaultCdfTables.GetTableCount(), Is.EqualTo(96));
        int totalValueCount = 0;
        HashSet<string> names = new HashSet<string>();
        int tableIndex = 0;
        while (tableIndex < Av1DefaultCdfTables.GetTableCount())
        {
            string name = Av1DefaultCdfTables.GetTableName(tableIndex);
            ushort[] values = Av1DefaultCdfTables.GetValues(name);
            Assert.Multiple(() =>
            {
                Assert.That(name, Does.StartWith("Default_"));
                Assert.That(names.Add(name), Is.True);
                Assert.That(Av1DefaultCdfTables.Contains(name), Is.True);
                Assert.That(Av1DefaultCdfTables.GetShape(name), Does.StartWith("["));
                Assert.That(values, Is.Not.Empty);
                Assert.That(values[^1], Is.Zero);
                Assert.That(values.All(value => value <= 32768), Is.True);
            });
            totalValueCount += values.Length;
            tableIndex++;
        }

        ushort[] motionVectorJoint = Av1DefaultCdfTables.GetValues(
            "Default_Mv_Joint_Cdf");
        Assert.Multiple(() =>
        {
            Assert.That(totalValueCount, Is.EqualTo(19677));
            Assert.That(motionVectorJoint,
                Is.EqualTo(new ushort[] { 4096, 11264, 19328, 32768, 0 }));
            Assert.That(Av1DefaultCdfTables.GetValueCount("Default_Txb_Skip_Cdf"),
                Is.EqualTo(780));
            Assert.That(Av1DefaultCdfTables.GetValueCount("Default_Coeff_Br_Cdf"),
                Is.EqualTo(4200));
            Assert.That(Av1DefaultCdfTables.GetValues("Default_Mv_Class0_Hp_Cdf"),
                Is.EqualTo(new ushort[] { 20480, 32768, 0 }));
            Assert.That(Av1DefaultCdfTables.GetValues("Default_Mv_Bit_Cdf")[0],
                Is.EqualTo(17408));
        });

        motionVectorJoint[0] = 0;
        Assert.That(Av1DefaultCdfTables.GetValues("Default_Mv_Joint_Cdf")[0],
            Is.EqualTo(4096));
    }
}
