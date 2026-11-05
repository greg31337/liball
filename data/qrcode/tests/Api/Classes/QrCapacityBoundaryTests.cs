using qrcode;

namespace qrcode.Tests;

[TestFixture]
public class QrCapacityBoundaryTests
{
    // These sources use test-owned reference tables, never production capacity or mode helpers.
    [TestCaseSource(typeof(Model2CapacityCalculatorTests), nameof(Model2CapacityCalculatorTests.PublicBoundaryCases))]
    [TestCaseSource(typeof(MicroCapacityCalculatorTests), nameof(MicroCapacityCalculatorTests.PublicBoundaryCases))]
    [TestCaseSource(typeof(RectangularMicroCapacityCalculatorTests), nameof(RectangularMicroCapacityCalculatorTests.PublicBoundaryCases))]
    public void ChecksPublicCapacityAtEveryProfileAndModeBoundary(
        QrCapacityRequest request, int countWidth, int headerBits, int capacity, int dataCodewords, int totalCodewords, int remainderBits)
    {
        IQrCapacityCalculator calculator = new QrCapacityCalculator();
        int maximumCount = (1 << countWidth) - 1;
        int largestFittingCount = 0;
        while (largestFittingCount < maximumCount
            && headerBits + PayloadBits(request.EncodingMode, largestFittingCount + 1) <= capacity)
            largestFittingCount++;

        // Whole-character boundaries must be tested separately from a one-bit control increment.
        foreach (int count in new[] { 0, largestFittingCount - 1, largestFittingCount, largestFittingCount + 1, maximumCount })
        {
            if (count < 0 || count > maximumCount) continue;
            SetPayload(request, count);
            CheckResult(calculator, request, headerBits + request.PayloadBitCount, capacity, dataCodewords, totalCodewords, remainderBits);
        }

        SetPayload(request, largestFittingCount);
        if (request.SymbolKind == QrSymbolKind.Micro)
        {
            // Micro has no control-bit allowance. Test every representable count, including
            // natural exact fits and one-bit overflows, without inventing invalid padding controls.
            for (int count = 0; count <= maximumCount; count++)
            {
                SetPayload(request, count);
                CheckResult(calculator, request, headerBits + request.PayloadBitCount, capacity, dataCodewords, totalCodewords, remainderBits);
            }

            SetPayload(request, largestFittingCount);
            CheckForcedSelection(calculator, request, headerBits + request.PayloadBitCount, capacity);
            SetPayload(request, largestFittingCount + 1);
            CheckForcedSelection(calculator, request, headerBits + request.PayloadBitCount, capacity);
            SetPayload(request, largestFittingCount);
            int validPayload = request.PayloadBitCount;
            foreach (int delta in new[] { 0, 1 })
            {
                request.PayloadBitCount = capacity + delta - headerBits;
                if (request.PayloadBitCount == validPayload)
                    CheckResult(calculator, request, capacity + delta, capacity, dataCodewords, totalCodewords, remainderBits);
                else
                    AssertInvalid(calculator, request, "PayloadBitCount");
            }

            request.PayloadBitCount = validPayload;
            request.ControlBitCount = 1;
            AssertInvalid(calculator, request, "ControlBitCount");
            request.ControlBitCount = 0;
        }
        else
        {
            // Leave at least one control bit available so all three neighboring bit totals
            // are valid requests, even when the largest whole-character payload fits exactly.
            SetPayload(request, largestFittingCount - 1);
            int exactControls = capacity - headerBits - request.PayloadBitCount;
            foreach (int delta in new[] { -1, 0, 1 })
            {
                request.ControlBitCount = exactControls + delta;
                CheckResult(calculator, request, capacity + delta, capacity, dataCodewords, totalCodewords, remainderBits);
                CheckForcedSelection(calculator, request, capacity + delta, capacity);
            }

            // Large control counts must report capacity overflow or a bounded-arithmetic
            // exception, never wrap around into a fitting result.
            request.ControlBitCount = int.MaxValue - headerBits - request.PayloadBitCount;
            CheckResult(calculator, request, int.MaxValue, capacity, dataCodewords, totalCodewords, remainderBits);
            request.ControlBitCount++;
            Assert.Throws<QrResourceLimitException>(() => calculator.CalculateCapacity(request));
            request.ControlBitCount = 0;
        }

        SetPayload(request, largestFittingCount);
        int expectedPayload = request.PayloadBitCount;
        foreach (int delta in new[] { -1, 1 })
        {
            request.PayloadBitCount = expectedPayload + delta;
            AssertInvalid(calculator, request, "PayloadBitCount");
        }

        // Count-field overflow remains an invalid request rather than a capacity result.
        SetPayload(request, maximumCount + 1);
        AssertInvalid(calculator, request, "CharacterCount");
    }

    static void CheckResult(IQrCapacityCalculator calculator, QrCapacityRequest request, int required,
        int capacity, int dataCodewords, int totalCodewords, int remainderBits)
    {
        var before = Snapshot(request);
        QrCapacityResult result = calculator.CalculateCapacity(request);
        Assert.Multiple(() =>
        {
            Assert.That(result.Fits, Is.EqualTo(required <= capacity));
            Assert.That(result.RequiredBitCount, Is.EqualTo(required));
            Assert.That(result.DataBitCapacity, Is.EqualTo(capacity));
            Assert.That(result.UnusedBitCount, Is.EqualTo(capacity - required));
            Assert.That(result.DataCodewordCount, Is.EqualTo(dataCodewords));
            Assert.That(result.TotalCodewordCount, Is.EqualTo(totalCodewords));
            Assert.That(result.ErrorCorrectionCodewordCount, Is.EqualTo(totalCodewords - dataCodewords));
            Assert.That(result.RemainderBitCount, Is.EqualTo(remainderBits));
            Assert.That(Snapshot(request), Is.EqualTo(before));
        });
    }

    static void AssertInvalid(IQrCapacityCalculator calculator, QrCapacityRequest request, string inputName)
    {
        var before = Snapshot(request);
        QrInputException error = Assert.Throws<QrInputException>(() => calculator.CalculateCapacity(request))!;
        Assert.That(error.InputName, Is.EqualTo(inputName));
        Assert.That(Snapshot(request), Is.EqualTo(before));
    }

    static void CheckForcedSelection(IQrCapacityCalculator calculator, QrCapacityRequest request, int required, int capacity)
    {
        int width = request.SymbolKind == QrSymbolKind.Model2 ? 17 + 4 * request.Version
            : request.SymbolKind == QrSymbolKind.Micro ? 9 + 2 * request.Version : request.Width;
        int height = request.SymbolKind == QrSymbolKind.rMQR ? request.Height : width;
        QrEncodeOptions options = new QrEncodeOptions
        {
            ForceSymbolKind = true, SymbolKind = request.SymbolKind,
            ForceErrorCorrectionLevel = true, ErrorCorrectionLevel = request.ErrorCorrectionLevel,
            BoostErrorCorrectionLevel = false, MinimumVersion = request.Version,
            MaximumVersion = request.Version == 0 ? 40 : request.Version,
            MinimumWidth = width, MaximumWidth = width, MinimumHeight = height, MaximumHeight = height
        };
        var before = Snapshot(request);
        if (required <= capacity)
        {
            QrCapacityRequest selected = calculator.SelectSymbolParameters(request, options);
            Assert.That(selected, Is.Not.SameAs(request));
            Assert.That(selected.SymbolKind, Is.EqualTo(request.SymbolKind));
            Assert.That(selected.Version, Is.EqualTo(request.Version));
            Assert.That(selected.Width, Is.EqualTo(width));
            Assert.That(selected.Height, Is.EqualTo(height));
            Assert.That(selected.ErrorCorrectionLevel, Is.EqualTo(request.ErrorCorrectionLevel));
            Assert.That(selected.EncodingMode, Is.EqualTo(request.EncodingMode));
            Assert.That(selected.CharacterCount, Is.EqualTo(request.CharacterCount));
            Assert.That(selected.PayloadBitCount, Is.EqualTo(request.PayloadBitCount));
            Assert.That(selected.ControlBitCount, Is.EqualTo(request.ControlBitCount));
            Assert.That(calculator.CalculateCapacity(selected).RequiredBitCount, Is.EqualTo(required));
        }
        else
        {
            QrCapacityException error = Assert.Throws<QrCapacityException>(() => calculator.SelectSymbolParameters(request, options))!;
            Assert.That(error.RequiredBitCount, Is.EqualTo(required));
            Assert.That(error.AvailableBitCount, Is.EqualTo(capacity));
            Assert.That(error.SymbolKind, Is.EqualTo(request.SymbolKind));
            Assert.That(error.FailureReason, Is.EqualTo(QrFailureReason.CapacityExceeded));
        }

        Assert.That(Snapshot(request), Is.EqualTo(before));
    }

    static object Snapshot(QrCapacityRequest request) => (request.SymbolKind, request.Version, request.Width, request.Height,
        request.ErrorCorrectionLevel, request.EncodingMode, request.CharacterCount, request.PayloadBitCount, request.ControlBitCount);

    static void SetPayload(QrCapacityRequest request, int count)
    {
        request.CharacterCount = count;
        request.PayloadBitCount = PayloadBits(request.EncodingMode, count);
    }

    static int PayloadBits(QrEncodingMode mode, int count) => mode switch
    {
        QrEncodingMode.Numeric => count / 3 * 10 + (count % 3 == 0 ? 0 : count % 3 == 1 ? 4 : 7),
        QrEncodingMode.Alphanumeric => count / 2 * 11 + count % 2 * 6,
        QrEncodingMode.Byte => count * 8,
        QrEncodingMode.Kanji or QrEncodingMode.Hanzi => count * 13,
        _ => throw new ArgumentException("Unexpected payload mode.")
    };
}
