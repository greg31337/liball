namespace barcode;

internal class Code128Plan
{
    internal Code128CodeSet StartCodeSet;
    internal int[] Codewords = new int[0];
}

internal class Code128PlanCandidate
{
    internal int Cost;
    internal int ControlCount;
    internal Code128CodeSet StartCodeSet;
    internal Code128PlanCandidate? PreviousCandidate;
    internal int[] StepCodewords = new int[0];
}

internal static class Code128Planner
{
    internal const int Function1Token = 256;
    internal const int Function2Token = 257;
    internal const int Function3Token = 258;
    internal const int Function4Token = 259;
    internal const int PendingStateCount = 3;
    internal const int LatchStateCount = 2;
    internal const int CodeSetStateCount = 3;

    internal static Code128Plan PlanBytes(byte[] bytes, Code128Options? options)
    {
        if (bytes == null)
        {
            throw new BarcodeEncodingException("Code 128 payload bytes must not be null.");
        }

        int[] tokens = new int[bytes.Length];
        int index = 0;
        while (index < bytes.Length)
        {
            tokens[index] = bytes[index];
            index = index + 1;
        }

        return PlanTokens(tokens, options);
    }

    internal static Code128Plan PlanTokens(int[] tokens, Code128Options? options)
    {
        ValidateTokens(tokens);
        Code128Options? settings = options;
        if (settings == null)
        {
            settings = new Code128Options();
        }

        ValidateRequestedCodeSet(settings.CodeSet);
        int positionCount = CheckedMath.Add(tokens.Length, 1);
        int stateCount = CheckedMath.Multiply(positionCount, CodeSetStateCount * LatchStateCount * PendingStateCount);
        Code128PlanCandidate?[] candidates = new Code128PlanCandidate?[stateCount];
        AddInitialCandidates(candidates, settings.CodeSet);

        int position = 0;
        while (position < tokens.Length)
        {
            int codeSetIndex = 0;
            while (codeSetIndex < CodeSetStateCount)
            {
                int latchIndex = 0;
                while (latchIndex < LatchStateCount)
                {
                    int pendingState = 0;
                    while (pendingState < PendingStateCount)
                    {
                        int stateIndex = GetStateIndex(position, codeSetIndex, latchIndex, pendingState);
                        Code128PlanCandidate? candidate = candidates[stateIndex];
                        if (candidate != null)
                        {
                            Code128CodeSet codeSet = GetCodeSet(codeSetIndex);
                            AddTokenTransitions(candidates, tokens, position, codeSet, latchIndex == 1, pendingState, candidate, settings);
                        }

                        pendingState = pendingState + 1;
                    }

                    latchIndex = latchIndex + 1;
                }

                codeSetIndex = codeSetIndex + 1;
            }

            position = position + 1;
        }

        Code128PlanCandidate? best = FindBestFinishedCandidate(candidates, tokens.Length);
        if (best == null)
        {
            throw new BarcodeEncodingException("The Code 128 payload cannot be represented with the requested code-set and function options.");
        }

        return CreatePlan(best);
    }

    internal static void AddInitialCandidates(Code128PlanCandidate?[] candidates, Code128CodeSet requestedCodeSet)
    {
        if (requestedCodeSet == Code128CodeSet.Automatic)
        {
            AddInitialCandidate(candidates, Code128CodeSet.B);
            AddInitialCandidate(candidates, Code128CodeSet.A);
            AddInitialCandidate(candidates, Code128CodeSet.C);
            return;
        }

        AddInitialCandidate(candidates, requestedCodeSet);
    }

    internal static void AddInitialCandidate(Code128PlanCandidate?[] candidates, Code128CodeSet codeSet)
    {
        int stateIndex = GetStateIndex(0, GetCodeSetIndex(codeSet), 0, 0);
        Code128PlanCandidate candidate = new Code128PlanCandidate();
        candidate.Cost = 0;
        candidate.ControlCount = 0;
        candidate.StartCodeSet = codeSet;
        candidate.PreviousCandidate = null;
        candidate.StepCodewords = new int[0];
        candidates[stateIndex] = candidate;
    }

    internal static void AddTokenTransitions(
        Code128PlanCandidate?[] candidates,
        int[] tokens,
        int position,
        Code128CodeSet codeSet,
        bool upperShiftLatched,
        int pendingState,
        Code128PlanCandidate candidate,
        Code128Options options)
    {
        int token = tokens[position];
        if (token <= 255)
        {
            AddByteTransitions(candidates, tokens, position, codeSet, upperShiftLatched, pendingState, candidate, options);
            return;
        }

        AddFunctionTransition(candidates, position, codeSet, upperShiftLatched, pendingState, candidate, options, token);
    }

    internal static void AddByteTransitions(
        Code128PlanCandidate?[] candidates,
        int[] tokens,
        int position,
        Code128CodeSet codeSet,
        bool upperShiftLatched,
        int pendingState,
        Code128PlanCandidate candidate,
        Code128Options options)
    {
        bool forced = options.CodeSet != Code128CodeSet.Automatic;
        int targetIndex = 0;
        while (targetIndex < 2)
        {
            Code128CodeSet targetCodeSet = GetCodeSet(targetIndex);
            if (!forced || targetCodeSet == codeSet)
            {
                AddByteInCodeSet(candidates, tokens[position], position, codeSet, targetCodeSet, upperShiftLatched, pendingState, candidate, options);
            }

            targetIndex = targetIndex + 1;
        }

        if (!forced && (codeSet == Code128CodeSet.A || codeSet == Code128CodeSet.B))
        {
            AddShiftedByte(candidates, tokens[position], position, codeSet, upperShiftLatched, pendingState, candidate, options);
        }

        if (pendingState == 0 && position + 1 < tokens.Length && IsDigitToken(tokens[position]) && IsDigitToken(tokens[position + 1]))
        {
            if (!forced || codeSet == Code128CodeSet.C)
            {
                AddDigitPair(candidates, tokens, position, codeSet, upperShiftLatched, candidate);
            }
        }
    }

    internal static void AddByteInCodeSet(
        Code128PlanCandidate?[] candidates,
        int byteValue,
        int position,
        Code128CodeSet currentCodeSet,
        Code128CodeSet targetCodeSet,
        bool upperShiftLatched,
        int pendingState,
        Code128PlanCandidate candidate,
        Code128Options options)
    {
        int switchCodeword = -1;
        int switchControlCount = 0;
        if (currentCodeSet != targetCodeSet)
        {
            switchCodeword = Code128CodeSets.GetSwitchCodeword(currentCodeSet, targetCodeSet);
            switchControlCount = 1;
        }

        bool effectiveUpperShift = upperShiftLatched;
        if (pendingState != 0)
        {
            effectiveUpperShift = !effectiveUpperShift;
        }

        if (Code128CodeSets.CanEncodeByte(byteValue, targetCodeSet, effectiveUpperShift))
        {
            int dataCodeword = Code128CodeSets.EncodeByte(byteValue, targetCodeSet, effectiveUpperShift);
            int[] directStep = CreatePrefixedStep(switchCodeword, dataCodeword);
            UpdateCandidate(candidates, position + 1, targetCodeSet, upperShiftLatched, 0, candidate, directStep, switchControlCount);
        }

        if (pendingState != 0 || !CanPlanUpperShift(options))
        {
            return;
        }

        bool oppositeUpperShift = !upperShiftLatched;
        if (!Code128CodeSets.CanEncodeByte(byteValue, targetCodeSet, oppositeUpperShift))
        {
            return;
        }

        int function4 = Code128CodeSets.GetFunctionCodeword(4, targetCodeSet);
        int shiftedData = Code128CodeSets.EncodeByte(byteValue, targetCodeSet, oppositeUpperShift);
        int[] singleStep = CreateUpperShiftStep(switchCodeword, function4, shiftedData, false);
        UpdateCandidate(candidates, position + 1, targetCodeSet, upperShiftLatched, 0, candidate, singleStep, switchControlCount + 1);
        int[] latchedStep = CreateUpperShiftStep(switchCodeword, function4, shiftedData, true);
        UpdateCandidate(candidates, position + 1, targetCodeSet, !upperShiftLatched, 0, candidate, latchedStep, switchControlCount + 2);
    }

    internal static void AddShiftedByte(
        Code128PlanCandidate?[] candidates,
        int byteValue,
        int position,
        Code128CodeSet currentCodeSet,
        bool upperShiftLatched,
        int pendingState,
        Code128PlanCandidate candidate,
        Code128Options options)
    {
        Code128CodeSet shiftedCodeSet = Code128CodeSets.GetShiftedCodeSet(currentCodeSet);
        bool effectiveUpperShift = upperShiftLatched;
        if (pendingState != 0)
        {
            effectiveUpperShift = !effectiveUpperShift;
        }

        if (Code128CodeSets.CanEncodeByte(byteValue, shiftedCodeSet, effectiveUpperShift))
        {
            int dataCodeword = Code128CodeSets.EncodeByte(byteValue, shiftedCodeSet, effectiveUpperShift);
            int[] directStep = new int[] { Code128Patterns.ShiftCodeword, dataCodeword };
            UpdateCandidate(candidates, position + 1, currentCodeSet, upperShiftLatched, 0, candidate, directStep, 1);
        }

        if (pendingState != 0 || !CanPlanUpperShift(options))
        {
            return;
        }

        bool oppositeUpperShift = !upperShiftLatched;
        if (!Code128CodeSets.CanEncodeByte(byteValue, shiftedCodeSet, oppositeUpperShift))
        {
            return;
        }

        int function4 = Code128CodeSets.GetFunctionCodeword(4, currentCodeSet);
        int shiftedData = Code128CodeSets.EncodeByte(byteValue, shiftedCodeSet, oppositeUpperShift);
        int[] singleStep = new int[] { function4, Code128Patterns.ShiftCodeword, shiftedData };
        UpdateCandidate(candidates, position + 1, currentCodeSet, upperShiftLatched, 0, candidate, singleStep, 2);
        int[] latchedStep = new int[] { function4, function4, Code128Patterns.ShiftCodeword, shiftedData };
        UpdateCandidate(candidates, position + 1, currentCodeSet, !upperShiftLatched, 0, candidate, latchedStep, 3);
    }

    internal static void AddDigitPair(
        Code128PlanCandidate?[] candidates,
        int[] tokens,
        int position,
        Code128CodeSet currentCodeSet,
        bool upperShiftLatched,
        Code128PlanCandidate candidate)
    {
        int pairCodeword = Code128CodeSets.EncodeDigitPair(tokens[position], tokens[position + 1]);
        int switchCodeword = -1;
        int controls = 0;
        if (currentCodeSet != Code128CodeSet.C)
        {
            switchCodeword = Code128CodeSets.GetSwitchCodeword(currentCodeSet, Code128CodeSet.C);
            controls = 1;
        }

        int[] step = CreatePrefixedStep(switchCodeword, pairCodeword);
        UpdateCandidate(candidates, position + 2, Code128CodeSet.C, upperShiftLatched, 0, candidate, step, controls);
    }

    internal static void AddFunctionTransition(
        Code128PlanCandidate?[] candidates,
        int position,
        Code128CodeSet codeSet,
        bool upperShiftLatched,
        int pendingState,
        Code128PlanCandidate candidate,
        Code128Options options,
        int token)
    {
        ValidateFunctionPermission(token, options);
        if (token == Function4Token)
        {
            if (pendingState == 2)
            {
                return;
            }

            if (codeSet == Code128CodeSet.C)
            {
                if (options.CodeSet == Code128CodeSet.Automatic && pendingState == 0)
                {
                    AddSwitchedFunction4(candidates, position, codeSet, Code128CodeSet.B, upperShiftLatched, candidate);
                    AddSwitchedFunction4(candidates, position, codeSet, Code128CodeSet.A, upperShiftLatched, candidate);
                }

                return;
            }

            int function4 = Code128CodeSets.GetFunctionCodeword(4, codeSet);
            int nextPendingState = 1;
            bool nextLatch = upperShiftLatched;
            if (pendingState == 1)
            {
                nextPendingState = 0;
                nextLatch = !upperShiftLatched;
            }

            UpdateCandidate(candidates, position + 1, codeSet, nextLatch, nextPendingState, candidate, new int[] { function4 }, 1);
            return;
        }

        int functionNumber = token - 255;
        if (functionNumber == 1)
        {
            int function1 = Code128CodeSets.GetFunctionCodeword(1, codeSet);
            int nextPending = PromotePendingState(pendingState);
            UpdateCandidate(candidates, position + 1, codeSet, upperShiftLatched, nextPending, candidate, new int[] { function1 }, 1);
            return;
        }

        if (codeSet == Code128CodeSet.A || codeSet == Code128CodeSet.B)
        {
            int functionCodeword = Code128CodeSets.GetFunctionCodeword(functionNumber, codeSet);
            int nextPending = PromotePendingState(pendingState);
            UpdateCandidate(candidates, position + 1, codeSet, upperShiftLatched, nextPending, candidate, new int[] { functionCodeword }, 1);
            return;
        }

        if (options.CodeSet != Code128CodeSet.Automatic || pendingState != 0)
        {
            return;
        }

        AddSwitchedFunction(candidates, position, codeSet, Code128CodeSet.B, upperShiftLatched, candidate, functionNumber);
        AddSwitchedFunction(candidates, position, codeSet, Code128CodeSet.A, upperShiftLatched, candidate, functionNumber);
    }

    internal static void AddSwitchedFunction4(
        Code128PlanCandidate?[] candidates,
        int position,
        Code128CodeSet currentCodeSet,
        Code128CodeSet targetCodeSet,
        bool upperShiftLatched,
        Code128PlanCandidate candidate)
    {
        int switchCodeword = Code128CodeSets.GetSwitchCodeword(currentCodeSet, targetCodeSet);
        int function4Codeword = Code128CodeSets.GetFunctionCodeword(4, targetCodeSet);
        UpdateCandidate(candidates, position + 1, targetCodeSet, upperShiftLatched, 1, candidate, new int[] { switchCodeword, function4Codeword }, 2);
    }

    internal static void AddSwitchedFunction(
        Code128PlanCandidate?[] candidates,
        int position,
        Code128CodeSet currentCodeSet,
        Code128CodeSet targetCodeSet,
        bool upperShiftLatched,
        Code128PlanCandidate candidate,
        int functionNumber)
    {
        int switchCodeword = Code128CodeSets.GetSwitchCodeword(currentCodeSet, targetCodeSet);
        int functionCodeword = Code128CodeSets.GetFunctionCodeword(functionNumber, targetCodeSet);
        UpdateCandidate(candidates, position + 1, targetCodeSet, upperShiftLatched, 0, candidate, new int[] { switchCodeword, functionCodeword }, 2);
    }

    internal static void UpdateCandidate(
        Code128PlanCandidate?[] candidates,
        int position,
        Code128CodeSet codeSet,
        bool upperShiftLatched,
        int pendingState,
        Code128PlanCandidate previous,
        int[] stepCodewords,
        int addedControlCount)
    {
        int targetIndex = GetStateIndex(position, GetCodeSetIndex(codeSet), upperShiftLatched ? 1 : 0, pendingState);
        int newCost = CheckedMath.Add(previous.Cost, stepCodewords.Length);
        int newControlCount = CheckedMath.Add(previous.ControlCount, addedControlCount);
        Code128PlanCandidate? existing = candidates[targetIndex];
        if (existing != null && !IsBetterCandidate(newCost, newControlCount, previous.StartCodeSet, existing))
        {
            return;
        }

        Code128PlanCandidate candidate = new Code128PlanCandidate();
        candidate.Cost = newCost;
        candidate.ControlCount = newControlCount;
        candidate.StartCodeSet = previous.StartCodeSet;
        candidate.PreviousCandidate = previous;
        candidate.StepCodewords = stepCodewords;
        candidates[targetIndex] = candidate;
    }

    internal static bool IsBetterCandidate(int cost, int controlCount, Code128CodeSet startCodeSet, Code128PlanCandidate existing)
    {
        if (cost != existing.Cost)
        {
            return cost < existing.Cost;
        }

        if (controlCount != existing.ControlCount)
        {
            return controlCount < existing.ControlCount;
        }

        return GetStartRank(startCodeSet) < GetStartRank(existing.StartCodeSet);
    }

    internal static Code128PlanCandidate? FindBestFinishedCandidate(Code128PlanCandidate?[] candidates, int position)
    {
        Code128PlanCandidate? best = null;
        int codeSetIndex = 0;
        while (codeSetIndex < CodeSetStateCount)
        {
            int latchIndex = 0;
            while (latchIndex < LatchStateCount)
            {
                int stateIndex = GetStateIndex(position, codeSetIndex, latchIndex, 0);
                Code128PlanCandidate? candidate = candidates[stateIndex];
                if (candidate != null)
                {
                    if (best == null || IsBetterCandidate(candidate.Cost, candidate.ControlCount, candidate.StartCodeSet, best))
                    {
                        best = candidate;
                    }
                }

                latchIndex = latchIndex + 1;
            }

            codeSetIndex = codeSetIndex + 1;
        }

        return best;
    }

    internal static Code128Plan CreatePlan(Code128PlanCandidate best)
    {
        int[] codewords = new int[best.Cost];
        int outputIndex = codewords.Length;
        Code128PlanCandidate? candidate = best;
        while (candidate != null && candidate.PreviousCandidate != null)
        {
            int[] step = candidate.StepCodewords;
            outputIndex = outputIndex - step.Length;
            int stepIndex = 0;
            while (stepIndex < step.Length)
            {
                codewords[outputIndex + stepIndex] = step[stepIndex];
                stepIndex = stepIndex + 1;
            }

            candidate = candidate.PreviousCandidate;
        }

        if (candidate == null || outputIndex != 0)
        {
            throw new BarcodeEncodingException("Code 128 plan reconstruction is incomplete.");
        }

        Code128Plan plan = new Code128Plan();
        plan.StartCodeSet = best.StartCodeSet;
        plan.Codewords = codewords;
        return plan;
    }

    internal static int[] CreatePrefixedStep(int prefixCodeword, int dataCodeword)
    {
        if (prefixCodeword < 0)
        {
            return new int[] { dataCodeword };
        }

        return new int[] { prefixCodeword, dataCodeword };
    }

    internal static int[] CreateUpperShiftStep(int switchCodeword, int function4Codeword, int dataCodeword, bool latch)
    {
        if (switchCodeword < 0)
        {
            if (latch)
            {
                return new int[] { function4Codeword, function4Codeword, dataCodeword };
            }

            return new int[] { function4Codeword, dataCodeword };
        }

        if (latch)
        {
            return new int[] { switchCodeword, function4Codeword, function4Codeword, dataCodeword };
        }

        return new int[] { switchCodeword, function4Codeword, dataCodeword };
    }

    internal static int PromotePendingState(int pendingState)
    {
        if (pendingState == 1)
        {
            return 2;
        }

        return pendingState;
    }

    internal static bool CanPlanUpperShift(Code128Options options)
    {
        return options.AllowFnc4 && options.UseLatin1UpperShift;
    }

    internal static bool IsDigitToken(int token)
    {
        return token >= '0' && token <= '9';
    }

    internal static void ValidateFunctionPermission(int token, Code128Options options)
    {
        bool permitted = false;
        switch (token)
        {
            case Function1Token:
                permitted = options.AllowFnc1;
                break;
            case Function2Token:
                permitted = options.AllowFnc2;
                break;
            case Function3Token:
                permitted = options.AllowFnc3;
                break;
            case Function4Token:
                permitted = options.AllowFnc4;
                break;
        }

        if (!permitted)
        {
            throw new BarcodeEncodingException("The Code 128 payload contains a function marker that is not permitted by the options.");
        }
    }

    internal static void ValidateTokens(int[] tokens)
    {
        if (tokens == null)
        {
            throw new BarcodeEncodingException("Code 128 input tokens must not be null.");
        }

        if (tokens.Length == 0)
        {
            throw new BarcodeEncodingException("Code 128 input must not be empty.");
        }

        int index = 0;
        while (index < tokens.Length)
        {
            int token = tokens[index];
            if (token < 0 || token > Function4Token)
            {
                throw new BarcodeEncodingException("Code 128 input contains an unsupported token.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateRequestedCodeSet(Code128CodeSet codeSet)
    {
        if (codeSet != Code128CodeSet.Automatic && codeSet != Code128CodeSet.A && codeSet != Code128CodeSet.B && codeSet != Code128CodeSet.C)
        {
            throw new BarcodeEncodingException("The requested Code 128 code set is invalid.");
        }
    }

    internal static int GetStateIndex(int position, int codeSetIndex, int latchIndex, int pendingState)
    {
        int stateIndex = (position * CodeSetStateCount) + codeSetIndex;
        stateIndex = (stateIndex * LatchStateCount) + latchIndex;
        return (stateIndex * PendingStateCount) + pendingState;
    }

    internal static int GetCodeSetIndex(Code128CodeSet codeSet)
    {
        switch (codeSet)
        {
            case Code128CodeSet.A:
                return 0;
            case Code128CodeSet.B:
                return 1;
            case Code128CodeSet.C:
                return 2;
            default:
                throw new BarcodeEncodingException("A concrete Code 128 code set is required by the planner.");
        }
    }

    internal static Code128CodeSet GetCodeSet(int codeSetIndex)
    {
        switch (codeSetIndex)
        {
            case 0:
                return Code128CodeSet.A;
            case 1:
                return Code128CodeSet.B;
            case 2:
                return Code128CodeSet.C;
            default:
                throw new BarcodeEncodingException("Code 128 planner code-set state is invalid.");
        }
    }

    internal static int GetStartRank(Code128CodeSet codeSet)
    {
        switch (codeSet)
        {
            case Code128CodeSet.B:
                return 0;
            case Code128CodeSet.A:
                return 1;
            case Code128CodeSet.C:
                return 2;
            default:
                return 3;
        }
    }
}
