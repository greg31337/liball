namespace random.Tests;

public class SobolDirectionNumbersTests
{
    [Test]
    public void MetadataMatchesTheJoeKuoDataCoverage()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SobolDirectionNumbers.DimensionCount(), Is.EqualTo(21201));
            Assert.That(SobolDirectionNumbers.BitCount(), Is.EqualTo(64));
        });
    }

    [Test]
    public void FirstDimensionContainsTheBinaryDirectionBasis()
    {
        ulong[] actual = new ulong[64];

        SobolDirectionNumbers.Fill(1, actual);

        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(actual[index], Is.EqualTo(1UL << (63 - index)));
        }
    }

    [Test]
    public void EarlyDimensionsMatchJoeKuoRecurrenceReferences()
    {
        ulong[][] expected =
        [
            [
                0x8000000000000000UL,
                0xC000000000000000UL,
                0xA000000000000000UL,
                0xF000000000000000UL,
                0x8800000000000000UL,
                0xCC00000000000000UL,
                0xAA00000000000000UL,
                0xFF00000000000000UL
            ],
            [
                0x8000000000000000UL,
                0xC000000000000000UL,
                0x6000000000000000UL,
                0x9000000000000000UL,
                0xE800000000000000UL,
                0x5C00000000000000UL,
                0x8E00000000000000UL,
                0xC500000000000000UL
            ],
            [
                0x8000000000000000UL,
                0xC000000000000000UL,
                0x2000000000000000UL,
                0x5000000000000000UL,
                0xF800000000000000UL,
                0x7400000000000000UL
            ]
        ];

        for (int dimension = 2; dimension <= 4; dimension++)
        {
            ulong[] actual = new ulong[64];
            SobolDirectionNumbers.Fill(dimension, actual);
            ulong[] dimensionExpected = expected[dimension - 2];
            for (int index = 0; index < dimensionExpected.Length; index++)
            {
                Assert.That(actual[index], Is.EqualTo(dimensionExpected[index]), $"dimension {dimension}, direction {index + 1}");
            }
        }
    }

    [Test]
    public void LastSupportedDimensionRetainsItsInitialDirectionParameters()
    {
        ulong[] actual = new ulong[64];
        uint[] initialValues =
        [
            1U, 1U, 7U, 11U, 15U, 7U, 37U, 239U, 337U,
            245U, 1557U, 3681U, 7357U, 9639U, 27367U, 26869U,
            114603U, 86317U
        ];

        SobolDirectionNumbers.Fill(21201, actual);

        for (int index = 0; index < initialValues.Length; index++)
        {
            ulong expected = (ulong)initialValues[index] << (63 - index);
            Assert.That(actual[index], Is.EqualTo(expected));
        }
    }

    [Test]
    public void EveryGeneratedDirectionHasTheRequiredOddNumeratorAlignment()
    {
        ulong[] directions = new ulong[64];
        for (int dimension = 1; dimension <= SobolDirectionNumbers.DimensionCount(); dimension++)
        {
            SobolDirectionNumbers.Fill(dimension, directions);
            for (int index = 0; index < directions.Length; index++)
            {
                int trailingBits = 63 - index;
                ulong lowMask = 0UL;
                if (trailingBits != 0)
                {
                    lowMask = (1UL << trailingBits) - 1UL;
                }

                Assert.That(directions[index] & lowMask, Is.Zero, $"dimension {dimension}, direction {index + 1}");
                Assert.That((directions[index] >> trailingBits) & 1UL, Is.EqualTo(1UL), $"dimension {dimension}, direction {index + 1}");
            }
        }
    }

    [Test]
    public void InvalidDimensionsAndDestinationLengthsAreRejectedWithoutWriting()
    {
        ulong[] destination = new ulong[64];
        Array.Fill(destination, 0xDEADBEEFDEADBEEFUL);
        object[] cases =
        [
            new object[] { (TestDelegate)(() => SobolDirectionNumbers.Fill(0, destination)), "dimension" },
            new object[] { (TestDelegate)(() => SobolDirectionNumbers.Fill(21202, destination)), "dimension" },
            new object[] { (TestDelegate)(() => SobolDirectionNumbers.Fill(1, destination.AsSpan(0, 63))), "destination" },
            new object[] { (TestDelegate)(() => SobolDirectionNumbers.Fill(1, new ulong[65])), "destination" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(destination, Is.All.EqualTo(0xDEADBEEFDEADBEEFUL));
        }
    }
}
