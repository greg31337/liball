namespace cookies;

internal static class CookieSecureOriginEvaluator
{
    internal static CookieBooleanResult Evaluate(
        ISecureOriginPolicy policy,
        global::System.Uri origin)
    {
        if (policy == null)
        {
            return new CookieBooleanResult();
        }

        global::System.Threading.Tasks.Task<bool> evaluation =
            new global::System.Threading.Tasks.Task<bool>(() => policy.IsSecureOrigin(origin));
        evaluation.RunSynchronously(global::System.Threading.Tasks.TaskScheduler.Default);
        if (!evaluation.IsCompletedSuccessfully)
        {
            evaluation.Exception?.Handle(exception => true);
            return new CookieBooleanResult();
        }

        CookieBooleanResult result = new CookieBooleanResult();
        result.Success = true;
        result.Value = evaluation.Result;
        return result;
    }
}
