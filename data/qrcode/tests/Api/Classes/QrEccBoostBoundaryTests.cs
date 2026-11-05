using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrEccBoostBoundaryTests
{
    static IEnumerable<TestCaseData> References() => Model2CapacityCalculatorTests.PublicBoundaryCases()
        .Concat(MicroCapacityCalculatorTests.PublicBoundaryCases()).Concat(RectangularMicroCapacityCalculatorTests.PublicBoundaryCases())
        .Where(c => ((QrCapacityRequest)c.Arguments[0]!).EncodingMode == QrEncodingMode.Numeric);

    static IEnumerable<TestCaseData> BoostingCases()
    {
        foreach (TestCaseData reference in References())
        {
            QrCapacityRequest shape = (QrCapacityRequest)reference.Arguments[0]!;
            int header = (int)reference.Arguments[2]!;
            int capacity = (int)reference.Arguments[3]!;
            int maximum = 0;
            while (header + NumericBits(maximum + 1) <= capacity) maximum++;
            yield return new TestCaseData(shape, header, maximum)
                .SetName($"BoostBoundary_{shape.SymbolKind}_{shape.Version}_{shape.Width}x{shape.Height}_{shape.ErrorCorrectionLevel}");
        }
    }

    [TestCaseSource(nameof(BoostingCases))]
    public void BoostsToStrongestFittingEccAtEverySizeAndLevelBoundary(QrCapacityRequest shape, int header, int maximum)
    {
        var levels = References().Where(c => SameSize((QrCapacityRequest)c.Arguments[0]!, shape))
            .Select(c => (Level: ((QrCapacityRequest)c.Arguments[0]!).ErrorCorrectionLevel, Capacity: (int)c.Arguments[3]!))
            .OrderBy(c => c.Level).ToArray();
        foreach (int count in new[] { maximum, maximum + 1 })
        {
            int required = header + NumericBits(count);
            if (required > levels[0].Capacity) continue;
            QrErrorCorrectionLevel expected = levels.Where(c => c.Capacity >= required).Max(c => c.Level);
            QrCapacityRequest request = new QrCapacityRequest
            {
                SymbolKind = shape.SymbolKind, Version = shape.Version, Width = shape.Width, Height = shape.Height,
                EncodingMode = QrEncodingMode.Numeric, CharacterCount = count, PayloadBitCount = NumericBits(count)
            };
            int width = shape.SymbolKind == QrSymbolKind.Model2 ? 17 + 4 * shape.Version
                : shape.SymbolKind == QrSymbolKind.Micro ? 9 + 2 * shape.Version : shape.Width;
            int height = shape.SymbolKind == QrSymbolKind.rMQR ? shape.Height : width;
            foreach (bool force in new[] { false, true })
                foreach (bool boost in new[] { false, true })
                {
                    QrEncodeOptions options = new QrEncodeOptions
                    {
                        ForceSymbolKind = true, SymbolKind = shape.SymbolKind,
                        MinimumVersion = shape.Version, MaximumVersion = shape.Version == 0 ? 40 : shape.Version,
                        MinimumWidth = width, MaximumWidth = width, MinimumHeight = height, MaximumHeight = height,
                        ErrorCorrectionLevel = levels[0].Level, BoostErrorCorrectionLevel = boost, ForceErrorCorrectionLevel = force
                    };
                    QrCapacityRequest selected = new QrCapacityCalculator().SelectSymbolParameters(request, options);
                    Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(boost && !force ? expected : levels[0].Level));
                    Assert.That(selected.SymbolKind, Is.EqualTo(shape.SymbolKind));
                    Assert.That(selected.Version, Is.EqualTo(shape.Version));
                    Assert.That((selected.Width, selected.Height), Is.EqualTo((width, height)));
                    Assert.That(selected.CharacterCount, Is.EqualTo(count));
                    Assert.That(request.CharacterCount, Is.EqualTo(count));
                    Assert.That(options.ErrorCorrectionLevel, Is.EqualTo(levels[0].Level));
                }
        }
    }

    static bool SameSize(QrCapacityRequest left, QrCapacityRequest right) => left.SymbolKind == right.SymbolKind
        && left.Version == right.Version && left.Width == right.Width && left.Height == right.Height;

    static int NumericBits(int count) => count / 3 * 10 + (count % 3 == 0 ? 0 : count % 3 == 1 ? 4 : 7);
}
