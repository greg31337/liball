using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class MatrixTransformerTests
{
    [TestCase(QrSymbolKind.Model2, 4)]
    [TestCase(QrSymbolKind.Micro, 2)]
    [TestCase(QrSymbolKind.rMQR, 2)]
    [TestCase(QrSymbolKind.LegacyModel1, 4)]
    public void StandardQuietZoneWidthMatchesTheSymbolProfile(
        QrSymbolKind symbolKind,
        int expectedWidth)
    {
        Assert.That(
            MatrixTransformer.GetStandardQuietZoneWidth(symbolKind),
            Is.EqualTo(expectedWidth));
    }

    [TestCase(QrSymbolKind.Model2, 4)]
    [TestCase(QrSymbolKind.Micro, 2)]
    [TestCase(QrSymbolKind.rMQR, 2)]
    [TestCase(QrSymbolKind.LegacyModel1, 4)]
    public void ProfileDefaultAddsLightModulesAndPreservesEncodingMetadata(
        QrSymbolKind symbolKind,
        int expectedQuietZone)
    {
        QrMatrix source = CreateEncodingMatrix();

        QrMatrix result = MatrixTransformer.AddQuietZone(source, symbolKind, -1);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(3 + expectedQuietZone * 2));
            Assert.That(result.Height, Is.EqualTo(2 + expectedQuietZone * 2));
            Assert.That(result.QuietZoneLeft, Is.EqualTo(expectedQuietZone));
            Assert.That(result.QuietZoneTop, Is.EqualTo(expectedQuietZone));
            Assert.That(result.QuietZoneRight, Is.EqualTo(expectedQuietZone));
            Assert.That(result.QuietZoneBottom, Is.EqualTo(expectedQuietZone));
            Assert.That(MatrixTransformer.GetEncodingRegionWidth(result), Is.EqualTo(3));
            Assert.That(MatrixTransformer.GetEncodingRegionHeight(result), Is.EqualTo(2));
        });
        AssertQuietZone(result, expectedQuietZone);
        AssertEncodingRegion(result, expectedQuietZone, expectedQuietZone);
    }

    [Test]
    public void ExplicitWidthReplacesExistingAsymmetricQuietZoneAroundTheEncodingRegion()
    {
        QrMatrix source = CreateAsymmetricallyPaddedMatrix();

        QrMatrix result = MatrixTransformer.AddQuietZone(source, QrSymbolKind.Model2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(7));
            Assert.That(result.Height, Is.EqualTo(6));
            Assert.That(result.QuietZoneLeft, Is.EqualTo(2));
            Assert.That(result.QuietZoneTop, Is.EqualTo(2));
            Assert.That(result.QuietZoneRight, Is.EqualTo(2));
            Assert.That(result.QuietZoneBottom, Is.EqualTo(2));
            Assert.That(MatrixTransformer.GetEncodingRegionWidth(result), Is.EqualTo(3));
            Assert.That(MatrixTransformer.GetEncodingRegionHeight(result), Is.EqualTo(2));
        });
        AssertQuietZone(result, 2);
        AssertEncodingRegion(result, 2, 2);
    }

    [Test]
    public void RemovingQuietZoneRestoresTheExactEncodingRegion()
    {
        QrMatrix source = CreateEncodingMatrix();
        QrMatrix padded = MatrixTransformer.AddQuietZone(source, QrSymbolKind.Model2, 3);

        QrMatrix result = MatrixTransformer.RemoveQuietZone(padded);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(source.Width));
            Assert.That(result.Height, Is.EqualTo(source.Height));
            Assert.That(result.Values, Is.EqualTo(source.Values));
            Assert.That(result.Roles, Is.EqualTo(source.Roles));
            Assert.That(result.Confidence, Is.EqualTo(source.Confidence));
            Assert.That(result.QuietZoneLeft, Is.Zero);
            Assert.That(result.QuietZoneTop, Is.Zero);
            Assert.That(result.QuietZoneRight, Is.Zero);
            Assert.That(result.QuietZoneBottom, Is.Zero);
        });
    }

    [Test]
    public void OptionalMetadataRemainsAbsentAndInputsAreNotAliased()
    {
        QrMatrix source = new QrMatrix
        {
            Width = 2,
            Height = 2,
            Values = new QrModuleValue[]
            {
                QrModuleValue.Dark,
                QrModuleValue.Light,
                QrModuleValue.Unknown,
                QrModuleValue.Dark
            }
        };

        QrMatrix padded = MatrixTransformer.AddQuietZone(source, QrSymbolKind.Micro, 1);
        QrMatrix unpadded = MatrixTransformer.RemoveQuietZone(padded);
        padded.Values[5] = QrModuleValue.Unknown;
        unpadded.Values[0] = QrModuleValue.Light;

        Assert.Multiple(() =>
        {
            Assert.That(padded.Roles, Is.Null);
            Assert.That(padded.Confidence, Is.Null);
            Assert.That(unpadded.Roles, Is.Null);
            Assert.That(unpadded.Confidence, Is.Null);
            Assert.That(source.Values[0], Is.EqualTo(QrModuleValue.Dark));
            Assert.That(source.Values[1], Is.EqualTo(QrModuleValue.Light));
        });
    }

    [Test]
    public void ZeroWidthProducesAnUnpaddedCopy()
    {
        QrMatrix source = CreateAsymmetricallyPaddedMatrix();

        QrMatrix result = MatrixTransformer.AddQuietZone(source, QrSymbolKind.rMQR, 0);

        Assert.Multiple(() =>
        {
            Assert.That(result.Width, Is.EqualTo(3));
            Assert.That(result.Height, Is.EqualTo(2));
            Assert.That(result.Values, Is.EqualTo(CreateEncodingMatrix().Values));
            Assert.That(result.QuietZoneLeft, Is.Zero);
            Assert.That(result.QuietZoneTop, Is.Zero);
            Assert.That(result.QuietZoneRight, Is.Zero);
            Assert.That(result.QuietZoneBottom, Is.Zero);
        });
    }

    [Test]
    public void InvalidProfileWidthAndMatrixLayoutsAreRejected()
    {
        QrMatrix valid = CreateEncodingMatrix();
        QrMatrix shortValues = CreateEncodingMatrix();
        shortValues.Values = new QrModuleValue[5];
        QrMatrix shortRoles = CreateEncodingMatrix();
        shortRoles.Roles = new QrModuleRole[5];
        QrMatrix shortConfidence = CreateEncodingMatrix();
        shortConfidence.Confidence = new byte[5];
        QrMatrix negativeExtent = CreateEncodingMatrix();
        negativeExtent.QuietZoneLeft = -1;
        QrMatrix emptyEncodingRegion = CreateEncodingMatrix();
        emptyEncodingRegion.QuietZoneLeft = 1;
        emptyEncodingRegion.QuietZoneRight = 2;
        QrMatrix overflowingExtents = CreateEncodingMatrix();
        overflowingExtents.QuietZoneLeft = int.MaxValue;
        overflowingExtents.QuietZoneRight = int.MaxValue;

        Assert.Multiple(() =>
        {
            Assert.That(
                () => MatrixTransformer.AddQuietZone(null!, QrSymbolKind.Model2, -1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.AddQuietZone(valid, (QrSymbolKind)99, -1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.AddQuietZone(valid, (QrSymbolKind)99, 1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.AddQuietZone(valid, QrSymbolKind.Model2, -2),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.AddQuietZone(shortValues, QrSymbolKind.Model2, 1),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.RemoveQuietZone(shortRoles),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.RemoveQuietZone(shortConfidence),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.RemoveQuietZone(negativeExtent),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.RemoveQuietZone(emptyEncodingRegion),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.RemoveQuietZone(overflowingExtents),
                Throws.TypeOf<QrInputException>());
            Assert.That(
                () => MatrixTransformer.AddQuietZone(valid, QrSymbolKind.Model2, int.MaxValue),
                Throws.TypeOf<QrResourceLimitException>());
        });
    }

    static QrMatrix CreateEncodingMatrix()
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
                QrModuleValue.Dark,
                QrModuleValue.Dark
            },
            Roles = new QrModuleRole[]
            {
                QrModuleRole.Finder,
                QrModuleRole.Separator,
                QrModuleRole.Unknown,
                QrModuleRole.Data,
                QrModuleRole.ErrorCorrection,
                QrModuleRole.Remainder
            },
            Confidence = new byte[] { 255, 240, 0, 200, 180, 160 }
        };
    }

    static QrMatrix CreateAsymmetricallyPaddedMatrix()
    {
        QrMatrix core = CreateEncodingMatrix();
        QrMatrix result = new QrMatrix
        {
            Width = 7,
            Height = 5,
            Values = new QrModuleValue[35],
            Roles = new QrModuleRole[35],
            Confidence = new byte[35],
            QuietZoneLeft = 1,
            QuietZoneTop = 2,
            QuietZoneRight = 3,
            QuietZoneBottom = 1
        };

        for (int index = 0; index < result.Values.Length; index++)
        {
            result.Values[index] = QrModuleValue.Unknown;
            result.Roles[index] = QrModuleRole.QuietZone;
            result.Confidence[index] = 1;
        }

        for (int row = 0; row < core.Height; row++)
        {
            for (int column = 0; column < core.Width; column++)
            {
                int sourceIndex = row * core.Width + column;
                int destinationIndex = (row + 2) * result.Width + column + 1;
                result.Values[destinationIndex] = core.Values[sourceIndex];
                result.Roles[destinationIndex] = core.Roles![sourceIndex];
                result.Confidence[destinationIndex] = core.Confidence![sourceIndex];
            }
        }

        return result;
    }

    static void AssertQuietZone(QrMatrix matrix, int width)
    {
        for (int row = 0; row < matrix.Height; row++)
        {
            for (int column = 0; column < matrix.Width; column++)
            {
                bool quiet = row < width
                    || row >= matrix.Height - width
                    || column < width
                    || column >= matrix.Width - width;
                if (quiet)
                {
                    int index = row * matrix.Width + column;
                    Assert.Multiple(() =>
                    {
                        Assert.That(matrix.Values[index], Is.EqualTo(QrModuleValue.Light));
                        Assert.That(matrix.Roles![index], Is.EqualTo(QrModuleRole.QuietZone));
                        Assert.That(matrix.Confidence![index], Is.EqualTo(255));
                    });
                }
            }
        }
    }

    static void AssertEncodingRegion(QrMatrix matrix, int left, int top)
    {
        QrMatrix expected = CreateEncodingMatrix();
        for (int row = 0; row < expected.Height; row++)
        {
            for (int column = 0; column < expected.Width; column++)
            {
                int expectedIndex = row * expected.Width + column;
                int actualIndex = (row + top) * matrix.Width + column + left;
                Assert.Multiple(() =>
                {
                    Assert.That(matrix.Values[actualIndex], Is.EqualTo(expected.Values[expectedIndex]));
                    Assert.That(matrix.Roles![actualIndex], Is.EqualTo(expected.Roles![expectedIndex]));
                    Assert.That(matrix.Confidence![actualIndex], Is.EqualTo(expected.Confidence![expectedIndex]));
                });
            }
        }
    }
}
