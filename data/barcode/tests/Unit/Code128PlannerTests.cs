namespace barcode.Tests;

public class Code128PlannerTests
{
    [Test]
    public void ForcedCodeSetsMapPayloadWithoutSwitches()
    {
        Code128Options setA = CreateOptions(Code128CodeSet.A);
        Code128Options setB = CreateOptions(Code128CodeSet.B);
        Code128Options setC = CreateOptions(Code128CodeSet.C);

        Code128Plan planA = Code128Planner.PlanBytes(new byte[] { 0, 65, 95 }, setA);
        Code128Plan planB = Code128Planner.PlanBytes(new byte[] { 32, 65, 127 }, setB);
        Code128Plan planC = Code128Planner.PlanBytes(new byte[] { 48, 48, 49, 50, 57, 57 }, setC);

        Assert.Multiple(() =>
        {
            Assert.That(planA.StartCodeSet, Is.EqualTo(Code128CodeSet.A));
            Assert.That(planA.Codewords, Is.EqualTo(new int[] { 64, 33, 63 }));
            Assert.That(planB.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(planB.Codewords, Is.EqualTo(new int[] { 0, 33, 95 }));
            Assert.That(planC.StartCodeSet, Is.EqualTo(Code128CodeSet.C));
            Assert.That(planC.Codewords, Is.EqualTo(new int[] { 0, 12, 99 }));
        });
    }

    [Test]
    public void AutomaticPlanningChoosesNaturalSingleSetEncodings()
    {
        Code128Plan text = Code128Planner.PlanBytes(new byte[] { 65, 66, 67 }, null);
        Code128Plan control = Code128Planner.PlanBytes(new byte[] { 0, 1, 31 }, null);
        Code128Plan digits = Code128Planner.PlanBytes(new byte[] { 49, 50, 51, 52, 53, 54 }, null);

        Assert.Multiple(() =>
        {
            Assert.That(text.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(text.Codewords, Is.EqualTo(new int[] { 33, 34, 35 }));
            Assert.That(control.StartCodeSet, Is.EqualTo(Code128CodeSet.A));
            Assert.That(control.Codewords, Is.EqualTo(new int[] { 64, 65, 95 }));
            Assert.That(digits.StartCodeSet, Is.EqualTo(Code128CodeSet.C));
            Assert.That(digits.Codewords, Is.EqualTo(new int[] { 12, 34, 56 }));
        });
    }

    [Test]
    public void PlannerUsesStableTieBreakingBeforeNumericCompaction()
    {
        Code128Plan tied = Code128Planner.PlanBytes(new byte[] { 49, 50, 65 }, null);
        Code128Plan compacted = Code128Planner.PlanBytes(new byte[] { 49, 50, 51, 52, 65 }, null);

        Assert.Multiple(() =>
        {
            Assert.That(tied.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(tied.Codewords, Is.EqualTo(new int[] { 17, 18, 33 }));
            Assert.That(compacted.StartCodeSet, Is.EqualTo(Code128CodeSet.C));
            Assert.That(compacted.Codewords, Is.EqualTo(new int[] { 12, 34, 101, 33 }));
        });
    }

    [Test]
    public void PlannerUsesShiftForOneCharacterAndSwitchesForRuns()
    {
        Code128Plan shifted = Code128Planner.PlanBytes(new byte[] { 97, 1, 98 }, null);
        Code128Plan switched = Code128Planner.PlanBytes(new byte[] { 97, 1, 2, 3, 98 }, null);

        Assert.Multiple(() =>
        {
            Assert.That(shifted.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(shifted.Codewords, Is.EqualTo(new int[] { 65, 98, 65, 66 }));
            Assert.That(switched.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(switched.Codewords, Is.EqualTo(new int[] { 65, 101, 65, 66, 67, 98, 66 }));
        });
    }

    [Test]
    public void AutomaticPlanningCompactsOnlyNumericRunsThatReduceLength()
    {
        Code128Plan plan = Code128Planner.PlanBytes(new byte[] { 65, 49, 50, 51, 52, 53, 54, 66 }, null);

        Assert.Multiple(() =>
        {
            Assert.That(plan.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(plan.Codewords, Is.EqualTo(new int[] { 33, 99, 12, 34, 56, 101, 34 }));
            Assert.That(plan.Codewords, Has.Length.EqualTo(7));
        });
    }

    [Test]
    public void Latin1PlanningUsesSingleAndLatchedFunction4Forms()
    {
        Code128Options options = CreateOptions(Code128CodeSet.Automatic);
        options.AllowFnc4 = true;
        options.UseLatin1UpperShift = true;

        Code128Plan single = Code128Planner.PlanBytes(new byte[] { 193 }, options);
        Code128Plan latched = Code128Planner.PlanBytes(new byte[] { 193, 194, 195 }, options);

        Assert.Multiple(() =>
        {
            Assert.That(single.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(single.Codewords, Is.EqualTo(new int[] { 100, 33 }));
            Assert.That(latched.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(latched.Codewords, Is.EqualTo(new int[] { 100, 100, 33, 34, 35 }));
        });
    }

    [Test]
    public void ForcedLatin1PlanningUsesTheRequestedFunction4Codeword()
    {
        Code128Options setA = CreateOptions(Code128CodeSet.A);
        setA.AllowFnc4 = true;
        setA.UseLatin1UpperShift = true;
        Code128Options setB = CreateOptions(Code128CodeSet.B);
        setB.AllowFnc4 = true;
        setB.UseLatin1UpperShift = true;

        Code128Plan planA = Code128Planner.PlanBytes(new byte[] { 193 }, setA);
        Code128Plan planB = Code128Planner.PlanBytes(new byte[] { 193 }, setB);

        Assert.Multiple(() =>
        {
            Assert.That(planA.Codewords, Is.EqualTo(new int[] { 101, 33 }));
            Assert.That(planB.Codewords, Is.EqualTo(new int[] { 100, 33 }));
        });
    }

    [Test]
    public void FunctionTokensUsePermittedCodeSetsAndPreserveNumericCompaction()
    {
        Code128Options options = CreateOptions(Code128CodeSet.Automatic);
        options.AllowFnc1 = true;
        options.AllowFnc2 = true;
        options.AllowFnc3 = true;
        int[] tokens = new int[]
        {
            Code128Planner.Function1Token,
            '1',
            '2'
        };

        Code128Plan fnc1 = Code128Planner.PlanTokens(tokens, options);
        Code128Plan fnc2 = Code128Planner.PlanTokens(new int[] { Code128Planner.Function2Token, 'a' }, options);
        Code128Plan fnc3 = Code128Planner.PlanTokens(new int[] { Code128Planner.Function3Token, 'A' }, options);

        Assert.Multiple(() =>
        {
            Assert.That(fnc1.StartCodeSet, Is.EqualTo(Code128CodeSet.C));
            Assert.That(fnc1.Codewords, Is.EqualTo(new int[] { 102, 12 }));
            Assert.That(fnc2.StartCodeSet, Is.EqualTo(Code128CodeSet.B));
            Assert.That(fnc2.Codewords, Is.EqualTo(new int[] { 97, 65 }));
            Assert.That(fnc3.Codewords, Is.EqualTo(new int[] { 96, 33 }));
        });
    }

    [Test]
    public void ExplicitFunction4TokensApplySingleAndLatchedUpperShiftSemantics()
    {
        Code128Options options = CreateOptions(Code128CodeSet.B);
        options.AllowFnc4 = true;

        Code128Plan single = Code128Planner.PlanTokens(
            new int[] { Code128Planner.Function4Token, 193, 66 },
            options);
        Code128Plan latched = Code128Planner.PlanTokens(
            new int[] { Code128Planner.Function4Token, Code128Planner.Function4Token, 193, 194 },
            options);

        Assert.Multiple(() =>
        {
            Assert.That(single.Codewords, Is.EqualTo(new int[] { 100, 33, 34 }));
            Assert.That(latched.Codewords, Is.EqualTo(new int[] { 100, 100, 33, 34 }));
        });
    }

    [Test]
    public void PlannerExploresFunctionAndUpperShiftTransitionsFromCodeSetC()
    {
        Code128Options options = CreateOptions(Code128CodeSet.Automatic);
        options.AllowFnc1 = true;
        options.AllowFnc2 = true;
        options.AllowFnc4 = true;
        options.UseLatin1UpperShift = true;

        Code128Plan switchedFunction4 = Code128Planner.PlanTokens(
            new int[] { '1', '2', Code128Planner.Function4Token, 193 },
            options);
        Code128Plan switchedFunction2 = Code128Planner.PlanTokens(
            new int[] { '1', '2', Code128Planner.Function2Token, 'A' },
            options);
        Code128Plan separatedFunction4 = Code128Planner.PlanTokens(
            new int[]
            {
                Code128Planner.Function4Token,
                Code128Planner.Function1Token,
                193
            },
            options);

        Assert.Multiple(() =>
        {
            Assert.That(switchedFunction4.Codewords, Does.Contain(Code128Patterns.Function4InCodeSetBCodeword).Or.Contain(Code128Patterns.Function4InCodeSetACodeword));
            Assert.That(switchedFunction2.Codewords, Does.Contain(Code128Patterns.Function2Codeword));
            Assert.That(separatedFunction4.Codewords, Does.Contain(Code128Patterns.Function1Codeword));
        });
    }

    [Test]
    public void PendingFunction4CanCombineWithShiftForLatin1ControlBytes()
    {
        Code128Options options = CreateOptions(Code128CodeSet.B);
        options.AllowFnc4 = true;
        Code128PlanCandidate candidate = new Code128PlanCandidate();
        candidate.StartCodeSet = Code128CodeSet.B;
        Code128PlanCandidate?[] candidates = new Code128PlanCandidate?[36];

        Code128Planner.AddShiftedByte(
            candidates,
            129,
            0,
            Code128CodeSet.B,
            false,
            1,
            candidate,
            options);
        int stateIndex = Code128Planner.GetStateIndex(1, 1, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.That(candidates[stateIndex], Is.Not.Null);
            Assert.That(candidates[stateIndex]!.StepCodewords, Is.EqualTo(
                new int[] { Code128Patterns.ShiftCodeword, 65 }));
        });
    }

    [Test]
    public void PlannerStateHelpersRejectInvalidStateAndIncompleteHistory()
    {
        Code128PlanCandidate incomplete = new Code128PlanCandidate();
        incomplete.Cost = 1;
        incomplete.StartCodeSet = Code128CodeSet.B;

        Assert.Multiple(() =>
        {
            Assert.That(Code128Planner.PromotePendingState(1), Is.EqualTo(2));
            Assert.That(Code128Planner.PromotePendingState(2), Is.EqualTo(2));
            Assert.That(Code128Planner.GetStartRank(Code128CodeSet.Automatic), Is.EqualTo(3));
            Assert.That(() => Code128Planner.GetCodeSetIndex(Code128CodeSet.Automatic), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.GetCodeSet(3), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.CreatePlan(incomplete), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void FunctionTransitionsDiscardUnrepresentablePendingStates()
    {
        Code128PlanCandidate candidate = new Code128PlanCandidate();
        candidate.StartCodeSet = Code128CodeSet.C;
        Code128Options automatic = CreateOptions(Code128CodeSet.Automatic);
        automatic.AllowFnc2 = true;
        automatic.AllowFnc4 = true;
        Code128Options forcedC = CreateOptions(Code128CodeSet.C);
        forcedC.AllowFnc2 = true;
        forcedC.AllowFnc4 = true;
        Code128PlanCandidate?[] candidates = new Code128PlanCandidate?[36];

        Code128Planner.AddFunctionTransition(
            candidates,
            0,
            Code128CodeSet.B,
            false,
            2,
            candidate,
            automatic,
            Code128Planner.Function4Token);
        Code128Planner.AddFunctionTransition(
            candidates,
            0,
            Code128CodeSet.C,
            false,
            0,
            candidate,
            forcedC,
            Code128Planner.Function4Token);
        Code128Planner.AddFunctionTransition(
            candidates,
            0,
            Code128CodeSet.C,
            false,
            0,
            candidate,
            forcedC,
            Code128Planner.Function2Token);
        Code128Planner.AddFunctionTransition(
            candidates,
            0,
            Code128CodeSet.C,
            false,
            1,
            candidate,
            automatic,
            Code128Planner.Function2Token);

        Assert.That(candidates, Is.All.Null);
    }

    [Test]
    public void ForcedCodeSetsRejectPayloadsThatRequireAnotherSet()
    {
        Code128Options setA = CreateOptions(Code128CodeSet.A);
        Code128Options setB = CreateOptions(Code128CodeSet.B);
        Code128Options setC = CreateOptions(Code128CodeSet.C);

        Assert.Multiple(() =>
        {
            Assert.That(() => Code128Planner.PlanBytes(new byte[] { 97 }, setA), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanBytes(new byte[] { 0 }, setB), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanBytes(new byte[] { 49 }, setC), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanBytes(new byte[] { 49, 65 }, setC), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanBytes(new byte[] { 193 }, setB), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void PlannerRejectsInvalidInputOptionsAndFunctionMarkers()
    {
        Code128Options invalidSet = CreateOptions((Code128CodeSet)99);
        Code128Options noFunctions = CreateOptions(Code128CodeSet.Automatic);
        Code128Options allowFnc4 = CreateOptions(Code128CodeSet.B);
        allowFnc4.AllowFnc4 = true;

        Assert.Multiple(() =>
        {
            Assert.That(() => Code128Planner.PlanBytes(null!, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanBytes(new byte[0], null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanTokens(null!, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanTokens(new int[] { -1 }, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanTokens(new int[] { 260 }, null), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanBytes(new byte[] { 65 }, invalidSet), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanTokens(new int[] { Code128Planner.Function1Token }, noFunctions), Throws.TypeOf<BarcodeEncodingException>());
            Assert.That(() => Code128Planner.PlanTokens(new int[] { Code128Planner.Function4Token }, allowFnc4), Throws.TypeOf<BarcodeEncodingException>());
        });
    }

    [Test]
    public void RepeatedPlanningProducesTheSameOptimalPlan()
    {
        byte[] input = new byte[] { 65, 49, 50, 51, 52, 53, 54, 1, 97, 98 };

        Code128Plan first = Code128Planner.PlanBytes(input, null);
        Code128Plan second = Code128Planner.PlanBytes(input, null);

        Assert.Multiple(() =>
        {
            Assert.That(second.StartCodeSet, Is.EqualTo(first.StartCodeSet));
            Assert.That(second.Codewords, Is.EqualTo(first.Codewords));
            Assert.That(second.Codewords, Is.Not.SameAs(first.Codewords));
        });
    }

    static Code128Options CreateOptions(Code128CodeSet codeSet)
    {
        Code128Options options = new Code128Options();
        options.CodeSet = codeSet;
        return options;
    }
}
