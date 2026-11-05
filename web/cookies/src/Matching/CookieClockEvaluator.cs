namespace cookies;

internal static class CookieClockEvaluator
{
    internal static CookieClockResult Evaluate(ICookieClock clock)
    {
        if (clock == null)
        {
            return new CookieClockResult();
        }

        global::System.Threading.Tasks.Task<global::System.DateTimeOffset> evaluation =
            new global::System.Threading.Tasks.Task<global::System.DateTimeOffset>(
                () => clock.GetUtcNow());
        evaluation.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);
        if (!evaluation.IsCompletedSuccessfully)
        {
            evaluation.Exception?.Handle(exception => true);
            return new CookieClockResult();
        }

        CookieClockResult result = new CookieClockResult();
        result.Success = true;
        result.Value = evaluation.Result.ToUniversalTime();
        return result;
    }
}
