using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MatrixTransformOperationTests
{
    [Test]
    public void RotationMapsEveryModuleAndOptionalMetadataForAllQuarterTurns()
    {
        QrMatrix source = CreateSmallMatrix();
        int[][] expectedSourceIndexes = new int[][]
        {
            new int[] { 0, 1, 2, 3, 4, 5 },
            new int[] { 3, 0, 4, 1, 5, 2 },
            new int[] { 5, 4, 3, 2, 1, 0 },
            new int[] { 2, 5, 1, 4, 0, 3 }
        };
        int[] expectedWidths = new int[] { 3, 2, 3, 2 };
        int[] expectedHeights = new int[] { 2, 3, 2, 3 };

        for (int turns = 0; turns < 4; turns++)
        {
            QrMatrix result = MatrixTransformer.Rotate(source, turns);

            Assert.Multiple(() =>
            {
                Assert.That(result.Width, Is.EqualTo(expectedWidths[turns]));
                Assert.That(result.Height, Is.EqualTo(expectedHeights[turns]));
                AssertMappedModules(source, result, expectedSourceIndexes[turns]);
            });
        }
    }

    [Test]
    public void RotationMovesAsymmetricQuietZoneExtentsWithTheEdges()
    {
        QrMatrix source = CreateAsymmetricMatrix();
        int[][] expectedExtents = new int[][]
        {
            new int[] { 1, 2, 3, 1 },
            new int[] { 1, 1, 2, 3 },
            new int[] { 3, 1, 1, 2 },
            new int[] { 2, 3, 1, 1 }
        };

        for (int turns = 0; turns < 4; turns++)
        {
            QrMatrix result = MatrixTransformer.Rotate(source, turns);

            Assert.Multiple(() =>
            {
                Assert.That(result.QuietZoneLeft, Is.EqualTo(expectedExtents[turns][0]));
                Assert.That(result.QuietZoneTop, Is.EqualTo(expectedExtents[turns][1]));
                Assert.That(result.QuietZoneRight, Is.EqualTo(expectedExtents[turns][2]));
                Assert.That(result.QuietZoneBottom, Is.EqualTo(expectedExtents[turns][3]));
                Assert.That(
                    MatrixTransformer.GetEncodingRegionWidth(result),
                    Is.EqualTo(turns % 2 == 0 ? 4 : 3));
                Assert.That(
                    MatrixTransformer.GetEncodingRegionHeight(result),
                    Is.EqualTo(turns % 2 == 0 ? 3 : 4));
            });
        }

        QrMatrix cycled = source;
        for (int count = 0; count < 4; count++)
        {
            cycled = MatrixTransformer.Rotate(cycled, 1);
        }

        AssertMatrixEquals(source, cycled);
    }

    [Test]
    public void MirrorTransposesModulesDimensionsAndQuietZoneExtents()
    {
        QrMatrix small = CreateSmallMatrix();
        QrMatrix source = CreateAsymmetricMatrix();

        QrMatrix smallResult = MatrixTransformer.Mirror(small);
        QrMatrix result = MatrixTransformer.Mirror(source);

        Assert.Multiple(() =>
        {
            Assert.That(smallResult.Width, Is.EqualTo(2));
            Assert.That(smallResult.Height, Is.EqualTo(3));
            AssertMappedModules(small, smallResult, new int[] { 0, 3, 1, 4, 2, 5 });
            Assert.That(result.Width, Is.EqualTo(source.Height));
            Assert.That(result.Height, Is.EqualTo(source.Width));
            Assert.That(result.QuietZoneLeft, Is.EqualTo(2));
            Assert.That(result.QuietZoneTop, Is.EqualTo(1));
            Assert.That(result.QuietZoneRight, Is.EqualTo(1));
            Assert.That(result.QuietZoneBottom, Is.EqualTo(3));
            Assert.That(MatrixTransformer.GetEncodingRegionWidth(result), Is.EqualTo(3));
            Assert.That(MatrixTransformer.GetEncodingRegionHeight(result), Is.EqualTo(4));
        });

        AssertMatrixEquals(source, MatrixTransformer.Mirror(result));
    }

    [Test]
    public void PolarityInversionSwapsLightAndDarkAndPreservesUnknownMetadata()
    {
        QrMatrix source = CreateSmallMatrix();
        QrModuleValue[] original = (QrModuleValue[])source.Values.Clone();

        QrMatrix result = MatrixTransformer.InvertPolarity(source);

        Assert.Multiple(() =>
        {
            Assert.That(result.Values, Is.EqualTo(new QrModuleValue[]
            {
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Unknown,
                QrModuleValue.Dark,
                QrModuleValue.Unknown,
                QrModuleValue.Light
            }));
            Assert.That(result.Roles, Is.EqualTo(source.Roles));
            Assert.That(result.Confidence, Is.EqualTo(source.Confidence));
            Assert.That(source.Values, Is.EqualTo(original));
            Assert.That(MatrixTransformer.InvertPolarity(result).Values, Is.EqualTo(source.Values));
        });
    }

    [Test]
    public void CropCopiesTheRequestedRectangleAndRetainsRemainingQuietZoneExtents()
    {
        QrMatrix source = CreateAsymmetricMatrix();

        QrMatrix result = MatrixTransformer.Crop(source, 0, 1, 6, 5);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(6));
            Assert.That(result.Height, Is.EqualTo(5));
            Assert.That(result.QuietZoneLeft, Is.EqualTo(1));
            Assert.That(result.QuietZoneTop, Is.EqualTo(1));
            Assert.That(result.QuietZoneRight, Is.EqualTo(1));
            Assert.That(result.QuietZoneBottom, Is.EqualTo(1));
            Assert.That(MatrixTransformer.GetEncodingRegionWidth(result), Is.EqualTo(4));
            Assert.That(MatrixTransformer.GetEncodingRegionHeight(result), Is.EqualTo(3));
        });
        AssertCroppedModules(source, result, 0, 1);
    }

    [Test]
    public void CropCanRemovePartOfTheEncodingRegionWithoutMislabelingQuietZone()
    {
        QrMatrix source = CreateAsymmetricMatrix();

        QrMatrix result = MatrixTransformer.Crop(source, 2, 3, 5, 3);

        Assert.Multiple(() =>
        {
            Assert.That(result.QuietZoneLeft, Is.Zero);
            Assert.That(result.QuietZoneTop, Is.Zero);
            Assert.That(result.QuietZoneRight, Is.EqualTo(2));
            Assert.That(result.QuietZoneBottom, Is.EqualTo(1));
            Assert.That(MatrixTransformer.GetEncodingRegionWidth(result), Is.EqualTo(3));
            Assert.That(MatrixTransformer.GetEncodingRegionHeight(result), Is.EqualTo(2));
        });
        AssertCroppedModules(source, result, 2, 3);
    }

    [Test]
    public void IntegerScaleReplicatesEveryModuleAndScalesQuietZoneExtents()
    {
        QrMatrix source = CreateAsymmetricMatrix();

        QrMatrix result = MatrixTransformer.Scale(source, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(16));
            Assert.That(result.Height, Is.EqualTo(12));
            Assert.That(result.QuietZoneLeft, Is.EqualTo(2));
            Assert.That(result.QuietZoneTop, Is.EqualTo(4));
            Assert.That(result.QuietZoneRight, Is.EqualTo(6));
            Assert.That(result.QuietZoneBottom, Is.EqualTo(2));
            Assert.That(MatrixTransformer.GetEncodingRegionWidth(result), Is.EqualTo(8));
            Assert.That(MatrixTransformer.GetEncodingRegionHeight(result), Is.EqualTo(6));
        });

        for (int row = 0; row < result.Height; row++)
        {
            for (int column = 0; column < result.Width; column++)
            {
                int sourceIndex = row / 2 * source.Width + column / 2;
                int resultIndex = row * result.Width + column;
                Assert.Multiple(() =>
                {
                    Assert.That(result.Values[resultIndex], Is.EqualTo(source.Values[sourceIndex]));
                    Assert.That(result.Roles![resultIndex], Is.EqualTo(source.Roles![sourceIndex]));
                    Assert.That(result.Confidence![resultIndex], Is.EqualTo(source.Confidence![sourceIndex]));
                });
            }
        }

        QrMatrix copy = MatrixTransformer.Scale(source, 1);
        copy.Values[0] = QrModuleValue.Unknown;
        Assert.That(source.Values[0], Is.Not.EqualTo(QrModuleValue.Unknown));
    }

    [Test]
    public void TransformsKeepOptionalMetadataAbsent()
    {
        QrMatrix source = new QrMatrix
        {
            Width = 3,
            Height = 2,
            Values = new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Unknown,
                QrModuleValue.Light,
                QrModuleValue.Dark,
                QrModuleValue.Light
            }
        };
        QrMatrix[] results = new QrMatrix[]
        {
            MatrixTransformer.Rotate(source, 1),
            MatrixTransformer.Mirror(source),
            MatrixTransformer.InvertPolarity(source),
            MatrixTransformer.Crop(source, 0, 0, 2, 2),
            MatrixTransformer.Scale(source, 2)
        };

        for (int index = 0; index < results.Length; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(results[index].Roles, Is.Null);
                Assert.That(results[index].Confidence, Is.Null);
            });
        }
    }

    [Test]
    public void InvalidTransformArgumentsAreRejectedBeforeBufferAccess()
    {
        QrMatrix source = CreateAsymmetricMatrix();

        Assert.Multiple(() =>
        {
            Assert.That(() => MatrixTransformer.Rotate(null!, 0), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Rotate(source, -1), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Rotate(source, 4), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Crop(source, -1, 0, 1, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Crop(source, 0, -1, 1, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Crop(source, 0, 0, 0, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Crop(source, 0, 0, 1, 0), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Crop(source, 7, 0, 2, 1), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Crop(source, 5, 0, 3, 6), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Crop(source, 0, 0, 1, 2), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Scale(source, 0), Throws.TypeOf<QrInputException>());
            Assert.That(() => MatrixTransformer.Scale(source, -1), Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.Scale(source, int.MaxValue),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static QrMatrix CreateSmallMatrix()
    {
        return new QrMatrix
        {
            Width = 3,
            Height = 2,
            Values = new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Unknown,
                QrModuleValue.Light,
                QrModuleValue.Unknown,
                QrModuleValue.Dark
            },
            Roles = new QrModuleRole[]
            {
                QrModuleRole.Finder,
                QrModuleRole.Separator,
                QrModuleRole.Timing,
                QrModuleRole.Data,
                QrModuleRole.ErrorCorrection,
                QrModuleRole.Remainder
            },
            Confidence = new byte[] { 1, 2, 3, 4, 5, 6 }
        };
    }

    static QrMatrix CreateAsymmetricMatrix()
    {
        int width = 8;
        int height = 6;
        QrMatrix result = new QrMatrix
        {
            Width = width,
            Height = height,
            Values = new QrModuleValue[width * height],
            Roles = new QrModuleRole[width * height],
            Confidence = new byte[width * height],
            QuietZoneLeft = 1,
            QuietZoneTop = 2,
            QuietZoneRight = 3,
            QuietZoneBottom = 1
        };

        for (int index = 0; index < result.Values.Length; index++)
        {
            result.Values[index] = index % 3 == 0
                ? QrModuleValue.Dark
                : QrModuleValue.Light;
            result.Roles[index] = (QrModuleRole)(index % 12);
            result.Confidence[index] = (byte)(index + 1);
        }

        return result;
    }

    static void AssertMappedModules(
        QrMatrix source,
        QrMatrix result,
        int[] expectedSourceIndexes)
    {
        Assert.That(result.Values.Length, Is.EqualTo(expectedSourceIndexes.Length));
        for (int index = 0; index < expectedSourceIndexes.Length; index++)
        {
            int sourceIndex = expectedSourceIndexes[index];
            Assert.Multiple(() =>
            {
                Assert.That(result.Values[index], Is.EqualTo(source.Values[sourceIndex]));
                Assert.That(result.Roles![index], Is.EqualTo(source.Roles![sourceIndex]));
                Assert.That(result.Confidence![index], Is.EqualTo(source.Confidence![sourceIndex]));
            });
        }
    }

    static void AssertCroppedModules(
        QrMatrix source,
        QrMatrix result,
        int left,
        int top)
    {
        for (int row = 0; row < result.Height; row++)
        {
            for (int column = 0; column < result.Width; column++)
            {
                int sourceIndex = (row + top) * source.Width + column + left;
                int resultIndex = row * result.Width + column;
                Assert.Multiple(() =>
                {
                    Assert.That(result.Values[resultIndex], Is.EqualTo(source.Values[sourceIndex]));
                    Assert.That(result.Roles![resultIndex], Is.EqualTo(source.Roles![sourceIndex]));
                    Assert.That(result.Confidence![resultIndex], Is.EqualTo(source.Confidence![sourceIndex]));
                });
            }
        }
    }

    static void AssertMatrixEquals(QrMatrix expected, QrMatrix actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Width, Is.EqualTo(expected.Width));
            Assert.That(actual.Height, Is.EqualTo(expected.Height));
            Assert.That(actual.Values, Is.EqualTo(expected.Values));
            Assert.That(actual.Roles, Is.EqualTo(expected.Roles));
            Assert.That(actual.Confidence, Is.EqualTo(expected.Confidence));
            Assert.That(actual.QuietZoneLeft, Is.EqualTo(expected.QuietZoneLeft));
            Assert.That(actual.QuietZoneTop, Is.EqualTo(expected.QuietZoneTop));
            Assert.That(actual.QuietZoneRight, Is.EqualTo(expected.QuietZoneRight));
            Assert.That(actual.QuietZoneBottom, Is.EqualTo(expected.QuietZoneBottom));
        });
    }
}
