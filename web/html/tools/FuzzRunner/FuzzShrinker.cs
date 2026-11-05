namespace UniversalHtml.FuzzRunner;

internal sealed class ShrinkResult
{
    public ShrinkResult(string inputValue, FuzzEvaluation evaluationValue,
        int attemptsValue)
    {
        Input = inputValue;
        Evaluation = evaluationValue;
        Attempts = attemptsValue;
    }

    public string Input { get; }
    public FuzzEvaluation Evaluation { get; }
    public int Attempts { get; }
}

internal static class FuzzShrinker
{
    public static ShrinkResult Shrink(string input,
        FuzzEvaluation originalEvaluation,
        Func<string, FuzzEvaluation> evaluate, FuzzBudget budget,
        int maximumAttempts)
    {
        if (!originalEvaluation.Failed || input.Length == 0
            || maximumAttempts == 0)
        {
            return new ShrinkResult(input, originalEvaluation, 0);
        }

        string current = input;
        FuzzEvaluation currentEvaluation = originalEvaluation;
        int granularity = 2;
        int attempts = 0;
        while (current.Length > 0 && attempts < maximumAttempts
            && budget.IsAvailable())
        {
            int chunkSize = (current.Length + granularity - 1) / granularity;
            bool reduced = false;
            for (int start = 0; start < current.Length
                && attempts < maximumAttempts && budget.TrySpend(1);
                start += chunkSize)
            {
                int length = Math.Min(chunkSize, current.Length - start);
                string candidate = current.Remove(start, length);
                attempts++;
                FuzzEvaluation evaluation = evaluate(candidate);
                if (evaluation.Failed
                    && evaluation.FailureKind
                        == originalEvaluation.FailureKind)
                {
                    current = candidate;
                    currentEvaluation = evaluation;
                    granularity = Math.Max(2, granularity - 1);
                    reduced = true;
                    break;
                }
            }

            if (reduced)
            {
                continue;
            }

            if (granularity >= current.Length)
            {
                break;
            }

            granularity = Math.Min(current.Length, granularity * 2);
        }

        return new ShrinkResult(current, currentEvaluation, attempts);
    }
}
