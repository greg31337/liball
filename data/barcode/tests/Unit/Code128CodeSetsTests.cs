namespace barcode.Tests;

public class Code128CodeSetsTests
{
    [Test]
    public void CodeSetAMapsTheCompleteAsciiRanges()
    {
        int byteValue = 0;
        while (byteValue <= 95)
        {
            int expectedCodeword = byteValue - 32;
            if (byteValue < 32)
            {
                expectedCodeword = byteValue + 64;
            }

            int codeword = Code128CodeSets.EncodeByte(byteValue, Code128CodeSet.A, false);
            Assert.Multiple(() =>
            {
                Assert.That(codeword, Is.EqualTo(expectedCodeword), $"Base byte {byteValue}");
                Assert.That(Code128CodeSets.DecodeByte(codeword, Code128CodeSet.A, false), Is.EqualTo(byteValue));
                Assert.That(Code128CodeSets.CanEncodeByte(byteValue, Code128CodeSet.A, false), Is.True);
            });
            byteValue = byteValue + 1;
        }

        byteValue = 128;
        while (byteValue <= 223)
        {
            int codeword = Code128CodeSets.EncodeByte(byteValue, Code128CodeSet.A, true);
            Assert.Multiple(() =>
            {
                Assert.That(Code128CodeSets.DecodeByte(codeword, Code128CodeSet.A, true), Is.EqualTo(byteValue));
                Assert.That(Code128CodeSets.RequiresUpperShift(byteValue, Code128CodeSet.A), Is.True);
            });
            byteValue = byteValue + 1;
        }
    }

    [Test]
    public void CodeSetBMapsTheCompleteAsciiRanges()
    {
        int byteValue = 32;
        while (byteValue <= 127)
        {
            int codeword = Code128CodeSets.EncodeByte(byteValue, Code128CodeSet.B, false);
            Assert.Multiple(() =>
            {
                Assert.That(codeword, Is.EqualTo(byteValue - 32), $"Base byte {byteValue}");
                Assert.That(Code128CodeSets.DecodeByte(codeword, Code128CodeSet.B, false), Is.EqualTo(byteValue));
                Assert.That(Code128CodeSets.RequiresUpperShift(byteValue, Code128CodeSet.B), Is.False);
            });
            byteValue = byteValue + 1;
        }

        byteValue = 160;
        while (byteValue <= 255)
        {
            int codeword = Code128CodeSets.EncodeByte(byteValue, Code128CodeSet.B, true);
            Assert.Multiple(() =>
            {
                Assert.That(Code128CodeSets.DecodeByte(codeword, Code128CodeSet.B, true), Is.EqualTo(byteValue));
                Assert.That(Code128CodeSets.RequiresUpperShift(byteValue, Code128CodeSet.B), Is.True);
            });
            byteValue = byteValue + 1;
        }
    }

    [Test]
    public void ByteMappingRejectsValuesOutsideEachCodeSetRange()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code128CodeSets.CanEncodeByte(-1, Code128CodeSet.A, false), Is.False);
            Assert.That(Code128CodeSets.CanEncodeByte(256, Code128CodeSet.B, true), Is.False);
            Assert.That(Code128CodeSets.CanEncodeByte(96, Code128CodeSet.A, false), Is.False);
            Assert.That(Code128CodeSets.CanEncodeByte(224, Code128CodeSet.A, true), Is.False);
            Assert.That(Code128CodeSets.CanEncodeByte(31, Code128CodeSet.B, false), Is.False);
            Assert.That(Code128CodeSets.CanEncodeByte(159, Code128CodeSet.B, true), Is.False);
            Assert.That(Code128CodeSets.CanEncodeByte('0', Code128CodeSet.C, false), Is.False);
            Assert.That(() => Code128CodeSets.CanEncodeByte(65, Code128CodeSet.Automatic, false), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.EncodeByte(128, Code128CodeSet.A, false), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.EncodeByte(65, Code128CodeSet.A, true), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.DecodeByte(96, Code128CodeSet.A, false), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.DecodeByte(0, Code128CodeSet.C, false), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.RequiresUpperShift(127, Code128CodeSet.A), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.RequiresUpperShift(128, Code128CodeSet.B), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void CodeSetCMapsEveryAsciiDigitPair()
    {
        int codeword = 0;
        while (codeword <= 99)
        {
            int[] digits = Code128CodeSets.DecodeDigitPair(codeword);
            Assert.Multiple(() =>
            {
                Assert.That(digits, Has.Length.EqualTo(2));
                Assert.That(digits[0], Is.InRange((int)'0', (int)'9'));
                Assert.That(digits[1], Is.InRange((int)'0', (int)'9'));
                Assert.That(Code128CodeSets.EncodeDigitPair(digits[0], digits[1]), Is.EqualTo(codeword));
            });
            codeword = codeword + 1;
        }
    }

    [Test]
    public void CodeSetCRejectsMalformedPairsAndNonDataCodewords()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code128CodeSets.EncodeDigitPair('0', '0'), Is.Zero);
            Assert.That(Code128CodeSets.EncodeDigitPair('9', '9'), Is.EqualTo(99));
            Assert.That(() => Code128CodeSets.EncodeDigitPair('/', '0'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.EncodeDigitPair('0', ':'), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.DecodeDigitPair(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.DecodeDigitPair(100), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void ShiftAndCodeSetSwitchesFollowTheActiveSetTable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code128CodeSets.GetShiftedCodeSet(Code128CodeSet.A), Is.EqualTo(Code128CodeSet.B));
            Assert.That(Code128CodeSets.GetShiftedCodeSet(Code128CodeSet.B), Is.EqualTo(Code128CodeSet.A));
            Assert.That(Code128CodeSets.GetSwitchCodeword(Code128CodeSet.A, Code128CodeSet.B), Is.EqualTo(100));
            Assert.That(Code128CodeSets.GetSwitchCodeword(Code128CodeSet.A, Code128CodeSet.C), Is.EqualTo(99));
            Assert.That(Code128CodeSets.GetSwitchCodeword(Code128CodeSet.B, Code128CodeSet.A), Is.EqualTo(101));
            Assert.That(Code128CodeSets.GetSwitchCodeword(Code128CodeSet.B, Code128CodeSet.C), Is.EqualTo(99));
            Assert.That(Code128CodeSets.GetSwitchCodeword(Code128CodeSet.C, Code128CodeSet.A), Is.EqualTo(101));
            Assert.That(Code128CodeSets.GetSwitchCodeword(Code128CodeSet.C, Code128CodeSet.B), Is.EqualTo(100));
            Assert.That(Code128CodeSets.ApplySwitchCodeword(Code128CodeSet.A, 100), Is.EqualTo(Code128CodeSet.B));
            Assert.That(Code128CodeSets.ApplySwitchCodeword(Code128CodeSet.A, 99), Is.EqualTo(Code128CodeSet.C));
            Assert.That(Code128CodeSets.ApplySwitchCodeword(Code128CodeSet.B, 101), Is.EqualTo(Code128CodeSet.A));
            Assert.That(Code128CodeSets.ApplySwitchCodeword(Code128CodeSet.C, 100), Is.EqualTo(Code128CodeSet.B));
        });
    }

    [Test]
    public void ShiftAndSwitchMappingRejectInvalidTransitions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Code128CodeSets.GetShiftedCodeSet(Code128CodeSet.C), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.GetShiftedCodeSet(Code128CodeSet.Automatic), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.GetSwitchCodeword(Code128CodeSet.A, Code128CodeSet.A), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.GetSwitchCodeword(Code128CodeSet.Automatic, Code128CodeSet.B), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.ApplySwitchCodeword(Code128CodeSet.A, 101), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.ApplySwitchCodeword(Code128CodeSet.C, 99), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void ShiftStateChangesOneDataCharacterThenReverts()
    {
        Code128CodeSetState fromA = new Code128CodeSetState(Code128CodeSet.A);
        fromA.ReadShift();
        Code128CodeSet shiftedFromA = fromA.ConsumeDataCodeSet();
        Code128CodeSet revertedToA = fromA.ConsumeDataCodeSet();
        Code128CodeSetState fromB = new Code128CodeSetState(Code128CodeSet.B);
        fromB.ReadShift();
        Code128CodeSet shiftedFromB = fromB.ConsumeDataCodeSet();

        Assert.Multiple(() =>
        {
            Assert.That(shiftedFromA, Is.EqualTo(Code128CodeSet.B));
            Assert.That(revertedToA, Is.EqualTo(Code128CodeSet.A));
            Assert.That(fromA.GetActiveCodeSet(), Is.EqualTo(Code128CodeSet.A));
            Assert.That(fromA.HasPendingShift(), Is.False);
            Assert.That(shiftedFromB, Is.EqualTo(Code128CodeSet.A));
            Assert.That(fromB.GetActiveCodeSet(), Is.EqualTo(Code128CodeSet.B));
        });
    }

    [Test]
    public void CodeSetStateKeepsSwitchesActiveUntilAnotherSwitch()
    {
        Code128CodeSetState state = new Code128CodeSetState(Code128CodeSet.A);

        state.ReadSwitch(99);
        Code128CodeSet first = state.ConsumeDataCodeSet();
        Code128CodeSet second = state.ConsumeDataCodeSet();
        state.ReadSwitch(100);
        Code128CodeSet third = state.ConsumeDataCodeSet();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(Code128CodeSet.C));
            Assert.That(second, Is.EqualTo(Code128CodeSet.C));
            Assert.That(third, Is.EqualTo(Code128CodeSet.B));
            Assert.That(state.GetActiveCodeSet(), Is.EqualTo(Code128CodeSet.B));
        });
    }

    [Test]
    public void FinishedCodeSetStateAcceptsConsumedAndNeverShiftedStates()
    {
        Code128CodeSetState unchanged = new Code128CodeSetState(
            Code128CodeSet.B);
        Code128CodeSetState consumed = new Code128CodeSetState(
            Code128CodeSet.A);
        consumed.ReadShift();
        consumed.ConsumeDataCodeSet();

        Assert.Multiple(() =>
        {
            Assert.That(() => unchanged.ValidateFinished(), Throws.Nothing);
            Assert.That(() => consumed.ValidateFinished(), Throws.Nothing);
        });
    }

    [Test]
    public void CodeSetStateRejectsMalformedShiftAndSwitchSequences()
    {
        Code128CodeSetState setC = new Code128CodeSetState(Code128CodeSet.C);
        Code128CodeSetState doubleShift = new Code128CodeSetState(Code128CodeSet.A);
        doubleShift.ReadShift();
        Code128CodeSetState switchedShift = new Code128CodeSetState(Code128CodeSet.B);
        switchedShift.ReadShift();

        Assert.Multiple(() =>
        {
            Assert.That(() => new Code128CodeSetState(Code128CodeSet.Automatic), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => setC.ReadShift(), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => doubleShift.ReadShift(), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => doubleShift.ValidateFinished(), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => switchedShift.ReadSwitch(101), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void FunctionCodewordsRespectCodeSetSpecificMeanings()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Code128CodeSets.GetFunctionCodeword(1, Code128CodeSet.A), Is.EqualTo(102));
            Assert.That(Code128CodeSets.GetFunctionCodeword(1, Code128CodeSet.B), Is.EqualTo(102));
            Assert.That(Code128CodeSets.GetFunctionCodeword(1, Code128CodeSet.C), Is.EqualTo(102));
            Assert.That(Code128CodeSets.GetFunctionCodeword(2, Code128CodeSet.A), Is.EqualTo(97));
            Assert.That(Code128CodeSets.GetFunctionCodeword(2, Code128CodeSet.B), Is.EqualTo(97));
            Assert.That(Code128CodeSets.GetFunctionCodeword(3, Code128CodeSet.A), Is.EqualTo(96));
            Assert.That(Code128CodeSets.GetFunctionCodeword(3, Code128CodeSet.B), Is.EqualTo(96));
            Assert.That(Code128CodeSets.GetFunctionCodeword(4, Code128CodeSet.A), Is.EqualTo(101));
            Assert.That(Code128CodeSets.GetFunctionCodeword(4, Code128CodeSet.B), Is.EqualTo(100));
        });
    }

    [Test]
    public void FunctionMappingRejectsUnavailableFunctions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => Code128CodeSets.GetFunctionCodeword(0, Code128CodeSet.A), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.GetFunctionCodeword(5, Code128CodeSet.B), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.GetFunctionCodeword(2, Code128CodeSet.C), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.GetFunctionCodeword(3, Code128CodeSet.C), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.GetFunctionCodeword(4, Code128CodeSet.C), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => Code128CodeSets.GetFunctionCodeword(1, Code128CodeSet.Automatic), Throws.TypeOf<BarcodeValidationException>());
        });
    }

    [Test]
    public void SingleFunction4UpperShiftsOnlyTheNextDataByte()
    {
        Code128UpperShiftState state = new Code128UpperShiftState();

        state.ReadFunction4();
        int first = state.ApplyToDataByte(65);
        int second = state.ApplyToDataByte(66);
        state.ValidateFinished();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(193));
            Assert.That(second, Is.EqualTo(66));
            Assert.That(state.IsLatched(), Is.False);
            Assert.That(state.HasPendingSingleShift(), Is.False);
        });
    }

    [Test]
    public void ConsecutiveFunction4CharactersToggleLatchedUpperShift()
    {
        Code128UpperShiftState state = new Code128UpperShiftState();

        state.ReadFunction4();
        state.ReadFunction4();
        int upperA = state.ApplyToDataByte(65);
        int upperB = state.ApplyToDataByte(66);
        state.ReadFunction4();
        state.ReadFunction4();
        int lowerC = state.ApplyToDataByte(67);

        Assert.Multiple(() =>
        {
            Assert.That(upperA, Is.EqualTo(193));
            Assert.That(upperB, Is.EqualTo(194));
            Assert.That(lowerC, Is.EqualTo(67));
            Assert.That(state.IsLatched(), Is.False);
        });
    }

    [Test]
    public void SingleFunction4InsideLatchedModeTemporarilySelectsLowerHalf()
    {
        Code128UpperShiftState state = new Code128UpperShiftState();
        state.ReadFunction4();
        state.ReadFunction4();

        state.ReadFunction4();
        int lower = state.ApplyToDataByte(65);
        int upper = state.ApplyToDataByte(66);

        Assert.Multiple(() =>
        {
            Assert.That(lower, Is.EqualTo(65));
            Assert.That(upper, Is.EqualTo(194));
            Assert.That(state.IsLatched(), Is.True);
        });
    }

    [Test]
    public void Function4RemainsPendingAcrossShiftAndOtherControls()
    {
        Code128UpperShiftState state = new Code128UpperShiftState();
        state.ReadFunction4();
        state.ReadOtherControl();
        state.ReadOtherControl();

        int value = state.ApplyToDataByte(31);

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(159));
            Assert.That(state.HasPendingSingleShift(), Is.False);
        });
    }

    [Test]
    public void UpperShiftStateRejectsMalformedOrUnfinishedSequences()
    {
        Code128UpperShiftState unfinished = new Code128UpperShiftState();
        unfinished.ReadFunction4();
        Code128UpperShiftState separated = new Code128UpperShiftState();
        separated.ReadFunction4();
        separated.ReadOtherControl();

        Assert.Multiple(() =>
        {
            Assert.That(() => unfinished.ValidateFinished(), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => separated.ReadFunction4(), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => separated.ApplyToDataByte(-1), Throws.TypeOf<BarcodeValidationException>());
            Assert.That(() => separated.ApplyToDataByte(128), Throws.TypeOf<BarcodeValidationException>());
        });
    }
}
